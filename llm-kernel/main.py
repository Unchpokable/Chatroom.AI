import uvicorn

from src.server.app import create_app
from src.utils.config import get_config


def main() -> None:
    """Entry point for LLM Kernel server."""
    config = get_config()

    app = create_app()

    uvicorn.run(
        app,
        host=config.server.host,
        port=config.server.port,
        ws_max_size=config.websocket.max_message_size_bytes,
        ws_ping_interval=config.websocket.ping_interval_sec,
        ws_ping_timeout=config.websocket.ping_timeout_sec,
    )


if __name__ == "__main__":
    main()
