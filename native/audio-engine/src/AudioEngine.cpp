#include "louderme/AudioEngine.hpp"
#include <algorithm>
#include <cmath>
#include <stdexcept>

namespace louderme::audio {
namespace {
constexpr double Pi = 3.14159265358979323846;
constexpr double KneeStart = 0.88;
constexpr double KneeWidth = 1.0 - KneeStart;

// Bounded soft limiter. This is a safety stage, not a transparent true-peak limiter.
double limitOutput(double x) noexcept {
    if (!std::isfinite(x)) return 0.0;
    const auto magnitude = std::abs(x);
    if (magnitude <= KneeStart) return x;
    const auto limited = KneeStart + KneeWidth *
        std::tanh((magnitude - KneeStart) / KneeWidth);
    return std::copysign(std::min(1.0, limited), x);
}
}

AudioEngine::AudioEngine(int rate, int channels)
    : sampleRate_(rate), channels_(channels),
      smoothingCoefficient_(rate > 0
          ? 1.0 - std::exp(-1.0 / (0.005 * rate)) : 1.0),
      filters_(channels >= 1 && channels <= 8
          ? static_cast<std::size_t>(channels) : 0) {
    if (rate < 8000 || rate > 384000 || channels < 1 || channels > 8)
        throw std::invalid_argument("Unsupported sample rate or channels");
    configureFilters();
}

void AudioEngine::setBoostEnabled(bool enabled) noexcept {
    boostEnabled_ = enabled;
    targetGain_ = enabled ? boostPercent_ / 100.0 : 1.0;
}

void AudioEngine::setBoostPercent(int percent) noexcept {
    boostPercent_ = std::clamp(percent, 100, 250);
    targetGain_ = boostEnabled_ ? boostPercent_ / 100.0 : 1.0;
}

void AudioEngine::setEqEnabled(bool enabled) noexcept {
    if (eqEnabled_ != enabled) {
        eqEnabled_ = enabled;
        reset();
    }
}

void AudioEngine::setEqBandsDb(
    const std::array<float, BandCount>& gainsDb) noexcept {
    for (std::size_t i = 0; i < BandCount; ++i) {
        bandGainsDb_[i] = std::isfinite(gainsDb[i])
            ? std::clamp(gainsDb[i], -10.0f, 10.0f) : 0.0f;
    }
    configureFilters();
}

void AudioEngine::Biquad::configurePeak(
    double sampleRate, double frequency, double gainDb) noexcept {
    const auto freq = std::min(frequency, sampleRate * 0.45);
    const double omega = 2.0 * Pi * freq / sampleRate;
    const double amplitude = std::pow(10.0, gainDb / 40.0);
    const double alpha = std::sin(omega) / (2.0 * 1.2); // peaking EQ, Q=1.2
    const double a0 = 1.0 + alpha / amplitude;
    b0 = (1.0 + alpha * amplitude) / a0;
    b1 = (-2.0 * std::cos(omega)) / a0;
    b2 = (1.0 - alpha * amplitude) / a0;
    a1 = b1;
    a2 = (1.0 - alpha / amplitude) / a0;
    clear();
}

double AudioEngine::Biquad::tick(double input) noexcept {
    const double output = b0 * input + z1;
    z1 = b1 * input - a1 * output + z2;
    z2 = b2 * input - a2 * output;
    if (!std::isfinite(output) || !std::isfinite(z1) || !std::isfinite(z2)) {
        clear();
        return 0.0;
    }
    return output;
}

void AudioEngine::configureFilters() noexcept {
    for (auto& channel : filters_)
        for (std::size_t band = 0; band < BandCount; ++band)
            channel[band].configurePeak(
                sampleRate_, CenterHz[band], bandGainsDb_[band]);
}

void AudioEngine::reset() noexcept {
    for (auto& channel : filters_)
        for (auto& band : channel) band.clear();
}

void AudioEngine::process(float* samples, std::size_t frames) noexcept {
    if (!samples || frames == 0) return;
    for (std::size_t frame = 0; frame < frames; ++frame) {
        currentGain_ += smoothingCoefficient_ * (targetGain_ - currentGain_);
        for (int channel = 0; channel < channels_; ++channel) {
            const auto index = frame * static_cast<std::size_t>(channels_) +
                static_cast<std::size_t>(channel);
            double value = std::isfinite(samples[index])
                ? static_cast<double>(samples[index]) : 0.0;
            if (eqEnabled_)
                for (auto& filter : filters_[channel])
                    value = filter.tick(value);
            samples[index] = static_cast<float>(
                limitOutput(value * currentGain_));
        }
    }
}
} // namespace louderme::audio
