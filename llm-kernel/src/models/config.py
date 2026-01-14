from pydantic import BaseModel, Field


class ServerConfig(BaseModel):
    host: str = "127.0.0.1"
    port: int = Field(default=8765, ge=1, le=65535)
    ws_path: str = "/ws"
    api_prefix: str = "/api"


class WebSocketConfig(BaseModel):
    max_message_size_mb: int = Field(default=100, ge=1, le=500)
    ping_interval_sec: int = Field(default=30, ge=5)
    ping_timeout_sec: int = Field(default=10, ge=5)

    @property
    def max_message_size_bytes(self) -> int:
        return self.max_message_size_mb * 1024 * 1024


class OpenRouterConfig(BaseModel):
    base_url: str = "https://openrouter.ai/api/v1"
    timeout_sec: int = Field(default=600, ge=30)
    max_retries: int = Field(default=3, ge=0, le=10)


class DefaultsConfig(BaseModel):
    model: str = "anthropic/claude-3.5-sonnet"
    max_tokens: int = Field(default=4096, ge=1)


class AppConfig(BaseModel):
    server: ServerConfig = Field(default_factory=ServerConfig)
    websocket: WebSocketConfig = Field(default_factory=WebSocketConfig)
    openrouter: OpenRouterConfig = Field(default_factory=OpenRouterConfig)
    defaults: DefaultsConfig = Field(default_factory=DefaultsConfig)
