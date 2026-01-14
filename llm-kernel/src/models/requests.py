from pydantic import BaseModel, Field


class LLMRequest(BaseModel):
    """Incoming request from WebSocket client."""

    request_id: str = Field(..., min_length=1, description="Unique request identifier (UUID)")
    model: str = Field(..., min_length=1, description="Model name (e.g., 'anthropic/claude-3.5-sonnet')")
    system_prompt: str = Field(default="", description="System prompt for the model")
    user_prompt: str = Field(..., min_length=1, description="User prompt for the model")
    stream: bool = Field(default=True, description="Whether to stream the response")
