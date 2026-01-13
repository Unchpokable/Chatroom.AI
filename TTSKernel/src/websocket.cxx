#include "pch.hxx"

#include "websocket.hxx"

namespace
{
std::vector<message_callback> message_callbacks;
std::vector<error_callback> error_callbacks;
} // namespace

namespace
{
std::unique_ptr<ix::WebSocketServer> web_socket;
} // namespace

namespace
{
void websocket_common_callback(
    std::shared_ptr<ix::ConnectionState> connection_state, ix::WebSocket& web_socket, const ix::WebSocketMessagePtr& msg)
{
    if(msg->type == ix::WebSocketMessageType::Message) {
        for(const auto& callback : message_callbacks) {
            callback(web_socket, msg->str);
        }
    }
    else if(msg->type == ix::WebSocketMessageType::Error) {
        for(const auto& callback : error_callbacks) {
            callback(web_socket, msg->errorInfo);
        }
    }
    else if(msg->type == ix::WebSocketMessageType::Open) {
        LOG_INFO("WebSocket connection opened");
    }
}
} // namespace

ipc::websocket::Request ipc::websocket::parse(const nlohmann::json& json)
{
    return Request(json.value("pipe_name", ""),
        json.value("content", ""),
        json.value("model_name", ""),
        json.value("samplerate", 44100),
        json.value("should_stream", false),
        json.value("chunk_size", 0));
}

void ipc::websocket::initialize(std::uint16_t port)
{
    ix::initNetSystem();

    web_socket = std::make_unique<ix::WebSocketServer>(port, "127.0.0.1");

    web_socket->setOnClientMessageCallback(websocket_common_callback);

    auto result = web_socket->listen();

    if(!result.first) {
        LOG_ERROR("Failed to start WebSocket server");
        return;
    }

    web_socket->start();
}

void ipc::websocket::shutdown()
{
    web_socket->stop();
    web_socket->wait();
}

void ipc::websocket::add_message_callback(message_callback callback)
{
    message_callbacks.push_back(std::move(callback));
}

void ipc::websocket::add_error_callback(error_callback callback)
{
    error_callbacks.push_back(std::move(callback));
}
