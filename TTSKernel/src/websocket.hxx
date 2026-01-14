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