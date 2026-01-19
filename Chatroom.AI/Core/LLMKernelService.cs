using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Chatroom.AI.Models;

namespace Chatroom.AI.Core;

/// <summary>
/// Result of a completion that may contain either text content or tool calls
/// </summary>
internal sealed record CompletionResult(
    string? Content,
    ToolCall[]? ToolCalls,
    string? FinishReason
)
{
    public bool HasToolCalls => ToolCalls is { Length: > 0 };
}

internal class LlmKernelService
{
    private readonly string _openRouterBaseApi = "https://openrouter.ai/api/v1/";
    private readonly string _openRouterChatApi = "chat/completions";
    private readonly string _openRouterModelsApi = "models";

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string? ApiKey { get; set; }

    public LlmKernelService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_openRouterBaseApi);
    }

    public async Task<OpenRouterLlmDescription?> GetAvailableModels()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_openRouterModelsApi}?refresh=true");
        request.Headers.Add("Authorization", $"Bearer {ApiKey}");

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();

        var resultObject = JsonSerializer.Deserialize<OpenRouterLlmDescription>(result, _jsonOptions);

        return resultObject;
    }

    /// <summary>
    /// Performs a completion without tools (returns text content only)
    /// </summary>
    public async Task<string> Complete(string model, ContextMessage systemPrompt, List<ContextMessage> messageHistory, List<string> modalities)
    {
        var result = await CompleteWithTools(model, systemPrompt, messageHistory, modalities, tools: null);
        return result.Content ?? string.Empty;
    }

    /// <summary>
    /// Performs a completion with optional tools support
    /// </summary>
    public async Task<CompletionResult> CompleteWithTools(
        string model,
        ContextMessage systemPrompt,
        List<ContextMessage> messageHistory,
        List<string> modalities,
        List<ToolDefinition>? tools,
        ToolChoice? toolChoice = null,
        bool? parallelToolCalls = null)
    {
        var messages = new List<ContextMessage> { systemPrompt };
        messages.AddRange(messageHistory);

        var requestBody = BuildRequestBody(model, messages, modalities, stream: false, tools, toolChoice, parallelToolCalls);

        var request = new HttpRequestMessage(HttpMethod.Post, _openRouterChatApi)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("Authorization", $"Bearer {ApiKey}");

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CompletionResponse>(_jsonOptions);

        if (result?.Choices is not { Length: > 0 })
            throw new InvalidOperationException("No choices in response");

        var choice = result.Choices[0];
        if (choice.Error is not null)
            throw new InvalidOperationException($"OpenRouter error {choice.Error.Code}: {choice.Error.Message}");

        return new CompletionResult(
            Content: choice.Message.Content,
            ToolCalls: choice.Message.ToolCalls,
            FinishReason: choice.FinishReason
        );
    }

    /// <summary>
    /// Performs a streaming completion without tools (yields text content only)
    /// </summary>
    public async IAsyncEnumerable<string> CompleteStream(string model, ContextMessage systemPrompt,
        List<ContextMessage> messageHistory, List<string> modalities)
    {
        await foreach (var chunk in CompleteStreamWithTools(model, systemPrompt, messageHistory, modalities, tools: null))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
                yield return chunk.Content;
        }
    }

    /// <summary>
    /// Performs a streaming completion with optional tools support.
    /// Yields StreamChunk objects containing either content or accumulated tool calls.
    /// </summary>
    public async IAsyncEnumerable<StreamChunk> CompleteStreamWithTools(
        string model,
        ContextMessage systemPrompt,
        List<ContextMessage> messageHistory,
        List<string> modalities,
        List<ToolDefinition>? tools,
        ToolChoice? toolChoice = null,
        bool? parallelToolCalls = null)
    {
        var messages = new List<ContextMessage> { systemPrompt };
        messages.AddRange(messageHistory);

        var requestBody = BuildRequestBody(model, messages, modalities, stream: true, tools, toolChoice, parallelToolCalls);

        var request = new HttpRequestMessage(HttpMethod.Post, _openRouterChatApi)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, _jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        request.Headers.Add("Authorization", $"Bearer {ApiKey}");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var toolCallBuilders = new Dictionary<int, ToolCallBuilder>();

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var jsonData = line.Substring("data: ".Length).Trim();
            if (jsonData == "[DONE]")
            {
                // Yield any accumulated tool calls
                if (toolCallBuilders.Count > 0)
                {
                    yield return StreamChunk.WithToolCalls(toolCallBuilders.Values.Select(builder => builder.Build()).ToArray(), "tool_calls");
                }
                yield break;
            }

            var chunk = JsonSerializer.Deserialize<SseChunk>(jsonData, _jsonOptions);

            if (chunk?.Choices is not { Length: > 0 })
                continue;

            var choice = chunk.Choices[0];
            if (choice.Error is not null)
                throw new InvalidOperationException($"OpenRouter error {choice.Error.Code}: {choice.Error.Message}");

            // Handle text content
            var content = choice.Delta.Content;
            if (!string.IsNullOrEmpty(content))
                yield return StreamChunk.WithContent(content);

            // Handle tool calls (accumulate across chunks)
            if (choice.Delta.ToolCalls is { Length: > 0 })
            {
                foreach (var toolCall in choice.Delta.ToolCalls)
                {
                    var index = toolCall.Index ?? 0;
                    if (!toolCallBuilders.ContainsKey(index))
                        toolCallBuilders[index] = new ToolCallBuilder();

                    toolCallBuilders[index].Append(toolCall);
                }
            }

            // Check for finish_reason indicating tool calls are complete
            if (choice.FinishReason == "tool_calls" && toolCallBuilders.Count > 0)
            {
                yield return StreamChunk.WithToolCalls(toolCallBuilders.Values.Select(builder => builder.Build()).ToArray(), "tool_calls");
                toolCallBuilders.Clear();
            }
        }
    }

    private object BuildRequestBody(
        string model,
        List<ContextMessage> messages,
        List<string> modalities,
        bool stream,
        List<ToolDefinition>? tools,
        ToolChoice? toolChoice,
        bool? parallelToolCalls)
    {
        var body = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = messages,
            ["stream"] = stream,
            ["modalities"] = modalities
        };

        if (tools is { Count: > 0 })
        {
            body["tools"] = tools;

            if (toolChoice is not null)
            {
                body["tool_choice"] = SerializeToolChoice(toolChoice);
            }

            if (parallelToolCalls.HasValue)
            {
                body["parallel_tool_calls"] = parallelToolCalls.Value;
            }
        }

        return body;
    }

    private static object SerializeToolChoice(ToolChoice toolChoice) => toolChoice switch
    {
        ToolChoiceAuto => "auto",
        ToolChoiceNone => "none",
        ToolChoiceFunction f => new
        {
            type = "function",
            function = new { name = f.FunctionName }
        },
        _ => "auto"
    };
}

/// <summary>
/// Represents a chunk from streaming completion
/// </summary>
internal sealed record StreamChunk(
    string? Content,
    ToolCall[]? ToolCalls,
    string? FinishReason
)
{
    public bool HasToolCalls => ToolCalls is { Length: > 0 };

    public static StreamChunk WithContent(string content) => new(content, null, null);
    public static StreamChunk WithToolCalls(ToolCall[] toolCalls, string finishReason) => new(null, toolCalls, finishReason);
}
