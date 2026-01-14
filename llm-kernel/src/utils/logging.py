import logging
import sys


def setup_logging(level: int = logging.INFO) -> logging.Logger:
    """Setup application logging."""
    logger = logging.getLogger("llm-kernel")
    logger.setLevel(level)

    # Avoid duplicate handlers
    if logger.handlers:
        return logger

    # Console handler
    handler = logging.StreamHandler(sys.stdout)
    handler.setLevel(level)

    # Format
    formatter = logging.Formatter(
        "[%(asctime)s] %(levelname)s %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    )
    handler.setFormatter(formatter)
    logger.addHandler(handler)

    return logger


def get_logger(name: str | None = None) -> logging.Logger:
    """Get a logger instance."""
    if name:
        return logging.getLogger(f"llm-kernel.{name}")
    return logging.getLogger("llm-kernel")
