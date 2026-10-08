#pragma once
#include <array>
#include <cstddef>
#include <vector>

namespace louderme::audio {
// Portable, Michel's Lab-owned PCM processor; OS audio routing is a separate adapter.
class AudioEngine final {
public:
    static constexpr std::size_t BandCount = 7;
    static constexpr std::array<float, BandCount> CenterHz{
        60.0f, 170.0f, 310.0f, 600.0f, 1000.0f, 3000.0f, 12000.0f
    };
    AudioEngine(int sampleRate, int channels);
    int sampleRate() const noexcept { return sampleRate_; }
    int channels() const noexcept { return channels_; }
    // Call setters from the audio thread or while processing is stopped.
    void setBoostEnabled(bool enabled) noexcept;
    void setBoostPercent(int percent) noexcept;
    void setEqEnabled(bool enabled) noexcept;
    void setEqBandsDb(const std::array<float, BandCount>& gainsDb) noexcept;
    void reset() noexcept;
    // In-place interleaved normalized float32 PCM. No allocations in process().
    void process(float* samples, std::size_t frames) noexcept;

private:
    struct Biquad {
        double b0{1}, b1{0}, b2{0}, a1{0}, a2{0};
        double z1{0}, z2{0};
        void configurePeak(double sampleRate, double centerHz, double gainDb) noexcept;
        void clear() noexcept { z1 = z2 = 0; }
        double tick(double x) noexcept;
    };
    void configureFilters() noexcept;
    int sampleRate_;
    int channels_;
    bool boostEnabled_{false};
    bool eqEnabled_{false};
    int boostPercent_{100};
    double currentGain_{1.0};
    double targetGain_{1.0};
    double smoothingCoefficient_;
    std::array<float, BandCount> bandGainsDb_{};
    std::vector<std::array<Biquad, BandCount>> filters_;
};
} // namespace louderme::audio
