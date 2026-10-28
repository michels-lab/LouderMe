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
    constexpr int rate = 48000;
    constexpr int frames = rate;
    constexpr std::array<float, AudioEngine::BandCount> productBands{
        60.0f, 150.0f, 400.0f, 1000.0f, 2500.0f, 6000.0f, 12000.0f
    };
    check(AudioEngine::CenterHz == productBands,
          "Native seven-band ordering differs from Android/Desktop product EQ");
    // One independent 48-kHz DSP instance per advertised band. Each peak
    // must affect the ACTUAL tone at its own frequency, not only a hardcoded
    // legacy index. Synthetic low-amplitude PCM cannot prove physical audio.
    for (std::size_t band = 0; band < AudioEngine::BandCount; ++band) {
        AudioEngine engine(rate, 1);
        std::array<float, AudioEngine::BandCount> gains{};
        gains[band] = 8.0f;
        engine.setEqBandsDb(gains);
        engine.setEqEnabled(true);
        std::vector<float> original(frames), treated(frames);
        for (int frame = 0; frame < frames; ++frame) {
            const float sample = 0.02f * static_cast<float>(
                std::sin(2.0 * 3.14159265358979323846 *
                         productBands[band] * frame / rate));
            original[frame] = treated[frame] = sample;
        }
        engine.process(treated.data(), treated.size());
        const double ratio = rms(treated, frames / 2) /
                             rms(original, frames / 2);
        check(ratio > 1.85 && ratio < 3.15,
              "Configured EQ band fails response at advertised center Hz");
    }

    AudioEngine bypassEngine(rate, 1);
    std::array<float, AudioEngine::BandCount> oneKhz{};
    oneKhz[3] = 8.0f; // index 3 is 1 kHz in all product EQs.
    bypassEngine.setEqBandsDb(oneKhz);
    bypassEngine.setEqEnabled(true);
    std::vector<float> warm(frames, 0.05f);
    bypassEngine.process(warm.data(), warm.size());
    bypassEngine.setEqEnabled(false);
    std::vector<float> bypass(frames, 0.05f);
    bypassEngine.process(bypass.data(), bypass.size());
    check(std::abs(bypass.back() - 0.05f) < 0.0001f, "EQ bypass");
}
// An abrupt EQ bypass used to jump from a strongly boosted 1 kHz crest
// to the unprocessed sample in a single frame (an audible click). The wet/dry
// ramp must preserve the first frame and settle to exact dry output.
void smoothEqBypassTransition() {
    constexpr int sampleRate = 48000;
    AudioEngine engine(sampleRate, 1);
    std::array<float, AudioEngine::BandCount> eq{};
    eq[3] = 10.0f;
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
    bands[3] = 10.0f;
    engine.setEqBandsDb(bands);
    engine.setEqEnabled(true);

    std::vector<float> warm(rate + 12);
    for (std::size_t i = 0; i < warm.size(); ++i)
        warm[i] = 0.05f * static_cast<float>(std::sin(
            2.0 * 3.14159265358979323846 * 1000.0 * i / rate));
    engine.process(warm.data(), warm.size());
    check(warm.back() > 0.12f, "Live EQ baseline not established");

    bands[3] = -10.0f;
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
    bands[3] = 10.0f;
    stereo.setEqBandsDb(bands);
    stereo.setEqEnabled(true);
    std::vector<float> interleaved(4096 * 2, 0.0f);
    for (std::size_t i = 0; i < interleaved.size(); i += 2)
        interleaved[i] = 0.04f;
    stereo.process(interleaved.data(), 4096);
    bands[3] = -10.0f;
    stereo.setEqBandsDb(bands);
    stereo.process(interleaved.data(), 4096);
    for (std::size_t i = 1; i < interleaved.size(); i += 2)
        check(interleaved[i] == 0.0f, "Live EQ change leaked to silent channel");
}

// A user may toggle EQ off then on faster than one PCM callback. With no
// processed audio between those commands the result must be identical to
// uninterrupted EQ (filter history must not be reset).
void rapidEqTogglePreservesHistory() {
    AudioEngine steady(48000, 1), toggled(48000, 1);
    std::array<float, AudioEngine::BandCount> bands{};
    bands[3] = 10.0f;
    for (auto* engine : {&steady, &toggled}) {
        engine->setEqBandsDb(bands);
        engine->setEqEnabled(true);
    }
    std::vector<float> warm(3000);
    for (std::size_t i = 0; i < warm.size(); ++i)
        warm[i] = 0.05f * static_cast<float>(std::sin(
            2.0 * 3.14159265358979323846 * 1000.0 * i / 48000.0));
    std::vector<float> copied = warm;
    steady.process(warm.data(), warm.size());
    toggled.process(copied.data(), copied.size());
    toggled.setEqEnabled(false);
    toggled.setEqEnabled(true);
    std::vector<float> a(256), b(256);
    for (std::size_t i = 0; i < a.size(); ++i)
        a[i] = b[i] = 0.05f * static_cast<float>(std::sin(
            2.0 * 3.14159265358979323846 * 1000.0 * (i + 3000) / 48000.0));
    steady.process(a.data(), a.size());
    toggled.process(b.data(), b.size());
    for (std::size_t i = 0; i < a.size(); ++i)
        check(std::abs(a[i] - b[i]) < 0.000001f,
              "Rapid EQ off-on reset live filter history");
}

