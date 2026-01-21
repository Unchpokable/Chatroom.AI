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

    private TokenLocker _apiTokenLocker = new();

    public MainWindowViewModel()
    {
        _apiTokenLocker.LoadFromFile("openrouter_tokens");

        _llmService.ApiKey = _apiTokenLocker.GetKey("Chatroom.AI.Key");

        if (string.IsNullOrEmpty(_llmService.ApiKey))
        {
            Greeting = "Ошибка: не найден подходящий ключ по умолчанию, API не доступно";
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

            var response = _llmService.CompleteStream(
                model: "deepseek/deepseek-chat-v3-0324",
                systemPrompt: systemPrompt,
                messageHistory: history,
                modalities: modalities
            );

            Greeting = $"Китайский генератор случайных чисел нагаллюцинировал:\n\n";

            await foreach (var completion in response)
            {
                Greeting += completion;
            }
        }
        catch (Exception ex)
        {
            Greeting = $"Ошибка: {ex.Message}";
        }
    }
}
