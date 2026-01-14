from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

from src.models.config import AppConfig
from src.utils.config import get_api_key, get_config, save_config, set_api_key


router = APIRouter(tags=["settings"])


class ApiKeyUpdate(BaseModel):
    api_key: str


class ApiKeyStatus(BaseModel):
    is_set: bool
    masked: str


@router.get("/config", response_model=AppConfig)
async def get_configuration() -> AppConfig:
    """Get current configuration."""
    return get_config()


@router.put("/config", response_model=AppConfig)
async def update_configuration(config: AppConfig) -> AppConfig:
    """Update and save configuration."""
    save_config(config)
    return config


@router.get("/config/apikey", response_model=ApiKeyStatus)
async def get_api_key_status() -> ApiKeyStatus:
    """Get API key status (masked)."""
    try:
        key = get_api_key()
        # Mask key, show only last 4 characters
        masked = f"sk-or-...{key[-4:]}" if len(key) > 4 else "****"
        return ApiKeyStatus(is_set=True, masked=masked)
    except ValueError:
        return ApiKeyStatus(is_set=False, masked="")


@router.put("/config/apikey")
async def update_api_key(update: ApiKeyUpdate) -> dict:
    """Update API key."""
    if not update.api_key:
        raise HTTPException(status_code=400, detail="API key cannot be empty")

    set_api_key(update.api_key)
    return {"status": "ok", "message": "API key updated"}
