import json
import os
from pathlib import Path

from dotenv import load_dotenv

from src.models.config import AppConfig


_config: AppConfig | None = None
_config_path: Path | None = None


def get_project_root() -> Path:
    """Get project root directory."""
    return Path(__file__).parent.parent.parent


def load_config(config_path: Path | None = None) -> AppConfig:
    """Load configuration from JSON file and environment variables."""
    global _config, _config_path

    if config_path is None:
        config_path = get_project_root() / "config.json"

    _config_path = config_path

    # Load .env file
    env_path = get_project_root() / ".env"
    load_dotenv(env_path)

    # Load config.json
    if config_path.exists():
        with open(config_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        _config = AppConfig.model_validate(data)
    else:
        _config = AppConfig()

    return _config


def get_config() -> AppConfig:
    """Get current configuration. Loads from file if not already loaded."""
    global _config
    if _config is None:
        return load_config()
    return _config


def save_config(config: AppConfig | None = None) -> None:
    """Save configuration to JSON file."""
    global _config, _config_path

    if config is not None:
        _config = config

    if _config is None:
        raise ValueError("No configuration to save")

    if _config_path is None:
        _config_path = get_project_root() / "config.json"

    with open(_config_path, "w", encoding="utf-8") as f:
        json.dump(_config.model_dump(), f, indent=2)


def get_api_key() -> str:
    """Get OpenRouter API key from environment."""
    key = os.getenv("OPENROUTER_API_KEY", "")
    if not key:
        raise ValueError("OPENROUTER_API_KEY environment variable is not set")
    return key


def set_api_key(key: str) -> None:
    """Set OpenRouter API key in .env file."""
    env_path = get_project_root() / ".env"

    # Read existing .env content
    lines: list[str] = []
    if env_path.exists():
        with open(env_path, "r", encoding="utf-8") as f:
            lines = f.readlines()

    # Update or add OPENROUTER_API_KEY
    key_found = False
    for i, line in enumerate(lines):
        if line.startswith("OPENROUTER_API_KEY="):
            lines[i] = f"OPENROUTER_API_KEY={key}\n"
            key_found = True
            break

    if not key_found:
        lines.append(f"OPENROUTER_API_KEY={key}\n")

    # Write back
    with open(env_path, "w", encoding="utf-8") as f:
        f.writelines(lines)

    # Update environment
    os.environ["OPENROUTER_API_KEY"] = key
