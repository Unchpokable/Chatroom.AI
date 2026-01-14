import json
from collections.abc import AsyncIterator

import httpx

from src.models.config import OpenRouterConfig
from src.models.openrouter import (
    ChatCompletionRequest,
    ChatCompletionResponse,
    ModelsResponse,
    OpenRouterModel,
    StreamChunk,
)
from src.utils.logging import get_logger


logger = get_logger("openrouter")


class OpenRouterError(Exception):
    """OpenRouter API error."""

    def __init__(self, message: str, status_code: int | None = None):
        super().__init__(message)
        self.status_code = status_code


class OpenRouterClient:
    """Async client for OpenRouter API."""

    def __init__(self, api_key: str, config: OpenRouterConfig):
        self.api_key = api_key
        self.config = config
        self._client: httpx.AsyncClient | None = None

    async def _get_client(self) -> httpx.AsyncClient:
        """Get or create HTTP client."""
        if self._client is None or self._client.is_closed:
            self._client = httpx.AsyncClient(
                base_url=self.config.base_url,
                headers={
                    "Authorization": f"Bearer {self.api_key}",
                    "Content-Type": "application/json",
                    "HTTP-Referer": "https://github.com/Chatroom-AI/llm-kernel",
                    "X-Title": "LLM Kernel",
                },
                timeout=httpx.Timeout(self.config.timeout_sec),
                limits=httpx.Limits(
                    max_connections=50,
                    max_keepalive_connections=20,
                    keepalive_expiry=30,
                ),
            )
        return self._client

    async def close(self) -> None:
        """Close HTTP client."""
        if self._client is not None and not self._client.is_closed:
            await self._client.aclose()
            self._client = None

    async def chat_completion(
        self, request: ChatCompletionRequest
    ) -> ChatCompletionResponse:
        """Send non-streaming chat completion request."""
        client = await self._get_client()

        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = False

        for attempt in range(self.config.max_retries + 1):
            try:
                response = await client.post(
                    "/chat/completions",
                    json=request_data,
                )
                response.raise_for_status()
                return ChatCompletionResponse.model_validate(response.json())

            except httpx.HTTPStatusError as e:
                logger.error(f"HTTP error {e.response.status_code}: {e.response.text}")
                if attempt == self.config.max_retries:
                    raise OpenRouterError(
                        f"OpenRouter API error: {e.response.text}",
                        status_code=e.response.status_code,
                    ) from e

            except httpx.RequestError as e:
                logger.error(f"Request error: {e}")
                if attempt == self.config.max_retries:
                    raise OpenRouterError(f"Request failed: {e}") from e

        raise OpenRouterError("Max retries exceeded")

    async def chat_completion_stream(
        self, request: ChatCompletionRequest
    ) -> AsyncIterator[StreamChunk]:
        """Send streaming chat completion request, yields chunks."""
        client = await self._get_client()

        request_data = request.model_dump(exclude_none=True)
        request_data["stream"] = True

        try:
            async with client.stream(
                "POST",
                "/chat/completions",
                json=request_data,
            ) as response:
                response.raise_for_status()

                async for line in response.aiter_lines():
                    if not line:
                        continue

                    # SSE format: "data: {...}" or "data: [DONE]"
                    if line.startswith("data: "):
                        data = line[6:]  # Remove "data: " prefix

                        if data == "[DONE]":
                            break

                        try:
                            chunk = StreamChunk.model_validate(json.loads(data))
                            yield chunk
                        except json.JSONDecodeError as e:
                            logger.warning(f"Failed to parse SSE chunk: {e}")
                            continue

        except httpx.HTTPStatusError as e:
            logger.error(f"HTTP error {e.response.status_code}: {e.response.text}")
            raise OpenRouterError(
                f"OpenRouter API error: {e.response.text}",
                status_code=e.response.status_code,
            ) from e

        except httpx.RequestError as e:
            logger.error(f"Request error: {e}")
            raise OpenRouterError(f"Request failed: {e}") from e

    async def list_models(self) -> list[OpenRouterModel]:
        """Get list of available models."""
        client = await self._get_client()

        try:
            response = await client.get("/models")
            response.raise_for_status()
            models_response = ModelsResponse.model_validate(response.json())
            return models_response.data

        except httpx.HTTPStatusError as e:
            logger.error(f"Failed to fetch models: {e.response.status_code}")
            raise OpenRouterError(
                f"Failed to fetch models: {e.response.text}",
                status_code=e.response.status_code,
            ) from e

        except httpx.RequestError as e:
            logger.error(f"Request error: {e}")
            raise OpenRouterError(f"Request failed: {e}") from e

    async def __aenter__(self) -> "OpenRouterClient":
        await self._get_client()
        return self

    async def __aexit__(self, *args) -> None:
        await self.close()
