from fastapi import APIRouter, WebSocket, WebSocketDisconnect, Query
from pydantic import ValidationError

from src.core.message_builder import build_chat_request
from src.core.openrouter import OpenRouterClient, OpenRouterError
from src.models.responses import WebSocketResponse
from src.server.app import get_app_config, get_openrouter_client
from src.server.protocol import (
    ProtocolError,
    SerializationFormat,
    create_ack,
    create_chunk,
    create_complete,
    create_error,
    deserialize_request,
    serialize_response,
)
from src.utils.config import get_config
from src.utils.logging import get_logger


logger = get_logger("websocket")
router = APIRouter()


async def send_response(
    websocket: WebSocket,
    response: WebSocketResponse,
    fmt: SerializationFormat,
) -> None:
    """Send response to WebSocket client."""
    data = serialize_response(response, fmt)
    if fmt == SerializationFormat.PROTOBUF:
        await websocket.send_bytes(data)
    else:
        await websocket.send_text(data)


async def handle_llm_request(
    websocket: WebSocket,
    raw_data: bytes | str,
    fmt: SerializationFormat,
    client: OpenRouterClient,
) -> None:
    """Handle incoming LLM request."""
    config = get_app_config()
    request_id = "unknown"

    try:
        # Deserialize request
        request = deserialize_request(raw_data, fmt)
        request_id = request.request_id
        logger.info(f"Request {request_id}: model={request.model}, stream={request.stream}")

        # Send ACK
        await send_response(
            websocket,
            create_ack(request_id, accepted=True),
            fmt,
        )

        # Build OpenRouter request
        chat_request = build_chat_request(request, config.defaults)

        if request.stream:
            # Streaming mode
            accumulated_content = ""
            finish_reason = None
            prompt_tokens = 0
            completion_tokens = 0

            async for chunk in client.chat_completion_stream(chat_request):
                for choice in chunk.choices:
                    if choice.delta and choice.delta.content:
                        content = choice.delta.content
                        accumulated_content += content

                        # Send chunk to client
                        await send_response(
                            websocket,
                            create_chunk(request_id, content),
                            fmt,
                        )

                    if choice.finish_reason:
                        finish_reason = choice.finish_reason

                # Usage may be in final chunk
                if chunk.usage:
                    prompt_tokens = chunk.usage.prompt_tokens
                    completion_tokens = chunk.usage.completion_tokens

            # Send done message
            await send_response(
                websocket,
                create_complete(
                    request_id=request_id,
                    content=None,  # Content already sent via chunks
                    finish_reason=finish_reason,
                    prompt_tokens=prompt_tokens,
                    completion_tokens=completion_tokens,
                    is_stream_done=True,
                ),
                fmt,
            )
            logger.info(f"Request {request_id}: streaming completed")

        else:
            # Non-streaming mode
            response = await client.chat_completion(chat_request)

            content = ""
            finish_reason = None
            if response.choices:
                choice = response.choices[0]
                if choice.message:
                    content = choice.message.content
                finish_reason = choice.finish_reason

            usage = response.usage
            prompt_tokens = usage.prompt_tokens if usage else 0
            completion_tokens = usage.completion_tokens if usage else 0

            await send_response(
                websocket,
                create_complete(
                    request_id=request_id,
                    content=content,
                    finish_reason=finish_reason,
                    prompt_tokens=prompt_tokens,
                    completion_tokens=completion_tokens,
                    is_stream_done=False,
                ),
                fmt,
            )
            logger.info(f"Request {request_id}: completed (non-streaming)")

    except ProtocolError as e:
        logger.error(f"Protocol error: {e}")
        await send_response(
            websocket,
            create_error(request_id, "PROTOCOL_ERROR", str(e)),
            fmt,
        )

    except ValidationError as e:
        logger.error(f"Validation error: {e}")
        await send_response(
            websocket,
            create_ack(request_id, accepted=False, error_code="VALIDATION_ERROR", error_message=str(e)),
            fmt,
        )

    except OpenRouterError as e:
        logger.error(f"OpenRouter error: {e}")
        await send_response(
            websocket,
            create_error(request_id, "OPENROUTER_ERROR", str(e)),
            fmt,
        )

    except Exception as e:
        logger.exception(f"Unexpected error: {e}")
        await send_response(
            websocket,
            create_error(request_id, "INTERNAL_ERROR", str(e)),
            fmt,
        )


@router.websocket("/ws")
async def websocket_endpoint(
    websocket: WebSocket,
    format: str = Query(default="json", alias="format"),
) -> None:
    """WebSocket endpoint for LLM requests."""
    # Parse format
    try:
        fmt = SerializationFormat(format.lower())
    except ValueError:
        fmt = SerializationFormat.JSON
        logger.warning(f"Unknown format '{format}', falling back to JSON")

    await websocket.accept()
    logger.info(f"Client connected, format={fmt.value}")

    client = get_openrouter_client()

    try:
        while True:
            # Receive message
            if fmt == SerializationFormat.PROTOBUF:
                data = await websocket.receive_bytes()
            else:
                data = await websocket.receive_text()

            # Handle request
            await handle_llm_request(websocket, data, fmt, client)

    except WebSocketDisconnect:
        logger.info("Client disconnected")

    except Exception as e:
        logger.exception(f"WebSocket error: {e}")
