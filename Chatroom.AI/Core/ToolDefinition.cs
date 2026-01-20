using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Chatroom.AI.Core;

/// <summary>
/// JSON Schema for function parameters
/// </summary>
public sealed record ParametersSchema(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("properties")] Dictionary<string, ParameterProperty>? Properties = null,
    [property: JsonPropertyName("required")] string[]? Required = null
)
{
    public static ParametersSchema Object(
        Dictionary<string, ParameterProperty>? properties = null,
        string[]? required = null) => new("object", properties, required);
}

/// <summary>
/// Property definition within parameters schema
/// </summary>
public sealed record ParameterProperty(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("enum")] string[]? Enum = null,
    [property: JsonPropertyName("items")] ParameterProperty? Items = null
);

/// <summary>
/// Function definition for a tool
/// </summary>
public sealed record FunctionDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")] ParametersSchema Parameters
);

/// <summary>
/// Tool definition for OpenRouter API
/// </summary>
public sealed record ToolDefinition(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] FunctionDefinition Function
)
{
    public static ToolDefinition CreateFunction(string name, string description, ParametersSchema parameters)
        => new("function", new FunctionDefinition(name, description, parameters));
}

/// <summary>
/// Tool choice options for controlling tool usage
/// </summary>
public abstract record ToolChoice
{
    public static ToolChoice Auto => new ToolChoiceAuto();
    public static ToolChoice None => new ToolChoiceNone();
    public static ToolChoice ForceFunction(string functionName) => new ToolChoiceFunction(functionName);
}

public sealed record ToolChoiceAuto : ToolChoice;
public sealed record ToolChoiceNone : ToolChoice;
public sealed record ToolChoiceFunction(string FunctionName) : ToolChoice;
