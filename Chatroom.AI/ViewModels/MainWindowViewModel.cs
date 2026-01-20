using System;
using System.Collections.Generic;
using Chatroom.AI.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chatroom.AI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LlmKernelService _llmService = new();

    [ObservableProperty]
    private string _greeting = "Загрузка...";

    public MainWindowViewModel()
    {
        _llmService.ApiKey = "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";

        if (string.IsNullOrEmpty(_llmService.ApiKey))
        {
            Greeting = "Ошибка: не задана переменная окружения OPENROUTER_API_KEY";
            return;
        }

        _ = TestApiAsync();
    }

    private async System.Threading.Tasks.Task TestApiAsync()
    {
        try
        {
            Greeting = "Отправляю запрос к OpenRouter API...";

            var systemPrompt = ContextMessage.AsSystem("Ты — дружелюбный ассистент. Отвечай кратко.");
            var history = new List<ContextMessage>
            {
                ContextMessage.AsUser("Привет! Скажи что-нибудь смешное в одном предложении.")
            };
            var modalities = new List<string> { "text" };

            var response = await _llmService.Complete(
                model: "mistralai/mistral-large-2512",
                systemPrompt: systemPrompt,
                messageHistory: history,
                modalities: modalities
            );

            Greeting = $"Французский генератор случайных чисел нагаллюцинировал:\n\n{response}";
        }
        catch (Exception ex)
        {
            Greeting = $"Ошибка: {ex.Message}";
        }
    }
}
