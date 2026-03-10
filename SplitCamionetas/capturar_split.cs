using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.Annotation;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Com.Honeywell.Aidc;
using Java.Lang;
using Java.Util;
using Plugin.DeviceInfo;
using PreSplitCamionetas.Data;
using PreSplitCamionetas.Modal;
using PreSplitCamionetas.Models;
using PreSplitCamionetas.Scanner;
using SQLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static Android.Graphics.Paint;
using static Android.Provider.Telephony;


namespace PreSplitCamionetas
{
    [Activity(Label = "Capturar pedidos")]

    public partial class capturar_split : Activity, Android.Text.ITextWatcher
    {
        #region VARIABLES        
        //public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira1;Initial Catalog =GAB_Irapuato; server=tcp:189.206.160.206,2352; Connect Timeout = 0";
        public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira1;Initial Catalog =GAB_Irapuato; server=tcp:192.168.123.6,2352; Connect Timeout = 0";
        public static int valido = 0, veces = 0;
        public static string cvvehiculo, cvresponsable;
        public static string vehiculo, responsable;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", mAutoriza = "", user = "", motfolade = "";
        public string cvecam = "", muser = "", mconcen = "1", Version = "3.7";
        public static string AutoPed = "N";
        public static string EtiquetaExiste = "S", EtiquetaCapturada = "S";
        public static string HayExistencias = "S";
        public static string Surtidomayor = "S";
        public static string ValiFechacad = "S";
        public static string EstructuraEtiqueta = "S";
        private string currentVersionName;
        public static SQLiteConnection db2;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        public static DataTable det_pedidos = new DataTable("det_pedidos");
        public static DataTable det_pedidos2 = new DataTable("det_pedidos2");
        public static DataTable productos_leidos = new DataTable("productos_leidos");
        string query = "", prod_clave = "", folio = "", tipo = "", cadena = "", prod_nombre = "";
        int tarima = 0, caja = 0, tarimaf = 0;
        bool find = false;
        ArrayAdapter<System.String> comboAdapter;
        System.String[] strFrutas;

        int Desactivarhabilitarreimprimir = 0;
        int FolioCampo = 0;

        public static string imei = "";


        DataTable CatProd = new DataTable();

        //Declarar los datos de los items en el layout CapturarSplit
        EditText foliocaptura;
        TextView total;
        TextView pedidoencaptura;
        Button Guardar;

        TextView nosplit;

        Int32 TotCaj;

        CheckBox Eliminar_caja;

        string valorfinal = "";
        EditText et;

        //Datos supervisor
        EditText supervisor;
        EditText passwordsupervisor;

        //Radio button
        RadioButton etiblanca;
        RadioButton etiverde;
        private Database db;
        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        string SerialShippingContainerCode = "0000796631";
        string patron = @"^00007966310*([1-9]\d*).$";

        DataTable Foliosleidos = new DataTable();
        DataTable FoliosleidosPresplit = new DataTable();

        int total_caja_verde = 0;
        private readonly ScannerInputHandler _scannerHandler = new ScannerInputHandler();
        #endregion
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CapturarSplit);

            await InitializeDatabaseAsync();

            InitializeUIElements();
            GetIntentExtras();
            imei = getDeviceID();

            Guardar.Click += BtnGuardar_Click;
            Guardar.Enabled = false;

            TotCaj = 0;
            muser = SolicitarPed.responsable;
            cvecam = SolicitarPed.cvvehiculo;

            try
            {
                await LoadDataAsync();
                await UpdateUI();
            }
            catch (Java.Lang.Exception ex)
            {
                Toast.MakeText(this, "Error en la inicialización: " + ex.Message, ToastLength.Short).Show();
            }

