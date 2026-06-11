using System;
using System.Threading;
using System.Threading.Tasks;

namespace PreSplitCamionetas.Scanner
{
    /// <summary>
    /// Serializa los eventos de entrada del scanner Honeywell.
    ///
    /// PROBLEMA ORIGINAL:
    ///   ITextWatcher.OnTextChanged es async void. El scanner Honeywell emite
    ///   cada carácter como evento separado, disparando N instancias concurrentes
    ///   de OnTextChanged antes de que termine la primera. Esto provoca:
    ///     - Inserciones duplicadas en xprod (race condition en GuardarEnBD)
    ///     - Corrupción de listItem (Add concurrente en List<T> no thread-safe)
    ///     - thisConnection.Open() concurrente → "connection already open"
    ///     - Crash al escanear tarimas completas (muchos códigos rápidos)
    ///
    /// SOLUCIÓN:
    ///   SemaphoreSlim(1,1) garantiza que solo un escaneo se procesa a la vez.
    ///   El código de detección de "lectura completa" evita procesar caracteres
    ///   parciales — el scanner Honeywell termina cada código con '\n' o '\r'.
    /// </summary>
    public class ScannerInputHandler
    {
        // Semáforo binario: solo 1 procesamiento activo a la vez.
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // Última lectura procesada para evitar re-procesamiento del mismo código.
        private string _lastProcessed = string.Empty;

        // Delimitadores que el scanner Honeywell agrega al final del código.
        // En la mayoría de configuraciones Honeywell AIDC es '\n' (0x0A).
        private static readonly char[] Terminators = { '\n', '\r', '\t' };

        /// <summary>
        /// Llamar desde ITextWatcher.OnTextChanged.
        /// Retorna true y el texto limpio si el código está completo y listo
        /// para procesar. Retorna false si es un carácter parcial o duplicado.
        /// </summary>
        public bool TryGetCompleteScan(string rawText, out string cleanCode)
        {
            cleanCode = string.Empty;

            if (string.IsNullOrEmpty(rawText))
                return false;

            // El scanner Honeywell termina con \n. Si el texto no termina en
            // terminador, aún está recibiendo caracteres → ignorar.
            bool hasTerminator = rawText.EndsWith("\n") || rawText.EndsWith("\r");
            string candidate = rawText.TrimEnd(Terminators).Trim();

            // Requiere mínimo 5 caracteres para ser un código válido.
            if (candidate.Length < 5)
                return false;

            // Evita reprocesar el mismo código (p.ej. si el campo no se limpió).
            if (candidate == _lastProcessed)
                return false;

            // Si no tiene terminador y es el mismo texto anterior, es parcial.
            if (!hasTerminator && candidate == _lastProcessed)
                return false;

            _lastProcessed = candidate;
            cleanCode = candidate;
            return true;
        }

        /// <summary>
        /// Ejecuta la acción de procesamiento de forma serializada.
        /// Si ya hay un procesamiento en curso, descarta la llamada entrante
        /// (el scanner no debe acumular colas de lecturas simultáneas).
        /// </summary>
        /// <param name="action">Lógica asíncrona a ejecutar (EtiquetasBlanca, etiquestaverde, etc.)</param>
        public async Task ProcessScanAsync(Func<Task> action)
        {
            // TryWait(0): intenta adquirir sin bloquear.
            // Si ya está ocupado, descarta esta lectura → evita acumulación.
            bool acquired = await _semaphore.WaitAsync(0);
            if (!acquired)
                return;

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                // Log silencioso — no relanzar para no crashear el hilo UI.
                Android.Util.Log.Error("ScannerInputHandler", $"Error procesando scan: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Versión con timeout: si el procesamiento tarda más de maxWaitMs,
        /// descarta la entrada en lugar de acumular.
        /// </summary>
        public async Task ProcessScanWithTimeoutAsync(Func<Task> action, int maxWaitMs = 2000)
        {
            bool acquired = await _semaphore.WaitAsync(maxWaitMs);
            if (!acquired)
                return;

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("ScannerInputHandler", $"Error procesando scan: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Resetea el estado interno. Llamar al limpiar el campo de captura.
        /// </summary>
        public void Reset()
        {
            _lastProcessed = string.Empty;
        }
    }
}
