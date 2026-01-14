from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

from src.core.openrouter import OpenRouterError
from src.server.app import get_openrouter_client


router = APIRouter(tags=["models"])


class ModelInfo(BaseModel):
    id: str
    name: str
    description: str
    context_length: int


class ModelsListResponse(BaseModel):
    models: list[ModelInfo]


# Simple in-memory cache
_models_cache: list[ModelInfo] | None = None


@router.get("/models", response_model=ModelsListResponse)
async def list_models(refresh: bool = False) -> ModelsListResponse:
    """
    Get list of available models from OpenRouter.

    Args:
        refresh: Force refresh cache if True
    """
    global _models_cache

    if _models_cache is None or refresh:
        try:
            client = get_openrouter_client()
            raw_models = await client.list_models()
            _models_cache = [
                ModelInfo(
                    id=m.id,
                    name=m.name or m.id,
                    description=m.description,
                    context_length=m.context_length,
                )
                for m in raw_models
            ]
        except OpenRouterError as e:
            raise HTTPException(status_code=502, detail=str(e))

    return ModelsListResponse(models=_models_cache)
