# PreSplitCamionetas

Aplicación Android desarrollada en **Xamarin (C#)** para la gestión de pre-split de pedidos en camionetas. Diseñada para operar en dispositivos industriales Honeywell con lector de código de barras integrado.

---

## Descripción

PreSplitCamionetas permite a los operadores capturar etiquetas de trazabilidad (verdes y blancas) para realizar el pre-split de pedidos antes de cargar una camioneta. Los datos se almacenan temporalmente en SQLite local y se sincronizan con SQL Server al confirmar la operación.

---

## Tecnologías

| Componente | Tecnología |
|---|---|
| Plataforma | Xamarin Android (C#) |
| Base de datos local | SQLite (`sqlite-net-pcl`) |
| Base de datos remota | SQL Server |
| Dispositivo objetivo | Honeywell AIDC (lector integrado) |
| Target Framework | MonoAndroid 11.0 (Android 10) |
| Min SDK | API 19 (Android 4.4 KitKat) |
| Target SDK | API 30 (Android 11) |
| Versión de la app | 4.9 (Version Code 39) |

---

## Requisitos

- Visual Studio 2019 o superior con workload **Mobile development with .NET**
- Xamarin.Android instalado
- Acceso a la red interna donde corre SQL Server
- Dispositivo Honeywell con Android 4.4 o superior (o emulador para desarrollo)

---

## Estructura del Proyecto

```
PreSplitCamionetas/
├── Activities/
│   ├── MainActivity.cs          # Login y selección de vehículo/responsable
│   ├── SolicitarPed.cs          # Selección y carga de pedidos pendientes
│   ├── capturar_split.cs        # Captura de etiquetas (flujo principal)
│   └── CancelarSplit.cs         # Cancelación de splits capturados
│
├── Data/
│   ├── Database.cs              # Singleton SQLite async (Pre_Split_Camionetas.db3)
│   └── DatabaseContext.cs       # Contexto alternativo de base de datos
│
├── Models/
│   ├── xprod.cs                 # Productos capturados (etiquetas leídas)
│   ├── ConPedidos.cs            # Control de pedidos con contador de surtido
│   ├── Pedidos.cs               # Pedidos pendientes de surtir
│   ├── xLote.cs                 # Lotes temporales durante la captura
│   ├── xLoteFinal.cs            # Lotes finales antes de commit
│   ├── XLoteSug.cs              # Sugerencias de folios alternativos
│   ├── Mensajes.cs              # Mensajes de validación para el usuario
│   └── FlimStarInfo.cs          # Modelo para ítems del GridView
│
├── Modal/
│   ├── DialogFragment.cs        # Diálogo de autorización por supervisor
│   └── myGVItemAdapter.cs       # Adaptador para el GridView de capturas
│
├── Utils/
│   └── GuardaLocal.cs           # Log de errores en archivo local (errores.txt)
│
└── WebServiceEmbarquesLocal/
    └── WebServiceEmbarques      # Service reference para embarques (correos, versiones)
```

---

## Flujo Principal

```
MainActivity (Login)
    └── SolicitarPed (selección de pedido)
            └── capturar_split (captura de etiquetas)
                    ├── Leer etiqueta verde  → etiquestaverde()
                    ├── Leer etiqueta blanca → EtiquetasBlancaAsync()
                    ├── Eliminar caja        → eliminaretiquetablanca()
                    └── Guardar              → BtnGuardar_Click() → SolicitarPed
```

### Detalle del flujo en `capturar_split`

1. El operador selecciona el modo de captura: etiqueta verde, blanca o eliminar caja.
2. El scanner Honeywell emite el código al campo `foliocaptura`.
3. `ITextWatcher.OnTextChanged` detecta el cambio y despacha al método correspondiente.
4. El método valida la etiqueta contra SQL Server (existencia, disponibilidad, fecha de caducidad).
5. Si es válida, se guarda en SQLite local (`xprod`, `ConPedidos`).
6. Al presionar **Guardar**, los datos se envían a SQL Server y SQLite se limpia.

---

## Tipos de Etiqueta Soportados

| Tipo | Descripción | Formato |
|---|---|---|
| **Verde PTC** | Trazabilidad de producción propia | `pti_clave` en `tb_det_trazabilidad` |
| **Verde PTP** | Trazabilidad de producto terminado | `folio` en `tb_det_eti_final` |
| **SSCC** | Serial Shipping Container Code (GS1-128) | Regex sobre código de barras |
| **Famoso** | Código interno de 12 dígitos | Campo `pti_famous` |
| **Blanca** | Etiqueta de pre-split ya generada | Parseo posicional de cadena |

---

## Modos de Operación

**Modo Normal** — captura y pre-split estándar de pedidos.

**Modo Concentrado** (`mconcen = "2"`) — solo consulta, no permite capturar. Se activa desde `SolicitarPed` al seleccionar un pedido en modo lectura.

**Folios Adelantados** — permite capturar etiquetas de un folio aún no cargado en la sesión. Requiere autorización con contraseña de supervisor mediante `DialogFragment`.

---

## Base de Datos Local (SQLite)

Archivo: `Pre_Split_Camionetas.db3` en almacenamiento interno del dispositivo.

| Tabla | Uso |
|---|---|
| `xprod` | Etiquetas capturadas en la sesión activa. Campo `Lecturabd` con constraint `UNIQUE` para evitar duplicados. |
| `ConPedidos` | Acumulado de cajas surtidas por producto en la sesión. |
| `Pedidos` | Pedidos cargados desde SQL Server al iniciar sesión. |
| `xLote` | Lotes temporales usados durante validación de fechas de caducidad. |
| `xLoteFinal` | Lotes listos para enviar a SQL Server. |
| `XLoteSug` | Sugerencias de folios alternativos cuando hay escasez. |
| `Mensajes` | Mensajes de estado y validación. |

---

## Configuración de Conexión

La cadena de conexión a SQL Server se define en `MainActivity.cs`:

```csharp
public static string cadenaConexion = "Server=...;Database=...;User Id=...;Password=...;";
```

Todos los módulos de la app consumen `MainActivity.cadenaConexion` como fuente única.

---

## Permisos Android Requeridos

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_PHONE_STATE" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.INSTALL_PACKAGES" />
<uses-permission android:name="android.permission.REQUEST_INSTALL_PACKAGES" />
<uses-permission android:name="com.honeywell.decode.permission.DECODE" />
```

El permiso `DECODE` es específico de Honeywell AIDC y habilita el acceso al lector de código de barras integrado.

---

## Compilación

```bash
# Debug
msbuild PreSplitCamionetas.sln /p:Configuration=Debug

# Release
msbuild PreSplitCamionetas.sln /p:Configuration=Release
```

Se recomienda compilar desde **Visual Studio** para gestionar las dependencias de Xamarin y el signing del APK.

---

## Auto-Update

La aplicación incluye un mecanismo de actualización automática. Al iniciar, compara la versión instalada (`currentVersionName`) con la disponible en el servidor interno vía `WebServiceEmbarques`. Si hay una versión nueva, descarga e instala el APK sin intervención del usuario.

---

## Log de Errores

Los errores de runtime se persisten localmente en:

```
/storage/emulated/0/errores.txt
```

Gestionado por `GuardaLocal.cs`. Útil para diagnóstico en campo cuando no hay acceso a depurador.

---

## Notas de Desarrollo

- Usar siempre `await` para operaciones sobre `Database` (SQLite-net async). El uso de `.Result` sobre `Task` en el hilo UI produce deadlock en el `SynchronizationContext` de Android.
- Las actualizaciones de UI (texto, adapters) deben ocurrir en el hilo principal. Usar `RunOnUiThread(() => { ... })` cuando el código async pueda estar en un hilo de pool.
- `thisConnection` (`SqlConnection`) es un campo de instancia compartido. Para operaciones concurrentes o desde `Task.Run`, crear una conexión local con `using (var conn = new SqlConnection(cadena))`.
- El adapter `myGVItemAdapter` recibe una copia defensiva de la lista al construirse. No modificar la lista de origen y esperar que el adapter refleje el cambio en tiempo real — reasignar el adapter con `gvObject.Adapter = new myGVItemAdapter(this, listItem)`.
