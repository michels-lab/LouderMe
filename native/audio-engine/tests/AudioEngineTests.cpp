#include "louderme/AudioEngine.hpp"
#include <algorithm>
#include <array>
#include <cmath>
#include <iostream>
#include <limits>
#include <stdexcept>
#include <vector>

using louderme::audio::AudioEngine;

void check(bool condition, const char* message) {
    if (!condition) throw std::runtime_error(message);
}
double rms(const std::vector<float>& data, std::size_t offset) {
    double sum = 0.0;
    for (std::size_t i = offset; i < data.size(); ++i)
        sum += static_cast<double>(data[i]) * data[i];
    return std::sqrt(sum / (data.size() - offset));
}
void unityAndBoost() {
    AudioEngine engine(48000, 1);
    std::vector<float> mono(6000, 0.1f);
    engine.process(mono.data(), mono.size());
    check(std::abs(mono.back() - 0.1f) < 0.00001f, "unity gain");
    engine.setBoostPercent(150);
    engine.setBoostEnabled(true);
    std::fill(mono.begin(), mono.end(), 0.1f);
    engine.process(mono.data(), mono.size());
    check(std::abs(mono.back() - 0.15f) < 0.001f, "150% gain");
    engine.setBoostEnabled(false);
    std::fill(mono.begin(), mono.end(), 0.1f);
    engine.process(mono.data(), mono.size());
    check(std::abs(mono.back() - 0.1f) < 0.001f, "boost disable");
}
void limiterAndChannels() {
    AudioEngine engine(48000, 2);
    engine.setBoostPercent(250);
    engine.setBoostEnabled(true);
    std::vector<float> pcm(12000, 0.95f);
    for (std::size_t i = 1; i < pcm.size(); i += 2) pcm[i] = -0.95f;
    engine.process(pcm.data(), pcm.size() / 2);
    for (float v : pcm)
        check(std::isfinite(v) && v >= -1.0f && v <= 1.0f, "full-scale overflow");
    check(pcm.back() < 0.0f, "channel sign");
}
void equalizerFrequencyResponse() {
    AudioEngine engine(48000, 1);
    constexpr int frames = 48000;
    std::vector<float> original(frames), treated(frames);
    for (int i = 0; i < frames; ++i) {
        const float v = 0.04f * static_cast<float>(
            std::sin(2.0 * 3.14159265358979323846 * 1000.0 * i / 48000.0));
        original[i] = v;
        treated[i] = v;
    }
    std::array<float, AudioEngine::BandCount> eq{};
    eq[4] = 8.0f;
    engine.setEqBandsDb(eq);
    engine.setEqEnabled(true);
    engine.process(treated.data(), treated.size());
    check(rms(treated, 24000) > 2.0 * rms(original, 24000),
          "1kHz EQ peak not amplified");
    engine.setEqEnabled(false);
    std::vector<float> bypass(frames, 0.05f);
    engine.process(bypass.data(), bypass.size());
    check(std::abs(bypass.back() - 0.05f) < 0.0001f, "EQ bypass");
}
void invalidAndFormats() {
    bool failed = false;
    try { AudioEngine invalid(48000, 0); }
    catch (const std::invalid_argument&) { failed = true; }
    check(failed, "invalid channels accepted");
    for (int rate : {44100, 48000, 96000}) {
        AudioEngine engine(rate, 8);
        engine.setEqEnabled(true);
        std::vector<float> samples(800, 0.0f);
        for (std::size_t i = 0; i < samples.size(); i += 8)
            samples[i] = 0.1f;
        samples[80] = std::numeric_limits<float>::quiet_NaN();
        engine.process(samples.data(), samples.size() / 8);
        for (float v : samples) check(std::isfinite(v), "invalid PCM passed");
        for (std::size_t i = 0; i < samples.size(); i += 8)
            for (std::size_t c = 1; c < 8; ++c)
                check(samples[i+c] == 0, "channel bleed");
    }
}
int main() {
    try {
        unityAndBoost();
        limiterAndChannels();
        equalizerFrequencyResponse();
        invalidAndFormats();
        std::cout << "LouderME native DSP: tests passed\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "FAIL: " << ex.what() << '\n';
        return 1;
    }
}
