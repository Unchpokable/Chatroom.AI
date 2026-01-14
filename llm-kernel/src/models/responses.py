from typing import Literal
from pydantic import BaseModel, Field


class TokenUsage(BaseModel):
    """Token usage statistics."""

    prompt_tokens: int = 0
    completion_tokens: int = 0

    @property
    def total_tokens(self) -> int:
        return self.prompt_tokens + self.completion_tokens


class AckResponse(BaseModel):
    """Acknowledgment response for incoming request."""

    type: Literal["ack"] = "ack"
    request_id: str
    accepted: bool
    error_code: str | None = None
    error_message: str | None = None


class ErrorResponse(BaseModel):
    """Error response."""

    type: Literal["error"] = "error"
    request_id: str
    code: str
    message: str


class StreamChunkResponse(BaseModel):
    """Streaming chunk response."""

    type: Literal["chunk"] = "chunk"
    request_id: str
    content: str


class LLMCompleteResponse(BaseModel):
    """Complete LLM response (non-streaming or final streaming message)."""

    type: Literal["complete", "done"] = "complete"
    request_id: str
    content: str | None = None  # Full content for non-stream, None for stream done
    finish_reason: str | None = None
    usage: TokenUsage = Field(default_factory=TokenUsage)


# Type alias for all possible response types
WebSocketResponse = AckResponse | ErrorResponse | StreamChunkResponse | LLMCompleteResponse
