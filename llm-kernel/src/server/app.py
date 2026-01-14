from contextlib import asynccontextmanager
from pathlib import Path
from typing import AsyncIterator

from fastapi import FastAPI
from fastapi.responses import FileResponse

from src.core.openrouter import OpenRouterClient
from src.models.config import AppConfig
from src.utils.config import get_api_key, get_config
from src.utils.logging import get_logger, setup_logging


logger = get_logger("app")


# Global state
_openrouter_client: OpenRouterClient | None = None
_app_config: AppConfig | None = None


def get_openrouter_client() -> OpenRouterClient:
    """Get OpenRouter client instance."""
    if _openrouter_client is None:
        raise RuntimeError("OpenRouter client not initialized")
    return _openrouter_client


def get_app_config() -> AppConfig:
    """Get application config."""
    if _app_config is None:
        raise RuntimeError("App config not initialized")
    return _app_config


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    """Application lifespan manager."""
    global _openrouter_client, _app_config

    # Startup
    setup_logging()
    logger.info("Starting LLM Kernel server...")

    _app_config = get_config()
    api_key = get_api_key()

    _openrouter_client = OpenRouterClient(
        api_key=api_key,
        config=_app_config.openrouter,
    )
    await _openrouter_client._get_client()  # Initialize client

    logger.info(f"Server configured on {_app_config.server.host}:{_app_config.server.port}")

    yield

    # Shutdown
    logger.info("Shutting down LLM Kernel server...")
    if _openrouter_client:
        await _openrouter_client.close()
    logger.info("Server stopped")


def create_app() -> FastAPI:
    """Create and configure FastAPI application."""
    app = FastAPI(
        title="LLM Kernel",
        description="Local proxy server for OpenRouter API",
        version="0.1.0",
        lifespan=lifespan,
    )

    # Import and include routers
    from src.api.routes import health, models, settings
    from src.server.websocket import router as ws_router

    config = get_config()

    # Mount API routes
    app.include_router(health.router, prefix=config.server.api_prefix)
    app.include_router(models.router, prefix=config.server.api_prefix)
    app.include_router(settings.router, prefix=config.server.api_prefix)

    # Mount WebSocket
    app.include_router(ws_router)

    # Admin UI
    static_dir = Path(__file__).parent.parent.parent / "static"

    @app.get("/admin")
    async def admin_page():
        """Serve admin UI."""
        return FileResponse(static_dir / "admin.html")

    @app.get("/")
    async def root_redirect():
        """Redirect root to admin."""
        return FileResponse(static_dir / "admin.html")

    return app
