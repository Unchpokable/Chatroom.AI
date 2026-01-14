from src.models.config import DefaultsConfig
from src.models.openrouter import ChatCompletionRequest, ChatMessage
from src.models.requests import LLMRequest


def build_chat_request(
    llm_request: LLMRequest,
    defaults: DefaultsConfig,
) -> ChatCompletionRequest:
    """
    Build OpenRouter ChatCompletionRequest from client LLMRequest.

    Args:
        llm_request: Incoming request from WebSocket client
        defaults: Default configuration values

    Returns:
        ChatCompletionRequest ready to send to OpenRouter API
    """
    messages: list[ChatMessage] = []

    # Add system message if provided
    if llm_request.system_prompt:
        messages.append(
            ChatMessage(role="system", content=llm_request.system_prompt)
        )

    # Add user message
    messages.append(
        ChatMessage(role="user", content=llm_request.user_prompt)
    )

    return ChatCompletionRequest(
        model=llm_request.model,
        messages=messages,
        stream=llm_request.stream,
        max_tokens=defaults.max_tokens,
    )
