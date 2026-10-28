# Operación Granada — 2026-10-28: cierre de invariantes de coherencia

## Cambio integrado
- PR: https://github.com/michels-lab/LouderMe/pull/127
- Commit integrado: `0bfbdfec14aa68ab6200ad789af23dcf60a1da62`
- `NoSourceControlCheck.ps1` rechaza coherencia > 1.0, en consonancia con `IndependentGainCheck.ps1`.
- `test_NoSourceControlCheck.ps1` añade un caso adversarial nuevo; ahora son 17.
- `tests/test_independent_source_contract.py` exige 17 pruebas y comprueba el nuevo caso, resolviendo el fallo inicial de CI.

## Evidencia automática en HEAD
- Native DSP: https://github.com/michels-lab/LouderMe/actions/runs/40629105183 — success.
- Android: https://github.com/michels-lab/LouderMe/actions/runs/40629105227 — success.
- Driver virtual experimental: https://github.com/michels-lab/LouderMe/actions/runs/40629105196 — success.

## Pendientes de producto (NO cerrados)
- #46 / #48: ruta Windows de audio de aplicaciones ajenas → endpoint virtual → DSP → salida física sin depender de APO, con driver distribuible y firmado.
- #74: efecto audible real de Boost y EQ no validado en hardware del propietario.
- #33 / #40 / #77: QA exacto de binarios, higiene administrativa de firmador histórico y Developer Edition.
- No se ha instalado un driver firmado, demostrado audio físico, ni publicado un nuevo release en este bloque.

## Próximo objetivo
Completar pruebas de dispositivo y recuperación de ruta, diferenciando de forma estricta validación sintética de la comprobación real de PCM de otra aplicación.