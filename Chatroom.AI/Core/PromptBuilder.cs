using System.Collections.Generic;
using System.Text;
using Chatroom.AI.Models;

namespace Chatroom.AI.Core;

/// <summary>
/// Сборка промптов для LLM из компонентов (персона, история, настройки).
/// Единая точка ответственности за форматирование.
/// </summary>
public class PromptBuilder
{
    /// <summary>
    /// Собирает полный системный промпт для персоны.
    /// </summary>
    /// <param name="globalPrompt">Общий системный промпт чата (правила, теги)</param>
    /// <param name="persona">Персона, для которой собирается промпт</param>
    public string BuildSystemPrompt(string globalPrompt, Persona persona)
    {
        var personaPrompt = FormatPersonaPrompt(persona);

        if (string.IsNullOrWhiteSpace(globalPrompt))
            return personaPrompt;

        return $"{globalPrompt}\n\n{personaPrompt}";
    }

    /// <summary>
    /// Форматирует описание личности персоны.
    /// </summary>
    public string FormatPersonaPrompt(Persona persona)
    {
        return persona.PersonaLanguageKey switch
        {
            Persona.Language.Ru => FormatPersonaPromptRu(persona),
            Persona.Language.En => FormatPersonaPromptEn(persona),
            _ => FormatPersonaPromptEn(persona) // fallback to English
        };
    }

    /// <summary>
    /// Форматирует историю сообщений для отправки в LLM.
    /// </summary>
    /// <param name="history">История сообщений</param>
    /// <param name="format">Формат (XML, Scenario, Messages)</param>
    /// <returns>Список ContextMessage для отправки в API</returns>
    public List<ContextMessage> FormatHistory(List<MessageEntry> history, HistoryFormat format)
    {
        return format switch
        {
            HistoryFormat.Xml => FormatHistoryAsXml(history),
            HistoryFormat.Scenario => FormatHistoryAsScenario(history),
            HistoryFormat.Messages => FormatHistoryAsMessages(history),
            _ => FormatHistoryAsXml(history)
        };
    }

    #region Persona Prompt Formatting

    private static string FormatPersonaPromptRu(Persona persona)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Ты — {persona.ShortPersonality}.");
        sb.AppendLine($"Тебя зовут {persona.AvatarName}.");

        if (!string.IsNullOrWhiteSpace(persona.Personality))
        {
            sb.AppendLine();
            sb.AppendLine("Детальное описание твоей личности:");
            sb.AppendLine(persona.Personality);
        }

        if (!string.IsNullOrWhiteSpace(persona.SpecialInstructions))
        {
            sb.AppendLine();
            sb.AppendLine("Ты ОБЯЗАН следовать этим инструкциям:");
            sb.AppendLine(persona.SpecialInstructions);
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatPersonaPromptEn(Persona persona)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are {persona.ShortPersonality}.");
        sb.AppendLine($"Your name is {persona.AvatarName}.");

        if (!string.IsNullOrWhiteSpace(persona.Personality))
        {
            sb.AppendLine();
            sb.AppendLine("Here is a detailed description of your personality:");
            sb.AppendLine(persona.Personality);
        }

        if (!string.IsNullOrWhiteSpace(persona.SpecialInstructions))
        {
            sb.AppendLine();
            sb.AppendLine("You MUST follow these instructions:");
            sb.AppendLine(persona.SpecialInstructions);
        }

        return sb.ToString().TrimEnd();
    }

    #endregion

    #region History Formatting

    private static List<ContextMessage> FormatHistoryAsXml(List<MessageEntry> history)
    {
        if (history.Count == 0)
            return new List<ContextMessage>();

        var sb = new StringBuilder();
        sb.AppendLine("<conversation>");

        foreach (var entry in history)
        {
            sb.AppendLine($"  <from name=\"{EscapeXml(entry.AuthorName)}\">{EscapeXml(entry.Content)}</from>");
        }

        sb.AppendLine("</conversation>");

        return new List<ContextMessage>
        {
            ContextMessage.AsUser(sb.ToString())
        };
    }

    private static List<ContextMessage> FormatHistoryAsScenario(List<MessageEntry> history)
    {
        if (history.Count == 0)
            return new List<ContextMessage>();

        var sb = new StringBuilder();

        foreach (var entry in history)
        {
            sb.AppendLine($"[{entry.AuthorName}]: {entry.Content}");
        }

        return new List<ContextMessage>
        {
            ContextMessage.AsUser(sb.ToString())
        };
    }

    private static List<ContextMessage> FormatHistoryAsMessages(List<MessageEntry> history)
    {
        var messages = new List<ContextMessage>();

        foreach (var entry in history)
        {
            // Все сообщения как user, чтобы модель не думала, что это её ответы
            // Имя автора в префиксе
            var content = $"[{entry.AuthorName}]: {entry.Content}";
            messages.Add(ContextMessage.AsUser(content));
        }

        return messages;
    }

    private static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    #endregion
}