// A clean native DSP must NOT change full-scale music when boost and EQ
// are off. The previous always-on soft-knee compressed 0.88..1.0 peaks.
void transparentFullScaleBypass() {
    for (int rate : {44100, 48000, 96000}) {
        AudioEngine engine(rate, 2);
        const std::array<float, 10> peaks{
            -1.0f, -0.9999f, -0.96f, -0.90f, -0.88f,
            0.0f, 0.88f, 0.92f, 0.9999f, 1.0f
        };
        std::vector<float> pcm;
        pcm.reserve(8000);
        for (int i = 0; i < 800; ++i)
            pcm.insert(pcm.end(), peaks.begin(), peaks.end());
        auto original = pcm;
        engine.process(pcm.data(), pcm.size() / 2);
        check(pcm == original, "unity bypass compressed full-scale PCM");
        engine.setBoostPercent(250);
        engine.setBoostEnabled(false);
        pcm = original;
        engine.process(pcm.data(), pcm.size() / 2);
        check(pcm == original, "disabled 250% Boost altered dry PCM");
        engine.setEqEnabled(true); // all seven gains at EXACT 0 dB
        pcm = original;
        engine.process(pcm.data(), pcm.size() / 2);
        for (std::size_t i = 0; i < pcm.size(); ++i)
            check(std::abs(pcm[i] - original[i]) < 1e-6f,
                  "flat enabled EQ altered the full-scale source");
        engine.setEqEnabled(false);
        pcm = original;
        engine.process(pcm.data(), pcm.size() / 2);
        check(pcm == original, "return to flat bypass changed PCM");
    }
}

void limiterEngagementAndSafeBoundaries() {
    constexpr int rate = 48000;
    AudioEngine engine(rate, 1);
    // At 0.99 normalized input, the old always-on limiter immediately
    // changed the sample even before Boost/EQ was turned on.
    float untouched = 0.99f;
    engine.process(&untouched, 1);
    check(untouched == 0.99f, "limiter changed dry 0.99 peak");
    engine.setBoostPercent(250);
    engine.setBoostEnabled(true);
    float first = 0.99f;
    engine.process(&first, 1);
    check(first > 0.97f && first <= 1.0f,
          "turning Boost on introduced abrupt dry-to-knee compression");
    std::vector<float> pcm(rate, 0.99f);
    engine.process(pcm.data(), pcm.size());
    for (const float v : pcm)
        check(std::isfinite(v) && v >= -1.0f && v <= 1.0f,
              "engaging Boost exceeded bounded float PCM");
    check(pcm.back() < 1.0f && pcm.back() > 0.88f,
          "active Boost bypassed the soft limiter");
    engine.setBoostEnabled(false);
    std::vector<float> off(rate, 0.99f);
    engine.process(off.data(), off.size());
    check(off.back() == 0.99f,
          "limiter remained active after fully settled Boost bypass");

    // EQ on/off should fade limiter engagement independently. A 0 dB EQ
    // should never cause the limiter's knee to engage at all.
    std::array<float, AudioEngine::BandCount> bands{};
    bands[3] = 10.0f;
    engine.setEqBandsDb(bands);
    engine.setEqEnabled(true);
    std::vector<float> wet(rate, 0.91f);
    engine.process(wet.data(), wet.size());
    for (const float sample : wet)
        check(std::isfinite(sample) && std::abs(sample) <= 1.0f,
              "EQ with active limiter produced unsafe PCM");
    engine.setEqEnabled(false);
    std::vector<float> restore(rate, -0.99f);
    engine.process(restore.data(), restore.size());
    check(restore.back() == -0.99f,
          "disabled EQ kept compressing high-amplitude dry PCM");

    // Any invalid source float still becomes safe regardless of whether
    // the limiter is enabled: no NaN or infinity reaches speakers.
    std::vector<float> invalid{
        std::numeric_limits<float>::quiet_NaN(),
        std::numeric_limits<float>::infinity(),
        -std::numeric_limits<float>::infinity(),
        2.0f, -2.0f
    };
    engine.process(invalid.data(), invalid.size());
    for (const float value : invalid)
        check(std::isfinite(value) && value >= -1.0f && value <= 1.0f,
              "invalid or out-of-range PCM escaped dry bypass");
}

void limiterPacketBoundaryParity() {
    constexpr int rate = 48000;
    AudioEngine whole(rate, 2), fragmented(rate, 2);
    for (auto* engine : {&whole, &fragmented}) {
        engine->setBoostPercent(150);
        engine->setBoostEnabled(true);
    }
    std::vector<float> a(24000), b(24000);
    for (std::size_t i = 0; i < a.size(); ++i) {
        a[i] = static_cast<float>(0.94 * std::sin(
            2.0 * 3.14159265358979323846 * 1000.0 * i / (rate * 2)));
        b[i] = a[i];
    }
    whole.process(a.data(), a.size() / 2);
    std::size_t done = 0;
    while (done < b.size() / 2) {
        const std::size_t frames = std::min<std::size_t>(
            (done % 17) + 1, b.size() / 2 - done);
        fragmented.process(b.data() + 2 * done, frames);
        done += frames;
    }
    check(a == b, "limiter activation differs across PCM packet boundaries");
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
        rapidEqTogglePreservesHistory();
        transparentFullScaleBypass();
        limiterEngagementAndSafeBoundaries();
        limiterPacketBoundaryParity();
        invalidAndFormats();
        std::cout << "LouderME native DSP: tests passed\n";
        return 0;
    } catch (const std::exception& ex) {
        std::cerr << "FAIL: " << ex.what() << '\n';
        return 1;
    }
}
