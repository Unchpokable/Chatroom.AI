using System.Text.Json.Serialization;

namespace Chatroom.AI.Core;

internal sealed record FunctionDescription(
    string Name,
    string Arguments
);

internal sealed record ToolCall(
    string Id,
    string Type,
    FunctionDescription Function
);

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
