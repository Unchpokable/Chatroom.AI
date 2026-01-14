# LLM Kernel Proxy Server — Архитектура

## Обзор

Локальный прокси-сервер между клиентами и OpenRouter API. Принимает запросы по WebSocket, транслирует их в HTTP запросы к OpenRouter, возвращает ответы (streaming или целиком) обратно клиенту.

## 1. Структура проекта

```
llm-kernel/
├── main.py                    # Точка входа
├── pyproject.toml
├── .env                       # Только OPENROUTER_API_KEY
├── config.json                # Персистентная конфигурация
│
├── proto/                     # Protobuf схемы
│   └── messages.proto         # Определения сообщений
│
├── src/
│   ├── __init__.py
│   │
│   ├── server/
│   │   ├── __init__.py
│   │   ├── app.py             # FastAPI приложение, lifecycle
│   │   ├── websocket.py       # WebSocket endpoint
│   │   └── protocol.py        # Сериализация (JSON / Protobuf)
│   │
│   ├── api/                   # REST админка
│   │   ├── __init__.py
│   │   └── routes/
│   │       ├── __init__.py
│   │       ├── settings.py    # CRUD конфигурации
│   │       ├── models.py      # Список моделей OpenRouter
│   │       └── health.py      # Health check
│   │
│   ├── core/
│   │   ├── __init__.py
│   │   ├── openrouter.py      # HTTP клиент к OpenRouter API
│   │   ├── message_builder.py # Сборка запросов
│   │   └── token_counter.py   # Подсчёт токенов (tiktoken)
│   │
│   ├── models/                # Pydantic схемы
│   │   ├── __init__.py
│   │   ├── requests.py        # Входящие от клиента
│   │   ├── responses.py       # Исходящие клиенту
│   │   ├── openrouter.py      # Схемы OpenRouter API
│   │   └── config.py          # Схема конфигурации
│   │
│   └── utils/
│       ├── __init__.py
│       ├── config.py          # Загрузка/сохранение config.json
│       └── logging.py
│
├── generated/                 # Сгенерированный Python код из .proto
│   └── messages_pb2.py
│
└── tests/
```

## 2. Технологический стек

| Компонент | Технология | Зачем |
|-----------|------------|-------|
| **ASGI** | `uvicorn[standard]` | Быстрый async сервер, uvloop |
| **Framework** | `FastAPI` | WebSocket + REST, автодоки |
| **HTTP клиент** | `httpx` | Async streaming к OpenRouter |
| **Валидация** | `pydantic` | Схемы, сериализация |
| **Сериализация** | `protobuf` | Для больших сообщений |
| **Конфигурация** | `python-dotenv` | Секреты из .env |
| **Токены** | `tiktoken` | Подсчёт токенов |


## 3. Двойной протокол сериализации

Клиент при подключении выбирает формат: **JSON** или **Protobuf**.

### 3.1 Выбор протокола

При handshake через query parameter:
```
ws://localhost:8765/ws?format=protobuf
ws://localhost:8765/ws?format=json      # default
```

### 3.2 Protobuf схема (`proto/messages.proto`)

```protobuf
syntax = "proto3";

package llmkernel;

// Запрос от клиента
message LLMRequest {
  string request_id = 1;
  string model = 2;
  string system_prompt = 3;
  string user_prompt = 4;
  bool stream = 5;
}

// Подтверждение приёма
message Ack {
  string request_id = 1;
  bool accepted = 2;
  string error_code = 3;    // пусто если accepted=true
  string error_message = 4;
}

// Chunk при стриминге
message StreamChunk {
  string request_id = 1;
  string content = 2;
}

// Финальный ответ (streaming done или complete response)
message LLMResponse {
  string request_id = 1;
  string content = 2;       // полный текст для non-stream, пусто для stream
  string finish_reason = 3;
  uint32 prompt_tokens = 4;
  uint32 completion_tokens = 5;
}

// Обёртка для WebSocket
message WebSocketMessage {
  oneof payload {
    LLMRequest request = 1;
    Ack ack = 2;
    StreamChunk chunk = 3;
    LLMResponse response = 4;
  }
}
```

### 3.3 Почему Protobuf

| Операция | JSON (100K токенов) | Protobuf |
|----------|---------------------|----------|
| Размер | ~400 KB | ~250 KB (≈40% меньше) |
| Parse time | ~50-100ms | ~5-10ms |
| Memory | Пики при парсинге | Стабильно низкое |

