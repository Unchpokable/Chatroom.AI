using System;
using Chatroom.AI.Utils;

namespace Chatroom.AI.Models;

internal sealed class Persona
{
    public enum Language
    {
        Ru,
        En,
        Fr,
        De
    }

    public required string ApiModelName { get; set; }
    public required string ModelNameAlias { get; set; }
    public required string AvatarName { get; set; }

    public string SpecialInstructions { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public string ShortPersonality { get; set; } = string.Empty;

    public Language PersonaLanguageKey { get; set; } = Language.Ru;

    public string FormatSystemPrompt()
    {
        return PersonaLanguageKey switch
        {
            Language.Ru => FormatSystemPromptRu(),
            Language.En => FormatSystemPromptEn(),
            Language.Fr or Language.De => throw new InvalidOperationException("Unsupported language key!"),
            _ => throw new ArgumentOutOfRangeException(nameof(PersonaLanguageKey), PersonaLanguageKey, null)
        };
    }

    private string FormatSystemPromptEn()
    {
        return
            $"""
You are {ShortPersonality}.
Your name is {AvatarName}.
Here is a detailed description of your personality:
{Personality}.

You MUST Follow these instructions:
{SpecialInstructions}.
""";
    }

    private string FormatSystemPromptRu()
    {
        return
            $"""
Ты - {ShortPersonality}.
Тебя зовут {AvatarName}.
Детальное описание твоей личности:
{Personality},

Ты ОБЯЗАН следовать следующим инструкицям:
{SpecialInstructions}.
""";
    }
}
