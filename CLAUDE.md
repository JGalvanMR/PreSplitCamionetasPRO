# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**PreSplitCamionetas** es una aplicación Android desarrollada en Xamarin (C#) para la gestión de pedidos en camionetas. La aplicación permite capturar y pre-splittear pedidos utilizando lectores de códigos de barras Honeywell integrados en dispositivos móviles.

## Arquitectura y Estructura

### Modelo de Datos (SQLite Local)

Las entidades se encuentran en `SplitCamionetas/Models/`:

| Clase | Tabla | Descripción |
|-------|-------|-------------|
| `Pedidos` | `Pedidos` | Pedidos pendientes de surtir |
| `ConPedidos` | `ConPedidos` | Control de pedidos con contador de surtido |
| `xLote` | `xLote` | Lotes temporales durante la captura |
| `xLoteFinal` | `xLoteFinal` | Lotes finales guardados antes de commit |
| `XLoteSug` | `XLoteSug` | Sugerencias de folios alternativos |
| `xprod` | `xprod` | Productos capturados (etiquetas leídas) |
| `Mensajes` | `Mensajes` | Mensajes de validación para el usuario |

### Capa de Datos

- `Data/Database.cs`: Singleton `Database` con conexión SQLite asíncrona (`Pre_Split_Camionetas.db3`)
- `Data/DatabaseContext.cs`: Contexto de base de datos (menos utilizado)

### Activities Principales

| Activity | Archivo | Descripción |
|----------|---------|-------------|
| `MainActivity` | `MainActivity.cs` | Pantalla de inicio con login y selección de vehículo/responsable |
| `SolicitarPed` | `SolicitarPed.cs` | Selección y carga de pedidos pendientes |
| `capturar_split` | `capturar_split.cs` | Captura de etiquetas (main flow) |
| `CancelarSplit` | `CancelarSplit.cs` | Cancelación de splits capturados |

### Web Service

- `WebServiceEmbarquesLocal/WebServiceEmbarques`: Service reference para operaciones de embarque (envío de correos, versiones, etc.)

## Flujo de Trabajo Principal

1. **Login** (`MainActivity`): Selección de vehículo y responsable
2. **Solicitar Pedidos** (`SolicitarPed`): Carga de pedidos pendientes desde SQL Server
3. **Capturar Split** (`capturar_split`):
   - Lectura de etiquetas verdes (trazabilidad) o blancas
   - Validación contra base de datos SQL Server
   - Guardado en SQLite (temporal)
   - Confirmación y envío a SQL Server

## Patrones y Convenciones

### Convenciones de Código

- Cadena de conexión SQL Server: `MainActivity.cadenaConexion` (static)
- Versión de la app: `capturar_split.Version` (actual: "3.7")
- Uso intensivo de `async/await` para operaciones de base de datos
- Toast messages para feedback al usuario
- Dialogs personalizados con formato HTML para mensajes

### Validaciones Principales

- `validaAsync()`: Valida etiquetas capturadas contra base de datos
- `validafecad()`: Validación de fechas de caducidad
- Validación de etiquetas duplicadas
- Validación de existencia en tarima

### Lectura de Códigos de Barras

La aplicación soporta múltiples formatos de etiquetas:
- Etiquetas verdes (formato PTP/PTC)
- Etiquetas blancas
- Lectura por SSCC (Serial Shipping Container Code)
- soporte para lectores Honeywell AIDC

## Herramientas de Build

```bash
# Compilar en Debug
msbuild PreSplitCamionetas.sln /p:Configuration=Debug

# Compilar en Release
msbuild PreSplitCamionetas.sln /p:Configuration=Release

# Build con Visual Studio (recomendado)
# Archivo de solución: PreSplitCamionetas.sln
```

## Configuración

- **Target Framework**: MonoAndroid 11.0 (Android 10)
- **Min SDK**: API 19 (Android 4.4)
- **Target SDK**: API 30 (Android 11)
- **Nombre del paquete**: `PreSplitCamionetas.PreSplitCamionetas`
- **Version Code**: 39
- **Version Name**: 4.9

## Permisos Requeridos

- `INTERNET`, `ACCESS_NETWORK_STATE`, `ACCESS_WIFI_STATE`
- `CAMERA`, `READ_EXTERNAL_STORAGE`, `WRITE_EXTERNAL_STORAGE`
- `READ_PHONE_STATE`, `ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION`
- `INSTALL_PACKAGES`, `REQUEST_INSTALL_PACKAGES` (para auto-update)
- `DECODE` (permiso Honeywell)

## Notas Importantes

- El proyecto utiliza `Task.Result` en algunos lugares (pattern obsoleto) - preferir `await`
- La aplicación soporta modo "Concentrado" (`mconcen = "2"`) para ver concentrado sin capturar
- Soporte para "Folios Adelantados" con autorización por password
- Persistencia de errores locales en archivo `errores.txt`
- Implementación de auto-update desde servidor interno
