using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Chatroom.AI.Core;
using Chatroom.AI.Utils;

namespace Chatroom.AI.Models;

internal class DialogContextManager
{
    public string SharedSummary { get; private set; } = string.Empty;
    public List<ContextMessage> SharedHistory { get; set; } = new();

    public Persona.Language DialogPrimaryLanguage { get; set; } = Persona.Language.Ru;

    public event EventHandler? SharedSummaryUpdated;

    public string SummarizePrompt { get; set; } = "Write a summary of following conversation history:\n";

    public string AppendSummaryPrompt { get; set; } = "Merge these summaries of single conversation:\n";

    private LlmKernelService _llmService = new();

    private List<string> _defaultTextModality = new List<string>() { "text" };

    void AddSharedHistoryMessage(ContextMessage message)
    {
        var simplified = message.WithoutTools();

        SharedHistory.Add(simplified);
    }

    async Task<string> Summarize(Persona agent)
    {
        // Summarizer agent should work with english language to use less tokens for system prompt.
        Assert.State(agent.PersonaLanguageKey == Persona.Language.En);

        var systemPrompt = agent.FormatSystemPrompt();

        var historyBuilder = new StringBuilder();


        var conversation = historyBuilder.ToString();

        var prompt = $"""
{SummarizePrompt}
{conversation}
""";

        var messageHistory = new List<ContextMessage> { ContextMessage.AsUser(prompt) };

        var summarization = await _llmService.Complete(agent.ApiModelName, ContextMessage.AsSystem(systemPrompt), messageHistory, _defaultTextModality);

        return summarization;
    }

    async Task AppendSummarization(Persona agent, string summarizationContent)
    {
        Assert.State(agent.PersonaLanguageKey == Persona.Language.En);

        var systemPrompt = agent.FormatSystemPrompt();

        var prompt = $"""
{AppendSummaryPrompt}
{summarizationContent}
""";

        var messageHistory = new List<ContextMessage> { ContextMessage.AsUser(prompt) };

        var result = await _llmService.Complete(agent.ApiModelName, ContextMessage.AsSystem(systemPrompt),
            messageHistory, _defaultTextModality);

        SharedSummary = result;

        SharedSummaryUpdated?.Invoke(this, EventArgs.Empty);
    }
}
