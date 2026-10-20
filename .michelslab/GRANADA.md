# GRANADA — continuidad sin interferencias

**Regla permanente de Michel's Lab.** Comando del propietario: **GRANADA**.

Si un bloque de desarrollo sigue incompleto, en pruebas, bloqueado por autenticación/hardware/release, o en manos de otra rama, PR o agente: **dejar constancia real** (issue/PR/SHA, áreas ocupadas, pruebas faltantes, responsable y punto de reentrada) y **pasar al siguiente bloque grande que sea independiente**. No rehacer el anterior, no borrar pendientes, no pisar cambios de otro equipo, no simular validaciones ni dejar procesos supuestamente trabajando en segundo plano.

Contrato maestro: [GRANADA Continuous Block Protocol](https://github.com/michels-lab/Michel-Software-Standards/blob/main/standards/GRANADA_CONTINUOUS_BLOCK_PROTOCOL.md).

En **michels-lab/LouderMe**:
- Contexto: LouderMe; Android and Windows.
- Bitácora/fuente local: `PROJECT_LOG.md`; issues y PRs activos tienen prioridad sobre una descripción antigua.
- Antes de empezar, comprobar HEAD, estado, PRs, claims, riesgos P0, archivos de propiedad y CI. Un trabajo bloqueado NO se considera completo; solo se aparca de manera reversible y documentada.
- Elegir un bloque diferente ya identificado por usuario o backlog existente, con límites de archivos y pruebas claros. Si hay colisión, cambiar de bloque.
- Ejecutar código y pruebas aplicables, registrar commit/resultado exacto, actualizar bitácora y master cuando corresponda; conservar estado y datos históricos.
- No tomar GRANADA como permiso para publicar versiones, alterar credenciales, borrar datos de producción, hacer merges sin QA ni ejecutar acciones inciertas sobre cuentas de Instagram.
- En repos solo de distribución o investigación sin interfaz, adaptar GRANADA a tareas de documentación, QA, herramientas científicas o releases expresamente autorizados; no inventar una app.

**Estados diferentes:** pendiente → implementado → pruebas superadas → integrado → publicado → validado por el usuario. No presentar una fase como otra.

**Punto de reentrada:** cualquier bloque aparcado conserva en el issue/bitácora su último paso seguro, SHA, dependencias y comprobaciones necesarias. Al siguiente comando **granada**, volver a buscar el siguiente bloque disponible que no interfiera.

## Cambio del propietario — 2026-10-20: GRANADA GRANDE

**Este repositorio hereda inmediatamente la escala obligatoria** del estándar maestro [GRANADA_CONTINUOUS_BLOCK_PROTOCOL.md](https://github.com/michels-lab/Michel-Software-Standards/blob/main/standards/GRANADA_CONTINUOUS_BLOCK_PROTOCOL.md#escala-obligatoria-desde-2026-10-20--granada-grande).

- Priorizar **un paquete amplio y funcional**, normalmente 3 entregables sustanciales relacionados (flujo completo, casos límite, persistencia o seguridad, pruebas de regresión y evidencia), no un mini-PR, una prueba suelta o solamente documentación. Un único P0 grande puede bastar; no inventar relleno.
- Revisar issues/PR/HEAD/claims antes de actuar; **la ambición no permite pisar trabajo de otros**, mezclar estados de usuario, arriesgar datos, reactivar plataformas pausadas ni publicar sin autorización. Sustituir subpartes bloqueadas por otras independientes.
- Cada bloque debe dejar **código/capacidad real y QA ejecutada cuando sea posible**, con estado A/B/C, SHAs, resultados y próximos gates. Un plan o una bitácora **no es un bloque funcional completado**.
- En repositorios de distribución, GRANADA GRANDE se traduce a paquetes de controles de integridad/procedencia/documentación o pruebas no destructivas; nunca crear versiones ni releases sin orden expresa.
- Mantener las bitácoras locales, los límites de cada producto y su punto de reentrada; al siguiente “operación granada”, empezar con el paquete sustancial seguro de mayor impacto.
