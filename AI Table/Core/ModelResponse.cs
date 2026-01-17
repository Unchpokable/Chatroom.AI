using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Chatroom.AI.Core;

// Common types
internal sealed record ResponseUsage(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens
);

internal sealed record ErrorResponse(
    int Code,
    string Message,
    Dictionary<string, object>? Metadata = null
);

// Streaming response (SSE chunks)
internal sealed record SseChunk(
    string Id,
    string Object,
    int Created,
    string Model,
    [property: JsonPropertyName("system_fingerprint")] string? SystemFingerprint,
    Choice[] Choices,
    ResponseUsage? Usage
);

internal sealed record Choice(
    int Index,
    Delta Delta,
    [property: JsonPropertyName("finish_reason")] string? FinishReason,
    [property: JsonPropertyName("native_finish_reason")] string? NativeFinishReason,
    ErrorResponse? Error
);

internal sealed record Delta(
    string? Content,
    string? Role,
    [property: JsonPropertyName("tool_calls")] ToolCall[]? ToolCalls
);

// Non-streaming response
internal sealed record CompletionResponse(
    string Id,
    string Object,
    int Created,
    string Model,
    [property: JsonPropertyName("system_fingerprint")] string? SystemFingerprint,
    CompletionChoice[] Choices,
    ResponseUsage? Usage
);

internal sealed record CompletionChoice(
    int Index,
    CompletionMessage Message,
    [property: JsonPropertyName("finish_reason")] string? FinishReason,
    [property: JsonPropertyName("native_finish_reason")] string? NativeFinishReason,
    ErrorResponse? Error
);

internal sealed record CompletionMessage(
    string Role,
    string? Content,
    [property: JsonPropertyName("tool_calls")] ToolCall[]? ToolCalls
);
