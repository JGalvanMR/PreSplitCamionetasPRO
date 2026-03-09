using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using PreSplitCamionetas.Modal;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Views.InputMethods;
using SQLite;
using PreSplitCamionetas.Models;
using System.IO;
using Android.Text;
using Java.Util;
using System.Globalization;
using System.Threading.Tasks;
using PreSplitCamionetas.Data;
using Xamarin.Essentials;

namespace PreSplitCamionetas
{
    [Activity(Label = "Detalle de Pedido ")]
    public partial class SolicitarPed : Activity
    {
        public static string cvvehiculo, cvresponsable;
        public static string vehiculo, responsable;
        public string Nombre = "", Mtipo = "", MProd = "", MTar = "", MFol = "", mUser = "", user = "";
        public string Mtipo2 = "", MProd2 = "", MTar2 = "", MFol2 = "", CveCam = "", mOp = "A", Version = "3.5";
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
        ArrayAdapter<String> comboAdapter;
        String[] strFrutas;

        DataTable CatProd = new DataTable();


        //traer los datos e id de cada uno de los elementos de la vista
        EditText pedido;
        TextView detalleped;
        TextView PedidosSurtidos;
        Spinner Pedidos;
        Button capturar;

        private static readonly object dbLock = new object();
        private Database db;
        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SolicitarPedidos);

            // Inicializa la base de datos
            await InitializeDatabaseAsync();

            // Obtener referencias a los elementos de la vista
            PedidosSurtidos = FindViewById<TextView>(Resource.Id.PEDSUR);
            capturar = FindViewById<Button>(Resource.Id.button1);
            capturar.Click += btnContinuar;

            // Llamar al método para cargar los productos
            await CargarCatalogoProductosAsync();

