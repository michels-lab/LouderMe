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
// An abrupt EQ bypass used to jump from a strongly boosted 1 kHz crest
// to the unprocessed sample in a single frame (an audible click). The wet/dry
// ramp must preserve the first frame and settle to exact dry output.
void smoothEqBypassTransition() {
    constexpr int sampleRate = 48000;
    AudioEngine engine(sampleRate, 1);
    std::array<float, AudioEngine::BandCount> eq{};
    eq[4] = 10.0f;
    engine.setEqBandsDb(eq);
    engine.setEqEnabled(true);

    // End one sample before the crest, then switch EQ off at the crest.
    std::vector<float> warm(sampleRate + 12);
    for (std::size_t frame = 0; frame < warm.size(); ++frame)
        warm[frame] = 0.08f * static_cast<float>(std::sin(
            2.0 * 3.14159265358979323846 * 1000.0 * frame / sampleRate));
    engine.process(warm.data(), warm.size());
    check(warm.back() > 0.18f, "EQ wet baseline not established");

    engine.setEqEnabled(false);
    float firstDry = 0.08f; // Next input frame: 1 kHz sine crest.
    engine.process(&firstDry, 1);
    check(firstDry > 0.14f && firstDry < 0.30f,
          "EQ bypass jumped immediately instead of fading");

    std::vector<float> settle(9600, 0.04f);
    engine.process(settle.data(), settle.size());
    check(std::abs(settle.back() - 0.04f) < 0.00001f,
          "EQ bypass did not settle to exact dry PCM");

    engine.setEqEnabled(true);
    float firstWet = 0.08f;
    engine.process(&firstWet, 1);
    check(std::abs(firstWet - 0.08f) < 0.005f,
          "EQ enable caused an abrupt gain jump");

    // Multichannel bypass must not introduce bleed during a fade.
    AudioEngine stereo(sampleRate, 2);
    stereo.setEqBandsDb(eq);
    stereo.setEqEnabled(true);
    std::vector<float> interleaved(4096 * 2, 0.0f);
    for (std::size_t i = 0; i < interleaved.size(); i += 2)
        interleaved[i] = 0.06f;
    stereo.process(interleaved.data(), 4096);
    stereo.setEqEnabled(false);
    stereo.process(interleaved.data(), 4096);
    for (std::size_t i = 1; i < interleaved.size(); i += 2)
        check(interleaved[i] == 0.0f, "EQ transition leaked into silent channel");
}

// Live EQ slider moves must fade between filter banks rather than resetting
// the active biquads and abruptly swapping their gain.
void smoothEqBandUpdate() {
    constexpr int rate = 48000;
    AudioEngine engine(rate, 1);
    std::array<float, AudioEngine::BandCount> bands{};
    bands[4] = 10.0f;
    engine.setEqBandsDb(bands);
    engine.setEqEnabled(true);

    std::vector<float> warm(rate + 12);
    for (std::size_t i = 0; i < warm.size(); ++i)
        warm[i] = 0.05f * static_cast<float>(std::sin(
            2.0 * 3.14159265358979323846 * 1000.0 * i / rate));
    engine.process(warm.data(), warm.size());
    check(warm.back() > 0.12f, "Live EQ baseline not established");

    bands[4] = -10.0f;
    engine.setEqBandsDb(bands);
    float first = 0.05f; // Continue the same tone at its crest.
    engine.process(&first, 1);
    check(first > 0.10f && first < 0.19f,
          "Live EQ band change jumped to new filter response in one sample");

    std::vector<float> next(rate), original(rate);
    for (int i = 0; i < rate; ++i)
        next[i] = original[i] = 0.05f * static_cast<float>(std::sin(
            2.0 * 3.14159265358979323846 * 1000.0 * (i + 13) / rate));
    engine.process(next.data(), next.size());
    check(rms(next, rate / 2) < 0.55 * rms(original, rate / 2),
          "Live EQ attenuation did not settle to the new -10 dB band");

    // The second channel must remain absolutely silent during dual-bank fade.
    AudioEngine stereo(rate, 2);
    bands[4] = 10.0f;
    stereo.setEqBandsDb(bands);
    stereo.setEqEnabled(true);
    std::vector<float> interleaved(4096 * 2, 0.0f);
    for (std::size_t i = 0; i < interleaved.size(); i += 2)
        interleaved[i] = 0.04f;
    stereo.process(interleaved.data(), 4096);
    bands[4] = -10.0f;
    stereo.setEqBandsDb(bands);
    stereo.process(interleaved.data(), 4096);
    for (std::size_t i = 1; i < interleaved.size(); i += 2)
        check(interleaved[i] == 0.0f, "Live EQ change leaked to silent channel");
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
        smoothEqBypassTransition();
        smoothEqBandUpdate();
        invalidAndFormats();
        std::cout << "LouderME native DSP: tests passed\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "FAIL: " << ex.what() << '\n';
        return 1;
    }
}
