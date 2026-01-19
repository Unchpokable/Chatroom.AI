using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chatroom.AI.Core;

/// <summary>
/// Represents a message in the conversation context.
/// Supports regular messages, assistant messages with tool calls, and tool result messages.
/// </summary>
internal sealed class ContextMessage
{
    private const string RoleUser = "user";
    private const string RoleAssistant = "assistant";
    private const string RoleSystem = "system";
    private const string RoleTool = "tool";

    [JsonPropertyName("role")]
    public string Role { get; init; }

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; init; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCall[]? ToolCalls { get; init; }

    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; init; }

    private ContextMessage(string role, string? content = null, ToolCall[]? toolCalls = null, string? toolCallId = null)
    {
        Role = role;
        Content = content;
        ToolCalls = toolCalls;
        ToolCallId = toolCallId;
    }

    /// <summary>
    /// Creates a user message
    /// </summary>
    public static ContextMessage AsUser(string content) => new(RoleUser, content);

    /// <summary>
    /// Creates an assistant message with text content
    /// </summary>
    public static ContextMessage AsAssistant(string content) => new(RoleAssistant, content);

    /// <summary>
    /// Creates an assistant message with tool calls (content is null per OpenRouter spec)
    /// </summary>
    public static ContextMessage AsAssistantWithToolCalls(ToolCall[] toolCalls)
        => new(RoleAssistant, content: null, toolCalls: toolCalls);

    /// <summary>
    /// Creates a system message
    /// </summary>
    public static ContextMessage AsSystem(string content) => new(RoleSystem, content);

    /// <summary>
    /// Creates a tool result message
    /// </summary>
    public static ContextMessage AsToolResult(string toolCallId, string content)
        => new(RoleTool, content, toolCallId: toolCallId);

    public string ToJson() => JsonSerializer.Serialize(this);

    public ContextMessage WithoutTools() => new(Role, Content);
}
