#pragma once

#ifndef TTS_SERVICE_H
#define TTS_SERVICE_H

namespace tts
{
void run(const std::string& shutdown_event_name, const std::string& models_root);
} // namespace tts

#endif
