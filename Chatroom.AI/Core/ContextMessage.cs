using System.Text.Json;

namespace Chatroom.AI.Core;

internal sealed record ContextMessage(string Role, string Content)
{
    private const string RoleUser = "user";
    private const string RoleAssistant = "assistant";
    private const string RoleSystem = "system";

    public static ContextMessage AsUser(string content) => new(RoleUser, content);
    public static ContextMessage AsAssistant(string content) => new(RoleAssistant, content);
    public static ContextMessage AsSystem(string content) => new(RoleSystem, content);

    public string ToJson() => JsonSerializer.Serialize(this);
}
