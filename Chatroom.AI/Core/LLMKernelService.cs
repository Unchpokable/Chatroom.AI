using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;

using Chatroom.AI.Models;

namespace Chatroom.AI.Core;


internal class LlmKernelService
{
    private readonly string _openRouterBaseApi = "https://openrouter.ai/api/v1/";
    private readonly string _openRouterChatApi = "chat/completions";
    private readonly string _openRouterModelsApi = "models";

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
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

    public async Task<string> Complete(string model, ContextMessage systemPrompt, List<ContextMessage> messageHistory, List<string> modalities)
    {
        messageHistory.Insert(0, systemPrompt);

        var request = new HttpRequestMessage(HttpMethod.Post, _openRouterChatApi)
        {
            Content = JsonContent.Create(new
            {
                model,
                messages = messageHistory,
                stream = false,
                modalities
            })
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

        return choice.Message.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> CompleteStream(string model, ContextMessage systemPrompt,
        List<ContextMessage> messageHistory, List<string> modalities)
    {
        messageHistory.Insert(0, systemPrompt);

        var request = new HttpRequestMessage(HttpMethod.Post, _openRouterChatApi)
        {
            Content = JsonContent.Create(new
            {
                model,
                messages = messageHistory,
                stream = true,
                modalities
            })
        };

        request.Headers.Add("Authorization", $"Bearer {ApiKey}");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var jsonData = line.Substring("data: ".Length).Trim();
            if (jsonData == "[DONE]")
                yield break;

            var chunk = JsonSerializer.Deserialize<SseChunk>(jsonData, _jsonOptions);

            if (chunk?.Choices is not { Length: > 0 })
                continue;

            var choice = chunk.Choices[0];
            if (choice.Error is not null)
                throw new InvalidOperationException($"OpenRouter error {choice.Error.Code}: {choice.Error.Message}");

            var content = choice.Delta.Content;
            if (!string.IsNullOrEmpty(content))
                yield return content;
        }
    }
}
