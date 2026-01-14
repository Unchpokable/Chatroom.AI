#include "pch.hxx"

#include "service.hxx"

int main(int argc, char* argv[])
{
    uulog::add_sink(uulog::builtin::stdout_ansi);

    if(argc < 3) {
        LOG_ERROR("Not enough arguments to run service");
        return -1;
    }

    std::string shutdown_event_name = argv[1];
    std::string models_root = argv[2];

    // std::cout << "TTSKernel - Sherpa-ONNX Text-to-Speech\n";
    // std::cout << "======================================\n";
    // std::cout << "Sherpa-ONNX version: " << sherpa_onnx::cxx::GetVersionStr() << "\n";
    // std::cout << "Git SHA1: " << sherpa_onnx::cxx::GetGitSha1() << "\n";
    // std::cout << "Git Date: " << sherpa_onnx::cxx::GetGitDate() << "\n\n";

    tts::run(shutdown_event_name, models_root);

    return 0;
}
