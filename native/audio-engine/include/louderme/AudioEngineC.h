#pragma once

/* Stable C ABI for Michel's Lab DSP. Process PCM supplied by an audio host.
 * This API does not access or modify operating-system audio devices. */
#include <stddef.h>
#include <stdint.h>

#if defined(_WIN32)
#  if defined(LOUDERME_AUDIO_ENGINE_EXPORTS)
#    define LOUDERME_AUDIO_API __declspec(dllexport)
#  else
#    define LOUDERME_AUDIO_API __declspec(dllimport)
#  endif
#elif defined(__GNUC__)
#  define LOUDERME_AUDIO_API __attribute__((visibility("default")))
#else
#  define LOUDERME_AUDIO_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef void* louderme_engine_handle;

/* ABI revision is incremented for incompatible API changes. */
LOUDERME_AUDIO_API uint32_t louderme_abi_version(void);
/* Returns NULL if sample rate or channels are unsupported or allocation fails. */
LOUDERME_AUDIO_API louderme_engine_handle louderme_engine_create(int sample_rate, int channels);
LOUDERME_AUDIO_API void louderme_engine_destroy(louderme_engine_handle engine);
/* Return 0 on success; negative means invalid engine/arguments. */
LOUDERME_AUDIO_API int louderme_engine_set_boost(louderme_engine_handle engine, int enabled, int percent);
LOUDERME_AUDIO_API int louderme_engine_set_eq(louderme_engine_handle engine, int enabled, const float* seven_band_gains_db);
/* Interleaved 32-bit floating point PCM, frames per channel.
 * Caller owns the memory. No allocations are made within process().
 * Setters must NOT race with process(); marshal commands on the audio thread. */
LOUDERME_AUDIO_API int louderme_engine_process(
    louderme_engine_handle engine, float* interleaved_pcm, size_t frames);

#ifdef __cplusplus
}
#endif
