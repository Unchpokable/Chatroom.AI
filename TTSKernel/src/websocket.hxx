#pragma once

#ifndef TTS_WEBSOCKET_H
#define TTS_WEBSOCKET_H

namespace
{
using message_callback = std::function<void(ix::WebSocket&, std::string_view)>;
using error_callback = std::function<void(ix::WebSocket&, const ix::WebSocketErrorInfo&)>;
} // namespace

namespace ipc::websocket
{
/* "say" request structure, represents following json:
{
    "pipe_name": "\\\\.\\pipe\\Chatroom.AI-Pipe_tts",
    "content": "Hello, my fellas! I am a Gemini 3 Pro, Smart and intellegent AI assistant designed by Google",
    "model_name" : "vits-piper-en_US-lessac-medium",
    "samplerate": 44100,
    "should_stream": true,
    "chunk_size": 256,
}
*/
struct Request {
    std::string pipe_name;
    std::string content;
    std::string model_name;
    std::uint32_t samplerate;
    bool should_stream;
    std::int32_t chunk_size;
};
} // namespace ipc::websocket

namespace ipc::websocket
{
Request parse(const nlohmann::json& json);
} // namespace ipc::websocket

namespace ipc::websocket
{
void initialize(std::uint16_t port = 45678);
void shutdown();
} // namespace ipc::websocket

namespace ipc::websocket
{
void add_message_callback(message_callback callback);
void add_error_callback(error_callback callback);
} // namespace ipc::websocket

#endif