#include "pch.hxx"

#include "tts_onnx.hxx"

namespace tts::onnx
{
std::uint8_t threads_count;
} // namespace tts::onnx

namespace
{
struct TtsEngine {
    TtsEngine(const sherpa_onnx::cxx::OfflineTtsConfig& config, sherpa_onnx::cxx::OfflineTts&& engine, std::string lang)
        : config(config), engine(std::move(engine)), lang(std::move(lang))
    {
    }

    TtsEngine() = delete;
    TtsEngine(const TtsEngine&) = delete;
    TtsEngine& operator=(const TtsEngine&) = delete;

    TtsEngine(TtsEngine&&);
    TtsEngine& operator=(TtsEngine&&);

    sherpa_onnx::cxx::OfflineTtsConfig config;
    sherpa_onnx::cxx::OfflineTts engine;
    std::string lang;
};

TtsEngine::TtsEngine(TtsEngine&& other)
    : config(std::move(other.config)), engine(std::move(other.engine)), lang(std::move(other.lang))
{
}

TtsEngine& TtsEngine::operator=(TtsEngine&& other)
{
    config = std::move(other.config);
    engine = std::move(other.engine);
    lang = std::move(other.lang);
    return *this;
}
} // namespace

namespace
{
std::unordered_map<std::string, TtsEngine> model_engines;
} // namespace

namespace
{
sherpa_onnx::cxx::Wave resample(const sherpa_onnx::cxx::Wave& wave, std::uint32_t target_sample_rate)
{
    if(wave.sample_rate == target_sample_rate) {
        return wave;
    }

    auto ratio = static_cast<double>(target_sample_rate) / wave.sample_rate;

    std::size_t output_size = static_cast<std::size_t>(wave.samples.size() * ratio);

    std::vector<float> output_samples(output_size);

    SRC_DATA src_data;
    src_data.data_in = wave.samples.data();
    src_data.data_out = output_samples.data();
    src_data.input_frames = wave.samples.size();
    src_data.output_frames = output_size;
    src_data.src_ratio = ratio;

    int error = src_simple(&src_data, SRC_SINC_MEDIUM_QUALITY, 1); // 1 = mono

    if(error != 0) {
        throw std::runtime_error(src_strerror(error));
    }

    output_samples.resize(src_data.output_frames_gen);

    sherpa_onnx::cxx::Wave result_wave;
    result_wave.sample_rate = target_sample_rate;
    result_wave.samples = output_samples;

    return result_wave;
}
} // namespace

void tts::onnx::configure_tts_threads_count(std::uint8_t count)
{
    threads_count = count;
}

void tts::onnx::setup_config(
    std::string_view model_name, std::string_view model_path, std::string_view tokens_path, std::string_view lang_key, std::string_view provider)
{
    sherpa_onnx::cxx::OfflineTtsConfig config;
    config.model.vits.model = model_path;
    config.model.vits.tokens = tokens_path;
    config.model.num_threads = threads_count;
    config.model.debug = false;
    config.model.provider = provider;

    auto tts = sherpa_onnx::cxx::OfflineTts::Create(config);
    if(!tts.Get()) {
        LOG_ERROR("Unable to create TTS engine for model: {}", model_name);
        return;
    }

    model_engines.emplace(std::string(model_name), TtsEngine(config, std::move(tts), std::string(lang_key)));
}

sherpa_onnx::cxx::Wave tts::onnx::say(std::string_view model_name, std::string_view text, std::uint32_t samplerate)
{
    auto it = model_engines.find(std::string(model_name));
    if(it == model_engines.end()) {
        throw std::runtime_error("TTS engine for requested model not initialized");
    }

    auto audio = it->second.engine.Generate(std::string(text), 0, 1.0);

    sherpa_onnx::cxx::Wave wave;
    wave.samples = audio.samples;
    wave.sample_rate = audio.sample_rate;

    if(wave.sample_rate != samplerate) {
        return resample(wave, samplerate);
    }

    return wave;
}