            SetupInitialView();
            foliocaptura.AddTextChangedListener(this);
        }

        private void InitializeUIElements()
        {
            foliocaptura = FindViewById<EditText>(Resource.Id.Folio);
            total = FindViewById<TextView>(Resource.Id.totalcapturado);
            pedidoencaptura = FindViewById<TextView>(Resource.Id.pedidoencaptura);
            nosplit = FindViewById<TextView>(Resource.Id.splitcantidad);
            Guardar = FindViewById<Button>(Resource.Id.GuardarCapturado);
            etiblanca = FindViewById<RadioButton>(Resource.Id.radio_blanco);
            etiverde = FindViewById<RadioButton>(Resource.Id.radio_verde);
            Eliminar_caja = FindViewById<CheckBox>(Resource.Id.Elicaja);
        }

        private void GetIntentExtras()
        {
            cvresponsable = Intent.GetStringExtra("cvresponsable")?.Trim();
            responsable = Intent.GetStringExtra("responsable")?.Trim();
            currentVersionName = Intent.GetStringExtra("currentVersionName")?.Trim();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                using (var thisConnection = new SqlConnection(MainActivity.cadenaConexion))
                {
                    await thisConnection.OpenAsync();

                    await LoadCatalogoProductosAsync(thisConnection);
                    await LoadNumeroSplitAsync();
                    await LoadConfiguracionAsync(thisConnection);
                }
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejar la excepción adecuadamente
                Toast.MakeText(this, "Error al cargar los datos: " + ex.Message, ToastLength.Short).Show();
            }
        }

        private async Task LoadCatalogoProductosAsync(SqlConnection connection)
        {
            string query = "SELECT prod_clave, prod_nombre FROM vwCatalogoProductos ORDER BY LEN(prod_clave) DESC";
            using (var command = new SqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                var dt = new DataTable();
                dt.Load(reader);
                CatProd = dt;
            }
        }

        private async Task LoadNumeroSplitAsync()
        {
            var pedidos = await db.GetItemsAsync<Pedidos>();
            var pedido = pedidos.LastOrDefault(); // Obtener el último pedido en la lista

            if (pedido != null)
            {
                nosplit.Text = $"Captura Pre-Split Numero: {NoSplit(pedido.folio.ToString())}";
                pedidoencaptura.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }

        private async Task LoadConfiguracionAsync(SqlConnection connection)
        {
            string query = @"
        SELECT 
            (SELECT sts_reetiquetado FROM Tb_Reetiquetadohabilitar) AS sts_reetiquetado,
            (SELECT inicio_campo FROM Tb_folio_campo) AS inicio_campo";

            using (var command = new SqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    Desactivarhabilitarreimprimir = reader.GetInt32(reader.GetOrdinal("sts_reetiquetado"));
                    FolioCampo = reader.GetInt32(reader.GetOrdinal("inicio_campo"));
                }
            }
        }

        private async Task UpdateUI()
        {
            try
            {
                // Obtener los datos de manera asincrónica
                List<FlimStarInfo> lstFlimStar = await ProductoCapturadoAsync();

                // Actualizar la interfaz de usuario
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);

                // Asignar el adaptador
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                //gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

                //// Si no se ha asignado previamente, asignar el evento ItemClick
                if (gvObject.Adapter == null)
                {
                    gvObject.ItemClick += OnGridView_ItemClicked;
                }


            }
            catch (Java.Lang.Exception ex)
            {
                // Manejo de excepciones
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Error al actualizar la interfaz de usuario: " + ex.Message, ToastLength.Short).Show();
                });
            }
        }

        private void SetupInitialView()
        {
            // Actualizar el texto del total
            total.Text = TotCaj.ToString("##0");

            // Solicitar el enfoque y mostrar el teclado solo si foliocaptura no tiene el enfoque actualmente
            if (!foliocaptura.HasFocus)
            {
                foliocaptura.RequestFocus();
                InputMethodManager immL = (InputMethodManager)GetSystemService(Context.InputMethodService);
                immL.ShowSoftInput(foliocaptura, ShowFlags.Implicit);
            }

            // Añadir el TextChangedListener solo si no ha sido añadido previamente

        }

        private async Task InitializeDatabaseAsync()
        {
            db = Database.Instance;
            bool dbExists = await db.DatabaseExistsAsync();

            if (!dbExists)
            {
                await db.InitializeDatabaseAsync();
            }
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            // Pass the event to the edit text to have the blinking cursor.
            v.OnTouchEvent(e);
            // Hide the input.
            var imm = ((InputMethodManager)v.Context.GetSystemService(Context.InputMethodService));
            imm?.HideSoftInputFromWindow(v.WindowToken, HideSoftInputFlags.None);
            return true;
        }

        //private void BtnGuardar_Click(object sender, EventArgs e)
        //{
        //    Guardar.Enabled = false;

        //    var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Guardando Pre-Split", true);


        //    new System.Threading.Thread(new ThreadStart(async delegate
        //    {
        //        await db.QueryAsync<xLoteFinal>("delete from  [xLoteFinal]");
        //        await db.QueryAsync<Pedidos>("UPDATE [Pedidos] SET surtido = '0'");
        //        thisConnection.Open();
        //        string mped = pedidoencaptura.Text.ToString().Trim();
        //        mped = mped.Replace("Pedido Actual: ", "");
        //        //Actualizacion de pedido leido por cada producto
        //        string mpedido = mped;
        //        //Actualizacion de pedido leido por cada producto

        //        await db.QueryAsync<xLote>("UPDATE [xLote] SET Pedido = '" + mpedido + "'");


        //        //validar datos habilitados 
        //        Java.IO.File sdCard = Android.OS.Environment.ExternalStorageDirectory; Java.IO.File dir = new Java.IO.File(sdCard.AbsolutePath + "/MyFolder"); dir.Mkdirs();
        //        Java.IO.File file = new Java.IO.File(dir, "habilitar.txt");
        //        string FileToRead = file.ToString();
        //        if (file.Exists())
        //        {
        //            string[] lines = System.IO.File.ReadAllLines(FileToRead);
        //            for (int i = 0; i < lines.Length; i++)
        //            {
        //                string cadenahabilitar = lines[i];
        //                SqlCommand cmdhabilitar = new SqlCommand(cadenahabilitar, thisConnection);
        //                cmdhabilitar.ExecuteNonQuery();
        //            }
        //            file.Delete();
        //        }

        //        var productoscapturados = await db.GetItemsAsync<xLote>();
        //        foreach (var captu in productoscapturados)
        //        {
        //            string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mdia = "", mmes = "", mfeccap = "";

        //            mtip = captu.Tipo.ToString().Trim();
        //            mfol = captu.Folio.ToString().Trim();
        //            mcod = captu.Codigo.ToString().Trim();
        //            mtar = captu.Tarima.ToString().Trim();
        //            mcaj = captu.Cajas.ToString().Trim();
        //            mdia = captu.diacad.ToString().Trim();
        //            mmes = captu.mescad.ToString().Trim();
        //            mfeccap = captu.fecha_captura.ToString().Trim();
        //            string lectura = mtip + mfol + mcod + mtar + mcaj;
        //            string nom = getNombreProducto(mcod);

        //            string cadena = "IF NOT EXISTS(SELECT Eti_Lectura FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = '" + mfol + "' AND  Eti_Producto  = '" + mcod + "' AND Eti_Caja  = '" + mcaj + "' AND Eti_TarIni  = '" + mtar + "' AND Estatus = 'A')  insert into Tb_Det_Etiqueta_Presplit(fecha, fecha_cap, Eti_Lectura, Eti_Recibo, Eti_Producto, Eti_Caja, Eti_TarIni, Eti_TarFin, Cve_Camioneta, FecCap, Version, Imei, Split, Estatus, responsable) " +
        //                            "Values('" + System.DateTime.Now.ToString("dd/MM/yyyy") + "','" + mfeccap + "','" + lectura + "','" + mfol + "','" + mcod + "','" + mcaj + "','" + mtar + "','" + mtar + "','" +
        //                            cvecam + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','" + Version + "','" + imei + "', '" + nosplit.Text.Replace("Captura Pre-Split Numero: ", "") + "', 'A', '" + responsable + "')";
        //            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
        //            //cmd.ExecuteNonQuery();

        //        }


        //        thisConnection.Close();



        //        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
        //        alertDialog.SetTitle(Html.FromHtml("<font color='#DF0101' size = 10>Informacion Almacenada</font>"));
        //        alertDialog.SetIcon(Resource.Drawable.exito);
        //        alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Información Grabada Correctamente!!! </font>"));
        //        alertDialog.SetCancelable(false);
        //        alertDialog.SetNeutralButton("Ok", delegate
        //        {
        //            alertDialog.Dispose();
        //            db.QueryAsync<Pedidos>("delete from  [Pedidos]");
        //            db.QueryAsync<ConPedidos>("delete from  [ConPedidos]");
        //            db.QueryAsync<xLote>("delete from  [xLote]");
        //            db.QueryAsync<xLoteFinal>("delete from  [xLoteFinal]");
        //            db.QueryAsync<xprod>("delete from  [xprod]");



        //            Intent intent = new Intent(this, typeof(SolicitarPed));
        //            intent.AddFlags(ActivityFlags.ClearTop);
        //            Intent.AddFlags(ActivityFlags.SingleTop);
        //            //intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
        //            intent.PutExtra("cvresponsable", cvresponsable.ToString());
        //            //intent.PutExtra("camioneta", vehiculo.ToString());
        //            intent.PutExtra("responsable", responsable.ToString());
        //            StartActivity(intent);
        //        });
        //        RunOnUiThread(() => alertDialog.Show());

        //        RunOnUiThread(() => Toast.MakeText(this, "Pre - Split Almacenado Correctamente.", ToastLength.Short).Show()); //HIDE PROGRESS DIALOG 
        //        RunOnUiThread(() => progressDialog.Hide());
        //    })).Start();

        //}

        private async void BtnGuarda_Click(object sender, EventArgs e)
        {
            Guardar.Enabled = false;

            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Guardando Pre-Split", true);

            try
            {
                // Eliminar datos previos
                await db.QueryAsync<xLoteFinal>("DELETE FROM [xLoteFinal]");
                await db.QueryAsync<Pedidos>("UPDATE [Pedidos] SET surtido = '0'");

                string mped = pedidoencaptura.Text.ToString().Trim().Replace("Pedido Actual: ", "");
                await db.QueryAsync<xLote>($"UPDATE [xLote] SET Pedido = '{mped}'");

                // Validar datos habilitados
                Java.IO.File sdCard = Android.OS.Environment.ExternalStorageDirectory;
                Java.IO.File dir = new Java.IO.File(sdCard.AbsolutePath + "/MyFolder");
                dir.Mkdirs();
                Java.IO.File file = new Java.IO.File(dir, "habilitar.txt");

                if (file.Exists())
                {
                    string[] lines = System.IO.File.ReadAllLines(file.ToString());
                    foreach (var cadenahabilitar in lines)
                    {
                        using (SqlCommand cmdhabilitar = new SqlCommand(cadenahabilitar, thisConnection))
                        {
                            cmdhabilitar.ExecuteNonQuery();
                        }
                    }
                    file.Delete();
                }
                //if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                var productoscapturados = await db.GetItemsAsync<xLote>();
                foreach (var captu in productoscapturados)
                {
                    string lectura = $"{captu.Tipo.Trim()}{captu.Folio.Trim()}{captu.Codigo.Trim()}{captu.Tarima.Trim()}{captu.Cajas.Trim()}";
                    string nom = getNombreProducto(captu.Codigo.Trim());

                    string cadena = $"IF NOT EXISTS(SELECT Eti_Lectura FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = '{captu.Folio.Trim()}' " +
                                    $"AND Eti_Producto = '{captu.Codigo.Trim()}' AND Eti_Caja = '{captu.Cajas.Trim()}' AND Eti_TarIni = '{captu.Tarima.Trim()}' AND Estatus = 'A') " +
                                    $"INSERT INTO Tb_Det_Etiqueta_Presplit(fecha, fecha_cap, Eti_Lectura, Eti_Recibo, Eti_Producto, Eti_Caja, Eti_TarIni, Eti_TarFin, Cve_Camioneta, FecCap, Version, Imei, Split, Estatus, responsable) " +
                                    $"VALUES('{DateTime.Now:dd/MM/yyyy}', '{captu.fecha_captura.Trim()}', '{lectura}', '{captu.Folio.Trim()}', '{captu.Codigo.Trim()}', '{captu.Cajas.Trim()}', '{captu.Tarima.Trim()}', " +
                                    $"'{captu.Tarima.Trim()}', '{cvecam}', '{DateTime.Now:dd/MM/yyyy HH:mm:ss}', '{currentVersionName}', '{imei}', '{nosplit.Text.Replace("Captura Pre-Split Numero: ", "")}', 'A', '{responsable}')";

                    using (SqlCommand cmd = new SqlCommand(cadena, thisConnection))
                    {
                        cmd.ExecuteNonQuery(); // Descomenta si deseas ejecutar la consulta
                    }
                }
                //if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }

                // Crear y mostrar mensaje de confirmación
                Android.App.AlertDialog.Builder alertDialogBuilder = new Android.App.AlertDialog.Builder(this);
                alertDialogBuilder.SetTitle(Html.FromHtml("<font color='#DF0101' size='10'>Informacion Almacenada</font>"))
                    .SetIcon(Resource.Drawable.exito)
                    .SetMessage(Html.FromHtml("<font color='#FFFFFF' size='10'>Información Grabada Correctamente!!!</font>"))
                    .SetCancelable(false)
                    .SetNeutralButton("Ok", delegate
                    {
                        // Código para eliminar registros y navegar a otra actividad
                        _ = db.QueryAsync<Pedidos>("DELETE FROM [Pedidos]");
                        _ = db.QueryAsync<ConPedidos>("DELETE FROM [ConPedidos]");
                        _ = db.QueryAsync<xLote>("DELETE FROM [xLote]");
                        _ = db.QueryAsync<xLoteFinal>("DELETE FROM [xLoteFinal]");
                        _ = db.QueryAsync<xprod>("DELETE FROM [xprod]");

                        Intent intent = new Intent(this, typeof(SolicitarPed));
                        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                        intent.PutExtra("cvresponsable", cvresponsable.ToString());
                        intent.PutExtra("responsable", responsable.ToString());
                        StartActivity(intent);
                    });

                RunOnUiThread(() =>
                {
                    var alertDialog = alertDialogBuilder.Create();
                    alertDialog.Show();
                    Toast.MakeText(this, "Pre-Split Almacenado Correctamente.", ToastLength.Short).Show();
                });
            }
            finally
            {
                RunOnUiThread(() => progressDialog.Hide());
            }
        }

        private async void BtnGuardar_Click(object sender, EventArgs e)
        {
            Guardar.Enabled = false;

            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Guardando Pre-Split", true);

            try
            {
                // Eliminar datos previos
                await db.ExecuteAsync("DELETE FROM [xLoteFinal]");
                await db.ExecuteAsync("UPDATE [Pedidos] SET surtido = '0'");

                string mped = pedidoencaptura.Text.ToString().Trim().Replace("Pedido Actual: ", "");
                await db.ExecuteAsync($"UPDATE [xLote] SET Pedido = '{mped}'");

                // Validar datos habilitados
                Java.IO.File sdCard = Android.OS.Environment.ExternalStorageDirectory;
                Java.IO.File dir = new Java.IO.File(sdCard.AbsolutePath + "/MyFolder");
                dir.Mkdirs();
                Java.IO.File file = new Java.IO.File(dir, "habilitar.txt");

                if (file.Exists())
                {
                    string[] lines = System.IO.File.ReadAllLines(file.ToString());
                    using (var thisConnection = new SqlConnection(MainActivity.cadenaConexion))
                    {
                        await thisConnection.OpenAsync();
                        foreach (var cadenahabilitar in lines)
                        {
                            using (SqlCommand cmdhabilitar = new SqlCommand(cadenahabilitar, thisConnection))
                            {
                                await cmdhabilitar.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    file.Delete();
                }
                #region INSERT INTO
                var productoscapturados = await db.GetItemsAsync<xLote>();
                using (var thisConnection = new SqlConnection(MainActivity.cadenaConexion))
                {
                    await thisConnection.OpenAsync();
                    foreach (var captu in productoscapturados)
                    {
                        string lectura = $"{captu.Tipo.Trim()}{captu.Folio.Trim()}{captu.Codigo.Trim()}{captu.Tarima.Trim()}{captu.Cajas.Trim()}";
                        string nom = getNombreProducto(captu.Codigo.Trim());

                        string cadena = $"IF NOT EXISTS(SELECT Eti_Lectura FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = '{captu.Folio.Trim()}' " +
                                        $"AND Eti_Producto = '{captu.Codigo.Trim()}' AND Eti_Caja = '{captu.Cajas.Trim()}' AND Eti_TarIni = '{captu.Tarima.Trim()}' AND Estatus = 'A') " +
                                        $"INSERT INTO Tb_Det_Etiqueta_Presplit(fecha, fecha_cap, Eti_Lectura, Eti_Recibo, Eti_Producto, Eti_Caja, Eti_TarIni, Eti_TarFin, Cve_Camioneta, FecCap, Version, Imei, Split, Estatus, responsable) " +
                                        $"VALUES('{DateTime.Now:dd/MM/yyyy}', '{captu.fecha_captura.Trim()}', '{lectura}', '{captu.Folio.Trim()}', '{captu.Codigo.Trim()}', '{captu.Cajas.Trim()}', '{captu.Tarima.Trim()}', " +
                                        $"'{captu.Tarima.Trim()}', '{cvecam}', '{DateTime.Now:dd/MM/yyyy HH:mm:ss}', '{currentVersionName}', LEFT('{imei}', 15), '{nosplit.Text.Replace("Captura Pre-Split Numero: ", "")}', 'A', '{responsable}')";

                        using (SqlCommand cmd = new SqlCommand(cadena, thisConnection))
                        {
                            await cmd.ExecuteNonQueryAsync(); // Ejecuta la consulta de forma asincrónica
                        }

                        string hay = "N";
                        var lotesproducto = await db.QueryAsync<xLoteFinal>("SELECT * FROM [xLoteFinal] Where tipo = '" + captu.Tipo.Trim() + "' and Pedido = '" + mped + "' and Folio = '" + captu.Folio.Trim() + "' and Codigo = '" + captu.Codigo.Trim() + "' and Tarima = '" + captu.Tarima.Trim() + "'");
                        foreach (var lotesencontrado in lotesproducto)
                        {
                            await db.QueryAsync<xLoteFinal>("UPDATE [xLoteFinal] SET Cajas = '" + (Convert.ToInt32(lotesencontrado.Cajas) + 1) + "' WHERE Tipo = '" + captu.Tipo.Trim() + "' and Pedido = '" + mped + "' and Folio = '" + captu.Folio.Trim() + "' and Codigo = '" + captu.Codigo.Trim() + "' and Tarima = '" + captu.Tarima.Trim() + "'");
                            hay = "S";
                        }
                        if (hay == "N")
                        {
                            xLoteFinal LoteFinal = new xLoteFinal { Tipo = captu.Tipo.Trim(), Pedido = mped, Folio = captu.Folio.Trim(), Codigo = captu.Codigo.Trim(), Tarima = captu.Tarima.Trim(), Cajas = "1", nombre = nom, diacad = captu.diacad.Trim(), mescad = captu.mescad.Trim() };
                            //Registra en la base de datos SQLite
                            await db.InsertAsync(LoteFinal);
                        }

                    }
                }
                #endregion
                #region sp_InsertarEtiquetaPresplit
                /*var productoscapturados = await db.GetItemsAsync<xLote>();
                using (var thisConnection = new SqlConnection(MainActivity.cadenaConexion))
                {
                    await thisConnection.OpenAsync();
                    foreach (var captu in productoscapturados)
                    {
                        string lectura = $"{captu.Tipo.Trim()}{captu.Folio.Trim()}{captu.Codigo.Trim()}{captu.Tarima.Trim()}{captu.Cajas.Trim()}";
                        string nom = getNombreProducto(captu.Codigo.Trim());

                        // Preparamos los parámetros para el procedimiento almacenado
                        using (SqlCommand cmd = new SqlCommand("sp_InsertarEtiquetaPresplit", thisConnection))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            // Parámetros requeridos
                            cmd.Parameters.AddWithValue("@Eti_Recibo", captu.Folio.Trim().Substring(0, Java.Lang.Math.Min(captu.Folio.Trim().Length, 10)));
                            cmd.Parameters.AddWithValue("@Eti_Producto", captu.Codigo.Trim().Substring(0, Java.Lang.Math.Min(captu.Codigo.Trim().Length, 10)));
                            cmd.Parameters.AddWithValue("@Eti_Caja", Convert.ToInt32(captu.Cajas.Trim()));
                            cmd.Parameters.AddWithValue("@Eti_TarIni", Convert.ToInt32(captu.Tarima.Trim()));
                            cmd.Parameters.AddWithValue("@Version", currentVersionName.Substring(0, Java.Lang.Math.Min(currentVersionName.Length, 12)));
                            cmd.Parameters.AddWithValue("@imei", imei.Substring(0, Java.Lang.Math.Min(imei.Length, 15)));
                            cmd.Parameters.AddWithValue("@responsable", responsable.Substring(0, Java.Lang.Math.Min(responsable.Length, 70)));

                            // Parámetros opcionales
                            cmd.Parameters.AddWithValue("@Eti_Lectura", lectura.Substring(0, Java.Lang.Math.Min(lectura.Length, 30)));
                            // Intentamos parsear la fecha de captura, si no, usamos DBNull.Value
                            if (DateTime.TryParse(captu.fecha_captura.Trim(), out DateTime fechaCap))
                            {
                                cmd.Parameters.AddWithValue("@fecha_captura", fechaCap);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@fecha_captura", DBNull.Value);
                            }
                            //cmd.Parameters.AddWithValue("@Cve_Camioneta", cvecam.Substring(0, Java.Lang.Math.Min(cvecam.Length, 10)));
                            cmd.Parameters.AddWithValue("@SplitText", nosplit.Text.Replace("Captura Pre-Split Numero: ", ""));

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // El resto del código (actualización de xLoteFinal) se mantiene igual
                        string hay = "N";
                        var lotesproducto = await db.QueryAsync<xLoteFinal>("SELECT * FROM [xLoteFinal] Where tipo = '" + captu.Tipo.Trim() + "' and Pedido = '" + mped + "' and Folio = '" + captu.Folio.Trim() + "' and Codigo = '" + captu.Codigo.Trim() + "' and Tarima = '" + captu.Tarima.Trim() + "'");
                        foreach (var lotesencontrado in lotesproducto)
                        {
                            await db.QueryAsync<xLoteFinal>("UPDATE [xLoteFinal] SET Cajas = '" + (Convert.ToInt32(lotesencontrado.Cajas) + 1) + "' WHERE Tipo = '" + captu.Tipo.Trim() + "' and Pedido = '" + mped + "' and Folio = '" + captu.Folio.Trim() + "' and Codigo = '" + captu.Codigo.Trim() + "' and Tarima = '" + captu.Tarima.Trim() + "'");
                            hay = "S";
                        }
                        if (hay == "N")
                        {
                            xLoteFinal LoteFinal = new xLoteFinal { Tipo = captu.Tipo.Trim(), Pedido = mped, Folio = captu.Folio.Trim(), Codigo = captu.Codigo.Trim(), Tarima = captu.Tarima.Trim(), Cajas = "1", nombre = nom, diacad = captu.diacad.Trim(), mescad = captu.mescad.Trim() };
                            //Registra en la base de datos SQLite
                            await db.InsertAsync(LoteFinal);
                        }
                    }
                }*/
                #endregion


                await AgregaDetaEtiAdelantado();

                // Crear y mostrar mensaje de confirmación
                RunOnUiThread(() =>
                {
                    var alertDialogBuilder = new Android.App.AlertDialog.Builder(this);
                    alertDialogBuilder.SetTitle(Html.FromHtml("<font color='#DF0101' size='10'>Informacion Almacenada</font>"))
                        .SetIcon(Resource.Drawable.exito)
                        .SetMessage(Html.FromHtml("<font color='#FFFFFF' size='10'>Información Grabada Correctamente!!!</font>"))
                        .SetCancelable(false)
                        .SetNeutralButton("Ok", async delegate
                        {
                            // Código para eliminar registros y navegar a otra actividad
                            await db.ExecuteAsync("DELETE FROM [Pedidos]");
                            await db.ExecuteAsync("DELETE FROM [ConPedidos]");
                            await db.ExecuteAsync("DELETE FROM [xLote]");
                            await db.ExecuteAsync("DELETE FROM [xLoteFinal]");
                            await db.ExecuteAsync("DELETE FROM [xprod]");

                            Intent intent = new Intent(this, typeof(SolicitarPed));
                            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                            intent.PutExtra("cvresponsable", cvresponsable.ToString());
                            intent.PutExtra("responsable", responsable.ToString());
                            StartActivity(intent);
                        });

                    var alertDialog = alertDialogBuilder.Create();
                    alertDialog.Show();
                    Toast.MakeText(this, "Pre-Split Almacenado Correctamente.", ToastLength.Short).Show();
                    Guardar.Enabled = false; // Rehabilitar el botón al finalizar
                });
            }
            catch (Java.Lang.Exception ex)
            {
                Toast.MakeText(this, $"Error al guardar: {ex.Message}", ToastLength.Short).Show();
            }
            finally
            {
                RunOnUiThread(() => progressDialog.Dismiss());
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menu_captura, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {

            switch (item.ItemId)
            {
                case Resource.Id.menu_edit:
                    _ = LimpiarInformacionAsync();
                    return true;
                case Resource.Id.menu_save:
                    _ = ValidarInformacionAsync();
                    return true;
                case Resource.Id.menu_preferences:
                    _ = HandleConcentradoModeAsync();
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        #region NEW FUNCTIONS FOR MENU
        private async Task LimpiarInformacionAsync()
        {
            // Crear y mostrar el diálogo con ProgressBar
            var progressBar = new ProgressBar(this) { Indeterminate = true };
            var builder = new AlertDialog.Builder(this)
                          .SetTitle("Espere Por Favor...")
                          .SetIcon(Resource.Drawable.Info)
                          .SetView(progressBar)
                          .SetCancelable(false)
                          .SetMessage("Limpiando Información...");
            var progressDialog = builder.Create();
            progressDialog.Show();

            // Actualizar la interfaz de usuario antes de comenzar las operaciones
            total.Text = "000";
            TotCaj = 0;
            foliocaptura.Text = "";
            Guardar.Enabled = false;
            foliocaptura.RequestFocus();

            try
            {
                // Ejecutar las operaciones de limpieza de manera asincrónica y en paralelo
                await Task.WhenAll(
                    LimpiarBaseDeDatosAsync(),
                    ProcesarPedidosAsync()
                );

                // Limpiar y actualizar la lista de productos
                await ActualizarGridViewAsync();

                mconcen = "1";

                // Mostrar mensaje en la interfaz de usuario
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "La información ha sido limpiada", ToastLength.Short).Show();
                });
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejo de excepciones general
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show();
                });
            }
            finally
            {
                // Ocultar el ProgressDialog
                RunOnUiThread(() => progressDialog.Dismiss());
            }
        }

        // Método para limpiar la base de datos
        private async Task LimpiarBaseDeDatosAsync()
        {
            try
            {
                await Task.WhenAll(
                    db.ExecuteAsync("DELETE FROM [xLote]"),
                    db.ExecuteAsync("DELETE FROM [xprod]"),
                    db.ExecuteAsync("UPDATE [Pedidos] SET surtido = '0'"),
                    db.ExecuteAsync("UPDATE [ConPedidos] SET surtido = '0'"),
                    db.ExecuteAsync("DELETE FROM [ConPedidos] WHERE pedido = '0'")
                );
            }
            catch (Java.Lang.Exception ex)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Error al limpiar información: " + ex.Message, ToastLength.Short).Show();
                });
                throw; // Rethrow the exception to be caught in the main method
            }
        }
        // Método para procesar los pedidos
        private async Task ProcesarPedidosAsync()
        {
            try
            {
                await ConsPedSurAsync();
                //var pedidos = await db.GetItemsAsync<Pedidos>();
                //foreach (var captu in pedidos)
                //{
                //    ConsPedSur(captu.folio.ToString());
                //}
            }
            catch (Java.Lang.Exception ex)
            {
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Error al procesar pedidos: " + ex.Message, ToastLength.Short).Show();
                });
                throw; // Rethrow the exception to be caught in the main method
            }
        }

        public async Task ConsPedSurAsync()
        {
            // Crear y mostrar el ProgressDialog
            //var progressDialog = new ProgressDialog(this);
            //progressDialog.Indeterminate = true;
            //progressDialog.SetCancelable(false);
            //progressDialog.SetMessage("Cargando datos, por favor espere...");
            //progressDialog.Show();

            try
            {
                // Eliminar datos existentes de las tablas
                try
                {
                    await db.ExecuteAsync("DELETE FROM [Pedidos]");
                    await db.ExecuteAsync("DELETE FROM [ConPedidos]");
                }
                catch (Java.Lang.Exception ex)
                {
                    Toast.MakeText(this, "Error al eliminar datos: " + ex.Message, ToastLength.Short).Show();
                }

                // Obtener la fecha actual
                string fechaactual = "IF CAST(CONVERT(VARCHAR, SYSDATETIME(), 108) AS TIME) > '04:00:00' SELECT GETDATE(); ELSE SELECT DATEADD(day, -1, GETDATE());";
                DateTime fechahoyserv;
                string fechahoy;
                int cantped = 0;
                int cantsur = 0;

                // Abrir conexión SQL y ejecutar consultas
                using (var thisConnection = new SqlConnection(MainActivity.cadenaConexion))
                {
                    await thisConnection.OpenAsync();

                    try
                    {
                        using (var cmd = thisConnection.CreateCommand())
                        {
                            cmd.CommandText = fechaactual;
                            var result = await cmd.ExecuteScalarAsync();
                            fechahoyserv = Convert.ToDateTime(result);
                            fechahoy = fechahoyserv.ToString("dd/MM/yyyy");
                        }

                        // Determinar la condición para la consulta SQL
                        DateTime dt = fechahoyserv;
                        string condicion = "";
                        string fechamañana = fechahoyserv.AddDays(1).ToString("dd/MM/yyyy");

                        if (dt.DayOfWeek == DayOfWeek.Sunday)
                        {
                            string diahoy = fechahoyserv.Day.ToString();
                            if (diahoy == "01")
                            {
                                condicion = $" (a.pdn_fecha = '{fechahoy.Trim()}' ";
                                fechahoy = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
                                condicion += $" OR a.pdn_fecha = '{fechahoy.Trim()}') ";
                            }
                            else
                            {
                                fechahoy = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
                                condicion = $" a.pdn_fecha = '{fechahoy.Trim()}' ";
                            }
                        }
                        else
                        {
                            condicion = $" a.pdn_fecha = '{fechahoy.Trim()}' ";
                        }

                        // Consulta SQL para obtener los datos
                        string consulta = $"SELECT b.prod_clave as Clave, c.prod_nombre as Nombre, SUM(b.pdn_num_unidades) as Cajas, " +
                                          $"(SELECT Count(Fecha) as cajas FROM Tb_Det_Etiqueta_Presplit WHERE fecha_cap > '{fechahoy} 09:00:00' " +
                                          $"AND fecha_cap < '{fechamañana} 04:00:00' AND Eti_Producto = b.prod_clave AND Estatus != 'C') AS Surtido " +
                                          $"FROM tb_mstr_pedidos_nal a, tb_det_pedidos b, tb_cat_producto c WHERE {condicion} " +
                                          $"AND a.prov_clave = 'MRLUCKY' AND a.pdn_folio = b.pdn_folio AND a.pdn_tipo = b.pdn_tipo AND b.prod_clave = c.prod_clave " +
                                          $"AND a.pdn_estatus != 'C' GROUP BY b.prod_clave, c.prod_nombre ORDER BY c.prod_nombre ASC";

                        using (var cmd = new SqlCommand(consulta, thisConnection))
                        {
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    //int surtido = 0;
                                    string mcod = reader["Clave"].ToString().Trim();
                                    string nombre = reader["Nombre"].ToString().Trim();
                                    int totalx = await TraeTotalAsync(mcod);
                                    int pedido = Convert.ToInt32(reader["Cajas"].ToString().Replace(".000", ""));
                                    int surtido = Convert.ToInt32(reader["Surtido"].ToString()) + totalx;

                                    // Insertar registros en la base de datos SQLite
                                    var productosolicitado = new Pedidos { folio = "0", prod_clave = mcod, nombre = nombre, pedido = pedido, surtido = surtido };
                                    await db.InsertAsync(productosolicitado);

                                    var consecutivo = new ConPedidos { prod_clave = mcod, nombre = nombre, pedido = pedido, surtido = surtido };
                                    await db.InsertAsync(consecutivo);




                                    cantped += pedido;
                                    cantsur += surtido;
                                }
                            }
                        }
                    }
                    catch (Java.Lang.Exception ex)
                    {
                        Toast.MakeText(this, "Error general: " + ex.Message, ToastLength.Short).Show();
                    }
                }

                // Actualizar datos existentes en la tabla xprod
                var quex = await db.GetItemsAsync<xprod>();
                foreach (var captu in quex)
                {
                    try
                    {
                        await db.ExecuteAsync("UPDATE [Pedidos] SET surtido = surtido + 1 WHERE prod_clave = ?", captu.Codigo.Trim());
                        await db.ExecuteAsync("UPDATE [ConPedidos] SET surtido = surtido + 1 WHERE prod_clave = ?", captu.Codigo.Trim());
                        cantsur += 1;
                    }
                    catch (Java.Lang.Exception ex)
                    {
                        Toast.MakeText(this, "Error al actualizar datos: " + ex.Message, ToastLength.Short).Show();
                    }
                }

                //// Actualizar la interfaz de usuario
                //RunOnUiThread(async () =>
                //{
                //    try
                //    {
                //        PedidosSurtidos.Text = "Pedidos: " + cantped + " Surtidos: " + cantsur;
                //        List<FlimStarInfo> lstFlimStar = await detalle_pedidoAsync();
                //        var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                //        gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                //        gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
                //    }
                //    catch (Exception ex)
                //    {
                //        Toast.MakeText(this, "Error al actualizar la interfaz de usuario: " + ex.Message, ToastLength.Short).Show();
                //    }
                //});
            }
            catch (Java.Lang.Exception ex)
            {
                Toast.MakeText(this, "Error en la carga de datos. Verifica los registros.", ToastLength.Short).Show();
            }
            finally
            {
                // Cerrar el ProgressDialog cuando todo haya terminado
                //progressDialog.Dismiss();
            }
        }

        // Método para actualizar el GridView
        private async Task ActualizarGridViewAsync()
        {
            try
            {
                // FIX: NO llamar Clear() después de DetallePedidoAsync.
                // DetallePedidoAsync ya hace listItem.Clear() internamente antes de llenar.
                List<FlimStarInfo> lstFlimStar = await DetallePedidoAsync();

                RunOnUiThread(() =>
                {
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                    gvObject.ItemClick -= OnGridView_ItemClicked;
                    gvObject.ItemClick += OnGridView_ItemClicked;
                    // El adapter hace copia defensiva → safe para pasar lstFlimStar directo
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                });
            }
            catch (Java.Lang.Exception ex)
            {
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Error al actualizar vista: " + ex.Message, ToastLength.Short).Show());
                throw;
            }
        }
        private async Task ValidarInformacionAsync1()
        {
            Java.IO.File file = PrepararArchivoLocal();
            var wakeLock = ObtenerWakeLock();

            using (var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Validando Información Capturada...", true))
            {
                try
                {
                    // Verificar si hay productos capturados
                    if (!await ExistenProductosCapturadosAsync())
                    {
                        MostrarMensajeSinProductos();
                        return;
                    }

                    // Limpiar mensajes de manera asincrónica si es necesario
                    await LimpiarMensajesAsync(); // Asegúrate de que este método sea asincrónico

                    AutoPed = "N";
                    RunOnUiThread(() => Guardar.Enabled = false);

                    // Verificar estado de preautorizado en paralelo si es necesario
                    string preautorizadoSplit = "NO";
                    if (Desactivarhabilitarreimprimir > 0)
                    {
                        //preautorizadoSplit = await ValidarPreautorizadoAsync();
                        preautorizadoSplit = await validapreautorizado();
                    }

                    // Validar y actualizar productos basándose en el estado de preautorizado
                    if (preautorizadoSplit == "NO")
                    {
                        await ValidarYActualizarProductosAsync();
                    }
                    else
                    {
                        ManejarPreautorizadoNoValido();
                    }
                }
                catch (Java.Lang.Exception ex)
                {
                    RunOnUiThread(() =>
                        Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show()
                    );
                }
                finally
                {
                    // Asegurarse de ocultar el ProgressDialog y liberar el WakeLock
                    RunOnUiThread(() => progressDialog.Hide());
                    if (wakeLock.IsHeld)
                    {
                        wakeLock.Release();
                    }
                }
            }
        }

        private async Task ValidarInformacionAsync()
        {
            Java.IO.File file = PrepararArchivoLocal();
            var wakeLock = ObtenerWakeLock();

            // Crear y mostrar el diálogo con ProgressBar
            var progressBar = new ProgressBar(this) { Indeterminate = true };
            var builder = new AlertDialog.Builder(this)
                          .SetTitle("Espere Por Favor...")
                          .SetIcon(Resource.Drawable.Info)
                          .SetView(progressBar)
                          .SetCancelable(false)
                          .SetMessage("Validando Información Capturada...");
            var progressDialog = builder.Create();
            progressDialog.Show();

            try
            {
                // Verificar si hay productos capturados
                if (!await ExistenProductosCapturadosAsync())
                {
                    MostrarMensajeSinProductos();
                    return;
                }

                // Limpiar mensajes de manera asincrónica si es necesario
                await LimpiarMensajesAsync(); // Asegúrate de que este método sea asincrónico

                AutoPed = "N";
                RunOnUiThread(() => Guardar.Enabled = false);

                // Verificar estado de preautorizado en paralelo si es necesario
                string preautorizadoSplit = "NO";
                if (Desactivarhabilitarreimprimir > 0)
                {
                    //preautorizadoSplit = await ValidarPreautorizadoAsync();
                    preautorizadoSplit = await validapreautorizado();
                }

                // Validar y actualizar productos basándose en el estado de preautorizado
                if (preautorizadoSplit == "NO")
                {
                    await ValidarYActualizarProductosAsync();
                }
                else
                {
                    ManejarPreautorizadoNoValido();
                }
            }
            catch (Java.Lang.Exception ex)
            {
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show()
                );
            }
            finally
            {
                // Asegurarse de ocultar el ProgressDialog y liberar el WakeLock
                RunOnUiThread(() => progressDialog.Dismiss());
                if (wakeLock.IsHeld)
                {
                    wakeLock.Release();
                }
            }
        }

        private async Task HandleConcentradoModeAsync1()
        {
            mconcen = "2";
            List<FlimStarInfo> lstFlimStar = await DetallePedidoAsync();

            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido
            Toast.MakeText(this, "Modo Concentrado Activado", ToastLength.Short).Show();
        }

        private async Task HandleConcentradoModeAsync()
        {
            // Crear y mostrar el diálogo con ProgressBar
            var progressBar = new ProgressBar(this) { Indeterminate = true };
            var builder = new AlertDialog.Builder(this)
                          .SetTitle("Espere Por Favor...")
                          .SetIcon(Resource.Drawable.Info)
                          .SetView(progressBar)
                          .SetCancelable(false)
                          .SetMessage("Activando Modo Concentrado...");
            var progressDialog = builder.Create();
            progressDialog.Show();

            try
            {
                mconcen = "2";

                // Obtener la lista de detalles del pedido de manera asíncrona
                List<FlimStarInfo> lstFlimStar = await DetallePedidoAsync();

                // Encontrar el GridView y asignar el adaptador
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);

                // Asignar el evento de clic para los ítems del GridView
                gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

                // Mostrar un mensaje indicando que el modo concentrado está activado
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Modo Concentrado Activado", ToastLength.Short).Show()
                );
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejo de excepciones: Mostrar un mensaje de error en caso de que ocurra alguna excepción
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show()
                );
            }
            finally
            {
                // Ocultar el ProgressDialog
                RunOnUiThread(() => progressDialog.Dismiss());
            }
        }

        #region NEW FUNCTIONS FOR VALIDARINFORMACION ASYNC
        public async Task<string> ValidarPreautorizadoAsync()
        {
            string okidoki = "NO";
            string cadena;
            int totalDisponibles = 0;
            int totalProdSimula;

            // Obtiene productos capturados
            var productosCapturados = await db.QueryAsync<xprod>("SELECT Codigo, Tipo FROM xprod GROUP BY Codigo, Tipo");

            foreach (var captu in productosCapturados)
            {
                string tipoProd = captu.Tipo.ToString().Trim();
                string producto = captu.Codigo.ToString().Trim();

                // Define la consulta SQL dependiendo del tipo de producto
                cadena = tipoProd == "PTP"
                    ? @"SELECT (num_cajas - cajas_sur) - (SELECT COUNT(Fecha) FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = tb_det_eti_final.folio AND Eti_Producto = tb_det_eti_final.cve_prod AND Eti_TarIni = tb_det_eti_final.tarima AND Estatus = 'A') AS disponible, folio AS recibo, tarima
               FROM tb_det_eti_final WHERE preautorizado = 'C' AND cve_prod = @producto AND estatus_sur <> 'S' AND (num_cajas - cajas_sur) - (SELECT COUNT(Fecha) FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = tb_det_eti_final.folio AND Eti_Producto = tb_det_eti_final.cve_prod AND Eti_TarIni = tb_det_eti_final.tarima AND Estatus = 'A') > 0
               ORDER BY folio"
                    : @"SELECT (etiqueta - surtido) - (SELECT COUNT(Fecha) FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = recibo AND Eti_Producto = prod_clave AND Eti_TarIni = tarima AND Estatus = 'A') AS disponible, recibo, tarima
               FROM TB_DET_TRAZABILIDAD WHERE preautorizado = 'C' AND PROD_CLAVE = @producto AND pti_estatus_sur = '' AND tipo = 'PTC' AND (etiqueta - surtido) - (SELECT COUNT(Fecha) FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = recibo AND Eti_Producto = prod_clave AND Eti_TarIni = tarima AND Estatus = 'A') > 0
               ORDER BY recibo";

                // Ejecuta la consulta
                var lotesPreautorizados = new List<(int disponible, string recibo, int tarima)>();
                using (var command = new SqlCommand(cadena, thisConnection))
                {
                    command.Parameters.AddWithValue("@producto", producto);

                    thisConnection.Open();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int disponible = reader.GetInt32(0);
                            string recibo = reader.GetString(1);
                            int tarima = reader.GetInt32(2);

                            lotesPreautorizados.Add((disponible, recibo, tarima));
                        }
                    }
                }

                // Obtiene el total de cajas capturadas
                totalProdSimula = await db.ExecuteScalarAsync<int>("SELECT COUNT(ID) FROM xLote WHERE Codigo = @producto", new { producto });

                // Procesa los resultados
                foreach (var (disponible, recibo, tarima) in lotesPreautorizados)
                {
                    if (totalProdSimula <= 0) break;

                    int usadas = await db.ExecuteScalarAsync<int>("SELECT COUNT(ID) FROM xLote WHERE Codigo = @producto AND Folio = @recibo AND CAST(Tarima AS int) = @tarima", new { producto, recibo, tarima });

                    int totalDis = disponible - usadas;
                    totalProdSimula -= usadas;

                    if (totalDis > 0)
                    {
                        totalDisponibles += totalDis;
                    }
                }

                // Actualiza el resultado final
                if (totalDisponibles > 0 && totalProdSimula > 0)
                {
                    okidoki = "SI";
                    var mensaje = new Mensajes
                    {
                        titulo = "FOLIOS PREAUTORIZADOS DISPONIBLES",
                        mensaje = $"Se han encontrado Folios Preautorizados Disponibles Para el producto {producto}, Favor de capturar primero estos folios antes de usar algún otro"
                    };
                    await db.InsertAsync(mensaje);
                }
            }

            return okidoki;
        }

        private Java.IO.File PrepararArchivoLocal()
        {
            Java.IO.File sdCard = Android.OS.Environment.ExternalStorageDirectory;
            Java.IO.File dir = new Java.IO.File(sdCard.AbsolutePath + "/MyFolder");
            dir.Mkdirs();
            Java.IO.File file = new Java.IO.File(dir, "habilitar.txt");

            if (file.Exists())
            {
                file.Delete();
            }

            return file;
        }
        private PowerManager.WakeLock ObtenerWakeLock()
        {
            Context context = this;
            var pm = PowerManager.FromContext(context);
            var wakeLock = pm.NewWakeLock(WakeLockFlags.Full, "Validar");
            wakeLock.Acquire();
            return wakeLock;
        }
        private async Task<bool> ExistenProductosCapturadosAsync()
        {
            var existeCapturado = await db.GetItemsAsync<xprod>();
            return existeCapturado.Any();
        }
        private async Task LimpiarMensajesAsync()
        {
            try
            {
                // Ejecutar la operación de eliminación de manera asincrónica
                await db.ExecuteAsync("DELETE FROM [Mensajes]");
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejo de excepciones con un mensaje claro al usuario
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Error al eliminar mensajes: " + ex.Message, ToastLength.Short).Show()
                );
            }
        }

        private async Task ValidarYActualizarProductosAsync()
        {
            // Ejecutar tareas en paralelo si son independientes
            var validaTask = validaAsync();
            await Task.WhenAll(validaTask);
            var validaprodTask = validaprodAsync();

            await Task.WhenAll(validaTask, validaprodTask);

            // Si Desactivarhabilitarreimprimir es 1, también se ejecuta validafecadMAXIMOSAsync
            Task validafecadTask = Desactivarhabilitarreimprimir == 1 ? validafecadMAXIMOSAsync() : Task.CompletedTask;

            // Esperar a que todas las tareas se completen
            await Task.WhenAll(validaTask, validaprodTask, validafecadTask);

            var validando = await validaTask;
            var producto = await validaprodTask;

            // Evaluar la lógica de actualización de UI y validaciones
            if (Surtidomayor == "NR" || EtiquetaCapturada == "N")
            {
                _ = ImprimirDialogsAsync(0);
                // Agrupar actualizaciones de UI en una sola llamada
                RunOnUiThread(() => Guardar.Enabled = false);
            }
            else
            {
                if (EtiquetaExiste == "S")
                {
                    await ProcesarValidacionEtiquetas(validando, producto);

                }
                else
                {
                    _ = ImprimirDialogsAsync(0);
                }
            }
        }

        private async Task ProcesarValidacionEtiquetas(string validando, string producto)
        {
            //var xLotes = await db.GetItemsAsync<xLote>();

            if (producto == "S" && validando == "S")
            {
                RunOnUiThread(() => Guardar.Enabled = true);
                if (ValiFechacad == "N" && Desactivarhabilitarreimprimir == 1)
                {
                    RunOnUiThread(() => Guardar.Enabled = false);
                    SolicitarAutorizacionFoliosAdelantados();
                    MostrarMensajeProcesoValidado();
                }
            }
            else
            {
                RunOnUiThread(() => Guardar.Enabled = false);
                //MostrarMensajeProcesoValidado();
            }

            // Solo permitimos guardar si NO todo está perfecto (alguna validación falló)
            // Y además estamos en caso especial: producto nuevo o sin stock
            if ((validando != "S" || producto != "S") && (producto == "N" || HayExistencias != "NE"))
            {
                RunOnUiThread(() => Guardar.Enabled = true);
                MostrarMensajeProcesoValidado();
            }

            await ImprimirDialogsAsync(0);
        }

        private void MostrarMensajeProcesoValidado()
        {
            RunOnUiThread(() =>
            {
                Toast.MakeText(this, "Proceso Validado correctamente.", ToastLength.Short).Show();
                //Guardar.Enabled = true;
            });
        }

        private void SolicitarAutorizacionFoliosAdelantados()
        {
            RunOnUiThread(() => Guardar.Enabled = false);

            et = new EditText(this)
            {
                InputType = Android.Text.InputTypes.TextVariationPassword | Android.Text.InputTypes.ClassText,
                LongClickable = false,
                Hint = "Password"
            };

            AlertDialog.Builder ad = new AlertDialog.Builder(this)
                .SetTitle("Autorizacion Folios Adelantados")
                .SetCancelable(false)
                .SetView(et)
                .SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Guardar</font>"), SaveName)
                .SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Cancelar</font>"), CancelAction);

            RunOnUiThread(() => ad.Show());
        }

        private void ManejarPreautorizadoNoValido()
        {
            _ = ImprimirDialogsAsync(0);
            RunOnUiThread(() => Guardar.Enabled = false);
        }

        private async void FinalizarProcesoValidacion(PowerManager.WakeLock wakeLock)
        {
            wakeLock.Release();
            //TotCaj = 0;
            List<FlimStarInfo> lstFlimStar = await ProductoCapturadoAsync();
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            RunOnUiThread(() => gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar));
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);

            RunOnUiThread(() => total.Text = TotCaj.ToString("##0"));
        }

        private void MostrarMensajeSinProductos()
        {
            RunOnUiThread(() =>
            {
                using (var alertDialog = new AlertDialog.Builder(this)
                    .SetTitle(Html.FromHtml("<font color='#dc3545' size='10'>Sin Productos Capturados</font>"))
                    .SetIcon(Resource.Drawable.no)
                    .SetMessage(Html.FromHtml("<font color='#FFFFFF' size='10'>No existen productos capturados para validar</font>"))
                    .SetCancelable(false)
                    .SetNeutralButton("Ok", (sender, args) => { })
                    .Create())
                {
                    alertDialog.Show();
                }
            });
        }

        #endregion

        #endregion

        private async Task ImprimirDialogsAsync(int mensaje)
        {
            var mensajes = await db.GetItemsAsync<Mensajes>();
            if (mensaje >= mensajes.Count) return;  // Asegúrate de no salir de los límites


            var mensajeActual = mensajes[mensaje];
            string color, textoColor;
            int drawableIcon;

            // Determinar propiedades del diálogo basado en el título del mensaje
            switch (mensajeActual.titulo.Trim())
            {
                case "Existe un folio anterior disponible":
                    color = "#ffc107";
                    textoColor = "#E0F1FA";
                    drawableIcon = Resource.Drawable.warning;
                    break;
                case "Etiqueta ya capturada":
                case "Etiqueta ya capturada En PreSplit":
                    color = "#0dcaf0";
                    textoColor = "#6edff6";
                    drawableIcon = Resource.Drawable.Info;
                    break;
                default:
                    color = "#dc3545";
                    textoColor = "#FFFFFF";
                    drawableIcon = Resource.Drawable.no;
                    break;
            }

            // Crear el diálogo
            var alertDialog = new Android.App.AlertDialog.Builder(this)
                .SetTitle(Html.FromHtml($"<font color='{color}' size = 10>{mensajeActual.titulo}</font>"))
                .SetIcon(drawableIcon)
                .SetMessage(Html.FromHtml($"<font color='{textoColor}' size = 10>{mensajeActual.mensaje}</font>"))
                .SetCancelable(false)
                .SetNeutralButton("Ok", (sender, args) =>
                {
                    // Llamar al siguiente diálogo después de que el actual se haya cerrado
                    Task.Run(() => ImprimirDialogsAsync(mensaje + 1));
                })
                .Create();

            // Mostrar el diálogo
            RunOnUiThread(() => alertDialog.Show());

        }

        public async Task<List<FlimStarInfo>> DetallePedidoAsync()
        {
            //var listItem = new List<FlimStarInfo>();
            listItem.Clear();

            var query = await db.GetItemsAsync<ConPedidos>(); // Suponiendo que tu clase Database tiene un método GetItemsAsync
            foreach (var captu in query)
            {
                listItem.Add(new FlimStarInfo()
                {
                    Name = captu.nombre,
                    Age = "Pedidos: " + captu.pedido + " Surtido: " + captu.surtido,
                    ImageID = Resource.Drawable.producto
                });
            }

            //mconcen = "2";

            return listItem;
        }

        private async Task<List<FlimStarInfo>> DetalleLoteAsync()
        {
            //var listItem = new List<FlimStarInfo>();
            listItem.Clear();

            try
            {
                // Abrir conexión de base de datos asincrónicamente
                await Task.Run(() => thisConnection.Open());

                // Obtener datos de la tabla 'xLote'
                var query = await db.GetItemsAsync<xLote>();

                // Procesar los datos obtenidos
                foreach (var captu in query)
                {
                    listItem.Add(new FlimStarInfo()
                    {
                        Name = captu.nombre,
                        Age = $"Folio: {captu.Folio} Tarima: {captu.Tarima} Caja: {captu.Cajas} Dia/Mes Caducidad: {captu.diacad}/{captu.mescad}",
                        ImageID = Resource.Drawable.producto
                    });
                }
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejo de excepciones
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show();
                });
            }
            finally
            {
                // Cerrar la conexión de base de datos
                await Task.Run(() => thisConnection.Close());
            }

            return listItem;
        }

        private async Task<List<FlimStarInfo>> DetalleSurtidoAsync()
        {
            //var listItem = new List<FlimStarInfo>();
            listItem.Clear();

            try
            {
                // Obtener datos de la tabla 'ConPedidos' de manera asincrónica
                var query = await db.GetItemsAsync<ConPedidos>();

                // Procesar los datos obtenidos
                foreach (var captu in query)
                {
                    listItem.Add(new FlimStarInfo()
                    {
                        Name = captu.nombre,
                        Age = $"Pedidos: {captu.pedido} Surtido: {captu.surtido}",
                        ImageID = Resource.Drawable.producto
                    });
                }
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejo de excepciones
                RunOnUiThread(() =>
                {
                    Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show();
                });
            }

            return listItem;
        }

        private async Task<List<FlimStarInfo>> ProductoCapturadoAsync()
        {
            listItem.Clear();
            // FIX: resetear TotCaj antes de contar
            TotCaj = 0;

            try
            {
                var query = await db.GetItemsAsync<xprod>();
                foreach (var captu in query)
                {
                    var nombreProducto = getNombreProducto(captu.Codigo.ToString().Trim()) ?? string.Empty;
                    listItem.Add(new FlimStarInfo()
                    {
                        Name = nombreProducto.Trim(),
                        Age = $"Recibo: {captu.Folio} Tarima: {captu.Tarima} Caja: {captu.Cajas}",
                        ImageID = Resource.Drawable.producto
                    });
                    TotCaj++;
                }
            }
            catch (Java.Lang.Exception ex)
            {
                RunOnUiThread(() =>
                    Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Short).Show());
            }
            return listItem;
        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

        }

        private string onTimeRealValidation()
        {
            return "a";
        }

        private async Task<string> validaAsync()
        {
            return await Task.Run(async () =>
            {
                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                DateTime horavalidacionleido = DateTime.Now;
                HayExistencias = "S";
                EtiquetaExiste = "S";
                EtiquetaCapturada = "S";
                EstructuraEtiqueta = "S";
                await db.QueryAsync<xLote>("delete from [xLote]");
                string ok = "S";
                int tot = 0, totok = 0;
                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                //Traer la horafecha de Validacion
                string consultahoramin = "Select CASE WHEN Convert(varchar(8),GetDate(), 108) < Convert(time, '5:00:00') THEN Convert(DATETIME, Concat(DATEPART(YEAR, dateadd(day, -1, GETDATE())), '-', DATEPART(DAY, dateadd(day, -1, GETDATE())), '-', DATEPART(MONTH, dateadd(day, -1, GETDATE())), ' 16:00:00')) Else Convert(DATETIME, Concat(DATEPART(YEAR, GETDATE()), '-', DATEPART(DAY, GETDATE()), '-', DATEPART(MONTH, GETDATE()), ' 16:00:00')) END AS InicioValidacion";
                SqlCommand cmdhoramin = new SqlCommand(consultahoramin, thisConnection);
                horavalidacionleido = Convert.ToDateTime(Convert.ToString(cmdhoramin.ExecuteScalar()));

                //Termina validacion de hora
                string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mfeccap = "";
                string amtip = "", amfol = "", amcod = "", amtar = "", amcaj = "", amfeccap = "";
                var conta = 0;
                var productoscapturados = await db.GetItemsAsync<xprod>();

                foreach (var captu in productoscapturados)
                {
                    string er = "";
                    mtip = captu.Tipo.ToString().Trim();
                    mfol = captu.Folio.ToString().Trim();
                    mcod = captu.Codigo.ToString().Trim();
                    mtar = captu.Tarima.ToString().Trim();
                    mcaj = captu.Cajas.ToString().Trim();
                    mfeccap = captu.fecha_captura.ToString().Trim();

                    string nom = getNombreProducto(captu.Codigo.ToString().Trim()).Trim();
                    string lectura = mtip + mfol + mcod + mtar + mcaj;
                    string fechacapone = ValidaCaja(lectura).Trim();
                    string[] fechas = fechacapone.Split('*');

                    string fechacap = Convert.ToString(fechas[0]);
                    string Embcap = Convert.ToString(fechas[1]);
                    string CamEmb = Convert.ToString(fechas[2]);
                    string fechacappre = "";
                    try
                    {
                        fechacappre = Convert.ToString(fechas[3]);
                    }
                    catch
                    {
                        fechacappre = "";
                    }

                    try
                    {
                        int vfol = Convert.ToInt32(mfol);
                        int vtar = Convert.ToInt32(mtar);
                        int vcaj = Convert.ToInt32(mcaj);
                    }
                    catch (System.Exception ex)
                    {
                        EstructuraEtiqueta = "N";
                        Mensajes mensa = new Mensajes { titulo = "Error en la estructura de Etiqueta", mensaje = "La etiqueta del Producto: [ " + mcod + " | " + nom + " | Recibo: " + mfol + " | Tarima: " + mtar + " | Caja: " + mcaj + " ] contiene un error en la tarima, recibo o folio, favor de informar al supervisor, validar la informacion, retirar y reetiquetar la caja y leer la nueva etiqueta" };
                        await db.InsertAsync(mensa);

                        //Borrado de Etiquetas capturadas
                        await db.QueryAsync<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                        await db.QueryAsync<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                        ok = "NO";
                    }

                    if (fechacap.Length > 0)
                    {
                        if (Desactivarhabilitarreimprimir == 0)
                        {
                            Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada", mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + "Embarque " + Embcap + "\n\r" + nom + "  Informe al Supervisor de Camionetas para Liberacion de Cajas" };
                            await db.InsertAsync(mensa);
                            //Borrado de Etiquetas capturadas
                            await db.QueryAsync<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                            await db.QueryAsync<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                            ok = "N";
                            er = "S";

                            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }

                            string reetiquetado = "insert into Tb_Det_Sol_Reetiquetado (Fecha, emb_folio, fecha_cap, Lectura, Recibo, Producto, Caja, TarIni, TarFin, Cve_Camioneta, Estatus, Obs, armador, autorizo, origen) values" +
                                " ('" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '',  GETDATE(), '" + lectura + "', '" + mfol + "', '" + mcod + "', '" + mcaj + "', '" + mtar + "', '" + mtar + "', '', 'A', 'SOLICITUD DE REIMPRESION POR ETIQUETA YA LEIDA CAMIONETAS', 'PRESPLIT', '', 'EMB')";
                            SqlCommand cmd = new SqlCommand(reetiquetado, thisConnection);
                            cmd.ExecuteNonQuery();
                            if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
                        }
                        else
                        {
                            string reetiquetado = "UPDATE Tb_Det_Etiqueta SET Estatus = 'R' WHERE Eti_Lectura = '" + lectura + "'";
                            DateTime fechacapsplit = Convert.ToDateTime(fechacap);
                            if (fechacapsplit > horavalidacionleido)
                            {
                                if (CamEmb.Trim().Length == 0)
                                {
                                    if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                                    SqlCommand cmd = new SqlCommand(reetiquetado, thisConnection);
                                    cmd.ExecuteNonQuery();
                                    if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
                                }
                                else
                                {
                                    Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada", mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + "Embarque " + Embcap + "\n\r" + nom + "  Informe al Supervisor de Camionetas para Liberacion de Cajas" };
                                    await db.InsertAsync(mensa);
                                    //Borrado de Etiquetas capturadas
                                    await db.QueryAsync<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                                    await db.QueryAsync<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                                    ok = "N";
                                    er = "S";
                                }
                            }
                            else
                            {
                                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                                SqlCommand cmd = new SqlCommand(reetiquetado, thisConnection);
                                cmd.ExecuteNonQuery();
                                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
                            }
                        }
                    }

                    if (fechacappre.Length > 0)
                    {
                        if (Desactivarhabilitarreimprimir == 0)
                        {
                            Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada En PreSplit", mensaje = "Etiqueta YA FUE CAPTURADA EN PRE-SPLIT y continua Activa, Favor de Reetiquetar!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacappre + "\n\r" + "Embarque " + Embcap + "\n\r" + nom + "  Informe al Supervisor de Camionetas para Liberacion de Cajas" };
                            await db.InsertAsync(mensa);
                            //Borrado de Etiquetas capturadas
                            await db.QueryAsync<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                            await db.QueryAsync<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                            EtiquetaCapturada = "N";
                            ok = "N";
                            er = "S";

                            //string reetiquetado = "insert into Tb_Det_Sol_Reetiquetado (Fecha, emb_folio, fecha_cap, Lectura, Recibo, Producto, Caja, TarIni, TarFin, Cve_Camioneta, Estatus, Obs, armador, autorizo, origen) values" +
                            //    " ('" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '',  GETDATE(), '" + lectura + "', '" + mfol + "', '" + mcod + "', '" + mcaj + "', '" + mtar + "', '" + mtar + "', '', 'A', 'SOLICITUD DE REIMPRESION POR ETIQUETA YA LEIDA PRE-SPLIT', 'PRESPLIT', '', 'EMB')";
                            //SqlCommand cmd = new SqlCommand(reetiquetado, thisConnection);
                            //cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            Mensajes mensa = new Mensajes { titulo = "Etiqueta ya capturada", mensaje = "Error Etiqueta YA FUE CAPTURADA!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj + "\n\r" + "Día " + fechacap + "\n\r" + "Embarque " + Embcap + "\n\r" + nom + "  Informe al Supervisor de Camionetas para Liberacion de Cajas" };
                            await db.InsertAsync(mensa);

                            await db.QueryAsync<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "' AND Cajas = '" + mcaj + "'");
                            await db.QueryAsync<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + 1 + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                            EtiquetaCapturada = "N";

                            ok = "N";
                            er = "S";
                        }
                    }
                    if (fechacappre.Length > 0)
                    {
                        string horavalidacionleidopresplit = Convert.ToString(horavalidacionleido).Trim();
                    }

                    xLote consecutivo = new xLote { Tipo = captu.Tipo.ToString(), Pedido = "", Folio = captu.Folio, Codigo = captu.Codigo, Tarima = captu.Tarima, Cajas = captu.Cajas, nombre = nom, diacad = "", mescad = "", fecha_captura = mfeccap };
                    await db.InsertAsync(consecutivo);

                    totok++;
                    conta++;

                    if (er == "S")
                    {
                        tot++;
                    }
                }


                var existencias = await db.QueryAsync<xprod>("Select Folio, Codigo, Tarima, Tipo, COUNT(Tipo) AS Cajas FROM xprod GROUP BY Folio, Codigo, Tarima, Tipo");

                foreach (var captu in existencias)
                {
                    string er = "";
                    mtip = captu.Tipo.ToString();
                    mfol = captu.Folio.ToString();
                    mcod = captu.Codigo.ToString();
                    mtar = captu.Tarima.ToString();
                    int mcajas = Convert.ToInt32(captu.Cajas.ToString());
                    string nom = getNombreProducto(captu.Codigo.ToString().Trim());

                    string cadena = "";
                    //traer nombre de producto para validar cuantos dias debo aumentar.

                    int diascad = 14;
                    if (traenom(captu.Codigo.ToString().Trim()).Contains("BETABEL"))
                    {
                        diascad = 60;
                    }
                    else if (traenom(captu.Codigo.ToString().Trim()).Contains("AJO"))
                    {
                        diascad = 180;
                    }
                    else if (traenom(captu.Codigo.ToString().Trim()).Contains("ADEREZO") || traenom(captu.Codigo.ToString().Trim()).Contains("VINAGRETA") || traenom(captu.Codigo.ToString().Trim()).Contains("QUESO"))
                    {
                        diascad = 90;
                    }

                    if (mtip == "PTC")
                        cadena = "SELECT ETIQUETA AS PROD,SURTIDO,FECHA_CAD AS FECCAD, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod + "' AND RECIBO = '" + mfol + "' " +
                                 "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar).ToString() + "' ";

                    else
                        cadena = "SELECT NUM_CAJAS AS PROD, CAJAS_SUR AS SURTIDO,NUM_LOTE AS FECCAD, ISNULL(fechacad, FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod + "' AND FOLIO = '" + mfol + "' " +
                            "AND TARIMA = '" + Convert.ToInt32(mtar).ToString() + "' ";

                    SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                    DataSet ds = new DataSet();

                    //MessageBox.Show(cadena); 
                    da.Fill(ds, "Info");
                    DataTable Info = ds.Tables["Info"];
                    //MessageBox.Show(Info.Rows.Count.ToString()); 
                    if (Info.Rows.Count == 0)
                    {
                        ok = "N";
                        EtiquetaExiste = "N";

                        Mensajes mensa = new Mensajes { titulo = "Etiqueta No Existe", mensaje = "Error Etiqueta No Existe!! " + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + "\n\r" + "\n\r" + traenom(captu.Codigo.ToString().Trim()) + " Informe al supervisor, Retire Todas las cajas - Total capturado: " + mcajas + ", Reetiquete y Leala nuevamente" };
                        await db.InsertAsync(mensa);


                        await db.QueryAsync<xprod>("delete from[xprod] Where Tipo = '" + mtip + "' AND Folio = '" + mfol + "' AND Codigo = '" + mcod + "' AND Tarima = '" + mtar + "'");
                        await db.QueryAsync<ConPedidos>("UPDATE [ConPedidos] SET surtido = surtido - " + mcajas + " WHERE prod_clave = '" + mcod.ToString().Trim() + "'");

                        tot++;
                        continue;
                    }

                    System.String diacaducidad = "";
                    System.String mescaducidad = "";

                    foreach (DataRow row in Info.Rows)
                    {
                        int mP = Convert.ToInt32(row["PROD"]);
                        int mS = Convert.ToInt32(row["SURTIDO"]);
                        int cant = 0;
                        int cantpresplit = 0;

                        string ProdPreSplit = getProductosPreSplit(mfol, mcod, mtar);

                        cantpresplit = Convert.ToInt32(ProdPreSplit);

                        mS = mS + cantpresplit;

                        //TRAER LA CANTIDAD DE CAJAS EXISTENTES EN LA LECTURA
                        var query1 = await db.QueryAsync<xprod>("SELECT * FROM [xprod] Where tipo = '" + mtip + "' and Folio = '" + mfol + "' and Codigo = '" + mcod + "' and Tarima = '" + mtar + "'");
                        foreach (var captu1 in query1)
                        {
                            cant = cant + 1;
                        }

                        if ((mS + cant) > mP)
                        {
                            ok = "N";
                            //LbxCap.SelectedIndex = i;
                            HayExistencias = "NE";
                            er = "S";

                            if (amtip != mtip || amfol != mfol || amcod != mcod || amtar != mtar)
                            {
                                if (Desactivarhabilitarreimprimir == 0)
                                {
                                    //Mensajes mensa = new Mensajes { titulo = "Etiqueta Sin Existencias", mensaje = "Error en la Etiqueta Ya No Hay Existecia!!" + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " \n\r" + nom + "\n\r" + cant.ToString() };
                                    //await db.InsertAsync(mensa);

                                    Mensajes mensa = new Mensajes { titulo = "Tarima Surtida Completamente", mensaje = "La Cantidad a Surtir Supera Por " + ((mS + cant) - mP) + " Cajas Lo Producido, \n\r Produ: " + mP + " | Surt: " + mS + " | Leidos: " + cant + "\n\r" + mtip + " | " + mfol + " | " + mcod + " | " + mtar + " \n\r" + traenom(captu.Codigo.ToString().Trim()) + " Favor de ir a Descargue y Habilitar sus folios de Esta orden" };
                                    await db.InsertAsync(mensa);
                                }
                            }

                            AgregaSolHabilitarFolios(mtip, mfol, mcod, traenom(captu.Codigo.ToString().Trim()), mtar, ((mS + cant) - mP).ToString());
                            //AgregaFolioSinExistencia(mtip, mfol, mcod, nom, mtar, cant.ToString());
                        }
                        string feccad = "";

                        diacaducidad = traediafecad(row["feccad"].ToString(), mtip);
                        mescaducidad = traemesfecad(row["feccad"].ToString(), mtip);

                        //Validacion de fecha de caduciadad que debe venir*****************************************************************************************************

                        if (diacaducidad == "|")
                        {
                            diacaducidad = traediafecadrec(row["fecha_cad"].ToString(), mtip);
                        }

                        if (mescaducidad == "|")
                        {
                            mescaducidad = traemesfecadrec(row["fecha_cad"].ToString(), mtip);
                        }

                        xLote consecutivo = new xLote { Tipo = captu.Tipo.ToString(), Pedido = "", Folio = captu.Folio, Codigo = captu.Codigo, Tarima = captu.Tarima, Cajas = captu.Cajas, nombre = traenom(captu.Codigo.ToString().Trim()), diacad = diacaducidad.Trim(), mescad = mescaducidad.Trim(), fecha_captura = mfeccap };


                        await db.QueryAsync<xLote>("UPDATE [xLote] SET mescad = '" + mescaducidad.Trim() + "', diacad = '" + diacaducidad.Trim() + "'  WHERE Codigo = '" + captu.Codigo + "' AND Folio = '" + captu.Folio + "' AND Tarima = '" + captu.Tarima + "' AND Tipo = '" + captu.Tipo + "'");
                        //Registra en la base de datos SQLite
                        //db.Insert(consecutivo);
                        //totok++;

                        if (er == "S")
                        {
                            tot++;
                        }


                        amtip = mtip;
                        amfol = mfol;
                        amcod = mcod;
                        amtar = mtar;
                        amcaj = mcaj;
                    }
                }

                if (tot > 0)
                {
                    Mensajes mensa = new Mensajes
                    {
                        titulo = "Se detectaron etiquetas con ERROR",
                        mensaje = "Se detectaron " + tot + " Etiquetas con error" + "\n\r" + "Detalle de la Etiqueta: \n\r" +
                        "Tipo: " + mtip + "\n\r" +
                        "Folio: " + mfol + "\n\r" +
                        "Producto: " + mcod + "\n\r" +
                        "Tarima: " + mtar + "\n\r" +
                        "Caja: " + mcaj + "\n\r" +
                        "Nombre: " + traenom(mcod.Trim()) + ""
                    };
                    await db.InsertAsync(mensa);

                }

                thisConnection.Close();
                //List<FlimStarInfo> lstFlimStar = detalle_Surtido();
                //var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                //RunOnUiThread(() => gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar));
                //gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); //detalle_pedido


                RunOnUiThread(() => total.Text = totok.ToString("##0"));
                return ok;


                //if (ok == "S")
                //{
                //    //HoraFechaValidacion = horavalidacionleido;
                //    if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                //    string hora_min_lectura = "select convert(varchar(8),GetDate(), 108) as hhmm";
                //    SqlCommand cmdhoraminlectura = new SqlCommand(hora_min_lectura, thisConnection);
                //    horavalidacionleido = Convert.ToDateTime(Convert.ToString(cmdhoraminlectura.ExecuteScalar()));
                //    string lecturadelama = "";
                //    DateTime lectura = Convert.ToDateTime(horavalidacionleido);
                //    thisConnection.Close();
                //}
                //thisConnection.Close();
                //return ok;
            });

        }

        private string getProductosPreSplit(string mfol, string mcod, string mtar)
        {
            string Valor = "";
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            //tRAER LA CANTIDAD DE CAJAS LEIDAS EN PRESPLIT
            string cantleidapresplit = "SELECT COUNT(Eti_Lectura) FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = '" + mfol + "' AND Eti_Producto = '" + mcod + "' AND Eti_TarIni = '" + mtar + "' AND Estatus = 'A'";
            SqlCommand cmdPRES = new SqlCommand(cantleidapresplit, thisConnection);
            string ValorPres = Convert.ToString(cmdPRES.ExecuteScalar());
            return ValorPres;
        }

        private string ValidaCaja(string cadena)
        {
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            //string Cadena = "SELECT CONCAT(ISNULL((Select CONCAT(fecha_cap, '*', emb_folio) From tb_Det_Etiqueta Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'), '*'), '*', (Select fecha_cap From Tb_Det_Etiqueta_Presplit " +
            //"Where Eti_Lectura = '" + cadena + "' AND Estatus = 'A'))";
            string Cadena = "SELECT CONCAT(ISNULL((Select CONCAT(fecha_cap, '*', RTRIM(emb_folio), '*', RTRIM(Cve_Camioneta)) From tb_Det_Etiqueta Where Eti_Lectura = '" + cadena + "' AND Estatus NOT IN ('C', 'R')), CONCAT('*', '*')), '*', (Select fecha_cap From Tb_Det_Etiqueta_Presplit Where Eti_Lectura = '" + cadena + "' AND Estatus = 'S'))";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }

        private void AgregaSolHabilitarFolios(string mTi, string mFo, string mPr, string mNo, string mTa, string mCa)
        {
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            if (Desactivarhabilitarreimprimir == 0)
            {
                string mped = "";
                //mped = mped.Replace("Pedido Actual: ", "");
                string cadena = "IF NOT EXISTS(SELECT emb_folio FROM tb_Det_Sol_Mod_inventario WHERE emb_folio = '" + mped + "' AND orden = '" + mFo + "' AND  id_codigo = '" + mPr + "' AND  tarima = '" + mTa + "' AND tipo = '" + mTi + "' AND estatus = 'A') INSERT INTO  tb_Det_Sol_Mod_inventario(emb_folio, orden, tipo, id_codigo, descrip, cajas_mod, fecha_cap, capturo, motivo, tarima, estatus) " +
                                "VALUES('" + mped + "','" + mFo + "','" + mTi + "','" + mPr + "','" + mNo + "','" + mCa + "',GETDATE(),'PRESPLIT','Folio Modificado Por intervension de Split Camionetas','" + mTa + "','A')";
                //MessageBox.Show(cadena);
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();

            }
            else
            {
                string cadena = "";
                if (mTi == "PTP")
                {

                    cadena = "UPDATE TB_DET_ETI_FINAL SET CAJAS_SUR = CAJAS_SUR  - " + mCa + ", estatus_sur = ' ' WHERE FOLIO = '" + mFo.Trim() + "' AND CVE_PROD = '" + mPr.Trim() + "' AND  TARIMA = '" + mTa + "'";

                    /*cadena = "UPDATE tb_Det_Sol_Mod_inventario (emb_folio, orden, tipo, id_codigo, descrip, cajas_mod, fecha_cap, capturo, motivo, tarima, estatus) " +
                                "VALUES('" + mped + "','" + mFo + "','" + mTi + "','" + mPr + "','" + mNo + "','" + mCa + "',GETDATE(),'PRESPLIT','Folio Modificado Por intervension de Split Camionetas','" + mTa + "','A')";*/
                }
                else
                {
                    cadena = "UPDATE TB_DET_TRAZABILIDAD SET SURTIDO = SURTIDO - " + mCa + ", pti_estatus_sur = ' '  WHERE RECIBO = '" + mFo.Trim() + "' AND PROD_CLAVE = '" + mPr.Trim() + "' AND TARIMA = '" + mTa + "'";


                    /*cadena = "UPDATE tb_Det_Sol_Mod_inventario (emb_folio, orden, tipo, id_codigo, descrip, cajas_mod, fecha_cap, capturo, motivo, tarima, estatus) " +
                                "VALUES('" + mped + "','" + mFo + "','" + mTi + "','" + mPr + "','" + mNo + "','" + mCa + "',GETDATE(),'PRESPLIT','Folio Modificado Por intervension de Split Camionetas','" + mTa + "','A')";*/
                }
                cadena = cadena + "insert into tb_registro_movimientos (fecha, nom_compu, nom_usu, tipo_mov, op_clave, folio, detalle, sistema, mov_folio, Arm_solicita, usuario_autoriza, motivo) " +
                            "Values(GETDATE(),'LECTORAPRESPLIT','" + responsable + "','Hab','1.1','" + mFo + "', 'Habilitar Cajas " + mFo.Trim() + "|" + mPr.Trim() + "|" + mTa + "', 'PRESPLIT', '" + mFo.Trim() + "', '" + responsable + "', '" + responsable + "' , 'SPLITTRAILER')";
                PreSplitCamionetas.GuardaLocal GuardaConsulta = new PreSplitCamionetas.GuardaLocal();
                GuardaConsulta.creartxt(cadena);
            }



        }

        private string ValidaCajaEtiVerde(string cadena, DataTable foliosleidos)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);*/
            string Valor = "";

            DataRow[] datos = foliosleidos.Select("Eti_Lectura = '" + cadena + "'");

            if (datos.Length > 0)
            {
                Valor = datos[0].ItemArray[1].ToString();
            }

            return Valor;

        }

        private string ValidaCajaPreesplitVerde(string cadena, DataTable foliosleidos)
        {
            /*string Cadena = "Select fecha_cap From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);*/
            string Valor = "";

            DataRow[] datos = foliosleidos.Select("Eti_Lectura = '" + cadena + "'");

            if (datos.Length > 0)
            {
                Valor = datos[0].ItemArray[1].ToString();
            }

            return Valor;

        }

        private string ValidaEmb(string cadena)
        {
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            string Cadena = "Select emb_folio From tb_Det_Etiqueta " +
                           "Where Eti_Lectura = '" + cadena + "'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }

        private string ValidaCajaPreesplit(string cadena)
        {
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            string Cadena = "Select fecha_cap From Tb_Det_Etiqueta_Presplit " +
                          "Where Eti_Lectura = '" + cadena + "' AND Estatus = 'A'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string Valor = Convert.ToString(cmd.ExecuteScalar());
            return Valor;
        }

        private string traenom(string cve)
        {
            string nom = "";
            foreach (DataRow row in CatProd.Select("prod_clave = '" + cve + "'"))
                nom = row["prod_nombre"].ToString().Trim();

            nom = nom.Replace("'", " ");
            return nom;
        }

        public string getNombreProducto(string prod_clave)
        {
            if (string.IsNullOrEmpty(prod_clave)) return string.Empty;

            // FIX: conexión local — no comparte thisConnection
            try
            {
                using (var conn = new SqlConnection(MainActivity.cadenaConexion))
                {
                    conn.Open();
                    using (var command = new SqlCommand("SELECT dbo.getNombreProducto(@prod_clave)", conn))
                    {
                        command.Parameters.AddWithValue("@prod_clave", prod_clave);
                        // FIX: null check antes de .Trim()
                        var result = command.ExecuteScalar();
                        return result != null ? result.ToString().Trim() : string.Empty;
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private void AgregaFolioSinExistencia(string mTi, string mFo, string mPr, string mNo, string mTa, string mCa)
        {
            string cadena = "INSERT INTO TB_DET_SPLIT_FOLIOSINEXIS(FECHA,FECHACAP,CVE_CAMIONETA,NOM_CAPSPLIT,TIPO,FOLIO,PROD_CLAVE,PROD_NOMBRE,TARIMA,CAJA) " +
                            "VALUES('" + DateTime.Now.ToString("dd/MM/yyyy") + "','" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','" +
                            cvecam + "','" + muser + "','" + mTi + "','" + mFo + "','" + mPr + "','" + mNo + "','" + mTa + "','" + mCa + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();
        }

        private string traediafecad(string fecha, string tipo)
        {
            string Cad = "|";
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                    Cad = fecha.Substring(fecha.Length - 3, 2);
                else
                    Cad = fecha.Substring(0, 2);
            }
            return Cad;
        }

        private string traemesfecad(string fecha, string tipo)
        {
            string Cad = "|";
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                    Cad = fecha.Substring(fecha.Length - 6, 3);
                else
                    Cad = traemes(Convert.ToInt32(fecha.Substring(3, 2)));
            }
            return Cad;
        }

        private string traediafecadrec(string fecha, string tipo)
        {
            string Cad = " | ";
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                    Cad = fecha.Substring(fecha.Length - 2, 2);
                else
                    Cad = fecha.Substring(0, 2);
            }
            return Cad;
        }

        private string traemesfecadrec(string fecha, string tipo)
        {
            string Cad = " | ";
            if (fecha.Trim().Length > 0)
            {
                if (tipo == "PTP")
                    Cad = traemes(Convert.ToInt32(fecha.Substring(fecha.Length - 4, 2)));
                else
                    Cad = traemes(Convert.ToInt32(fecha.Substring(3, 2)));
            }
            return Cad;
        }

        private async Task<string> validafecad()
        {
            string Valor = "";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            var productoscapturados = await db.QueryAsync<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            await db.QueryAsync<XLoteSug>("delete from[XLoteSug]");

            var allItems = db.GetItemsAsync<xLote>();
            int count = allItems.Result.Count;
            int[] validados = new int[count + 1];
            int capturas = 0;
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
                int totalusadas;
                int simulador = 0;
                int totaldis = 0;
                string fechaant = "";

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());

                }

                int resttotal = await traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
                string todobien = "OK";
                int prod_cap = 0;
                int usadas = 0;
                int existefecant = 0;
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();
                string diacadant = "";
                string mescadant = "";
                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, 15, pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, 15, pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE preautorizado NOT IN ('C', 'A') AND  PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(fechacad, FORMAT( DATEADD(day, 15, fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND (num_cajas - cajas_sur) > 0 Order By fechacad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];


                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

                foreach (DataRow row in xlote.Rows)
                {

                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                  "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    int TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    thisConnection.Close();

                    row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;

                    if (Convert.ToInt32(row["disponible"]) > 0)
                    {

                        if (totalpro > 0)
                        {

                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            if ((diacadant == diacad && mescadant == mescad) || (diacadant == "" && mescadant == ""))
                            {
                                todobien = "OK";
                            }
                            else
                            {
                                if (totaldisponibles == 0)
                                {
                                    todobien = "OK";
                                }
                                else
                                {
                                    todobien = "NO";
                                }
                            }

                            if (todobien == "OK")
                            {
                                var prodcap = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                                foreach (var capturado in prodcap)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }

                                if (totaldis > 0)
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldis, Cajasusadas = usadas, foliomens = "" };
                                    await db.InsertAsync(sugeridos);
                                }
                                else
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = 0, Cajasusadas = usadas, foliomens = "" };
                                    await db.InsertAsync(sugeridos);
                                }

                                diacadant = diacad;
                                mescadant = mescad;

                            }
                            else
                            {
                                var loteSug = await db.QueryAsync<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");

                                foreach (var capturado in loteSug)
                                {
                                    string recibosug = capturado.recibosug;
                                    string fecrecsug = capturado.fecrecsug;
                                    string cveprod = capturado.cveprod;
                                    string tarima = capturado.Tarima;
                                    int cajasdis = capturado.Cajasdis;
                                    int cajasusadas = capturado.Cajasusadas;
                                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del" + capturado.fecrecsug };
                                    await db.InsertAsync(mensa);
                                    ValiFechacad = "N";
                                    await db.QueryAsync<XLoteSug>("DELETE  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 AND Cajasusadas <= 0");
                                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                                    await db.InsertAsync(sugeridosact);
                                    totalpro = 0;
                                }

                            }
                        }


                    }



                }


            }


            return Valor;


        }

        private string traemes(int mes)
        {
            string nom = "";
            switch (mes)
            {
                case 1: { nom = "ENE"; break; }
                case 2: { nom = "FEB"; break; }
                case 3: { nom = "MAR"; break; }
                case 4: { nom = "ABR"; break; }
                case 5: { nom = "MAY"; break; }
                case 6: { nom = "JUN"; break; }
                case 7: { nom = "JUL"; break; }
                case 8: { nom = "AGO"; break; }
                case 9: { nom = "SEP"; break; }
                case 10: { nom = "OCT"; break; }
                case 11: { nom = "NOV"; break; }
                case 12: { nom = "DIC"; break; }
            }
            return nom;
        }

        private async Task<string> validaprodAsync()
        {
            string ok = "S";
            Surtidomayor = "S";

            try
            {
                // Consulta los productos capturados con surtido mayor al pedido
                var productoscapturados = await Task.Run(() => db.QueryAsync<ConPedidos>("SELECT * FROM ConPedidos WHERE CAST(surtido AS INTEGER) > CAST(pedido AS INTEGER)"));

                // Itera sobre los productos encontrados
                foreach (var captu in productoscapturados)
                {
                    Mensajes mensa2 = new Mensajes { titulo = "Error En el Producto", mensaje = "Producto " + captu.nombre.ToString() + " Surtido es Mayor al Pedido " + "\n\r" + "\n\r" + " Pedidos: " + captu.pedido.ToString() + "  Surtidos: " + captu.surtido.ToString() + "\n\r" + " Debe de eliminar " + (captu.surtido - captu.pedido) + " cajas, para poder continuar con la validación." };
                    await db.InsertAsync(mensa2);
                    Surtidomayor = "NR";
                }
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejo de errores, por ejemplo:
                //Console.WriteLine("Error al validar productos: " + ex.Message);
                Toast.MakeText(this, "Error al validar productos: " + ex.Message, ToastLength.Short).Show();
                ok = "N"; // Cambiar a 'N' si ocurre un error
            }

            return ok;
        }

        void fnShowCustomAlertDialog()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            AlertDialog builder = new AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dismiss();

            };
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + password.Text.Trim() + "' AND clave = 'EM'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {
                    Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    thisConnection.Close();

                    AutoPed = "S";
                    Guardar.Enabled = true;
                    builder.Dismiss();
                }

            };
            builder.Show();
        }

        void fnShowCustomAlertDialogCancel()
        {
            //Inflate layout
            View view = LayoutInflater.Inflate(Resource.Layout.frmsupervisor, null);
            AlertDialog builder = new AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            TextView titulo = view.FindViewById<TextView>(Resource.Id.titleLogin);
            EditText password = view.FindViewById<EditText>(Resource.Id.txtPassword);
            Button buttonaceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            button.Click += delegate
            {
                builder.Dismiss();

            };
            titulo.Text = "Autorizacion Folios Adelantados";
            buttonaceptar.Click += delegate
            {
                thisConnection.Open();
                string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + password.Text.Trim() + "' AND clave = 'EM'";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                mAutoriza = Convert.ToString(cmd.ExecuteScalar());
                if (mAutoriza.Trim().Length == 0)
                {
                    Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                    thisConnection.Close();
                }
                else
                {
                    thisConnection.Close();
                    Guardar.Enabled = true;
                    builder.Dismiss();
                }

            };
            builder.Show();
        }

        private async Task<string> repetido(string mtip, string mfol, string mcod, string mtar, string mcaj)
        {
            string Ok = "N";
            var productoscapturados = await db.GetItemsAsync<xprod>();
            foreach (var captu in productoscapturados)
            {
                if (mtip == captu.Tipo && mfol == captu.Folio && mcod == captu.Codigo && mtar == captu.Tarima && mcaj == captu.Cajas)
                {
                    Ok = "S";
                    break;
                }
            }
            return Ok;
        }

        private async Task<int> traetotal(string mcod)
        {
            int total = 0;
            var productoscapturados = await db.GetItemsAsync<ConPedidos>();
            foreach (var captu in productoscapturados)
            {
                if (captu.prod_clave == mcod)
                {
                    total = Convert.ToInt32(captu.surtido);
                    break;
                }
            }
            return total;
        }

        private async Task<int> traerecibosvencidos(string codigo, string tipo)
        {
            int total = 0;
            var productoscapturados = await db.QueryAsync<xLote>("select Folio, Codigo, Tarima, Count(Cajas) AS Cajas FROM xLote Where Codigo = '" + codigo + "' AND Tipo = '" + tipo + "' Group by Folio, Codigo, Tarima");
            foreach (var captu in productoscapturados)
            {
                int total_recibo_cap = Convert.ToInt32(captu.Cajas);
                if (tipo == "PTC")
                    cadena = "SELECT ETIQUETA AS PROD,SURTIDO,FECHA_CAD AS FECCAD, pti_estatus_sur AS estatus_sur FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + captu.Codigo.Trim() + "' AND RECIBO = '" + captu.Folio.Trim() + "' " +
                             "AND TIPO = '" + tipo + "' AND TARIMA = '" + Convert.ToInt32(captu.Tarima).ToString() + "' ";

                else
                    cadena = "SELECT NUM_CAJAS AS PROD, CAJAS_SUR AS SURTIDO,NUM_LOTE AS FECCAD, estatus_sur FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + captu.Codigo.Trim() + "' AND FOLIO = '" + captu.Folio.Trim() + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(captu.Tarima).ToString() + "' ";

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();

                da.Fill(ds, "Info");
                DataTable Info = ds.Tables["Info"];

                foreach (DataRow row in Info.Rows)
                {
                    int mP = Convert.ToInt32(row["PROD"]);
                    int mS = Convert.ToInt32(row["SURTIDO"]);

                    if (((total_recibo_cap + mS) > mP) || (row["estatus_sur"].ToString().Trim() == "S"))
                    {
                        total = total + total_recibo_cap;
                        XLoteSug sugeridos = new XLoteSug
                        {
                            recibosug = captu.Folio.Trim(),
                            fecrecsug = "/",
                            cveprod = captu.Codigo.Trim(),
                            Tarima = Convert.ToInt32(captu.Tarima).ToString(),
                            Cajasdis = 0,
                            Cajasusadas = total_recibo_cap
                        };
                        await db.InsertAsync(sugeridos);
                    }
                }
            }
            return total;
        }

        public async void insertarinfo()
        {
            thisConnection.Open();
            var pedidoscapturados = await db.GetItemsAsync<Pedidos>();
            foreach (var captu in pedidoscapturados)
            {
                string cadena = "insert into Tb_Split_Pedidos(folio, prod_clave, nombre, pedido, surtido, fecha, veces) " +
                               "Values('" + captu.folio + "','" + captu.prod_clave + "','" + captu.nombre + "','" + captu.pedido + "','" + captu.surtido + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + veces + "' )";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }

            var concentradocapturados = await db.GetItemsAsync<ConPedidos>();
            foreach (var captu in concentradocapturados)
            {
                string cadena = "insert into Tb_Split_ConPedidos(prod_clave, nombre, pedido, surtido, fecha, veces) " +
                               "Values('" + captu.prod_clave + "','" + captu.nombre + "','" + captu.pedido + "','" + captu.surtido + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + veces + "')";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }

            var relXlote = await db.GetItemsAsync<xLote>();
            foreach (var captu in relXlote)
            {
                string cadena = "insert into Tb_split_xLote(Tipo, Pedido, Folio, Codigo, Tarima, Cajas, nombre, diacad, mescad, fecha, veces) " +
                               "Values('" + captu.Tipo + "','" + captu.Pedido + "','" + captu.Folio + "','" + captu.Codigo + "', '" + captu.Tarima + "', '" + captu.Cajas + "','" + captu.nombre + "','" + captu.diacad + "','" + captu.mescad + "', '" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + veces + "')";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }

            var relXprod = await db.GetItemsAsync<xprod>();
            foreach (var captu in relXprod)
            {
                string cadena = "insert into Tb_split_xprod(Tipo, Folio, Codigo, Tarima, Cajas, fecha, veces) " +
                               "Values('" + captu.Tipo + "','" + captu.Folio + "','" + captu.Codigo + "', '" + captu.Tarima + "', '" + captu.Cajas + "','" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "', '" + veces + "')";
                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();
            }

            thisConnection.Close();

            veces++;
        }

        private string NoSplit(string mped)
        {
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            string Cadena = "Select MAX(Split) from Tb_Det_Etiqueta_Presplit where fecha = '" + System.DateTime.Now.ToString("dd/MM/yyyy") + "'";
            SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
            string cad = Convert.ToString(cmd.ExecuteScalar());
            cad = (cad.Trim().Length == 0) ? "1" : (Convert.ToInt32(cad) + 1).ToString();
            return cad;
        }

        private void ConsPedSur(string mped)
        {
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            string cadena = "Select prod_clave as Codigo, nom_prod as Nombre, cant_ped as Pedido, 0 as Surtido from tb_ped_embarque Where emb_folio = '" + mped.Trim() + "' Order by nom_prod";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            var ConsPed = ds.Tables["ConsPed"];
            cadena = "Select prod_clave, sum(cajas) as cajas from tb_det_split Where emb_folio = '" + mped.Trim() + "'" +
                     "And estatus != '' Group By prod_clave Order by prod_clave ASC";
            da = new SqlDataAdapter(cadena, thisConnection);
            ds = new DataSet();
            da.Fill(ds, "PedSur");
            var PedSur = ds.Tables["PedSur"];
            int Cp = 0, Cs = 0, sur = 0;
            thisConnection.Close();
            foreach (DataRow Row in ConsPed.Rows)
            {
                sur = 0;
                foreach (DataRow row in PedSur.Select("prod_clave = '" + Row["Codigo"].ToString() + "'"))
                    sur = Convert.ToInt32(row["Cajas"]);

                db.QueryAsync<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + sur + "' WHERE prod_clave = '" + Row["Codigo"].ToString().Trim() + "'");

                Cp += Convert.ToInt32(Row["pedido"]);
                Cs += sur;
            }



        }

        private async Task AgregaDetaEtiAdelantado()
        {
            var recibosatrasusa = await db.QueryAsync<XLoteSug>("Select * FROM [XLoteSug] Where Cajasusadas != 0 Order By recibosug, cveprod ASC");
            foreach (var recibos in recibosatrasusa)
            {
                if (recibos.Cajasusadas > 0)
                {
                    await db.QueryAsync<xLote>("UPDATE [xLoteFinal] SET cajas = CAST(Cajas as int) - " + recibos.Cajasusadas + " WHERE Folio = '" + recibos.recibosug.ToString().Trim() + "' AND Codigo = '" + recibos.cveprod.ToString().Trim() + "' AND CAST(Tarima as int) = " + Convert.ToInt32(recibos.Tarima.ToString()));
                }
            }

            var productoscap = await db.QueryAsync<xLoteFinal>("Select DISTINCT(Codigo) FROM [xLoteFinal] Order By Codigo ASC");
            foreach (var productos in productoscap)
            {
                var recibosatras = await db.QueryAsync<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + productos.Codigo.Trim() + "' AND cajasdis != 0 AND foliomens = 'S' ORDER BY recibosug, Tarima LIMIT 1");
                foreach (var recibos in recibosatras)
                {

                    var folio = recibos.recibosug.Trim();
                    var producto = recibos.cveprod.Trim();
                    var tarima = recibos.Tarima.Trim();
                    var feccad = recibos.fecrecsug.Trim();

                    var recibosCapturados = await db.QueryAsync<xLote>("Select * FROM [xLoteFinal] Where Codigo = '" + recibos.cveprod.ToString().Trim() + "' Order By Pedido, codigo");
                    foreach (var reccapturado in recibosCapturados)
                    {
                        string fechacaducidadcapturado = reccapturado.diacad.Trim() + "/" + reccapturado.mescad.Trim();
                        if (reccapturado.Folio.Trim() != folio && reccapturado.Tarima.Trim() != tarima)
                        {
                            if (fechacaducidadcapturado != feccad)
                            {
                                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                                string cadena = "insert into tb_det_folio_adelantado (responsable, fecha, emb_folio, recibo_cap, fecreccap, recibo_sug, fecrecsug, prod_clave, producto, cantidad, autorizo, tarimacap, tarimasug, imei, motivo, fechareal) " +
                               "Values('" + responsable + "','" + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + "','" + reccapturado.Folio + "', '" + reccapturado.Folio + "', '" + reccapturado.diacad + "/" + reccapturado.mescad + "','" + recibos.recibosug + "', '" + recibos.fecrecsug + "', '" + recibos.cveprod + "', '" + reccapturado.nombre + "', '" + reccapturado.Cajas + "', '" + mAutoriza.Trim() + "', '" + reccapturado.Tarima.Trim() + "', '" + recibos.Tarima.Trim() + "', '" + imei + "', '" + motfolade.Trim() + " Pre-Split', GETDATE())";
                                SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                                cmd.ExecuteNonQuery();
                                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
                                break;
                            }
                        }
                    }



                    /*var recibosCapturados = db.Query<xLote>("Select * FROM [xLoteFinal] Where Codigo = '" + recibos.cveprod.ToString().Trim() + "' Order By Pedido, codigo ASC");
                    foreach (var reccapturado in recibosCapturados)
                    {
                        if (Convert.ToInt32(reccapturado.Cajas) > 0)
                        {
                            string cadena = "insert into tb_det_folio_adelantado (responsable, fecha, emb_folio, recibo_cap, fecreccap, recibo_sug, fecrecsug, prod_clave, producto, cantidad, autorizo, tarimacap, tarimasug) " +
                                "Values('" + responsable + "','" + DateTime.Now.ToString("dd/MM/yyyy") + "','" + reccapturado.Pedido + "', '" + reccapturado.Folio + "', '" + reccapturado.diacad + "/" + reccapturado.mescad + "','" + recibos.recibosug + "', '" + recibos.fecrecsug + "', '" + recibos.cveprod + "', '" + reccapturado.nombre + "', '" + reccapturado.Cajas + "', '" + mAutoriza.Trim() + "', '" + reccapturado.Tarima.Trim() + "', '" + recibos.Tarima.Trim() + "')";
                            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
                            cmd.ExecuteNonQuery();
                        }
                    }*/
                }
            }
        }

        async void ITextWatcher.AfterTextChanged(IEditable s)
        {

        }

        async void ITextWatcher.BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {

        }

        async void ITextWatcher.OnTextChanged(ICharSequence s, int start, int before, int count)
        {
            if (mconcen == "2")
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Modo concentrado Activado</font>"));
                alertDialog.SetIcon(Resource.Drawable.no);
                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Esta consultando el concentrado no se puede capturar código.</font>"));
                alertDialog.SetCancelable(false);
                alertDialog.SetNeutralButton("Ok", (sender, e) =>
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
                return;
            }

            Guardar.Enabled = false;
            string rawText = s?.ToString() ?? string.Empty;

            // FIX: ignorar caracteres parciales, solo procesar cuando el scanner
            // haya terminado de emitir el código completo
            if (!_scannerHandler.TryGetCompleteScan(rawText, out string cleanCode))
                return;

            // FIX: serializar — si hay un escaneo en curso, descartar este
            await _scannerHandler.ProcessScanAsync(async () =>
            {
                string folio = foliocaptura.Text;
                if (folio == valorfinal || folio == "") return;

                if (Eliminar_caja.Checked)
                    await eliminaretiquetablanca();
                else if (etiblanca.Checked)
                    await EtiquetasBlancaAsync();
                else
                    await etiquestaverde();
            });
        }

        public async Task ActualizarEstatusEtiquetaAsync(string recibo, string producto, string tarima, string caja)
        {
            using (SqlCommand command = new SqlCommand("ActualizarEstatusEtiqueta", thisConnection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@Eti_Recibo", recibo);
                command.Parameters.AddWithValue("@Eti_Producto", producto);
                command.Parameters.AddWithValue("@Eti_TarIni", tarima);
                command.Parameters.AddWithValue("@Eti_Caja", caja);
                SqlParameter filasParam = new SqlParameter("@FilasAfectadas", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(filasParam);

                try
                {
                    if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }

                    command.ExecuteNonQuery();

                    int filasAfectadas = (int)filasParam.Value;

                    if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }

                    if (filasAfectadas > 0)
                    {
                        await RestarUnoASurtidoAsync(producto);
                        //Toast.MakeText(this, "Estatus actualizado correctamente.", ToastLength.Short).Show();
                    }
                }
                catch (Java.Lang.Exception ex)
                {
                    Toast.MakeText(this, "Error al actualizar el estatus: " + ex.Message, ToastLength.Short).Show();
                }
            }
        }

        private async Task<bool> RestarUnoASurtidoAsync(string mcod)
        {
            try
            {
                // Primero consultamos el valor actual de 'surtido' para ese producto
                var resultado = await db.QueryAsync<ConPedidos>("SELECT surtido FROM ConPedidos WHERE prod_clave = '" + mcod.Trim() + "'");
                var item = resultado.FirstOrDefault();

                if (item != null && item.surtido > 0)
                {
                    // Restar 1 si el valor actual es mayor a 0
                    string query = "UPDATE ConPedidos SET surtido = surtido - 1 WHERE prod_clave = ?";
                    await db.ExecuteAsync(query, mcod.Trim());
                    return true;

                }
                else
                {
                    //Toast.MakeText(this, "El surtido ya es 0. No se puede restar más.", ToastLength.Short).Show();
                    return false;
                }
            }
            catch (Java.Lang.Exception ex)
            {
                Toast.MakeText(this, "Error al actualizar surtido: " + ex.Message, ToastLength.Short).Show();
                return false;
            }
        }


        public async Task etiquestaverde()
        {
            int tam = foliocaptura.Text.Length;
            string Vpti_clave = "", Bpti_clave = "";
            string mcaj = "", mtar = "", mcod = "", mfol = "", mtip = "", Ent = "N", mEtiqueta = "0";
            string Bcaj = "", Btar = "", Bcod = "", Bfol = "", Btip = "", BEnt = "N";
            string id_pallet = "";

            if (foliocaptura.Text.Trim().Length == 12)
            {
                string pti_famous = foliocaptura.Text.Trim();
                if (foliocaptura.Text.StartsWith("0"))
                {
                    pti_famous = foliocaptura.Text.TrimStart('0');
                }

                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                string querySSCC = "select*from tb_det_trazabilidad where pti_famous='" + pti_famous + "'";
                SqlCommand sqlCommand = new SqlCommand(querySSCC);
                sqlCommand.Connection = thisConnection;
                SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                while (sqlDataReader.Read())
                {
                    Vpti_clave = sqlDataReader["pti_clave"].ToString().Trim();
                    mfol = sqlDataReader["recibo"].ToString().Trim();
                    mtar = sqlDataReader["tarima"].ToString().Trim();
                    mcod = sqlDataReader["prod_clave"].ToString().Trim();
                    mtip = sqlDataReader["tipo"].ToString().Trim();
                    mEtiqueta = sqlDataReader["etiqueta"].ToString().Trim();
                }
                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
            }
            else if (foliocaptura.Text.Contains(SerialShippingContainerCode) == true)
            {
                Match match = Regex.Match(foliocaptura.Text, patron);
                id_pallet = match.Groups[1].Value;

                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                string querySSCC = "select*from tb_det_trazabilidad where id_Pallet='" + id_pallet + "'";
                SqlCommand sqlCommand = new SqlCommand(querySSCC);
                sqlCommand.Connection = thisConnection;
                SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                while (sqlDataReader.Read())
                {
                    Vpti_clave = sqlDataReader["pti_clave"].ToString().Trim();
                    mfol = sqlDataReader["recibo"].ToString().Trim();
                    mtar = sqlDataReader["tarima"].ToString().Trim();
                    mcod = sqlDataReader["prod_clave"].ToString().Trim();
                    mtip = sqlDataReader["tipo"].ToString().Trim();
                    mEtiqueta = sqlDataReader["etiqueta"].ToString().Trim();
                }
                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
            }
            else if (!Regex.IsMatch(foliocaptura.Text.Trim(), @"\s"))
            {
                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                string querySSCC = "select*from tb_det_trazabilidad where pti_clave='" + foliocaptura.Text.Trim() + "'";
                SqlCommand sqlCommand = new SqlCommand(querySSCC);
                sqlCommand.Connection = thisConnection;
                SqlDataReader sqlDataReader = sqlCommand.ExecuteReader();
                while (sqlDataReader.Read())
                {
                    Vpti_clave = sqlDataReader["pti_clave"].ToString().Trim();
                    mfol = sqlDataReader["recibo"].ToString().Trim();
                    mtar = sqlDataReader["tarima"].ToString().Trim();
                    mcod = sqlDataReader["prod_clave"].ToString().Trim();
                    mtip = sqlDataReader["tipo"].ToString().Trim();
                    mEtiqueta = sqlDataReader["etiqueta"].ToString().Trim();
                }
                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
            }
            else if (foliocaptura.Text.Trim().Contains(" ") == true)
            {
                if (tam < 18)
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 5);
                    mcod = foliocaptura.Text.Replace(mfol, "");
                    mcod = mcod.Replace(mtar, "");
                    mtar = mtar.Replace(" ", "0");
                    mtip = "PTC";
                }
                else
                {
                    mtar = foliocaptura.Text.Substring(tam - 3, 3);
                    mfol = foliocaptura.Text.Substring(0, 6);
                    mcod = foliocaptura.Text.Replace(mfol, "");
                    mcod = mcod.Replace(mtar, "");
                    //mtar = mtar.Replace(" ", "0");
                    mtip = "PTP";
                    if (mfol.Substring(0, 1) == "0")
                    {
                        mtip = "PTC";
                        mfol = Convert.ToInt32(mfol).ToString();
                    }
                }
            }
            else
            {
                for (int i = 0; i < CatProd.Rows.Count; i++)
                {
                    string producto_clave = CatProd.Rows[i]["Prod_Clave"].ToString().Trim();
                    bool esta = foliocaptura.Text.Contains(producto_clave);

                    if (esta)
                    {
                        mcod = producto_clave;
                        break;
                    }
                }

                int posprod = foliocaptura.Text.Trim().IndexOf(mcod);
                mfol = foliocaptura.Text.Substring(0, posprod).Trim();
                string restocaptura = foliocaptura.Text.Replace(mfol, "").Replace(mcod, "");
                if (restocaptura.Length == 6)
                {
                    mtip = "PTC";
                    mtar = restocaptura.Substring(0, 3);
                }
                else
                {
                    mtip = "PTC";
                    //mtar = restocaptura.Substring(0, 2);
                    mtar = restocaptura.Trim();
                }

                /*string mtari = foliocaptura.Text.Substring(tam - 4, 4);
                mtar = foliocaptura.Text.Substring(tam - 4, 2);
                mfol = foliocaptura.Text.Substring(0, 6);
                mcod = foliocaptura.Text.Replace(mfol, "");
                mcod = mcod.Replace(mtari, "");
                mtip = "PTC";*/

            }
            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            mtar = mtar.Trim();

            if (mtip == "PTP")
            {
                mtar = mtar.PadLeft(3, '0');
            }
            else
            {
                mtar = mtar.PadLeft(2, '0');
            }


            #region VALIDA QUE LA ETIQUETA VERDE EXISTA
            if (mtip == "" || mfol == "" || mcod == "" || mtar == "")
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>EXISTENCIA NO DISPONIBLE</font>"));
                alertDialog.SetIcon(Resource.Drawable.nota);
                alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>La Tarima Actual No Cuenta con Existencia Disponible, Favor de Depurar los folios correspondientes y volver a leer</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                return;
            }
            #endregion


            string CadenaFolios = "Select Eti_Lectura, fecha_cap From tb_Det_Etiqueta " +
                                   "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = " + Convert.ToInt32(mtar) + ") AND Estatus = 'A'";
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            SqlDataAdapter da = new SqlDataAdapter(CadenaFolios, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Foliosleidos");


            Foliosleidos = ds.Tables["Foliosleidos"];
            thisConnection.Close();


            string CadenaFoliospreesplit = "Select Eti_Lectura, fecha_cap From Tb_Det_Etiqueta_Presplit " +
                           "WHERE (Eti_Producto = '" + mcod + "') AND (Eti_Recibo = '" + mfol + "') AND (Eti_TarIni = " + Convert.ToInt32(mtar) + ") AND Estatus IN ('A', 'S')";
            thisConnection.Open();
            SqlDataAdapter dapre = new SqlDataAdapter(CadenaFoliospreesplit, thisConnection);
            DataSet dspre = new DataSet();
            dapre.Fill(dspre, "FoliosleidosPresplit");
            FoliosleidosPresplit = dspre.Tables["FoliosleidosPresplit"];
            thisConnection.Close();

            string cadenatarimacompleta = "";

            if (mtip == "PTP")
            {
                cadenatarimacompleta = "SELECT (num_cajas - CAJAS_SUR) AS DISPONIBLE FROM TB_DET_ETI_FINAL WHERE CVE_PROD = '" + mcod.Trim() + "' AND FOLIO = '" + mfol.Trim() + "' " +
            "AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";

            }
            else
            {
                cadenatarimacompleta = "SELECT (etiqueta - surtido) AS DISPONIBLE FROM TB_DET_TRAZABILIDAD WHERE PROD_CLAVE = '" + mcod.Trim() + "' AND RECIBO = '" + mfol.Trim() + "' " +
                 "AND TIPO = '" + mtip + "' AND TARIMA = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' ";
            }

            thisConnection.Open();
            SqlCommand cmd = new SqlCommand(cadenatarimacompleta, thisConnection);
            int disponible = Convert.ToInt32(cmd.ExecuteScalar());

            thisConnection.Close();

            //string strEti_Lectura = "SELECT COUNT(*) as Eti_Lectura FROM (SELECT Eti_Lectura FROM tb_Det_Etiqueta WHERE Eti_Producto = '" + mcod.Trim() + "' AND Eti_Recibo = '" + mfol.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' AND Estatus = 'A' UNION SELECT Eti_Lectura FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Producto = '" + mcod.Trim() + "' AND Eti_Recibo = '" + mfol.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(mtar.Trim()).ToString() + "' AND Estatus IN ('A', 'S')) AS Eti_Lectura\r\n";
            string strEti_Lectura = "SELECT sum(CAJA) AS CAJAS FROM (SELECT COUNT(Eti_Lectura) AS CAJA FROM tb_Det_Etiqueta WHERE Eti_Producto = '" + mcod.Trim() + "' AND Eti_Recibo = '" + mfol.Trim() + "' AND Eti_TarIni = " + Convert.ToInt32(mtar.Trim()) + " AND Estatus = 'A' UNION ALL SELECT COUNT(Eti_Lectura) as caja FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Producto = '" + mcod.Trim() + "' AND Eti_Recibo = '" + mfol.Trim() + "' AND Eti_TarIni = " + Convert.ToInt32(mtar.Trim()) + " AND Estatus = 'S' UNION ALL SELECT SUM(cajas) as caja FROM tb_det_split WHERE prod_clave = '" + mcod.Trim() + "' AND no_lote = '" + mfol.Trim() + "' AND TARINI = '" + mtar + "' AND Estatus = 'A')   AS Eti_Lectura";
            thisConnection.Open();
            SqlCommand cmdEti_Lectura = new SqlCommand(strEti_Lectura, thisConnection);
            int totalEti_Lectura = Convert.ToInt32(cmdEti_Lectura.ExecuteScalar());

            thisConnection.Close();




            if ((disponible > 0) || (totalEti_Lectura < Convert.ToInt32(mEtiqueta)))
            {
                total_caja_verde = 0;
                disponible++;
                int n = 1;
                int cajaactual = 1;
                // CÓDIGO CORREGIDO
                // mEtiqueta contiene el total de cajas de la tarima desde la BD.
                // Usamos cajaactual como límite absoluto para evitar bucle infinito
                // cuando todas las cajas ya están capturadas.
                int maxCaja = string.IsNullOrEmpty(mEtiqueta)
                    ? disponible + 50
                    : Convert.ToInt32(mEtiqueta) + 1;

                while (n < disponible && cajaactual < maxCaja)
                {
                    if (cajaactual.ToString().Length == 1)
                        mcaj = "00" + cajaactual.ToString();
                    else if (cajaactual.ToString().Length == 2)
                        mcaj = "0" + cajaactual.ToString();
                    else
                        mcaj = cajaactual.ToString();

                    mtip = mtip.Trim();
                    mfol = mfol.Trim();
                    mcod = mcod.Trim();
                    mtar = mtar.Trim();
                    mcaj = mcaj.Trim();

                    string lectura = mtip + mfol + mcod + mtar + mcaj;

                    thisConnection.Open();
                    string fechacap = ValidaCajaEtiVerde(lectura, Foliosleidos).Trim();
                    string fechacappre = ValidaCajaPreesplitVerde(lectura, FoliosleidosPresplit).Trim();
                    thisConnection.Close();

                    if (fechacap.Length > 0)
                    {
                        // Caja ya capturada en embarque → saltar, NO contar como disponible usada
                        cajaactual++;
                        // FIX: n++ aquí para que el bucle termine aunque todas estén tomadas
                        n++;
                    }
                    else if (fechacappre.Length > 0)
                    {
                        // Caja ya en presplit → saltar
                        cajaactual++;
                        // FIX: n++ aquí también
                        n++;
                    }
                    else
                    {
                        // Caja libre → capturar
                        string cad = mtip + " | " + mfol + " | " + mcod + " | " + mtar + " | " + mcaj;
                        if (await RepetidosAsync(mtip, mfol, mcod, mtar, mcaj) != "S" || disponible > 0)
                        {
                            string lectura2 = (mtip + mfol + mcod + mtar + mcaj).Trim();
                            try
                            {
                                xprod Pedidoscapturados = new xprod
                                {
                                    Tipo = mtip,
                                    Folio = mfol,
                                    Codigo = mcod,
                                    Tarima = mtar,
                                    Cajas = mcaj,
                                    fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                                    tipo_captura = "V",
                                    Lecturabd = lectura2
                                };
                                await db.InsertAsync(Pedidoscapturados);

                                int totalx = await traetotal(mcod);
                                totalx = totalx + 1;

                                var pedidos = await db.QueryAsync<ConPedidos>(
                                    "Select * FROM ConPedidos Where prod_clave = ?", mcod.Trim());

                                string existeprod = "NO";
                                foreach (var pedisur in pedidos)
                                {
                                    await db.QueryAsync<ConPedidos>(
                                        "UPDATE [ConPedidos] SET surtido = ? WHERE prod_clave = ?",
                                        totalx, mcod.ToString());
                                    existeprod = "SI";
                                }

                                if (existeprod == "NO")
                                {
                                    ConPedidos ConsecutivosPedidos = new ConPedidos
                                    {
                                        prod_clave = mcod.ToString(),
                                        nombre = traenom(mcod.ToString().Trim()),
                                        pedido = 0,
                                        surtido = Convert.ToInt16(totalx)
                                    };
                                    await db.InsertAsync(ConsecutivosPedidos);
                                }

                                total_caja_verde++;
                                // FIX: TotCaj y UI deben actualizarse en hilo principal
                                RunOnUiThread(() =>
                                {
                                    TotCaj++;
                                    total.Text = TotCaj.ToString("##0");
                                    listItem.Add(new FlimStarInfo()
                                    {
                                        Name = traenom(mcod.ToString().Trim()),
                                        Age = "Recibo: " + mfol + " Tarima: " + mtar + " Caja: " + mcaj,
                                        ImageID = Resource.Drawable.producto
                                    });
                                });
                            }
                            catch { /* Duplicidad de Lecturabd evitada por constraint UNIQUE */ }
                        }
                        cajaactual++;
                        n++;
                    }
                }
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                //iMPRESION DE MENSAJE QUE INDICARA CUANTO DE CADA TARIMA SE LOGRO CARGAR Y SIMULAR
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>LECTURA POR TARIMA</font>"));
                alertDialog.SetIcon(Resource.Drawable.nota);
                alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>Se han Capturado " + total_caja_verde + " Cajas,  Del Folio " + mfol + " De la tarima " + mtar + " Del Producto " + traenom(mcod.ToString().Trim()) + "</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
            }
            else
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#55F721' size = 10>EXISTENCIA NO DISPONIBLE</font>"));
                alertDialog.SetIcon(Resource.Drawable.nota);
                alertDialog.SetMessage(Html.FromHtml("<font color='#9FFA7A' size = 10>La Tarima Actual No Cuenta con Existencia Disponible, Favor de Depurar los folios correspondientes y volver a leer</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
            }

            // Actualizar GridView con snapshot inmutable (usa el adapter corregido)
            RunOnUiThread(() =>
            {
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                gvObject.Adapter = new myGVItemAdapter(this, listItem);
            });
        }

        public async Task EtiquetasBlancaAsync()
        {
            string ok = "S";
            string er = "S";
            string captura = foliocaptura.Text.Trim();
            int pos = captura.IndexOf("=");
            string totalEtiquetaVerde = "";
            string nombreProducto = "";

            if (pos == -1)
            {
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                return;
            }

            foliocaptura.Text = foliocaptura.Text.Substring(pos + 1, foliocaptura.Text.Length - (pos + 1)).Trim();
            captura = captura.Substring(pos + 1).Trim().Replace("=", "");

            string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mEtiqueta = "", mEtiquetaR = "";
            int mTarima = 0, mCaja = 0;

            #region Buscar Si Producto Existe
            foreach (DataRow row in CatProd.Rows)
            {
                string producto_clave = row["Prod_Clave"].ToString().Trim();
                bool esta = foliocaptura.Text.Contains(producto_clave);

                if (esta)
                {
                    mcod = producto_clave;
                    break;
                }
            }
            #endregion

            #region PROCESO ETIQUETA BLANCA
            int posprod = foliocaptura.Text.Trim().IndexOf(mcod);
            mfol = foliocaptura.Text.Substring(0, posprod).Trim();
            mtip = "PTP";
            string restocaptura = foliocaptura.Text.Replace(mfol, "").Replace(mcod, "");
            if (restocaptura.Length == 6)
            {
                if (mfol.Length == 5)
                {
                    mtip = "PTC";
                }
                mcaj = restocaptura.Substring(3, 3);
                mtar = restocaptura.Substring(0, 3);
            }
            else
            {
                mtip = "PTC";
                mcaj = restocaptura.Substring(4, 3);
                mtar = restocaptura.Substring(0, 2);
            }
            #endregion

            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            //nombreProducto = getNombreProducto(mcod);
            //nombreProducto = traenom(mcod);

            mTarima = Convert.ToInt32(mtar.Trim());
            mcaj = mcaj.Trim();

            mEtiqueta = mEtiqueta.Trim();
            mEtiqueta = mEtiqueta.PadLeft(3, '0');
            totalEtiquetaVerde = Convert.ToString(total_caja_verde);
            totalEtiquetaVerde = totalEtiquetaVerde.PadLeft(3, '0');

            if (mtip == "PTP")
            {
                //mtar = mtar.PadLeft(3, '0');
                mtar = Convert.ToString(mTarima).PadLeft(3, '0');
            }
            else
            {
                //mtar = mtar.PadLeft(2, '0');
                mtar = Convert.ToString(mTarima).PadLeft(2, '0');
            }

            string cad = $"{mtip} | {mfol} | {mcod} | {mtar} | {mcaj}";
            //bool esRepetido = await RepetidoAsync(mtip, mfol, mcod, mtar, mcaj);
            //if (!esRepetido)

            if (await RepetidosAsync(mtip, mfol, mcod, mtar, mcaj) != "S")
            {
                await GuardarEnBaseDeDatosAsync(mtip, mfol, mcod, mtar, mcaj);
                //await ActualizarTotalAsync(mcod);

                RunOnUiThread(() =>
                {
                    TotCaj++;
                    total.Text = TotCaj.ToString("##0");

                    listItem.Add(new FlimStarInfo()
                    {
                        Name = traenom(mcod),
                        Age = $"Recibo: {mfol} Tarima: {mtar} Caja: {mcaj}",
                        ImageID = Resource.Drawable.producto
                    });

                    List<FlimStarInfo> lstFlimStar = listItem;
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                });
            }
            LimpiarCampo();
        }

        public async Task EtiquetasBlancaAsyncOG2()
        {
            string ok = "S";
            string er = "S";
            string captura = foliocaptura.Text.Trim();
            int pos = captura.IndexOf("=");
            string totalEtiquetaVerde = "";
            string nombreProducto = "";

            if (pos == -1)
            {
                LimpiarCampo();
                return;
            }

            foliocaptura.Text = foliocaptura.Text.Substring(pos + 1, foliocaptura.Text.Length - (pos + 1)).Trim();
            captura = captura.Substring(pos + 1).Trim().Replace("=", "");

            string mtip, mfol, mcod, mtar, mcaj, mEtiqueta, mEtiquetaR;

            validarCapturas(captura, out mtip, out mfol, out mcod, out mtar, out mcaj, out mEtiqueta, out mEtiquetaR);

            if (mtip == null || mfol == null || mcod == null || mtar == null || mcaj == null || mEtiqueta == null)
            {
                ProcesarCaptura(captura, out mtip, out mfol, out mcod, out mtar, out mcaj);
                if (mtip == null || mfol == null || mcod == null || mtar == null || mcaj == null)
                {
                    //LimpiarCampo();
                    RunOnUiThread(() =>
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#fa6400' size = 10>Error en la estructura de Etiqueta</font>"));
                        alertDialog.SetIcon(Resource.Drawable.Info);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#fff000' size = 10>Etiqueta no encontrada, por favor tomar una evidencia de la etiqueta leída y ponerse en contacto con los desarrolladores.</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", (sender, e) => alertDialog.Dispose());

                        alertDialog.Show();
                    });
                    return;
                }
            }

            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            //nombreProducto = getNombreProducto(mcod);
            //nombreProducto = traenom(mcod);
            mtar = mtar.Trim();
            mcaj = mcaj.Trim();
            mEtiqueta = mEtiqueta.Trim();
            mEtiqueta = mEtiqueta.PadLeft(3, '0');
            totalEtiquetaVerde = Convert.ToString(total_caja_verde);
            totalEtiquetaVerde = totalEtiquetaVerde.PadLeft(3, '0');

            if (mtip == "PTP")
            {
                mtar = mtar.PadLeft(3, '0');
            }
            else
            {
                mtar = mtar.PadLeft(2, '0');
            }

            string cad = $"{mtip} | {mfol} | {mcod} | {mtar} | {mcaj}";
            //bool esRepetido = await RepetidoAsync(mtip, mfol, mcod, mtar, mcaj);
            //if (!esRepetido)

            if (await RepetidosAsync(mtip, mfol, mcod, mtar, mcaj) != "S")
            {
                await GuardarEnBaseDeDatosAsync(mtip, mfol, mcod, mtar, mcaj);
                //await ActualizarTotalAsync(mcod);

                RunOnUiThread(() =>
                {
                    TotCaj++;
                    total.Text = TotCaj.ToString("##0");

                    listItem.Add(new FlimStarInfo()
                    {
                        Name = traenom(mcod),
                        Age = $"Recibo: {mfol} Tarima: {mtar} Caja: {mcaj}",
                        ImageID = Resource.Drawable.producto
                    });

                    List<FlimStarInfo> lstFlimStar = listItem;
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                });
            }
            LimpiarCampo();
        }

        public async Task EtiquetasBlancaAsyncOG()
        {
            string ok = "S";
            string er = "S";
            string captura = foliocaptura.Text.Trim();
            int pos = captura.IndexOf("=");
            string totalEtiquetaVerde = "";
            string nombreProducto = "";

            if (pos == -1)
            {
                LimpiarCampo();
                return;
            }

            captura = captura.Substring(pos + 1).Trim().Replace("=", "");

            string mtip, mfol, mcod, mtar, mcaj, mEtiqueta, mEtiquetaR;

            validarCapturas(captura, out mtip, out mfol, out mcod, out mtar, out mcaj, out mEtiqueta, out mEtiquetaR);

            if (mtip == null || mfol == null || mcod == null || mtar == null || mcaj == null || mEtiqueta == null)
            {
                ProcesarCaptura(captura, out mtip, out mfol, out mcod, out mtar, out mcaj);
                if (mtip == null || mfol == null || mcod == null || mtar == null || mcaj == null)
                {
                    LimpiarCampo();
                    RunOnUiThread(() =>
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#fa6400' size = 10>Error en la estructura de Etiqueta</font>"));
                        alertDialog.SetIcon(Resource.Drawable.Info);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#fff000' size = 10>Etiqueta no encontrada, por favor tomar una evidencia de la etiqueta leída y ponerse en contacto con los desarrolladores.</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", (sender, e) => alertDialog.Dispose());

                        alertDialog.Show();
                    });
                    return;
                }
            }

            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            nombreProducto = getNombreProducto(mcod);
            mtar = mtar.Trim();
            mcaj = mcaj.Trim();
            mEtiqueta = mEtiqueta.Trim();
            mEtiqueta = mEtiqueta.PadLeft(3, '0');
            totalEtiquetaVerde = Convert.ToString(total_caja_verde);
            totalEtiquetaVerde = totalEtiquetaVerde.PadLeft(3, '0');

            if (mtip == "PTP")
            {
                mtar = mtar.PadLeft(3, '0');
            }
            else
            {
                mtar = mtar.PadLeft(2, '0');
            }

            string cad = $"{mtip} | {mfol} | {mcod} | {mtar} | {mcaj}";
            //bool esRepetido = await RepetidoAsync(mtip, mfol, mcod, mtar, mcaj);
            //if (!esRepetido)

            // Define la consulta
            var query = "SELECT COUNT(ID) AS Cajas FROM xProd WHERE Folio = ? AND Codigo = ? AND Tarima = ?";

            // Ejecuta la consulta
            var result = await db.QueryAsync<xLote>(query, mfol, mcod, mtar);

            string totalProdTar = "0";

            // Asegúrate de que hay al menos un resultado
            if (result.Count > 0)
            {
                // Obtén el primer resultado
                var countResult = result[0];

                // Convierte el valor de COUNT a string
                // Asume que xLote tiene una propiedad Cajas de tipo int que almacena el resultado
                totalProdTar = countResult.Cajas.ToString();
            }
            /*else
            {
                // Manejo en caso de que no haya resultados
                totalProdTar = "0";
            }*/

            if (await RepetidosAsync(mtip, mfol, mcod, mtar, mcaj) != "S")
            {
                if (Convert.ToInt32(mcaj) <= Convert.ToInt32(mEtiquetaR) && Convert.ToInt32(totalProdTar) <= Convert.ToInt32(mEtiqueta))
                {
                    try
                    {
                        string lectura = $"{mtip}{mfol}{mcod}{mtar}{mcaj}".Trim();
                        string fechacap = ValidaCaja(lectura, mfol, mcod, mtar, mcaj).Trim();
                        string embcap = ValidaEmb(lectura).Trim();

                        if (fechacap.Length == 0)
                        {
                            await GuardarEnBaseDeDatosAsync(mtip, mfol, mcod, mtar, mcaj);
                            //await ActualizarTotalAsync(mcod);

                            RunOnUiThread(() =>
                            {
                                TotCaj++;
                                total.Text = TotCaj.ToString("##0");

                                listItem.Add(new FlimStarInfo()
                                {
                                    Name = nombreProducto,
                                    Age = $"Recibo: {mfol} Tarima: {mtar} Caja: {mcaj}",
                                    ImageID = Resource.Drawable.producto
                                });

                                //ActualizarGridView();
                                List<FlimStarInfo> lstFlimStar = listItem;
                                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                            });
                        }
                        else
                        {
                            await db.ExecuteAsync("DELETE FROM Mensajes");

                            var mensa = new Mensajes
                            {
                                titulo = "Etiqueta ya capturada",
                                mensaje = $"Error Etiqueta YA FUE CAPTURADA!! \n\r{mtip} | {mfol} | {mcod} | {mtar} | {mcaj}\n\rDía {fechacap}\n\rEmbarque {embcap}\n\r{traenom(mcod)}"
                            };

                            var mensajes = await db.GetItemsAsync<Mensajes>();
                            bool existeMensaje = mensajes.Any(m => m.titulo == mensa.titulo && m.mensaje == mensa.mensaje);

                            if (!existeMensaje)
                            {
                                await db.InsertAsync(mensa);
                            }

                            EtiquetaCapturada = "N";
                            ok = "N";
                            er = "S";
                            await ImprimirDialogsAsync(0);
                        }
                    }
                    catch
                    {
                        //RunOnUiThread(() => Toast.MakeText(this, "Duplicidad Evitada", ToastLength.Short).Show());
                    }
                }
                /*else
                {
                    RunOnUiThread(() => Toast.MakeText(this, "Caja Mayor Al Numero De Etiquetas", ToastLength.Short).Show());
                }*/
            }
            /*else
            {
                RunOnUiThread(() => Toast.MakeText(this, "Duplicidad Evitada", ToastLength.Short).Show());
            }*/

            LimpiarCampo();
        }

        private async Task<bool> ExistenMensajesAsync(Mensajes mensa)
        {
            // Obtener la lista de mensajes de forma asincrónica
            var mensajes = await db.GetItemsAsync<Mensajes>();

            // Verificar si existe algún mensaje que cumpla con la condición
            bool existeMensaje = mensajes.Any(m => m.titulo == mensa.titulo && m.mensaje == mensa.mensaje);

            return existeMensaje;
        }

        #region NUEVOS METODOS PARA LA VALIDACION DE ETIQUETAS DE TRAZABILIDAD (BLANCA)
        private void LimpiarCampo()
        {
            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            foliocaptura.RequestFocus();
            valorfinal = foliocaptura.Text;
        }

        private void validarCapturas(string captura, out string mtip, out string mfol, out string mcod, out string mtar, out string mcaj, out string mEtiqueta, out string mEtiquetaR)
        {
            string folioCaptura = getPTI_Clave(captura);
            mtip = mfol = mcod = mtar = mcaj = mEtiqueta = mEtiquetaR = null;

            if (string.IsNullOrEmpty(captura) || captura.Length < 3)
            {
                //LimpiarCampo();
                //Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                //alertDialog.SetTitle(Html.FromHtml("<font color='#fa6400' size = 10>Error en la estructura de Etiqueta</font>"));
                //alertDialog.SetIcon(Resource.Drawable.Info);
                //alertDialog.SetMessage(Html.FromHtml("<font color='#fff000' size = 10>Etiqueta no encontrada por favor tomar una evidencia de la etiqueta leida y ponerse en contacto con los desarrolladores.</font>"));
                //alertDialog.SetCancelable(false);
                //alertDialog.SetNeutralButton("Ok", delegate
                //{
                //    alertDialog.Dispose();
                //});

                //RunOnUiThread(() => alertDialog.Show());

                return;
            }

            mcaj = captura.Substring(captura.Length - 3, 3);
            captura = captura.Substring(0, captura.Length - 3);

            //string querySSCC = "SELECT recibo, tarima, prod_clave, tipo, etiqueta FROM tb_det_trazabilidad WHERE pti_clave = @captura";
            string querySSCC = "SELECT recibo, tarima, prod_clave, tipo, etiqueta, CAST(etiqueta AS INT) + ISNULL(ConseReimp, 0) AS mEtiquetaR FROM tb_det_trazabilidad WHERE pti_clave = @captura";

            using (SqlConnection thisConnection = new SqlConnection(cadenaConexion))
            {
                thisConnection.Open();

                using (SqlCommand sqlCommand = new SqlCommand(querySSCC, thisConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@captura", captura.Trim());

                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        if (sqlDataReader.HasRows)
                        {
                            while (sqlDataReader.Read())
                            {
                                mfol = sqlDataReader["recibo"].ToString().Trim();
                                mtar = sqlDataReader["tarima"].ToString().Trim();
                                mcod = sqlDataReader["prod_clave"].ToString().Trim();
                                mtip = sqlDataReader["tipo"].ToString().Trim();
                                mEtiqueta = sqlDataReader["etiqueta"].ToString().Trim();
                                mEtiquetaR = sqlDataReader["mEtiquetaR"].ToString().Trim();
                            }
                        }
                        else
                        {
                            validarCapturaFolio(folioCaptura, out mtip, out mfol, out mcod, out mtar, out mEtiqueta, out mEtiquetaR);
                            //LimpiarCampo();
                            //Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            //alertDialog.SetTitle(Html.FromHtml("<font color='#fa6400' size = 10>Error en la estructura de Etiqueta</font>"));
                            //alertDialog.SetIcon(Resource.Drawable.Info);
                            //alertDialog.SetMessage(Html.FromHtml("<font color='#fff000' size = 10>Etiqueta no encontrada por favor tomar una evidencia de la etiqueta leida y ponerse en contacto con los desarrolladores.</font>"));
                            //alertDialog.SetCancelable(false);
                            //alertDialog.SetNeutralButton("Ok", delegate
                            //{
                            //    alertDialog.Dispose();
                            //});

                            //RunOnUiThread(() => alertDialog.Show());

                            return;
                        }
                    }
                }
            }
        }

        public void validarCapturaFolio(string captura, out string mtip, out string mfol, out string mcod, out string mtar, out string mEtiqueta, out string mEtiquetaR)
        {
            mtip = mfol = mcod = mtar = mEtiqueta = mEtiquetaR = null;

            //string querySSCC = "SELECT recibo, tarima, prod_clave, tipo, etiqueta FROM tb_det_trazabilidad WHERE pti_clave = @captura";
            string querySSCC = "SELECT recibo, tarima, prod_clave, tipo, etiqueta, CAST(etiqueta AS INT) + ISNULL(ConseReimp, 0) AS mEtiquetaR FROM tb_det_trazabilidad WHERE pti_clave = @captura";

            using (SqlConnection thisConnection = new SqlConnection(cadenaConexion))
            {
                thisConnection.Open();

                using (SqlCommand sqlCommand = new SqlCommand(querySSCC, thisConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@captura", captura.Trim());

                    using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                    {
                        if (sqlDataReader.HasRows)
                        {
                            while (sqlDataReader.Read())
                            {
                                mfol = sqlDataReader["recibo"].ToString().Trim();
                                mtar = sqlDataReader["tarima"].ToString().Trim();
                                mcod = sqlDataReader["prod_clave"].ToString().Trim();
                                mtip = sqlDataReader["tipo"].ToString().Trim();
                                mEtiqueta = sqlDataReader["etiqueta"].ToString().Trim();
                                mEtiquetaR = sqlDataReader["mEtiquetaR"].ToString().Trim();
                            }
                        }
                        else
                        {
                            //LimpiarCampo();
                            //Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            //alertDialog.SetTitle(Html.FromHtml("<font color='#fa6400' size = 10>Error en la estructura de Etiqueta</font>"));
                            //alertDialog.SetIcon(Resource.Drawable.Info);
                            //alertDialog.SetMessage(Html.FromHtml("<font color='#fff000' size = 10>Etiqueta no encontrada por favor tomar una evidencia de la etiqueta leida y ponerse en contacto con los desarrolladores.</font>"));
                            //alertDialog.SetCancelable(false);
                            //alertDialog.SetNeutralButton("Ok", delegate
                            //{
                            //    alertDialog.Dispose();
                            //});

                            //RunOnUiThread(() => alertDialog.Show());

                            return;
                        }
                    }
                }
            }
        }

        private string getPTI_Clave(string codigoEtiqueta)
        {
            string result = "";
            // Paso 1: Definir el patrón de la expresión regular
            string pattern = @"^(.{15})(\d{3})\d{3}$";
            // Paso 2: Aplicar la expresión regular a la cadena de entrada
            Match match = Regex.Match(codigoEtiqueta, pattern);

            // Paso 3: Verificar si la expresión regular encontró una coincidencia
            if (match.Success)
            {
                // Paso 4: Capturar los grupos de interés
                string prefix = match.Groups[1].Value; // Los primeros 15 caracteres
                string lastThreeDigits = match.Groups[2].Value; // Los 3 dígitos de interés
                //lastThreeDigits = Regex.Replace(lastThreeDigits, "0", "");
                // Paso 5: Transformar los dígitos según la lógica proporcionada
                // Aquí parece que la transformación correcta está basada en patrones específicos para cada ejemplo.
                //string transformedDigits = lastThreeDigits[0] + "" + lastThreeDigits[1] + lastThreeDigits[2] + lastThreeDigits[1];
                string transformedDigits = "" + lastThreeDigits[1] + lastThreeDigits[2] + lastThreeDigits[1] + lastThreeDigits[2];

                // Paso 6: Construir la cadena resultante
                result = prefix + transformedDigits;
                //result = prefix + lastThreeDigits + lastThreeDigits;
            }

            return result;
        }

        private void ProcesarCaptura(string captura, out string mtip, out string mfol, out string mcod, out string mtar, out string mcaj)
        {
            mtip = mfol = mcod = mtar = mcaj = null;
            int tam = captura.Length;

            Int32 ValorFolio = Convert.ToInt32(captura.Substring(0, 6));
            mtip = (ValorFolio > FolioCampo) ? "S" : "N";

            if (mtip == "N")
            {
                mcaj = captura.Substring(tam - 3, 3);
                mtar = captura.Substring(tam - 6, 3);
                int tam2 = tam - 6;
                mtip = "PTP";

                if (tam2 <= 14) // Etiqueta de Aguilares	
                {
                    mfol = captura.Substring(0, 4);
                    mcod = captura.Substring(4, tam - 10);
                }
                else
                {
                    mfol = captura.Substring(0, 5);
                    mcod = captura.Substring(5, tam - 11);
                }

                if (traenom(mcod) == "")
                {
                    mfol = captura.Substring(0, 6);
                    mcod = captura.Substring(6, tam - 12);
                    mtip = "PTP";
                }

                if (traenom(mcod) == "")
                {
                    mcaj = captura.Substring(tam - 2, 2);
                    mtar = captura.Substring(tam - 4, 2);
                    mfol = captura.Substring(0, 6);
                    mcod = captura.Substring(6, tam - 10);
                    mtip = "PTC";
                }

                if (traenom(mcod) == "")
                {
                    mcaj = captura.Substring(tam - 3, 3);
                    mtar = captura.Substring(tam - 6, 3);
                    mfol = captura.Substring(0, 5);
                    mcod = captura.Substring(5, tam - 11);
                    mtip = "PTC";
                }
            }
            else
            {
                mcaj = captura.Substring(tam - 3, 3);
                mtar = captura.Substring(tam - 7, 2);
                mfol = captura.Substring(0, 6);
                mcod = captura.Substring(6, tam - 13);
                mtip = "PTC";
            }
        }

        private string ValidaCaja(string cadena, string recibo, string producto, string tarima, string caja)
        {
            string fechaCap = "";

            try
            {
                if (thisConnection.State == ConnectionState.Closed)
                    thisConnection.Open();

                //string query = "SELECT fecha_cap FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Lectura = '" + cadena + "' AND Estatus != 'C'";
                string query = "SELECT fecha_cap FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = '" + recibo + "' AND Eti_Producto = '" + producto + "' AND Eti_TarIni = '" + tarima + "' AND Eti_Caja = '" + caja + "' AND Estatus != 'C'";
                SqlCommand cmd = new SqlCommand(query, thisConnection);
                //cmd.Parameters.AddWithValue("@cadena", cadena);

                fechaCap = Convert.ToString(cmd.ExecuteScalar());
                /*object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    fechaCap = result.ToString();
                }*/
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejar excepciones aquí
            }
            finally
            {
                if (thisConnection.State == ConnectionState.Open)
                    thisConnection.Close();
            }

            return fechaCap;
        }

        private string ValidasCaja(string cadena, string recibo, string producto, string tarima, string caja)
        {
            string fechaCap = "";

            try
            {
                if (thisConnection.State == ConnectionState.Closed)
                    thisConnection.Open();

                string query = "SELECT fecha_cap FROM Tb_Det_Etiqueta_Presplit WHERE Eti_Recibo = @recibo AND Eti_Producto = @producto AND Eti_TarIni = @tarima AND Eti_Caja = @caja AND Estatus != 'C'";
                using (SqlCommand cmd = new SqlCommand(query, thisConnection))
                {
                    cmd.Parameters.AddWithValue("@recibo", recibo);
                    cmd.Parameters.AddWithValue("@producto", producto);
                    cmd.Parameters.AddWithValue("@tarima", tarima);
                    cmd.Parameters.AddWithValue("@caja", caja);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        fechaCap = result.ToString();
                    }
                }
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejar excepciones aquí, por ejemplo, registrándolas en un log

                Toast.MakeText(this, "Error al validar la caja (T-SQL): " + ex.Message, ToastLength.Short).Show();
            }
            finally
            {
                if (thisConnection.State == ConnectionState.Open)
                    thisConnection.Close();
            }

            return fechaCap;
        }

        private async Task<bool> RepetidoAsync(string mtip, string mfol, string mcod, string mtar, string mcaj)
        {
            // Construir la cadena de lectura
            string lectura = (mtip + mfol + mcod + mtar + mcaj).Trim();

            // Obtener la lista de productos capturados de forma asincrónica
            var productos = await db.GetItemsAsync<xprod>();

            // Verificar si existe algún producto que cumpla con la condición
            return productos.Any(p => p.Lecturabd == lectura);
        }

        private async Task GuardarEnBaseDeDatosAsync(string mtip, string mfol, string mcod, string mtar, string mcaj)
        {
            string lectura = (mtip + mfol + mcod + mtar + mcaj).Trim();

            xprod nuevaCaja = new xprod
            {
                Tipo = mtip,
                Folio = mfol,
                Codigo = mcod,
                Tarima = mtar,
                Cajas = mcaj,
                fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                Lecturabd = lectura
            };
            await db.InsertAsync(nuevaCaja);

            int totalx = await traetotal(mcod);
            totalx++;

            // FIX: await correcto, no .Result
            var pedidos = await db.QueryAsync<ConPedidos>(
                "SELECT * FROM ConPedidos WHERE prod_clave = ?", mcod.Trim());

            if (pedidos.Count > 0)
            {
                await db.ExecuteAsync(
                    "UPDATE [ConPedidos] SET surtido = ? WHERE prod_clave = ?",
                    totalx, mcod.Trim());
            }
            else
            {
                ConPedidos nuevo = new ConPedidos
                {
                    prod_clave = mcod,
                    nombre = traenom(mcod.Trim()),
                    pedido = 0,
                    surtido = Convert.ToInt16(totalx)
                };
                await db.InsertAsync(nuevo);
            }
        }

        private async Task GuardarEnBaseDeDatosAsyncOG(string mtip, string mfol, string mcod, string mtar, string mcaj)
        {
            string lectura = (mtip + mfol + mcod + mtar + mcaj).Trim();

            // Crear el objeto xprod
            xprod Pedidoscapturados = new xprod
            {
                Tipo = mtip,
                Folio = mfol,
                Codigo = mcod,
                Tarima = mtar,
                Cajas = mcaj,
                fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                Lecturabd = lectura
            };

            // Insertar en la base de datos de forma asincrónica
            await db.InsertAsync(Pedidoscapturados);

            int totalx = await traetotal(mcod);

            totalx = totalx + 1;

            string existeprod = "NO";
            var pedidos = db.GetItemsAsync<ConPedidos>();

            foreach (var pedisur in pedidos.Result)
            {
                if (pedisur.prod_clave.ToString().Trim() == mcod.ToString().Trim())
                {
                    existeprod = "SI";
                }
            }


            if (existeprod == "SI")
            {
                await db.QueryAsync<ConPedidos>("UPDATE [ConPedidos] SET surtido = '" + totalx + "' WHERE prod_clave = '" + mcod.ToString() + "'");
            }
            else
            {
                ConPedidos ConsecutivosPedidos = new ConPedidos { prod_clave = mcod.ToString(), nombre = traenom(mcod.ToString().Trim()), pedido = 0, surtido = Convert.ToInt16(totalx) };
                await db.InsertAsync(ConsecutivosPedidos);
            }
        }

        private async Task ActualizarTotalAsync(string mcod)
        {
            // Obtener el total actual de forma asincrónica
            int totalActual = await traetotal(mcod);
            int totalx = totalActual + 1;

            // Actualizar el campo 'surtido' en ConPedidos
            await db.ExecuteAsync("UPDATE [ConPedidos] SET surtido = ? WHERE prod_clave = ?", totalx, mcod.Trim());

            // Insertar un nuevo registro si es necesario
            if (totalx == 1)
            {
                ConPedidos ConsecutivosPedidos = new ConPedidos
                {
                    prod_clave = mcod,
                    nombre = traenom(mcod.Trim()),
                    pedido = 0,
                    surtido = totalx
                };

                await db.InsertAsync(ConsecutivosPedidos);
            }
        }

        private string ValidasEmb(string cadena)
        {
            string embFolio = "";

            try
            {
                if (thisConnection.State == ConnectionState.Closed)
                    thisConnection.Open();

                string query = "SELECT emb_folio FROM tb_Det_Etiqueta WHERE Eti_Lectura = '" + cadena + "'";
                SqlCommand cmd = new SqlCommand(query, thisConnection);
                //cmd.Parameters.AddWithValue("@cadena", cadena);

                embFolio = Convert.ToString(cmd.ExecuteScalar());
                /*object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    embFolio = result.ToString();
                }*/
            }
            catch (Java.Lang.Exception ex)
            {
                // Manejar excepciones aquí
            }
            finally
            {
                if (thisConnection.State == ConnectionState.Open)
                    thisConnection.Close();
            }

            return embFolio;
        }

        private void ActualizarGridView()
        {
            //TotCaj++;
            //total.Text = TotCaj.ToString("##0");

            List<FlimStarInfo> lstFlimStar = listItem;
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
        }

        #endregion
        private async Task<string> validafecadMod()
        {
            string Valor = "";
            ValiFechacad = "S";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            var productoscapturados = await db.QueryAsync<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            await db.QueryAsync<XLoteSug>("delete from[XLoteSug]");

            var allItems = await db.GetItemsAsync<xLote>();
            int count = allItems.Count;
            int[] validados = new int[count + 1];
#pragma warning disable CS0219 // La variable 'capturas' está asignada pero su valor nunca se usa
            int capturas = 0;
#pragma warning restore CS0219 // La variable 'capturas' está asignada pero su valor nunca se usa
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
#pragma warning disable CS0168 // La variable 'totalusadas' se ha declarado pero nunca se usa
                int totalusadas;
#pragma warning restore CS0168 // La variable 'totalusadas' se ha declarado pero nunca se usa
                int simulador = 0;
                int totaldis = 0;
#pragma warning disable CS0219 // La variable 'fechaant' está asignada pero su valor nunca se usa
                string fechaant = "";
#pragma warning restore CS0219 // La variable 'fechaant' está asignada pero su valor nunca se usa
                int totaldisreal = 0;
                int totalprodsimulado = 0;

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());

                }

                int resttotal = await traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;
                totalprodsimulado = totalpro;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
