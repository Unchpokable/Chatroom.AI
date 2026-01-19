using System.Text.Json.Serialization;

namespace Chatroom.AI.Core;

/// <summary>
/// Function description within a tool call
/// </summary>
internal sealed record FunctionDescription(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] string Arguments
);

/// <summary>
/// Represents a tool call from the model's response
/// </summary>
internal sealed record ToolCall(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] FunctionDescription Function,
    [property: JsonPropertyName("index")] int? Index = null
);

/// <summary>
/// Builder for accumulating tool call data from streaming chunks
/// </summary>
internal sealed class ToolCallBuilder
{
    private ToolCall _state = new("", "", new FunctionDescription("", ""));

    public void Append(ToolCall block)
    {
        _state = new ToolCall(
            Id: string.IsNullOrEmpty(block.Id) ? _state.Id : block.Id,
            Type: string.IsNullOrEmpty(block.Type) ? _state.Type : block.Type,
            Function: new FunctionDescription(
                Name: string.IsNullOrEmpty(block.Function.Name)
                    ? _state.Function.Name
                    : block.Function.Name,
                Arguments: _state.Function.Arguments + block.Function.Arguments
            )
        );
    }

    public ToolCall Build() => _state;

    public void Reset() => _state = new("", "", new FunctionDescription("", ""));
}
