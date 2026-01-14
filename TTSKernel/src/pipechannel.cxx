#include "pch.hxx"

#include "pipechannel.hxx"

namespace
{
bool is_pipe(std::string_view file_name)
{
    return file_name.find_first_of("\\\\.\\pipe\\") != std::string_view::npos;
}
} // namespace

ipc::pipes::PipeStream ipc::pipes::PipeStream::make_invalid()
{
    return PipeStream();
}

ipc::pipes::PipeStream::PipeStream(std::string_view name)
{
    if(!is_pipe(name)) {
        LOG_ERROR("Trying to open a file which is not a pipe - {}", name);
        throw std::runtime_error("Invalid pipe name");
    }

    m_pipe = CreateFileA(name.data(), GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
}

ipc::pipes::PipeStream::~PipeStream()
{
    if(m_pipe != INVALID_HANDLE_VALUE) {
        CloseHandle(m_pipe);
    }
}

ipc::pipes::PipeStream::PipeStream(PipeStream&& other) noexcept
{
    m_pipe = other.m_pipe;
    other.m_pipe = INVALID_HANDLE_VALUE;
}

ipc::pipes::PipeStream& ipc::pipes::PipeStream::operator=(PipeStream&& other) noexcept
{
    if(this != &other) {
        if(m_pipe != INVALID_HANDLE_VALUE) {
            CloseHandle(m_pipe);
        }

        m_pipe = other.m_pipe;
        other.m_pipe = INVALID_HANDLE_VALUE;
    }

    return *this;
}

bool ipc::pipes::PipeStream::is_valid() const noexcept
{
    return m_pipe != INVALID_HANDLE_VALUE;
}

ipc::pipes::PipeStream ipc::pipes::stream(std::string_view name)
{
    if(!pipe_exists(name)) {
        return PipeStream::make_invalid();
    }

    return PipeStream(name);
}

bool ipc::pipes::pipe_exists(std::string_view pipe_name)
{
    return false;
}
