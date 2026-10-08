#include "louderme/AudioEngineC.h"

#include <array>
#include <cmath>
#include <cstdio>
#include <stdexcept>
#include <vector>

void require(bool condition, const char* message) {
    if (!condition) throw std::runtime_error(message);
}

int main() {
    try {
        require(louderme_abi_version() == 1, "ABI version mismatch");
        require(louderme_engine_create(44100, 0) == nullptr, "unsupported channels accepted");
        const auto engine = louderme_engine_create(48000, 2);
        require(engine != nullptr, "valid engine creation failed");
        std::vector<float> samples(2 * 4800, 0.1f);
        require(louderme_engine_set_boost(engine, 1, 150) == 0, "boost setter failed");
        require(louderme_engine_set_boost(engine, 1, 251) != 0, "invalid boost accepted");
        std::array<float, 7> flat{};
        require(louderme_engine_set_eq(engine, 0, flat.data()) == 0, "EQ setter failed");
        require(louderme_engine_set_eq(engine, 0, nullptr) != 0, "null EQ accepted");
        require(louderme_engine_process(engine, samples.data(), samples.size() / 2) == 0,
                "process failed");
        require(std::abs(samples.back() - 0.15f) < 0.001f, "ABI failed to process real gain");
        require(louderme_engine_process(engine, nullptr, 2) != 0, "null PCM accepted");
        louderme_engine_destroy(engine);
        std::puts("LouderME native C ABI: passed");
        return 0;
    } catch (const std::exception& ex) {
        std::fprintf(stderr, "FAIL: %s\n", ex.what());
        return 1;
    }
}
