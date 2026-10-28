# Operación Granada — validación de evidencia física de audio (2026-10-27)

## Cambio integrado
- PR #126: https://github.com/michels-lab/LouderMe/pull/126
- Commit de integración: `a329ec34b4615371d6a603f1f441e0df416d97cf`
- Se rechazan coherencias físicas mayores a 1.0 y contadores negativos de `render_starved_reads` en el validador `native/virtual-driver/IndependentGainCheck.ps1`.
- Dos nuevos fixtures adversariales agregados en `native/virtual-driver/test_IndependentGainCheck.ps1`.

## Evidencia de CI del HEAD del PR
- Native audio DSP: https://github.com/michels-lab/LouderMe/actions/runs/40618597589 — success
- Virtual Render Driver Lab: https://github.com/michels-lab/LouderMe/actions/runs/40618597596 — success
- Android CI: https://github.com/michels-lab/LouderMe/actions/runs/40618597597 — success

## Limitaciones verificables
Las pruebas validan lógica sintética y compilaciones; **no** prueban dispositivo físico, publicación de driver firmado, instalación, audio de otras aplicaciones, percepción audible, SPL o ganancia acústica. Los issues #46, #48, #74 siguen abiertos.

## Siguiente corte de aceptación
1. En una VM Windows descartable con driver de pruebas instalado bajo consentimiento, capturar evidencia identificada de una aplicación externa independiente, ruta virtual -> DSP -> salida física.
2. Comparar 100 % y 150 % con controles sin estímulo antes y después, misma sesión y mismos fingerprints de endpoints y binarios; rechazar ausencia de frames, descartes y fallos de relación de amplitudes.
3. Validar recuperación de audio, selección y restauración de dispositivo, fallos y desinstalación. No trasladar un driver test-signed a versión estable.
4. Probar en dispositivo real de propietario antes de presentar Boost y EQ como efectivamente aplicados; distinguir CI de audición.
5. Sin release ni cambios de versión por esta auditoría.
