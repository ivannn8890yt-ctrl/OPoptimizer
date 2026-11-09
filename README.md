# OPoptimizer 🔵

**Optimizador de Windows para gaming, de código abierto.** Interfaz negra y azul, hecha con C# / WPF / .NET 8. Licencia MIT.

> **Estado: v1.2 (MVP, corrección de compilación).** El código fuente está incluido, pero el ejecutable necesita compilarse en Windows. Esta versión no está probada en un equipo Windows real desde el entorno de generación.

## Corrección de compilación v1.2

Se incorporó `using System.IO;` explícitamente en cinco archivos que usan `Path`, `Directory`, `File`, `FileInfo`, `DriveInfo` y tipos relacionados. Soluciona los errores de símbolos `CS0103` / `CS0246` que aparecieron al compilar v1.1. No hace falta reinstalar .NET.

**Nota:** se verificó la estructura y coherencia estática de los archivos, pero no se pudo ejecutar la compilación WPF en este entorno Linux. Si .NET muestra otro error distinto, compartí la nueva salida de compilación.

## ¿Qué hace de verdad?

| Función | Estado |
| --- | --- |
| Panel con datos reales de CPU, RAM y espacio del disco de sistema | ✅ |
| Análisis de temporales del usuario con más de 7 días | ✅ |
| Limpieza de archivos antiguos con selección por categoría + confirmación | ✅ |
| Limpieza opcional de logs antiguos de Roblox y Minecraft | ✅ |
| Limpieza opcional de informes de fallos `.dmp` antiguos | ✅ |
| Detección de juegos de Steam, Epic y carpetas comunes de Roblox/Minecraft | ✅ |
| Cambio reversible de animaciones de Windows (API oficial por usuario) | ✅ |
| Acceso a ajustes nativos de Modo Juego | ✅ |
| Optimización automática de todos los juegos o aumento de FPS garantizado | ❌ |
| Cambios ocultos al registro, servicios o prioridades de procesos | ❌ |

**Importante:** limpiar temporales libera almacenamiento, pero no garantiza que los juegos ganen FPS. La opción de animaciones afecta a parte de la interfaz de Windows, no a las animaciones internas de los juegos. Los archivos eliminados no van a la papelera y no tienen botón de restauración; por eso se realiza vista previa y confirmación. El botón de restauración corresponde exclusivamente al ajuste de animaciones.

## Compilar con doble clic (recomendado)

1. Descomprimí el ZIP completo en Windows 10/11 (64 bits).
2. Hacé **doble clic en `COMPILAR_OPoptimizer.cmd`**.
3. Si no está instalado el SDK de .NET, el asistente te ofrecerá instalar la versión oficial 8 mediante `winget`, **solo con tu consentimiento**. También podés hacerlo manualmente.
4. Al terminar la compilación, encontrarás `dist\OPoptimizer.exe`.

**No hace falta modificar archivos C# ni abrir Visual Studio.** El `.cmd` es un asistente para compilar con el compilador oficial de Microsoft, no un compilador inventado. La primera compilación necesita internet para descargar dependencias. Esta automatización se preparó sin haber podido probarse en una PC Windows en este entorno.

## Compilar y ejecutar desde PowerShell (alternativa)

1. Instalá [**.NET 8 SDK**](https://dotnet.microsoft.com/download/dotnet/8.0) en Windows 10/11.
2. Descomprimí este ZIP en una carpeta.
3. Abrí esa carpeta en una terminal PowerShell y ejecutá:

   ```powershell
   .\build.ps1
   ```

   Si PowerShell impide ejecutar scripts, no cambies la política de seguridad: utilizá el comando directo de abajo.
4. Encontrarás `dist\OPoptimizer.exe`. Se puede usar sin instalar .NET en el equipo de destino si el publish autónomo termina correctamente. Como WPF incluye componentes nativos, la carpeta `dist` puede contener archivos adicionales; distribuí **la carpeta completa**.

### Alternativa sin PowerShell

```powershell
dotnet publish OPoptimizer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

### Abrir en Visual Studio

Abrí `OPoptimizer.csproj` con Visual Studio 2022 o posterior, con la carga de trabajo **Desarrollo de escritorio con .NET** instalada. Usá `F5` para ejecutar en modo desarrollo.

## Publicar como open source en GitHub

1. Creá un repositorio público llamado `OPoptimizer`.
2. Subí los archivos **de la carpeta OPoptimizer**, incluyendo `.github` y `LICENSE` (no solo el ZIP).
3. Abrí la pestaña **Actions** y seleccioná **Build Windows EXE**. La acción también se ejecutará con cada push a `main`/`master`.
4. En la ejecución exitosa, bajá el artefacto `OPoptimizer-Windows-x64` para obtener una compilación de Windows.
5. Si lo vas a distribuir, probalo personalmente en una PC Windows y publicá una Release; evitá afirmar mejoras de FPS sin mediciones.

## Seguridad de la limpieza

- **No se ejecuta ninguna eliminación al abrir el programa ni al analizar.** Solo después de pulsar «Limpiar seleccionados» y confirmar.
- Se trabaja exclusivamente con ubicaciones permitidas: temporales del usuario, informes de fallos viejos y logs antiguos de Roblox/Minecraft.
- Temporales: más de 7 días. Informes de fallos: más de 14 días. Logs gaming: más de 7 días.
- Se ignoran enlaces simbólicos/puntos de reanálisis durante el escaneo y la eliminación.
- Antes de borrar se comprueba que el archivo siga siendo el mismo (tamaño y fecha), que siga siendo antiguo y que continúe dentro de la carpeta autorizada.
- Se omiten archivos bloqueados o inaccesibles y se conserva `latest.log`.
- No se modifican directorios de instalación ni carpetas de mundos, partidas guardadas ni documentos.
- No se necesitan permisos de administrador para las funciones normales. Nunca pedimos desactivar antivirus.
- Algunas aplicaciones aún podrían necesitar un temporal viejo; cerrá tus aplicaciones antes de limpiar y revisá la selección.

## Estructura del proyecto

```text
OPoptimizer/
├── OPoptimizer.csproj
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── Models/
│   ├── CleanupGroup.cs
│   └── GameEntry.cs
├── Services/
│   ├── CleanerService.cs
│   ├── GameDiscoveryService.cs
│   ├── SettingsService.cs
│   ├── SystemStatsService.cs
│   └── WindowsAnimationsService.cs
├── Assets/app.ico (ícono del ejecutable)
├── .github/workflows/windows-build.yml
├── COMPILAR_OPoptimizer.cmd (doble clic para compilar)
├── build.ps1
├── LICENSE
└── README.md
```

## Licencia

**MIT**. Podés estudiar, modificar y publicar versiones propias del código respetando la licencia. OPoptimizer no recopila datos personales, no incluye anuncios ni efectúa peticiones de red.
