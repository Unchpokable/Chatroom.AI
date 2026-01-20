using System;

namespace Chatroom.AI.Models;

/// <summary>
/// Модель данных персоны — AI-участника чата.
/// Содержит только данные, форматирование промптов — в PromptBuilder.
/// </summary>
public sealed class Persona
{
    public enum Language
    {
        Ru,
        En,
        Fr,
        De
    }

    /// <summary>Уникальный идентификатор персоны</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Имя модели в OpenRouter API (например, "anthropic/claude-3.5-sonnet")</summary>
    public required string ApiModelName { get; set; }

    /// <summary>Отображаемое имя модели в UI (например, "Claude 3.5 Sonnet")</summary>
    public required string ModelNameAlias { get; set; }

    /// <summary>Имя персоны в чате (например, "Алиса")</summary>
    public required string AvatarName { get; set; }

    /// <summary>Краткое описание личности (1 фраза)</summary>
    public string ShortPersonality { get; set; } = string.Empty;

    /// <summary>Детальное описание личности</summary>
    public string Personality { get; set; } = string.Empty;

    /// <summary>Специальные инструкции для модели</summary>
    public string SpecialInstructions { get; set; } = string.Empty;

    /// <summary>Язык промптов персоны</summary>
    public Language PersonaLanguageKey { get; set; } = Language.Ru;
}