#pragma warning disable CS0219 // La variable 'todobien' está asignada pero su valor nunca se usa
                string todobien = "OK";
#pragma warning restore CS0219 // La variable 'todobien' está asignada pero su valor nunca se usa
#pragma warning disable CS0219 // La variable 'prod_cap' está asignada pero su valor nunca se usa
                int prod_cap = 0;
#pragma warning restore CS0219 // La variable 'prod_cap' está asignada pero su valor nunca se usa
                int usadas = 0;
#pragma warning disable CS0219 // La variable 'existefecant' está asignada pero su valor nunca se usa
                int existefecant = 0;
#pragma warning restore CS0219 // La variable 'existefecant' está asignada pero su valor nunca se usa
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();
#pragma warning disable CS0219 // La variable 'diacadant' está asignada pero su valor nunca se usa
                string diacadant = "";
#pragma warning restore CS0219 // La variable 'diacadant' está asignada pero su valor nunca se usa
#pragma warning disable CS0219 // La variable 'mescadant' está asignada pero su valor nunca se usa
                string mescadant = "";
#pragma warning restore CS0219 // La variable 'mescadant' está asignada pero su valor nunca se usa

                //traer nombre de producto para validar cuantos dias debo aumentar.
                string prodnom = captu.nombre.Trim();
                int diascad = 14;
                if (prodnom.Contains("BETABEL"))
                {
                    diascad = 60;
                }
                else if (prodnom.Contains("AJO"))
                {
                    diascad = 180;
                }
                else if (prodnom.Contains("ADEREZO") || prodnom.Contains("VINAGRETA") || prodnom.Contains("QUESO"))
                {
                    diascad = 90;
                }

                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(fechacad, FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND (num_cajas - cajas_sur) > 0 Order By fecha_cad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];
                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