            // Recuperar datos de la pantalla anterior
            cvresponsable = Intent.GetStringExtra("cvresponsable");
            responsable = Intent.GetStringExtra("responsable");
            currentVersionName = Intent.GetStringExtra("currentVersionName");
            TextView usuario = FindViewById<TextView>(Resource.Id.usuario);
            usuario.Text = $"{responsable.Trim()} - PRESPLIT CAMIONETAS";
        }

        private async Task InitializeDatabaseAsync()
        {
            db = Database.Instance;
            bool dbExists = await db.DatabaseExistsAsync();

            if (dbExists)
            {
                await db.InitializeDatabaseAsync();
            }
        }

        private async Task CargarCatalogoProductosAsync()
        {
            // Abrir conexión y ejecutar consulta asíncronamente
            using (var thisConnection = new SqlConnection(MainActivity.cadenaConexion)) // Asegúrate de tener definida la cadena de conexión adecuada
            {
                await thisConnection.OpenAsync();

                string cadena = "SELECT prod_clave, prod_nombre FROM vwCatalogoProductos ORDER BY LEN(prod_clave) DESC";
                using (SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection))
                {
                    DataSet ds = new DataSet();
                    await Task.Run(() => da.Fill(ds, "CatProd")); // Ejecutar fill de manera asíncrona

                    CatProd = ds.Tables["CatProd"];
                }
            }
        }

        public async Task ConsPedSurAsync()
        {
            // Crear y mostrar el ProgressDialog
            var progressDialog = new ProgressDialog(this);
            progressDialog.Indeterminate = true;
            progressDialog.SetCancelable(false);
            progressDialog.SetMessage("Cargando datos, por favor espere...");
            progressDialog.Show();

            try
            {
                // Eliminar datos existentes de las tablas
                try
                {
                    await db.ExecuteAsync("DELETE FROM [Pedidos]");
                    await db.ExecuteAsync("DELETE FROM [ConPedidos]");
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "Error al eliminar datos: " + ex.Message, ToastLength.Long).Show();
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
                                          $"(SELECT Count(Fecha) as cajas FROM Tb_Det_Etiqueta_Presplit WHERE fecha_cap > '{fechahoy} 14:00:00' " +
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
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "Error general: " + ex.Message, ToastLength.Long).Show();
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
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "Error al actualizar datos: " + ex.Message, ToastLength.Long).Show();
                    }
                }

                // Actualizar la interfaz de usuario
                RunOnUiThread(async () =>
                {
                    try
                    {
                        PedidosSurtidos.Text = "Pedidos: " + cantped + " Surtidos: " + cantsur;
                        List<FlimStarInfo> lstFlimStar = await detalle_pedidoAsync();
                        var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                        gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                        gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "Error al actualizar la interfaz de usuario: " + ex.Message, ToastLength.Long).Show();
                    }
                });
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Error en la carga de datos. Verifica los registros.", ToastLength.Long).Show();
            }
            finally
            {
                // Cerrar el ProgressDialog cuando todo haya terminado
                progressDialog.Dismiss();
            }
        }

        public async Task ConsPedSurAsync2()
        {
            // Mostrar ProgressDialog
            // Crear el ProgressDialog
            var progressDialog = new ProgressDialog(this);
            progressDialog.Indeterminate = true;
            progressDialog.SetCancelable(false);
            progressDialog.SetMessage("Cargando datos, por favor espere...");

            // Mostrar el ProgressDialog
            progressDialog.Show();

            try
            {
                // Eliminar datos existentes de las tablas
                try
                {
                    await db.ExecuteAsync("DELETE FROM [Pedidos]");
                    await db.ExecuteAsync("DELETE FROM [ConPedidos]");
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("Error al eliminar datos: " + ex.Message);
                    Toast.MakeText(this, "Error al eliminar datos: " + ex.Message, ToastLength.Long).Show();
                }

                // Obtener la fecha actual
                string fechaactual = "IF CAST(CONVERT(VARCHAR, SYSDATETIME(), 108) AS TIME) > '04:00:00' SELECT GETDATE(); ELSE SELECT DATEADD(day, -1, GETDATE());";
                DateTime fechahoyserv;
                string fechahoy;
                int cantped = 0;
                int cantsur = 0;

                try
                {
                    using (var cmd = thisConnection.CreateCommand())
                    {
                        //thisConnection.Open();
                        cmd.CommandText = fechaactual;
                        var result = cmd.ExecuteScalar();
                        fechahoyserv = Convert.ToDateTime(result);
                        fechahoy = fechahoyserv.ToString("dd/MM/yyyy");
                        //thisConnection.Close();
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
                                      $"(SELECT Count(Fecha) as cajas FROM Tb_Det_Etiqueta_Presplit WHERE fecha_cap > '{fechahoy} 14:00:00' " +
                                      $"AND fecha_cap < '{fechamañana} 04:00:00' AND Eti_Producto = b.prod_clave AND Estatus != 'C') AS Surtido " +
                                      $"FROM tb_mstr_pedidos_nal a, tb_det_pedidos b, tb_cat_producto c WHERE {condicion} " +
                                      $"AND a.prov_clave = 'MRLUCKY' AND a.pdn_folio = b.pdn_folio AND a.pdn_tipo = b.pdn_tipo AND b.prod_clave = c.prod_clave " +
                                      $"AND a.pdn_estatus != 'C' GROUP BY b.prod_clave, c.prod_nombre ORDER BY c.prod_nombre";

                    using (var cmd = new SqlCommand(consulta, thisConnection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            await thisConnection.OpenAsync();
                            while (await reader.ReadAsync())
                            {
                                int surtido = 0;
                                string mtip = reader["Clave"].ToString().Trim();
                                string nombre = reader["Nombre"].ToString().Trim();
                                int pedido = Convert.ToInt32(reader["Cajas"].ToString().Replace(".000", ""));


                                // Insertar registros en la base de datos SQLite
                                Pedidos productosolicitado = new Pedidos { folio = "0", prod_clave = mtip, nombre = nombre, pedido = pedido, surtido = surtido };
                                //var productosolicitado = new Pedidos { folio = "0", prod_clave = mtip, nombre = nombre, pedido = pedido, surtido = surtido };
                                await db.InsertAsync(productosolicitado);
                                //await db.InsertAsync(productosolicitado);

                                ConPedidos consecutivo = new ConPedidos { prod_clave = mtip, nombre = nombre, pedido = pedido, surtido = surtido };
                                //var consecutivo = new ConPedidos { prod_clave = mtip, nombre = nombre, pedido = pedido, surtido = surtido };
                                await db.InsertAsync(consecutivo);
                                //await db.InsertAsync(consecutivo);



                                int totalx = await TraeTotalAsync(mtip);
                                surtido = Convert.ToInt32(reader["Surtido"].ToString()) + totalx;

                                cantped += pedido;
                                cantsur += surtido;
                            }
                        }
                        //thisConnection.Close();
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
                        catch (Exception ex)
                        {
                            //Console.WriteLine("Error al actualizar datos: " + ex.Message);
                            Toast.MakeText(this, "Error al actualizar datos: " + ex.Message, ToastLength.Long).Show();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("Error general: " + ex.Message);
                    Toast.MakeText(this, "Error general: " + ex.Message, ToastLength.Long).Show();
                }

                // Actualizar la interfaz de usuario
                RunOnUiThread(async () =>
                {
                    try
                    {
                        PedidosSurtidos.Text = "Pedidos: " + cantped + " Surtidos: " + cantsur;
                        List<FlimStarInfo> lstFlimStar = await detalle_pedidoAsync();
                        var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
                        gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                        gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine("Error al actualizar la interfaz de usuario: " + ex.Message);
                        Toast.MakeText(this, "Error al actualizar la interfaz de usuario: " + ex.Message, ToastLength.Long).Show();
                    }
                });
            }
            catch (Exception ex)
            {
                // Manejar excepciones según sea necesario
                //Console.WriteLine($"Error en la carga de datos: {ex.Message}");
                Toast.MakeText(this, "Error en la carga de datos. Verifica los registros.", ToastLength.Long).Show();
            }
            finally
            {
                // Cerrar el ProgressDialog cuando todo haya terminado
                progressDialog.Dismiss();
            }
        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {

        }

        List<FlimStarInfo> GetFlimStarInformation()
        {
            throw new NotImplementedException();
        }

        private async void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            var folio = spinner.GetItemAtPosition(e.Position).ToString();

            // Llamar al método asincrónico y esperar el resultado
            List<FlimStarInfo> lstFlimStar = await detalle_pedidoAsync();

            // Obtener el GridView y actualizar el adaptador
            var gvObject = FindViewById<GridView>(Resource.Id.gvCtrl);
            gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);

            // Manejar el evento ItemClick si es necesario
            gvObject.ItemClick -= OnGridView_ItemClicked; // Eliminar el manejador existente si hay uno
            gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked);
        }

        public async Task<List<FlimStarInfo>> detalle_pedidoAsync()
        {
            listItem.Clear();

            // Obtener los registros de ConPedidos de manera asincrónica
            var conPedidos = await db.GetItemsAsync<ConPedidos>();

            foreach (var captu in conPedidos)
            {
                listItem.Add(new FlimStarInfo()
                {
                    Name = captu.nombre.Trim(),
                    Age = "Pedido: " + captu.pedido + " Surtido: " + captu.surtido,
                    ImageID = Resource.Drawable.producto
                });
            }

            return listItem;
        }

        public void btnContinuar(object sender, EventArgs e)
        {
            try
            {
                // Crear un Intent para iniciar la nueva actividad
                Intent intent = new Intent(this, typeof(capturar_split));

                // Agregar datos al Intent
                intent.PutExtra("cvresponsable", cvresponsable.ToString());
                intent.PutExtra("responsable", responsable.ToString());
                intent.PutExtra("currentVersionName", currentVersionName.ToString());

                // Iniciar la nueva actividad
                StartActivity(intent);

                // Si necesitas esperar a que la actividad se inicie o realizar otras tareas después
                // puedes incluir lógica adicional aquí. Aunque por lo general, StartActivity es síncrono,
                // así que no se espera que sea una operación asincrónica.
            }
            catch (Exception ex)
            {
                // Manejar excepciones si ocurre algún error al iniciar la actividad
                //Console.WriteLine($"Error al iniciar la actividad: {ex.Message}");
                Toast.MakeText(this, "No se pudo iniciar la actividad. Inténtelo de nuevo.", ToastLength.Long).Show();
            }
        }

        public override Boolean OnKeyDown(Keycode keyCode, KeyEvent e)
        {

            if (keyCode == Keycode.Back)
            {
                Intent intent = new Intent(this, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ClearTop);
                Intent.AddFlags(ActivityFlags.SingleTop);
                //intent.PutExtra("cvcamioneta", cvvehiculo.ToString());
                StartActivity(intent);
            }
            return true;
        }

        private async Task<int> TraeTotalAsync(string mcod)
        {
            int total = 0;

            try
            {
                var productoscapturados = await db.GetConPedidosAsync(mcod);
                if (productoscapturados.Any())
                {
                    total = productoscapturados.First().surtido;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error al obtener el total: " + ex.Message);
                Toast.MakeText(this, "Error al obtener el total: " + ex.Message, ToastLength.Long).Show();
            }

            return total;
        }

        protected override async void OnResume()
        {
            base.OnResume();
            await ConsPedSurAsync();
        }
    }
}