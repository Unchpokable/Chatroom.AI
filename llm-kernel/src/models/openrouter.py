from typing import Literal
from pydantic import BaseModel, Field


class ChatMessage(BaseModel):
    """Single message in chat completion request."""

    role: Literal["system", "user", "assistant"]
    content: str


class ChatCompletionRequest(BaseModel):
    """Request to OpenRouter /chat/completions endpoint."""

    model: str
    messages: list[ChatMessage]
    stream: bool = False
    max_tokens: int | None = None
    temperature: float | None = None
    top_p: float | None = None
    stop: list[str] | None = None


class ChatCompletionUsage(BaseModel):
    """Token usage in completion response."""

    prompt_tokens: int = 0
    completion_tokens: int = 0
    total_tokens: int = 0


class ChatCompletionChoice(BaseModel):
    """Single choice in completion response."""

    index: int = 0
    message: ChatMessage | None = None
    delta: ChatMessage | None = None  # For streaming
    finish_reason: str | None = None


class ChatCompletionResponse(BaseModel):
    """Response from OpenRouter /chat/completions endpoint."""

    id: str
    object: str = "chat.completion"
    created: int = 0
    model: str
    choices: list[ChatCompletionChoice]
    usage: ChatCompletionUsage | None = None


class StreamDelta(BaseModel):
    """Delta content in streaming response."""

    role: str | None = None
    content: str | None = None


class StreamChoice(BaseModel):
    """Choice in streaming response."""

    index: int = 0
    delta: StreamDelta = Field(default_factory=StreamDelta)
    finish_reason: str | None = None


class StreamChunk(BaseModel):
    """Single chunk in streaming response."""

    id: str
    object: str = "chat.completion.chunk"
    created: int = 0
    model: str
    choices: list[StreamChoice]
    usage: ChatCompletionUsage | None = None  # Present in final chunk


class OpenRouterModel(BaseModel):
    """Model info from OpenRouter /models endpoint."""

    id: str
    name: str = ""
    description: str = ""
    context_length: int = 0
    pricing: dict = Field(default_factory=dict)


class ModelsResponse(BaseModel):
    """Response from OpenRouter /models endpoint."""

    data: list[OpenRouterModel]
