import json
from enum import Enum

from src.models.requests import LLMRequest
from src.models.responses import (
    AckResponse,
    ErrorResponse,
    LLMCompleteResponse,
    StreamChunkResponse,
    TokenUsage,
    WebSocketResponse,
)
from src.utils.logging import get_logger

# Import generated protobuf messages
from generated import messages_pb2


logger = get_logger("protocol")


class SerializationFormat(str, Enum):
    JSON = "json"
    PROTOBUF = "protobuf"


class ProtocolError(Exception):
    """Protocol serialization/deserialization error."""
    pass


def deserialize_request(data: bytes | str, fmt: SerializationFormat) -> LLMRequest:
    """
    Deserialize incoming WebSocket message to LLMRequest.

    Args:
        data: Raw message data (bytes for protobuf, str for JSON)
        fmt: Serialization format

    Returns:
        Parsed LLMRequest

    Raises:
        ProtocolError: If deserialization fails
    """
    try:
        if fmt == SerializationFormat.JSON:
            if isinstance(data, bytes):
                data = data.decode("utf-8")
            parsed = json.loads(data)
            return LLMRequest.model_validate(parsed)

        elif fmt == SerializationFormat.PROTOBUF:
            if not isinstance(data, bytes):
                raise ProtocolError("Protobuf format requires binary data")

            ws_msg = messages_pb2.WebSocketMessage()
            ws_msg.ParseFromString(data)

            if not ws_msg.HasField("request"):
                raise ProtocolError("Expected LLMRequest in WebSocketMessage")

            req = ws_msg.request
            return LLMRequest(
                request_id=req.request_id,
                model=req.model,
                system_prompt=req.system_prompt,
                user_prompt=req.user_prompt,
                stream=req.stream,
            )

    except Exception as e:
        logger.error(f"Deserialization failed: {e}")
        raise ProtocolError(f"Failed to deserialize request: {e}") from e


def serialize_response(response: WebSocketResponse, fmt: SerializationFormat) -> bytes | str:
    """
    Serialize response to WebSocket message.

    Args:
        response: Response object to serialize
        fmt: Serialization format

    Returns:
        Serialized data (str for JSON, bytes for protobuf)
    """
    if fmt == SerializationFormat.JSON:
        return response.model_dump_json()

    elif fmt == SerializationFormat.PROTOBUF:
        ws_msg = messages_pb2.WebSocketMessage()

        if isinstance(response, AckResponse):
            ws_msg.ack.request_id = response.request_id
            ws_msg.ack.accepted = response.accepted
            ws_msg.ack.error_code = response.error_code or ""
            ws_msg.ack.error_message = response.error_message or ""

        elif isinstance(response, ErrorResponse):
            # Map to Ack with accepted=False
            ws_msg.ack.request_id = response.request_id
            ws_msg.ack.accepted = False
            ws_msg.ack.error_code = response.code
            ws_msg.ack.error_message = response.message

        elif isinstance(response, StreamChunkResponse):
            ws_msg.chunk.request_id = response.request_id
            ws_msg.chunk.content = response.content

        elif isinstance(response, LLMCompleteResponse):
            ws_msg.response.request_id = response.request_id
            ws_msg.response.content = response.content or ""
            ws_msg.response.finish_reason = response.finish_reason or ""
            ws_msg.response.prompt_tokens = response.usage.prompt_tokens
            ws_msg.response.completion_tokens = response.usage.completion_tokens

        return ws_msg.SerializeToString()

    raise ProtocolError(f"Unknown format: {fmt}")


def create_ack(request_id: str, accepted: bool = True, error_code: str = "", error_message: str = "") -> AckResponse:
    """Create acknowledgment response."""
    return AckResponse(
        request_id=request_id,
        accepted=accepted,
        error_code=error_code if error_code else None,
        error_message=error_message if error_message else None,
    )


def create_error(request_id: str, code: str, message: str) -> ErrorResponse:
    """Create error response."""
    return ErrorResponse(
        request_id=request_id,
        code=code,
        message=message,
    )


def create_chunk(request_id: str, content: str) -> StreamChunkResponse:
    """Create streaming chunk response."""
    return StreamChunkResponse(
        request_id=request_id,
        content=content,
    )


def create_complete(
    request_id: str,
    content: str | None,
    finish_reason: str | None,
    prompt_tokens: int = 0,
    completion_tokens: int = 0,
    is_stream_done: bool = False,
) -> LLMCompleteResponse:
    """Create complete response."""
    return LLMCompleteResponse(
        type="done" if is_stream_done else "complete",
        request_id=request_id,
        content=content,
        finish_reason=finish_reason,
        usage=TokenUsage(
            prompt_tokens=prompt_tokens,
            completion_tokens=completion_tokens,
        ),
    )
