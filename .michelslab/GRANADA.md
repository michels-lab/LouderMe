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
