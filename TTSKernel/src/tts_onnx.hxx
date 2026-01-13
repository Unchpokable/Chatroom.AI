#pragma once

#ifndef TTS_ONNX_H
#define TTS_ONNX_H

namespace tts::onnx
{
extern std::uint8_t threads_count;
} // namespace tts::onnx

namespace tts::onnx
{
void configure_tts_threads_count(std::uint8_t threads_count);
} // namespace tts::onnx

namespace tts::onnx
{
void setup_config(std::string_view model_name,
    std::string_view model_path,
    std::string_view tokens_path,
    std::string_view lang_key,
    std::string_view provider = "cpu");
} // namespace tts::onnx

namespace tts::onnx
{
sherpa_onnx::cxx::Wave say(std::string_view model_name, std::string_view text, std::uint32_t samplerate);
} // namespace tts::onnx

#endif