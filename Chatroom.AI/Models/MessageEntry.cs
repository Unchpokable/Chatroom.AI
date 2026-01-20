using System;

namespace Chatroom.AI.Models;

/// <summary>
/// Тип автора сообщения
/// </summary>
public enum AuthorType
{
    /// <summary>Ведущий (пользователь)</summary>
    Host,
    /// <summary>AI-персона</summary>
    Persona,
    /// <summary>Системное сообщение</summary>
    System
}

/// <summary>
/// Сообщение в истории чата.
/// Более высокоуровневая модель, чем ContextMessage — содержит метаданные об авторе.
/// </summary>
public class MessageEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Отображаемое имя автора</summary>
    public required string AuthorName { get; set; }

    /// <summary>Тип автора</summary>
    public required AuthorType AuthorType { get; set; }

    /// <summary>Текст сообщения</summary>
    public required string Content { get; set; }

    /// <summary>Время создания</summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// ID персоны, если автор — персона.
    /// Null для Host и System.
    /// </summary>
    public Guid? PersonaId { get; set; }
}
