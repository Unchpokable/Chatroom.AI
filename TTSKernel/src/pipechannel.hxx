#pragma once

#ifndef TTS_PIPECHANNEL_H
#define TTS_PIPECHANNEL_H

#ifdef _WIN32
#define NOMINMAX
#include <Windows.h>
#endif

namespace ipc::pipes
{
class PipeStream final {
public:
    static PipeStream make_invalid();

    explicit PipeStream(std::string_view name);
    ~PipeStream();

    PipeStream(const PipeStream&) = delete;
    PipeStream& operator=(const PipeStream&) = delete;

    PipeStream(PipeStream&& other) noexcept;
    PipeStream& operator=(PipeStream&& other) noexcept;

    template<typename T>
    PipeStream& operator<<(std::span<const T> data);

    bool is_valid() const noexcept;

private:
    PipeStream() = default;

    HANDLE m_pipe { INVALID_HANDLE_VALUE };
};

} // namespace ipc::pipes

namespace ipc::pipes
{
bool pipe_exists(std::string_view pipe_name);
} // namespace ipc::pipes

namespace ipc::pipes
{
PipeStream stream(std::string_view name);
} // namespace ipc::pipes

template<typename T>
ipc::pipes::PipeStream& ipc::pipes::PipeStream::operator<<(std::span<const T> data)
{
    DWORD written;
    if(!WriteFile(m_pipe, data.data(), static_cast<DWORD>(data.size_bytes()), &written, nullptr)) {
        LOG_ERROR("Failed to write to pipe, errc {}", GetLastError());
        throw std::runtime_error("Pipe failed to write!");
    }

    return *this;
}

#endif