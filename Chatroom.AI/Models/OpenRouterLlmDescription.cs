using System.Text.Json.Serialization;

namespace Chatroom.AI.Models;

public sealed record OpenRouterLlmDescription
{
    [JsonPropertyName("data")]
    public required Model[] Data { get; init; }
}

public sealed record Model
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("canonical_slug")]
    public required string CanonicalSlug { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("created")]
    public required long Created { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("context_length")]
    public required int ContextLength { get; init; }

    [JsonPropertyName("architecture")]
    public required Architecture Architecture { get; init; }

    [JsonPropertyName("pricing")]
    public required Pricing Pricing { get; init; }

    [JsonPropertyName("top_provider")]
    public required TopProvider TopProvider { get; init; }

    [JsonPropertyName("per_request_limits")]
    public object? PerRequestLimits { get; init; }

    [JsonPropertyName("supported_parameters")]
    public required string[] SupportedParameters { get; init; }
}

public sealed record Architecture
{
    [JsonPropertyName("input_modalities")]
    public required string[] InputModalities { get; init; }

    [JsonPropertyName("output_modalities")]
    public required string[] OutputModalities { get; init; }

    [JsonPropertyName("tokenizer")]
    public required string Tokenizer { get; init; }

    [JsonPropertyName("instruct_type")]
    public string? InstructType { get; init; }
}

public sealed record Pricing
{
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    [JsonPropertyName("completion")]
    public required string Completion { get; init; }

    [JsonPropertyName("request")]
    public required string Request { get; init; }

    [JsonPropertyName("image")]
    public required string Image { get; init; }

    [JsonPropertyName("web_search")]
    public required string WebSearch { get; init; }

    [JsonPropertyName("internal_reasoning")]
    public required string InternalReasoning { get; init; }

    [JsonPropertyName("input_cache_read")]
    public required string InputCacheRead { get; init; }

    [JsonPropertyName("input_cache_write")]
    public required string InputCacheWrite { get; init; }
}

public sealed record TopProvider
{
    [JsonPropertyName("context_length")]
    public required int ContextLength { get; init; }

    [JsonPropertyName("max_completion_tokens")]
    public required int MaxCompletionTokens { get; init; }

    [JsonPropertyName("is_moderated")]
    public required bool IsModerated { get; init; }
}