Для контекстов в 200K+ токенов разница критична.

## 4. Потоки данных

```
┌─────────────┐                      ┌─────────────────┐                    ┌──────────────┐
│   Client    │   WebSocket          │   LLM Kernel    │   HTTPS/SSE        │  OpenRouter  │
│   (C#)      │   JSON / Protobuf    │     Server      │   Streaming        │     API      │
│             │◄────────────────────►│                 │◄──────────────────►│              │
└─────────────┘   Binary frames      └─────────────────┘                    └──────────────┘
                                            │
                                            │ REST (JSON)
                                            ▼
                                     ┌─────────────┐
                                     │  Admin UI   │
                                     │  (Browser)  │
                                     └─────────────┘
```

## 5. Жизненный цикл запроса

```
Client                          Server                         OpenRouter
   │                               │                                │
   │──── LLMRequest ──────────────►│                                │
   │                               │── validate request             │
   │◄─── Ack (accepted/error) ─────│                                │
   │                               │                                │
   │                               │──── POST /chat/completions ───►│
   │                               │     (stream: true/false)       │
   │                               │                                │
   │                               │◄──── SSE chunks ───────────────│
   │◄─── StreamChunk ──────────────│     (если stream=true)         │
   │◄─── StreamChunk ──────────────│                                │
   │◄─── StreamChunk ──────────────│                                │
   │         ...                   │                                │
   │                               │◄──── [DONE] ───────────────────│
   │◄─── LLMResponse (done) ───────│                                │
   │                               │                                │
```

## 6. Конфигурация

### `.env` — только секреты
```env
OPENROUTER_API_KEY=sk-or-v1-xxxxx
```

### `config.json` — всё остальное
```json
{
  "server": {
    "host": "127.0.0.1",
    "port": 8765,
    "ws_path": "/ws",
    "api_prefix": "/api"
  },
  "websocket": {
    "max_message_size_mb": 100,
    "ping_interval_sec": 30,
    "ping_timeout_sec": 10
  },
  "openrouter": {
    "base_url": "https://openrouter.ai/api/v1",
    "timeout_sec": 600,
    "max_retries": 3
  },
  "defaults": {
    "model": "anthropic/claude-3.5-sonnet",
    "max_tokens": 4096
  }
}
```

Сервер при старте читает `config.json`, а админка позволяет редактировать и сохранять его через REST.

## 7. REST API админки

| Endpoint | Method | Описание |
|----------|--------|----------|
| `GET /api/health` | GET | `{"status": "ok"}` |
| `GET /api/models` | GET | Кешированный список моделей OpenRouter |
| `GET /api/config` | GET | Текущая конфигурация |
| `PUT /api/config` | PUT | Обновить и сохранить конфигурацию |
| `PUT /api/config/apikey` | PUT | Обновить API ключ (пишет в .env) |
| `GET /api/stats` | GET | Простая статистика (requests, tokens) |

## 8. Обработка больших данных

| Аспект | Решение |
|--------|---------|
| **Парсинг** | Protobuf вместо JSON для больших сообщений |
| **Streaming** | Async generators, нет накопления в памяти |
| **WebSocket** | Binary frames для Protobuf |
| **HTTP к OpenRouter** | `httpx` с `stream=True`, итерация по SSE |
| **Connection pool** | `httpx.Limits(max_connections=50, max_keepalive=20)` |
| **Лимит сообщения** | 100 MB max (настраивается) |

## 9. Компоненты

| Файл | Что делает |
|------|------------|
| `main.py` | Запуск uvicorn с конфигом |
| `server/app.py` | FastAPI app, lifespan (init httpx client) |
| `server/websocket.py` | WS endpoint, dispatch запросов |
| `server/protocol.py` | `serialize()` / `deserialize()` — JSON или Protobuf |
| `core/openrouter.py` | Async клиент, streaming SSE, retry |
| `core/message_builder.py` | `LLMRequest` → OpenRouter format |
| `api/routes/*` | REST endpoints админки |
| `utils/config.py` | Load/save `config.json` |

## 10. Принципы

- **Без аутентификации** — всё доверенное, локальное
- **Два формата** — JSON для простоты, Protobuf для производительности
- **Streaming end-to-end** — от OpenRouter до клиента без буферизации
- **Персистентный конфиг** — `config.json`, редактируется через админку
- **Минимум зависимостей** — FastAPI + uvicorn + protobuf поверх уже имеющихся
