#define LOUDERME_AUDIO_ENGINE_EXPORTS
#include "louderme/AudioEngineC.h"
#include "louderme/AudioEngine.hpp"

#include <array>
#include <memory>
#include <new>

using louderme::audio::AudioEngine;

extern "C" {

uint32_t louderme_abi_version(void) { return 1; }

louderme_engine_handle louderme_engine_create(int sample_rate, int channels) {
    try {
        return static_cast<void*>(new AudioEngine(sample_rate, channels));
    } catch (...) {
        return nullptr;
    }
}

void louderme_engine_destroy(louderme_engine_handle handle) {
    delete static_cast<AudioEngine*>(handle);
}

int louderme_engine_set_boost(louderme_engine_handle handle, int enabled, int percent) {
    if (!handle || percent < 100 || percent > 250) return -1;
    auto& engine = *static_cast<AudioEngine*>(handle);
    engine.setBoostPercent(percent);
    engine.setBoostEnabled(enabled != 0);
    return 0;
}

int louderme_engine_set_eq(
    louderme_engine_handle handle, int enabled, const float* gains) {
    if (!handle || !gains) return -1;
    auto& engine = *static_cast<AudioEngine*>(handle);
    std::array<float, AudioEngine::BandCount> bands{};
    for (size_t i = 0; i < bands.size(); ++i) bands[i] = gains[i];
    engine.setEqBandsDb(bands);
    engine.setEqEnabled(enabled != 0);
    return 0;
}

int louderme_engine_process(
    louderme_engine_handle handle, float* data, size_t frames) {
    if (!handle || (!data && frames != 0)) return -1;
    if (frames) static_cast<AudioEngine*>(handle)->process(data, frames);
    return 0;
}

} // extern "C"
