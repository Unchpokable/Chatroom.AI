#include "pch.hxx"

#include "service.hxx"

#include "pipechannel.hxx"
#include "tts_onnx.hxx"
#include "websocket.hxx"

namespace
{
std::vector<std::future<void>> tts_futures;
std::mutex tts_futures_mutex;
} // namespace

namespace
{
void process_tts_request(const ipc::websocket::Request& request)
{
    auto pipe = ipc::pipes::PipeStream(request.pipe_name);

    if(!pipe.is_valid()) {
        LOG_ERROR("Invalid pipe name: {}", request.pipe_name);
        return;
    }

    if(!request.should_stream) {
        auto audio =
            tts::onnx::say(request.model_name, std::string_view(request.content.data(), request.content.size()), request.samplerate);
        pipe << std::span<const float>(audio.samples.data(), audio.samples.size());
    }
    else {
        tts::onnx::say_stream(request.model_name, request.content, [&pipe](std::span<const float> samples, std::int32_t, float progress) {
            pipe << samples;
            return tts::onnx::SHERPA_CONTINUE;
        });
    }
}
} // namespace

namespace
{
void init_tts_from_path(const std::string& path)
{
    /* Given path should be a folder with models and its configuration for Sherpa-ONNX following:
        Model1/
            - <whatever>
            - conf.json

        Model2/
            - <whatever>
            - conf.json


        conf.json should have a following JSON structure:
        {
            "model": "<model_name>",
            "model_file": "<model_file>", // local to current directory
            "tokens_file": "<tokens_file>", // local to current directory
            "lang_key" : "<lang_key>" // arbitrary language description
        }
    */

    std::filesystem::path models_root(path);
    if(!std::filesystem::exists(models_root) || !std::filesystem::is_directory(models_root)) {
        LOG_ERROR("Models path does not exist or is not a directory: {}", path);
        return;
    }

    for(const auto& entry : std::filesystem::directory_iterator(models_root)) {
        if(!entry.is_directory()) {
            continue;
        }

        auto model_dir = entry.path();
        auto config_path = model_dir / "conf.json";

        if(!std::filesystem::exists(config_path)) {
            LOG_WARNING("No conf.json found in model directory: {}", model_dir.string());
            continue;
        }

        try {
            std::ifstream config_file(config_path);
            if(!config_file.is_open()) {
                LOG_ERROR("Failed to open config file: {}", config_path.string());
                continue;
            }

            auto config = nlohmann::json::parse(config_file);

            auto model_name = config.at("model").get<std::string>();
            auto model_file = config.at("model_file").get<std::string>();
            auto tokens_file = config.at("tokens_file").get<std::string>();
            auto lang_key = config.at("lang_key").get<std::string>();

            auto full_model_path = (model_dir / model_file).string();
            auto full_tokens_path = (model_dir / tokens_file).string();

            std::string provider = "cpu";
            if(config.contains("provider")) {
                provider = config.at("provider").get<std::string>();
            }

            tts::onnx::setup_config(model_name, full_model_path, full_tokens_path, lang_key, provider);

            LOG_INFO("Loaded TTS model '{}' from {}", model_name, model_dir.string());
        }
        catch(const nlohmann::json::exception& e) {
            LOG_ERROR("Failed to parse config file {}: {}", config_path.string(), e.what());
        }
        catch(const std::exception& e) {
            LOG_ERROR("Failed to load model from {}: {}", model_dir.string(), e.what());
        }
    }
}
} // namespace

void tts::run(const std::string& shutdown_event_name, const std::string& models_root)
{
    auto shutdown_event = CreateEventA(nullptr, TRUE, FALSE, shutdown_event_name.c_str());

    if(!std::filesystem::exists(models_root) || !std::filesystem::is_directory(models_root)) {
        LOG_ERROR("Models root directory does not exist or is not a directory: {}", models_root);
        MessageBoxA(
            nullptr, "Models root directory does not exist or is not a directory", "TTS Kernel startup error", MB_OK | MB_ICONERROR);
        return;
    }

    init_tts_from_path(models_root);

    ipc::websocket::initialize();
    ipc::websocket::add_error_callback([](const ix::WebSocket& ws, const ix::WebSocketErrorInfo& err) {
        LOG_ERROR("WebSocket error: {}", err.reason);
    });

    BS::thread_pool workers_pool(std::thread::hardware_concurrency() / 2);

    ipc::websocket::add_message_callback([&workers_pool](ix::WebSocket& ws, std::string_view msg) {
        auto json = nlohmann::json::parse(msg);

        if(json["type"] == "ask_say") {
            auto request = ipc::websocket::parse(json["payload"]);

            auto future = workers_pool.submit_task([request]() {
                process_tts_request(request);
            });

            {
                std::unique_lock lock(tts_futures_mutex);
                tts_futures.push_back(std::move(future));
            }
        }
        else if(json["type"] == "ask_config") {
            auto models = tts::onnx::enumerate_models();

            nlohmann::json json;
            auto models_array = nlohmann::json::array();

            for(auto& model_view : models) {
                nlohmann::json model_json;
                model_json["model_name"] = model_view.model_name;
                model_json["model_lang"] = model_view.engine.lang;
                model_json["model_samplerate"] = model_view.engine.engine.SampleRate();

                models_array.push_back(model_json);
            }

            json["models"] = models_array;

            ws.sendText(json.dump());
        }
    });

    static auto process_futures = []() {
        std::unique_lock lock(tts_futures_mutex);

        std::erase_if(tts_futures, [](std::future<void>& future) {
            if(future.wait_for(std::chrono::seconds(0)) == std::future_status::ready) {
                try {
                    future.get();
                }
                catch(const std::exception& ex) {
                    LOG_ERROR("TTS server failed task with {}", ex.what());
                }
                catch(...) {
                    LOG_ERROR("TTS server failed task without any resolvable exception");
                }

                return true;
            }

            return false;
        });
    };

    while(WaitForSingleObject(shutdown_event, 200) == WAIT_TIMEOUT) {
        process_futures();
    }

    while(!tts_futures.empty()) {
        process_futures();
    }

    ipc::websocket::shutdown();

    CloseHandle(shutdown_event);
}
