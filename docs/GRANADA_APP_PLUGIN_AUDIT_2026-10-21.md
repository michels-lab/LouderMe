# Operación Granada — auditoría de aplicación e integraciones ChatGPT

**Corte de evidencia:** 2026-10-21 UTC. **Tipo:** auditoría documental de árbol/README y últimos GitHub Actions consultados, NO prueba funcional en hardware, cuenta de cliente, ni validación completa del ejecutable. 

**Regla de costo:** usar primero las herramientas ya instaladas en ChatGPT Plus y los servicios existentes; **no contratar suscripciones extra**, activar facturación, subir de plan, comprar dominio ni desplegar sin autorización. Un plugin de ChatGPT no se incorpora automáticamente al código de la aplicación. “Instalado en ChatGPT” no prueba sesión activa, permisos o servicio operativo. [Bitácora central de ocho conectores instalados](https://github.com/michels-lab/Michel-Software-Standards/blob/main/docs/CHATGPT_PLUGIN_LEDGER.md). No guardar tokens ni datos de usuarios en documentación pública.

## Alcance y frontera
LouderMe es utilidad de audio Android/Windows. **No es una aplicación CFD**, no lee OpenFOAM, no gestiona Instagram ni tareas RPG. No confundir su repo de código con [LouderME-Releases](https://github.com/michels-lab/LouderME-Releases), canal público separado de binarios.

## Conectores y servicios: decisiones por proyecto
| Herramienta | Condición comprobada | Aplicación real / cautela | Decisión |
| --- | --- | --- | --- |
| **GitHub (ChatGPT)** | Instalado/habilitado y usado | Builds Android/Windows, pruebas, bitácora, PRs y canal de publicación con hashes/firmas | USAR |
| **Documents / Spreadsheets** | Instalados/habilitados | Documentar matrices de ganancia, clips, latencia, rutas, frames y metodología de ensayo SIN inventar lecturas | OPCIONAL para informe externo |
| **Supabase** | Instalado en ChatGPT, sin backend necesario verificado aquí | No agregar base de datos o cuentas a un ecualizador por conveniencia | NO AGREGAR |
| **Gmail / Calendar / Drive / Contacts** | Instalados/habilitados | Sin funcionalidad necesaria para audio local | NO INTEGRAR por defecto |
| **Vercel / hosting extra** | Sin conexión o despliegue verificados | No soluciona audio del sistema, firma de controlador o salida WASAPI | NO CONTRATAR |

**La ganancia software 150–250% no equivale a decibeles acústicos/SPL del altavoz.** No afirmar que un control del 250% duplica SPL ni que una prueba fuente/CI demuestra audio físico real.

## Últimas comprobaciones consultadas
- [File Steward #39664315311](https://github.com/michels-lab/LouderMe/actions/runs/39664315311): SUCCESS en head `66cb97fe`.
- [LouderMe Android CI & Direct Release #39664315248](https://github.com/michels-lab/LouderMe/actions/runs/39664315248): SUCCESS, mismo head. Esto no es evidencia de un nuevo binario público ni prueba de audio en el equipo real.
- README sitúa Android/Windows **v0.1.12** como versión pública; ruta física real de audio de otra app, controlador virtual instalado/firmado y aumento de ganancia bajo reproducción quedan sin verificar.

## Pendientes concretos
1. Mantener aislados los ensayos del controlador virtual, WASAPI y la app externa; comprobar la causalidad con fuente real y sin estímulo, no solo fixtures.
2. Confirmar seguridad auditiva/clipping, igualdad de bandas EQ y límites de ganancia en salidas/volúmenes de hardware concretos.
3. Developer Twin con diagnóstico de audio/reportes por sección, sin mentir sobre rutas/mediciones.
4. Evitar permisos, servicios o suscripciones nuevos mientras el enfoque es DSP, rutas y dispositivos locales.
5. Publicar solo release estable definitiva si pasa el gate de firmas, artefactos, hashes y controles; no RC.

**Estado:** AUDITADO DOCUMENTALMENTE / CI PARCIAL EXITOSA / ACEPTACIÓN DE AUDIO REAL PENDIENTE. Ver [PROJECT_LOG](../PROJECT_LOG.md) y [auditoría central](https://github.com/michels-lab/Michel-Software-Standards/blob/main/reports/2026-10-21_GRANADA_13_REPOS_PLUGIN_AUDIT.md).
