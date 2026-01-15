using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;

namespace Chatroom.AI.Core;


internal class LlmKernelService
{
    private readonly string _openRouterBaseApi = "https://openrouter.ai/api/v1";
    private readonly string _openRouterChatApi = "chat/completions";

    private readonly HttpClient _httpClient;

    public string ApiKey { get; set; }

    public LlmKernelService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_openRouterBaseApi);
    }

    public async Task<string> Complete(string model, ContextMessage systemPrompt, List<ContextMessage> messageHistory)
    {
        return "";
    }

    public async IAsyncEnumerable<ContextMessage> CompleteStream(string model, ContextMessage systemPrompt,
        List<ContextMessage> messageHistory)
    {
        messageHistory.Insert(0, systemPrompt);

        var request = new HttpRequestMessage(HttpMethod.Post, _openRouterChatApi)
        {
            Content = JsonContent.Create(new
            {
                model,
                messages = messageHistory,
                stream = true
            })
        };

        request.Headers.Add("Authorization", $"Bearer {ApiKey}");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var jsonData = line.Substring("data: ".Length).Trim();
            if (jsonData == "[DONE]")
                yield break;

            var chunk = JsonSerializer.Deserialize<SseChunk>(jsonData);

            yield return ContextMessage.AsAssistant(chunk?.Choices[0].Delta.Content ?? string.Empty);
        }
    }
}
