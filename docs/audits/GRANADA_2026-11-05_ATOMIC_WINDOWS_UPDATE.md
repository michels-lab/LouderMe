# Operación Granada — 2026-11-05: descarga atómica de actualizaciones Windows

## Causa raíz
DesktopUpdateService.DownloadInstallerAsync abría el ejecutable final con File.Create al comenzar una descarga HTTP. Una conexión interrumpida, cancelación, longitud truncada o checksum erróneo podía truncar un instalador anteriormente verificado de la misma versión o dejar un .exe incompleto.

## Implementación real
Se escribe primero un .part no ejecutable, aleatorio, en el MISMO directorio del instalador. Se limita el stream a 256 MiB, incluso si falta Content-Length; se verifica el tamaño declarado, el SHA256 contra el manifest oficial y la cancelación antes de mover el archivo al nombre final con File.Move overwrite. El instalador anterior solo se reemplaza una vez que el candidato pasa todas las comprobaciones. 100% de progreso significa que el fichero final ya se publicó. Los ficheros .part de intentos fallidos se eliminan en finally en la medida permitida por el sistema de archivos.

## Pruebas
El comando Windows --check-update-contract del binario verdadero ejecuta fixtures con MemoryStream y filesystem real: candidato correcto, SHA incorrecto, truncamiento, Content-Length excesivo y cancelación. Comprueba que el instalador previamente verificado conserva sus bytes y no quedan .part. No hay descarga externa, instalación real, validación Authenticode ni publicación.

## Gate
Requiere CI Windows del PR exacto. Sin RC ni release; #48 audio nativo físico y #33 evidencia de binario final permanecen por separado.
