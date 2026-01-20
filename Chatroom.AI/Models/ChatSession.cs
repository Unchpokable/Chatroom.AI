using System;
using System.Collections.Generic;

namespace Chatroom.AI.Models;

/// <summary>
/// Текущее состояние чата — история сообщений и активные персоны.
/// Это runtime-модель, не для сериализации напрямую.
/// </summary>
public class ChatSession
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>История сообщений чата</summary>
    public List<MessageEntry> History { get; set; } = new();

    /// <summary>Активные персоны в этом чате</summary>
    public List<Persona> Personas { get; set; } = new();

    /// <summary>Общее саммари диалога</summary>
    public string SharedSummary { get; set; } = string.Empty;

    /// <summary>Добавить сообщение от ведущего</summary>
    public MessageEntry AddHostMessage(string authorName, string content)
    {
        var entry = new MessageEntry
        {
            AuthorName = authorName,
            AuthorType = AuthorType.Host,
            Content = content
        };
        History.Add(entry);
        return entry;
    }

    /// <summary>Добавить сообщение от персоны</summary>
    public MessageEntry AddPersonaMessage(Persona persona, string content)
    {
        var entry = new MessageEntry
        {
            AuthorName = persona.AvatarName,
            AuthorType = AuthorType.Persona,
            Content = content,
            PersonaId = persona.Id
        };
        History.Add(entry);
        return entry;
    }

    /// <summary>Добавить системное сообщение</summary>
    public MessageEntry AddSystemMessage(string content)
    {
        var entry = new MessageEntry
        {
            AuthorName = "System",
            AuthorType = AuthorType.System,
            Content = content
        };
        History.Add(entry);
        return entry;
    }
}