#pragma warning disable CS0219 // La variable 'foliosAnt' está asignada pero su valor nunca se usa
                string foliosAnt = "";
#pragma warning restore CS0219 // La variable 'foliosAnt' está asignada pero su valor nunca se usa

                foreach (DataRow row in xlote.Rows)
                {
                    int total_prod_simula = totalprodsimulado;
                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                    "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    thisConnection.Open();
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    int TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    int usadasant = 0;
                    thisConnection.Close();

                    row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;

                    if (Convert.ToInt32(row["disponible"]) > 0)
                    {
                        if (totalpro > 0)
                        {

                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);



                            var prodcap = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                            foreach (var capturado in prodcap)
                            {

                                usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                usadasant = usadas;
                                totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                simulador = simulador + totaldis;
                                totalpro = totalpro - usadas;
                                totaldisponibles = totaldisponibles + totaldis;
                                totaldisreal = totaldis;
                            }

                            if (totaldis > 0 && totalpro > 0)
                            {
                                var prodcapfecad = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND diacad = '" + diacad + "'AND mescad = '" + mescad + "'");

                                foreach (var capturado in prodcapfecad)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim()) - usadasant;
                                    totaldis = Convert.ToInt32(totaldis) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }
                            }

                            if (totaldis > 0)
                            {
                                if (totalpro > 0)
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldisreal, Cajasusadas = usadas, foliomens = "" };
                                    await db.InsertAsync(sugeridos);

                                    break;

                                }

                            }

                            //diacadant = diacad;
                            //mescadant = mescad;



                        }
                        else
                        {
                            break;
                        }
                    }

                }

                var loteSug = await db.QueryAsync<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");
                foreach (var capturado in loteSug)
                {
                    string recibosug = capturado.recibosug;
                    string fecrecsug = capturado.fecrecsug;
                    string cveprod = capturado.cveprod;
                    string tarima = capturado.Tarima;
                    int cajasdis = capturado.Cajasdis;
                    int cajasusadas = capturado.Cajasusadas;
                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del" + capturado.fecrecsug };
                    await db.InsertAsync(mensa);
                    ValiFechacad = "N";

                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                    await db.InsertAsync(sugeridosact);
                    totalpro = 0;
                }


            }


            return Valor;


        }

        private void CancelAction(object sender, DialogClickEventArgs e)
        {
            RunOnUiThread(() =>
            { Guardar.Enabled = false; });
            return;
        }

        private void SaveName(object sender, DialogClickEventArgs e)
        {
            //nombre_recibido = et.Text.Trim().ToUpper();

            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            string cadena = "Select usuario,password From tb_Autoriza_OdeP Where password = '" + et.Text.Trim().ToUpper() + "' AND clave = 'EM' AND obs = 'Autoriza Caducidad'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            mAutoriza = Convert.ToString(cmd.ExecuteScalar());
            if (mAutoriza.Trim().Length == 0)
            {
                Toast.MakeText(this, "PASSWORD INCORRECTO!!!", ToastLength.Short).Show();
                thisConnection.Close();
                Guardar.Enabled = false;
            }
            else
            {
                if (mAutoriza.Trim() == "USER X")
                {

                    cadena = "SELECT DATENAME(dw, getdate())";
                    cmd = new SqlCommand(cadena, thisConnection);
                    string diadelasemana = Convert.ToString(cmd.ExecuteScalar());

                    if (diadelasemana == "Domingo")
                    {
                        thisConnection.Close();
                        AutoPed = "S";
                        Guardar.Enabled = true;
                        return;
                    }
                    else
                    {
                        cadena = "SELECT Convert(varchar(8),GetDate(), 108) HoraServidor";
                        cmd = new SqlCommand(cadena, thisConnection);
                        string horasemana = Convert.ToString(cmd.ExecuteScalar());

                        if ((Convert.ToDateTime(horasemana) > Convert.ToDateTime("22:45:00")) || (Convert.ToDateTime(horasemana) < Convert.ToDateTime("07:15:00")))
                        {
                            thisConnection.Close();
                            AutoPed = "S";
                            Guardar.Enabled = true;



                            AlertDialog.Builder ad = new AlertDialog.Builder(this);

                            string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };

                            ad.SetTitle("Motivo de Folio Adelantado");
                            ad.SetCancelable(false);
                            ad.SetSingleChoiceItems(items, 0, new EventHandler<DialogClickEventArgs>(delegate (object senderx, DialogClickEventArgs erre)
                            {
                                // Get reference to AlertDialog
                                var d = (senderx as Android.App.AlertDialog);

                                // Do something with selected index
                                Toast.MakeText(this, $"Seleccionado: {items[erre.Which]}", ToastLength.Short).Show();
                                motfolade = items[erre.Which].Trim();

                                //Dismiss Dialog
                                d.Dismiss();
                                //return;
                            }));
                            ad.SetPositiveButton("OK", delegate { });
                            ad.Show();

                            return;
                        }
                        else
                        {
                            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                            alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Usuario No Disponible para Autorizar Folio Adelantado</font>"));
                            alertDialog.SetIcon(Resource.Drawable.no);
                            alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El Usuario X estara disponible de 11:00 pm a 7:00 am para realizar Autorizaciones, Por favor Acuda con los encargados en turno</font>"));
                            alertDialog.SetCancelable(false);
                            alertDialog.SetNeutralButton("Ok", delegate
                            {
                                alertDialog.Dispose();
                            });
                            alertDialog.Show();

                        }
                    }
                }
                else
                {
                    thisConnection.Close();
                    AutoPed = "S";
                    Guardar.Enabled = true;


                    AlertDialog.Builder ad = new AlertDialog.Builder(this);

                    string[] items = new string[] { "Requerido Por Cliente", "Caja Inexistente", "Caja No Encontrada", "No Apto Para Carga" };

                    ad.SetTitle("Motivo de Folio Adelantado");
                    ad.SetCancelable(false);
                    ad.SetSingleChoiceItems(items, 0, new EventHandler<DialogClickEventArgs>(delegate (object senderx, DialogClickEventArgs erre)
                    {
                        // Get reference to AlertDialog
                        var d = (senderx as Android.App.AlertDialog);

                        // Do something with selected index
                        Toast.MakeText(this, $"Seleccionado: {items[erre.Which]}", ToastLength.Short).Show();
                        motfolade = items[erre.Which].Trim();

                        //Dismiss Dialog
                        d.Dismiss();
                        //return;
                    }));

                    ad.SetPositiveButton("OK", delegate { });
                    ad.Show();
                    return;
                }
            }

        }

        public async Task eliminaretiquetablanca()
        {
            string captura = foliocaptura.Text.Trim();
            int pos = captura.IndexOf("=");

            if (pos == -1)
            {
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                return;
            }

            foliocaptura.Text = foliocaptura.Text.Substring(pos + 1, foliocaptura.Text.Length - (pos + 1)).Trim();
            captura = captura.Substring(pos + 1).Trim().Replace("=", "");

            string mtip = "", mfol = "", mcod = "", mtar = "", mcaj = "", mEtiqueta = "", mEtiquetaR = "";
            int mTarima = 0, mCaja = 0;

            #region Buscar Si Producto Existe
            foreach (DataRow row in CatProd.Rows)
            {
                string producto_clave = row["Prod_Clave"].ToString().Trim();
                bool esta = foliocaptura.Text.Contains(producto_clave);

                if (esta)
                {
                    mcod = producto_clave;
                    break;
                }
            }
            #endregion

            #region PROCESO ETIQUETA BLANCA
            int posprod = foliocaptura.Text.Trim().IndexOf(mcod);
            mfol = foliocaptura.Text.Substring(0, posprod).Trim();
            mtip = "PTP";
            string restocaptura = foliocaptura.Text.Replace(mfol, "").Replace(mcod, "");
            if (restocaptura.Length == 6)
            {
                if (mfol.Length == 5)
                {
                    mtip = "PTC";
                }
                mcaj = restocaptura.Substring(3, 3);
                mtar = restocaptura.Substring(0, 3);
            }
            else
            {
                mtip = "PTC";
                mcaj = restocaptura.Substring(4, 3);
                mtar = restocaptura.Substring(0, 2);
            }
            #endregion

            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();

            mTarima = Convert.ToInt32(mtar.Trim());
            mcaj = mcaj.Trim();

            if (mtip == "PTP")
            {
                //mtar = mtar.PadLeft(3, '0');
                mtar = Convert.ToString(mTarima).PadLeft(3, '0');
            }
            else
            {
                //mtar = mtar.PadLeft(2, '0');
                mtar = Convert.ToString(mTarima).PadLeft(2, '0');
            }

            foliocaptura.Text = captura;

            string cad = $"{mtip} | {mfol} | {mcod} | {mtar} | {mcaj}";

            if (await RepetidosAsync(mtip, mfol, mcod, mtar, mcaj) == "S")
            {
                /*var pedidoscapturados = new xprod
                {
                    Tipo = mtip,
                    Folio = mfol,
                    Codigo = mcod,
                    Tarima = mtar,
                    Cajas = mcaj,
                    fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                };

                await db.InsertAsync(pedidoscapturados);*/

                await db.ExecuteAsync("DELETE FROM xprod WHERE Codigo = ? AND Folio = ? AND Tarima = ? AND Cajas = ? AND Tipo = ?", mcod, mfol, mtar, mcaj, mtip);

                int totalx = await traetotal(mcod);
                totalx--;

                var pedidos = await db.QueryAsync<ConPedidos>("SELECT * FROM ConPedidos WHERE prod_clave = ?", mcod);

                foreach (var pedisur in pedidos)
                {
                    await db.ExecuteAsync("UPDATE ConPedidos SET surtido = ? WHERE prod_clave = ?", totalx, mcod);
                }

                //Martes 13 de Agosto de 2024 se comentan las siguientes 4 lineas de codigo para evitar que el usuario tenga que salir de la aplicacion
                //y volver a entrar con el fin de agilizar la captura de los productos ya que esto elimina el producto de la lista de Pedidos solicitados por ventas.
                /*if (totalx < 1)
                {
                    await db.ExecuteAsync("DELETE FROM ConPedidos WHERE prod_clave = ?", mcod);
                }*/

                TotCaj--;
                RunOnUiThread(() => total.Text = TotCaj.ToString("##0"));

                //string nombreprod = getNombreProducto(mcod);
                string nombreprod = traenom(mcod);

                foreach (var item in listItem.ToArray())
                {
                    string descrip = $"Recibo: {mfol} Tarima: {mtar} Caja: {mcaj}";
                    //mtar.PadLeft(3, '0')
                    if (item.Name.Trim() == nombreprod.Trim() && item.Age == descrip)
                    {
                        listItem.Remove(item);
                    }
                }
            }

            await ActualizarEstatusEtiquetaAsync(mfol, mcod, mtar, mcaj);

            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            foliocaptura.RequestFocus();
            valorfinal = foliocaptura.Text;

            RunOnUiThread(() =>
            {
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
                gvObject.Adapter = new myGVItemAdapter(this, listItem);
            });

            //LimpiarCampo();
        }

        public async Task eliminaretiquetablancaOG()
        {
            string captura = foliocaptura.Text.Trim();
            int pos = captura.IndexOf("=");

            if (pos == -1)
            {
                foliocaptura.SetSelection(0, foliocaptura.Text.Length);
                foliocaptura.RequestFocus();
                valorfinal = foliocaptura.Text;
                return;
            }

            captura = captura.Substring(pos + 1).Trim().Replace("=", "");
            string mtip, mfol, mcod, mtar, mcaj, mEtiqueta, mEtiquetaR;

            validarCapturas(captura, out mtip, out mfol, out mcod, out mtar, out mcaj, out mEtiqueta, out mEtiquetaR);

            if (mtip == null || mfol == null || mcod == null || mtar == null || mcaj == null)
            {
                ProcesarCaptura(captura, out mtip, out mfol, out mcod, out mtar, out mcaj);

                if (mtip == null || mfol == null || mcod == null || mtar == null || mcaj == null)
                {
                    //LimpiarCampo();
                    RunOnUiThread(() =>
                    {
                        Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                        alertDialog.SetTitle(Html.FromHtml("<font color='#fa6400' size = 10>Error en la estructura de Etiqueta</font>"));
                        alertDialog.SetIcon(Resource.Drawable.Info);
                        alertDialog.SetMessage(Html.FromHtml("<font color='#fff000' size = 10>Etiqueta no encontrada, por favor tomar una evidencia de la etiqueta leída y ponerse en contacto con los desarrolladores.</font>"));
                        alertDialog.SetCancelable(false);
                        alertDialog.SetNeutralButton("Ok", (sender, e) => alertDialog.Dispose());

                        alertDialog.Show();
                    });
                    return;
                }
            }

            mtip = mtip.Trim();
            mfol = mfol.Trim();
            mcod = mcod.Trim();
            mtar = mtar.Trim();
            mcaj = mcaj.Trim();
            if (mtip == "PTP")
            {
                mtar = mtar.PadLeft(3, '0');
            }
            else
            {
                mtar = mtar.PadLeft(2, '0');
            }

            foliocaptura.Text = captura;

            string cad = $"{mtip} | {mfol} | {mcod} | {mtar} | {mcaj}";

            if (await RepetidosAsync(mtip, mfol, mcod, mtar, mcaj) == "S")
            {
                /*var pedidoscapturados = new xprod
                {
                    Tipo = mtip,
                    Folio = mfol,
                    Codigo = mcod,
                    Tarima = mtar,
                    Cajas = mcaj,
                    fecha_captura = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                };

                await db.InsertAsync(pedidoscapturados);*/

                await db.ExecuteAsync("DELETE FROM xprod WHERE Codigo = ? AND Folio = ? AND Tarima = ? AND Cajas = ? AND Tipo = ?", mcod, mfol, mtar, mcaj, mtip);

                int totalx = await traetotal(mcod);
                totalx--;

                var pedidos = await db.QueryAsync<ConPedidos>("SELECT * FROM ConPedidos WHERE prod_clave = ?", mcod);

                foreach (var pedisur in pedidos)
                {
                    await db.ExecuteAsync("UPDATE ConPedidos SET surtido = ? WHERE prod_clave = ?", totalx, mcod);
                }

                //Martes 13 de Agosto de 2024 se comentan las siguientes 4 lineas de codigo para evitar que el usuario tenga que salir de la aplicacion
                //y volver a entrar con el fin de agilizar la captura de los productos ya que esto elimina el producto de la lista de Pedidos solicitados por ventas.
                /*if (totalx < 1)
                {
                    await db.ExecuteAsync("DELETE FROM ConPedidos WHERE prod_clave = ?", mcod);
                }*/

                TotCaj--;
                RunOnUiThread(() => total.Text = TotCaj.ToString("##0"));

                //string nombreprod = getNombreProducto(mcod);
                string nombreprod = traenom(mcod);

                foreach (var item in listItem.ToArray())
                {
                    string descrip = $"Recibo: {mfol} Tarima: {mtar} Caja: {mcaj}";
                    //mtar.PadLeft(3, '0')
                    if (item.Name.Trim() == nombreprod.Trim() && item.Age == descrip)
                    {
                        listItem.Remove(item);
                    }
                }
            }

            foliocaptura.SetSelection(0, foliocaptura.Text.Length);
            foliocaptura.RequestFocus();
            valorfinal = foliocaptura.Text;

            var gvObject = FindViewById<GridView>(Resource.Id.gvCtr2);
            gvObject.Adapter = new myGVItemAdapter(this, listItem);

            //LimpiarCampo();
        }

        private async Task<string> RepetidosAsync(string mtip, string mfol, string mcod, string mtar, string mcaj)
        {
            string Ok = "N";
            var productoscapturados = await db.GetItemsAsync<xprod>();
            foreach (var captu in productoscapturados)
            {
                if (mtip == captu.Tipo && mfol == captu.Folio && mcod == captu.Codigo && mtar == captu.Tarima && mcaj == captu.Cajas)
                {
                    Ok = "S";
                    continue;
                }
            }
            return Ok;
        }

        private async Task<int> TraeTotalAsync(string mcod)
        {
            int total = 0;
            var productoscapturados = await db.GetItemsAsync<ConPedidos>();
            foreach (var captu in productoscapturados)
            {
                if (captu.prod_clave == mcod)
                {
                    total = Convert.ToInt32(captu.surtido);
                    break;
                }
            }
            return total;
        }

        public async Task<string> validapreautorizado()
        {
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }

            string okidoki = "NO";
            string cadena = "";
            int totaldis = 0;
            int totaldisponibles = 0;
            string producto;
            string tipoprod;
            var productoscapturados = await db.QueryAsync<xprod>("Select Codigo, Tipo FROM xprod GROUP BY Codigo, Tipo");

            foreach (var captu in productoscapturados)
            {
                tipoprod = captu.Tipo.ToString().Trim();
                producto = captu.Codigo.ToString().Trim();

                if (tipoprod == "PTP")
                {
                    cadena = "SELECT (num_cajas - cajas_sur) - (SELECT COUNT(Fecha) AS Expr1 FROM Tb_Det_Etiqueta_Presplit WHERE(Eti_Recibo = tb_det_eti_final.folio) AND(Eti_Producto = tb_det_eti_final.cve_prod) AND(Eti_TarIni = tb_det_eti_final.tarima) AND(Estatus = 'A')) AS disponible, folio AS recibo, tarima " +
                    "FROM tb_det_eti_final WHERE(preautorizado = 'C') AND(cve_prod = '" + producto.Trim() + "') AND(estatus_sur <> 'S') AND (((num_cajas - cajas_sur) - (SELECT COUNT(Fecha) AS Expr1 FROM Tb_Det_Etiqueta_Presplit WHERE(Eti_Recibo = tb_det_eti_final.folio) AND(Eti_Producto = tb_det_eti_final.cve_prod) AND(Eti_TarIni = tb_det_eti_final.tarima) AND(Estatus = 'A'))) > 0) ORDER BY folio";
                }
                else
                {
                    cadena = "SELECT (etiqueta - surtido) - (SELECT COUNT(Fecha) AS Expr1 FROM Tb_Det_Etiqueta_Presplit WHERE(Eti_Recibo = recibo) AND(Eti_Producto = prod_clave) AND(Eti_TarIni = tarima) AND(Estatus = 'A')) AS disponible, recibo, tarima " +
                    "FROM TB_DET_TRAZABILIDAD WHERE(preautorizado = 'C') AND PROD_CLAVE = '" + producto.Trim() + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND ((etiqueta - surtido) - (SELECT COUNT(Fecha) AS Expr1 FROM Tb_Det_Etiqueta_Presplit WHERE(Eti_Recibo = recibo) AND(Eti_Producto = prod_clave) AND(Eti_TarIni = tarima) AND(Estatus = 'A'))) > 0 ORDER BY recibo";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "lotespreautorizados");
                DataTable lotespreautorizados = ds.Tables["lotespreautorizados"];


                //traigo el total de cajas capturadas para hacer las validaciones correspondientes
                int total_prod_simula = 0;
                var prodcapx = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + producto.Trim() + "'");
                foreach (var capturadox in prodcapx)
                {
                    total_prod_simula = Convert.ToInt32(capturadox.Cajas.ToString().Trim());
                }

                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

                foreach (DataRow row in lotespreautorizados.Rows)
                {

                    if (total_prod_simula > 0)
                    {
                        var prodcap = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + producto.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");
                        int usadas = 0;
                        foreach (var capturado in prodcap)
                        {
                            usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                            totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                            total_prod_simula = total_prod_simula - usadas;
                            if (totaldis > 0)
                            {
                                totaldisponibles = totaldisponibles + totaldis;
                            }
                        }
                    }
                }

                if (totaldisponibles > 0 && total_prod_simula > 0)
                {
                    okidoki = "SI";
                    Mensajes mensa = new Mensajes { titulo = "FOLIOS PREAUTORIZADOS DISPONIBLES", mensaje = "Se han encontrado Folios Preautorizados Disponibles Para el producto " + producto + ", Favor de capturar primero estos folios antes de usar algun otro" };
                    await db.InsertAsync(mensa);
                }


            }
            return okidoki;
        }

        private async Task<string> validafecadMAXIMOS()
        {
            if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
            //***************************************INSTRUCCION QUE VALIDA QUE LAS FECHAS DE CADUCIDAD ACTUALES NO SE PASEN DE LOS DIAS DE CARGA DE LA ORDEN**************************************************
            string Valor = "";
            ValiFechacad = "S";
            //Obtener los productos con su tipo de lo que se ha leido******************************************************************
            //var productoscapturados = await db.QueryAsync<xLote>("Select Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre");
            var productoscapturados = await db.QueryAsync<xLote>("Select * FROM xLote GROUP BY Tipo, Codigo, nombre");
            await db.QueryAsync<XLoteSug>("delete from[XLoteSug]");

            var allItems = await db.GetItemsAsync<xLote>();
            int count = allItems.Count;
            int[] validados = new int[count + 1];
            int capturas = 0;
            foreach (var captu in productoscapturados)
            {
                int totalpro = 0;
                int totaldisponibles = 0;
                int totalusadas;
                int simulador = 0;
                int totaldis = 0;
                string fechaant = "";
                int totaldisreal = 0;
                int totalprodsimulado = 0;

                //traer el total de recibos vencidos para que no entren en la condicion
                var prodcapx = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "'");

                foreach (var capturadox in prodcapx)
                {
                    totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());

                }

                int resttotal = await traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());

                totalpro = totalpro - resttotal;
                totalprodsimulado = totalpro;

                //Obtener los diferentes folios disponibles dependiendo el codigo y el tipo
                string todobien = "OK";
                int prod_cap = 0;
                int usadas = 0;
                int existefecant = 0;
                string cadena = "";
                string tipo = captu.Tipo.Trim();
                string prod = captu.Codigo.Trim();

                string diacadant = "";
                string mescadant = "";

                //traer nombre de producto para validar cuantos dias debo aumentar.
                string prodnom = captu.nombre.Trim();
                int diascad = 14;
                if (prodnom.Contains("BETABEL"))
                {
                    diascad = 60;
                }
                else if (prodnom.Contains("AJO"))
                {
                    diascad = 180;
                }
                else if (prodnom.Contains("ADEREZO") || prodnom.Contains("VINAGRETA") || prodnom.Contains("QUESO"))
                {
                    diascad = 90;
                }


                if (tipo == "PTC")
                {
                    cadena = "SELECT  (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US' ) WHEN fecha_cad THEN fecha_cad END) AS fecha_cad, (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END) AS fecha_cadu, recibo, tarima, DATEDIFF(day, GETDATE(), (CASE fecha_cad WHEN '' THEN  FORMAT( DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US' ) WHEN fecha_cad THEN FORMAT(convert(datetime,fecha_cad), 'yyyyMMdd', 'en-US' ) END)) AS diasdisp, preautorizado  FROM TB_DET_TRAZABILIDAD Inner JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo WHERE (preautorizado = '' or preautorizado is null) AND PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND (rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND  (etiqueta - surtido) > 0 Order By fecha_cadu";
                }
                else
                {
                    cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(NULLIF(fechacad,' '), FORMAT( DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US' )) AS fecha_cad, folio AS recibo, tarima, DATEDIFF(day, GETDATE(), fechacad) AS diasdisp, preautorizado FROM tb_det_eti_final Inner JOIN tb_mstr_ordenes_prod ON folio = ordp_folio WHERE (preautorizado = '' or preautorizado is null) AND cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND etiqueta = 'S' AND (num_cajas - cajas_sur) > 0 Order By fecha_cad";
                }

                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                da.Fill(ds, "xlotes");
                DataTable xlote = ds.Tables["xlotes"];
                //Recorrido de cada uno de los folios y la validacion correspondiente hacia lo que tengo capturado************************

#pragma warning disable CS0219 // La variable 'foliosAnt' está asignada pero su valor nunca se usa
                string foliosAnt = "";
#pragma warning restore CS0219 // La variable 'foliosAnt' está asignada pero su valor nunca se usa

                foreach (DataRow row in xlote.Rows)
                {
                    usadas = 0;
                    int total_prod_simula = totalprodsimulado;
                    int TotalLeido = 0;
                    string Cadena = "Select Count(fecha) AS Total From Tb_Det_Etiqueta_Presplit " +
                                    "Where Eti_Recibo = '" + row["recibo"].ToString().Trim() + "' AND Eti_Producto = '" + captu.Codigo.Trim() + "' AND Eti_TarIni = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "' AND Estatus = 'A'";

                    if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                    SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                    TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                    int usadasant = 0;
                    thisConnection.Close();

                    row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;
                    if (Convert.ToInt32(row["disponible"]) > 0)
                    {
                        if (totalpro > 0)
                        {
                            string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                            var prodcap = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND Folio = '" + row["recibo"].ToString().Trim() + "'  AND CAST(Tarima as int) = '" + Convert.ToInt32(row["tarima"].ToString().Trim()) + "'");

                            foreach (var capturado in prodcap)
                            {
                                usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                usadasant = usadas;
                                totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                simulador = simulador + totaldis;
                                totalpro = totalpro - usadas;
                                totaldisponibles = totaldisponibles + totaldis;
                                totaldisreal = totaldis;
                            }

                            if (totaldis > 0 && totalpro > 0)
                            {
                                var prodcapfecad = await db.QueryAsync<xLote>("Select COUNT(ID) AS Cajas FROM xLote Where Codigo = '" + captu.Codigo.Trim() + "' AND diacad = '" + diacad + "'AND mescad = '" + mescad + "'");

                                foreach (var capturado in prodcapfecad)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim()) - usadasant;
                                    totaldis = Convert.ToInt32(totaldis) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                }
                            }

                            if (totaldis > 0)
                            {
                                if (totalpro > 0)
                                {
                                    XLoteSug sugeridos = new XLoteSug { recibosug = row["recibo"].ToString().Trim(), fecrecsug = diacad + "/" + mescad, cveprod = prod, Tarima = row["tarima"].ToString().Trim(), Cajasdis = totaldisreal, Cajasusadas = usadas, foliomens = row["diasdisp"].ToString().Trim() };
                                    await db.InsertAsync(sugeridos);
                                    break;
                                }

                            }

                            //diacadant = diacad;
                            //mescadant = mescad;

                        }
                        else
                        {
                            break;
                        }
                    }
                }

                var loteSug = await db.QueryAsync<XLoteSug>("Select  *  FROM XLoteSug Where cveprod = '" + captu.Codigo.Trim() + "' AND cajasdis != 0 LIMIT 1");
                foreach (var capturado in loteSug)
                {
                    string recibosug = capturado.recibosug;
                    string fecrecsug = capturado.fecrecsug;
                    string cveprod = capturado.cveprod;
                    string tarima = capturado.Tarima;
                    int cajasdis = capturado.Cajasdis;
                    int cajasusadas = capturado.Cajasusadas;
                    string diasdif = capturado.foliomens;
                    Mensajes mensa = new Mensajes { titulo = "Existe un folio anterior disponible", mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() + " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() + " Con Fecha de Caducidad del " + capturado.fecrecsug + ", Dias disponibles " + diasdif + ", Favor de Buscar a personal de Camaras Frias para la autorizacion" };
                    await db.InsertAsync(mensa);
                    ValiFechacad = "N";

                    XLoteSug sugeridosact = new XLoteSug { recibosug = recibosug.ToString().Trim(), fecrecsug = fecrecsug, cveprod = cveprod, Tarima = tarima.ToString().Trim(), Cajasdis = cajasdis, Cajasusadas = cajasusadas, foliomens = "S" };
                    await db.InsertAsync(sugeridosact);
                    totalpro = 0;
                }
            }
            return Valor;
        }

        private async Task<string> validafecadMAXIMOSAsync()
        {
            string Valor = "";
            ValiFechacad = "S";

            try
            {
                if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }

                // Obtener los productos con su tipo de lo que se ha leido
                var productoscapturados = await Task.Run(() => db.QueryAsync<xLote>("SELECT Tipo, Codigo, nombre FROM xLote GROUP BY Tipo, Codigo, nombre"));

                // Limpiar la tabla XLoteSug
                await db.QueryAsync<XLoteSug>("DELETE FROM XLoteSug");

                // Obtener todos los elementos de xLote
                var allItems = await db.GetItemsAsync<xLote>();
                int count = allItems.Count;
                int[] validados = new int[count + 1];
                int capturas = 0;

                foreach (var captu in productoscapturados)
                {
                    int totalpro = 0;
                    int totaldisponibles = 0;
                    int totalusadas;
                    int simulador = 0;
                    int totaldis = 0;
                    string fechaant = "";
                    int totaldisreal = 0;
                    int totalprodsimulado = 0;

                    // traer el total de recibos vencidos para que no entren en la condicion
                    var prodcapx = await db.QueryAsync<xLote>("SELECT COUNT(ID) AS Cajas FROM xLote WHERE Codigo = ? AND Tipo = ?", captu.Codigo.Trim(), captu.Tipo.Trim());

                    foreach (var capturadox in prodcapx)
                    {
                        totalpro = Convert.ToInt32(capturadox.Cajas.ToString().Trim());
                    }

                    int resttotal = await traerecibosvencidos(captu.Codigo.Trim(), captu.Tipo.Trim());
                    totalpro = totalpro - resttotal;
                    totalprodsimulado = totalpro;

                    // Obtener los diferentes folios disponibles dependiendo del codigo y el tipo
                    string todobien = "OK";
                    int prod_cap = 0;
                    int usadas = 0;
                    int existefecant = 0;
                    string cadena = "";
                    string tipo = captu.Tipo.Trim();
                    string prod = captu.Codigo.Trim();

                    string diacadant = "";
                    string mescadant = "";

                    // traer nombre de producto para validar cuantos dias debo aumentar.
                    string prodnom = captu.nombre.Trim();
                    int diascad = 14;

                    // Ajustar los días de caducidad según el nombre del producto
                    if (prodnom.Contains("BETABEL"))
                    {
                        diascad = 60;
                    }
                    else if (prodnom.Contains("AJO"))
                    {
                        diascad = 180;
                    }
                    else if (prodnom.Contains("ADEREZO") || prodnom.Contains("VINAGRETA") || prodnom.Contains("QUESO"))
                    {
                        diascad = 90;
                    }

                    // Construir la cadena de consulta según el tipo de producto
                    if (tipo == "PTC")
                    {
                        cadena = "SELECT (etiqueta - surtido) AS disponible, (CASE fecha_cad WHEN '' THEN FORMAT(DATEADD(day, " + diascad + ", pti_fecha), 'dd/MM/yyyy', 'en-US') ELSE fecha_cad END) AS fecha_cad, " +
                                 "(CASE fecha_cad WHEN '' THEN FORMAT(DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US') ELSE FORMAT(convert(datetime, fecha_cad), 'yyyyMMdd', 'en-US') END) AS fecha_cadu, " +
                                 "recibo, tarima, DATEDIFF(day, GETDATE(), (CASE fecha_cad WHEN '' THEN FORMAT(DATEADD(day, " + diascad + ", pti_fecha), 'yyyyMMdd', 'en-US') ELSE FORMAT(convert(datetime, fecha_cad), 'yyyyMMdd', 'en-US') END)) AS diasdisp, preautorizado " +
                                 "FROM TB_DET_TRAZABILIDAD INNER JOIN tb_mstr_recepcion_pt ON rpt_recibo = recibo " +
                                 "WHERE (preautorizado = '' or preautorizado is null) AND PROD_CLAVE = '" + prod + "' AND pti_estatus_sur = '' AND tipo = 'PTC' AND " +
                                 "(rpt_tipo != 'TR' OR (rpt_tipo != 'TR' AND rpt_inventario = 'S')) AND rpt_estatus = '' AND (etiqueta - surtido) > 0 ORDER BY fecha_cadu";
                    }
                    else
                    {
                        cadena = "SELECT (num_cajas - cajas_sur) AS disponible, ISNULL(NULLIF(fechacad, ' '), FORMAT(DATEADD(day, " + diascad + ", fecha), 'yyyyMMdd', 'en-US')) AS fecha_cad, " +
                                 "folio AS recibo, tarima, DATEDIFF(day, GETDATE(), fechacad) AS diasdisp, preautorizado " +
                                 "FROM tb_det_eti_final INNER JOIN tb_mstr_ordenes_prod ON folio = ordp_folio " +
                                 "WHERE (preautorizado = '' or preautorizado is null) AND cve_prod = '" + prod + "' AND estatus_sur != 'S' AND ordp_estatus != 'C' AND etiqueta = 'S' AND " +
                                 "(num_cajas - cajas_sur) > 0 ORDER BY fecha_cad";
                    }

                    SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                    DataSet ds = new DataSet();
                    // Llenar el DataSet con los datos de la consulta
                    da.Fill(ds, "xlotes");
                    DataTable xlote = ds.Tables["xlotes"];

                    // Recorrer cada fila de xlotes y validar caducidad
                    foreach (DataRow row in xlote.Rows)
                    {
                        usadas = 0;
                        int total_prod_simula = totalprodsimulado;
                        int TotalLeido = 0;
                        string Cadena = "SELECT Count(fecha) AS Total FROM Tb_Det_Etiqueta_Presplit " +
                                        "WHERE Eti_Recibo = @recibo AND Eti_Producto = @producto AND Eti_TarIni = @tarima AND Estatus = 'A'";
                        if (thisConnection.State == ConnectionState.Closed) { thisConnection.Open(); }
                        SqlCommand cmd = new SqlCommand(Cadena, thisConnection);
                        cmd.Parameters.AddWithValue("@recibo", row["recibo"].ToString().Trim());
                        cmd.Parameters.AddWithValue("@producto", captu.Codigo.Trim());
                        cmd.Parameters.AddWithValue("@tarima", Convert.ToInt32(row["tarima"].ToString().Trim()));
                        TotalLeido = Convert.ToInt32(cmd.ExecuteScalar());
                        int usadasant = 0;
                        thisConnection.Close();

                        row["disponible"] = Convert.ToInt32(row["disponible"].ToString().Trim()) - TotalLeido;

                        if (Convert.ToInt32(row["disponible"]) > 0)
                        {
                            if (totalpro > 0)
                            {
                                string diacad = traediafecadrec(row["fecha_cad"].ToString().Trim(), tipo);
                                string mescad = traemesfecadrec(row["fecha_cad"].ToString().Trim(), tipo);

                                var prodcap = await db.QueryAsync<xLote>("SELECT COUNT(ID) AS Cajas FROM xLote WHERE Codigo = ? AND Folio = ? AND CAST(Tarima AS int) = ?", captu.Codigo.Trim(), row["recibo"].ToString().Trim(), Convert.ToInt32(row["tarima"].ToString().Trim()));

                                foreach (var capturado in prodcap)
                                {
                                    usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    usadasant = usadas;
                                    totaldis = Convert.ToInt32(row["disponible"].ToString().Trim()) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                    simulador = simulador + totaldis;
                                    totalpro = totalpro - usadas;
                                    totaldisponibles = totaldisponibles + totaldis;
                                    totaldisreal = totaldis;
                                }

                                if (totaldis > 0 && totalpro > 0)
                                {
                                    var prodcapfecad = await db.QueryAsync<xLote>("SELECT COUNT(ID) AS Cajas FROM xLote WHERE Codigo = ? AND diacad = ? AND mescad = ?", captu.Codigo.Trim(), diacad, mescad);

                                    foreach (var capturado in prodcapfecad)
                                    {
                                        usadas = Convert.ToInt32(capturado.Cajas.ToString().Trim()) - usadasant;
                                        totaldis = Convert.ToInt32(totaldis) - Convert.ToInt32(capturado.Cajas.ToString().Trim());
                                        simulador = simulador + totaldis;
                                        totalpro = totalpro - usadas;
                                        totaldisponibles = totaldisponibles + totaldis;
                                    }
                                }

                                if (totaldis > 0)
                                {
                                    if (totalpro > 0)
                                    {
                                        XLoteSug sugeridos = new XLoteSug
                                        {
                                            recibosug = row["recibo"].ToString().Trim(),
                                            fecrecsug = diacad + "/" + mescad,
                                            cveprod = prod,
                                            Tarima = row["tarima"].ToString().Trim(),
                                            Cajasdis = totaldisreal,
                                            Cajasusadas = usadas,
                                            foliomens = row["diasdisp"].ToString().Trim()
                                        };
                                        await db.InsertAsync(sugeridos);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    // Obtener sugerencias de XLoteSug
                    var loteSug = await db.QueryAsync<XLoteSug>("SELECT * FROM XLoteSug WHERE cveprod = ? AND cajasdis != 0 LIMIT 1", captu.Codigo.Trim());

                    foreach (var capturado in loteSug)
                    {
                        string recibosug = capturado.recibosug;
                        string fecrecsug = capturado.fecrecsug;
                        string cveprod = capturado.cveprod;
                        string tarima = capturado.Tarima;
                        int cajasdis = capturado.Cajasdis;
                        int cajasusadas = capturado.Cajasusadas;
                        string diasdif = capturado.foliomens;

                        Mensajes mensa = new Mensajes
                        {
                            titulo = "Existe un folio anterior disponible",
                            mensaje = "El recibo " + "\n\r" + capturado.recibosug.ToString().Trim() + " De la tarima  " + capturado.Tarima.Trim() +
                                      " Tiene  " + capturado.Cajasdis + " cajas disponibles del producto: " + captu.nombre.Trim() +
                                      " Con Fecha de Caducidad del " + capturado.fecrecsug + ", Dias disponibles " + diasdif +
                                      ", Favor de Buscar a personal de Camaras Frias para la autorizacion"
                        };
                        await db.InsertAsync(mensa);
                        ValiFechacad = "N";

                        XLoteSug sugeridosact = new XLoteSug
                        {
                            recibosug = recibosug.ToString().Trim(),
                            fecrecsug = fecrecsug,
                            cveprod = cveprod,
                            Tarima = tarima.ToString().Trim(),
                            Cajasdis = cajasdis,
                            Cajasusadas = cajasusadas,
                            foliomens = "S"
                        };
                        await db.InsertAsync(sugeridosact);
                        totalpro = 0;
                    }
                }
                return Valor;
            }
            catch (Java.Lang.Exception ex)
            {
                //Console.WriteLine("Error al validar fechas de caducidad: " + ex.Message);
                Toast.MakeText(this, "Error al validar fechas de caducidad: " + ex.Message, ToastLength.Short).Show();
                Valor = "N"; // Cambiar a 'N' si ocurre un error
            }
            finally
            {
                if (thisConnection.State == ConnectionState.Open) { thisConnection.Close(); }
            }

            return Valor;
        }

        private string getDeviceID()
        {
            Android.Telephony.TelephonyManager telephonyManager;
            telephonyManager = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //string deviceid=telephonyManager.DeviceId;
            string deviceid = CrossDeviceInfo.Current.Id;
            return deviceid;
        }
    }
}