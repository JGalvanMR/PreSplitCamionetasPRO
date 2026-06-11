using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Data;
using System.Data.SqlClient;
using Android.Content;
using System.Net;
using Android.Net;
using Android.Locations;
using Android.Text;
using System.Collections;
using Java.Util;
using Android.Telephony;
using System.Threading.Tasks;
using Java.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Text;
using Org.Json;
using Android;
using Android.Content.PM;
using Android.Support.V4.Content;
using Android.Net.Wifi;
using Plugin.DeviceInfo;
using PreSplitCamionetas.Data;
using Android.Support.V4.App;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Timers;
using Android.Nfc;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.Core.Graphics;
using Java.Interop;
using System.Text.RegularExpressions;
using Xamarin.Essentials;


namespace PreSplitCamionetas
{
    [Activity(Label = "PreSplitCamionetas", Theme = "@android:style/Theme.Material", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]

    public class MainActivity : Activity
    {
        const int RequestPhoneStatePermissionId = 1000;
        //public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira2026$;Initial Catalog = GAB_Irapuato; server=tcp:189.206.160.206,2352; MultipleActiveResultSets=true; Connect Timeout = 0";
        public static string cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira2026$;Initial Catalog = GAB_Irapuato; server=tcp:192.168.123.6,1433; MultipleActiveResultSets=true; Connect Timeout = 0";

        public static int captura = 0;
        SqlCommand cmnd = new SqlCommand();
        SqlDataReader reader;
        SqlCommand cmnd1 = new SqlCommand();
        SqlDataReader reader1;
        String[] strFrutas;
        ArrayAdapter<String> comboAdapter;
        SqlDataAdapter da;
        SqlDataAdapter da1;
        public static DataTable camionetas = new DataTable("camionetas");
        public static DataTable responsables = new DataTable("responsables");
        public static DataTable vehiculos = new DataTable("vehiculos");
        public static DataTable version = new DataTable("version");
        public static DataTable formulario = new DataTable("formulario");
        TextView versionApp;
        string query = "";
        DataSet ds = new DataSet();
        DataSet ds1 = new DataSet();
        public static string vehiculo = "";
        public static string responsablesplit = "";
        public static string imei = "";
        SqlConnection thisConnection;


        public static Int32 foliocampo = 0;


        //Variables del servicio Web
        Context context;
        Java.Lang.Runnable listener;
        //private static string INFO_FILE = "http://mrlucky.com.mx/ventasnew/PreSplitCamionetas/Version.txt";
        private static string INFO_FILE = "http://192.168.123.4:81/EmbarquesApk/APK_PreSplitCamionetas/version.txt";
        //private static string INFO_FILE = "http://189.206.160.206:81/EmbarquesApk/APK_PreSplitCamionetas/version.txt";
        private int currentVersionCode;
        private string currentVersionName;
        private int latestVersionCode;
        private string latestVersionName;
        private string downloadURL;
        private Database db;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Llenado de Spinner 1
            //LoadSpinner1();

            // Llenado de Spinner 2
            LoadSpinner2();

            // Solicitar permisos al dispositivo
            await RequestPermissionsAsync();

            Button log = FindViewById<Button>(Resource.Id.btnlogin);
            log.Click += Btnlogin_Click;

            validaWiFi();
            validaConexionRed();

            imei = getDeviceID();
            validaservidores();

            versionApp = FindViewById<TextView>(Resource.Id.versionApp);



            // Validar actualización de la aplicación
            await ValidateAppUpdateAsync();
        }

        private async Task InitializeDatabaseAsync()
        {
            var database = Database.Instance;
            bool dbExists = await database.DatabaseExistsAsync();

            if (dbExists)
            {
                await database.InitializeDatabaseAsync();
            }
        }

        private async Task RequestPermissionsAsync()
        {
            string[] permisos = new string[]
            {
        Android.Manifest.Permission.AccessFineLocation,
        Android.Manifest.Permission.ReadPhoneState,
        Android.Manifest.Permission.AccessWifiState,
        Android.Manifest.Permission.Camera,
        Android.Manifest.Permission.WriteExternalStorage,
        Android.Manifest.Permission.InstallPackages
            };

            // Verificar permisos de forma asincrónica para evitar bloquear el hilo principal
            var allPermissionsGranted = await Task.Run(() => permisos.All(p => ContextCompat.CheckSelfPermission(this, p) == (int)Android.Content.PM.Permission.Granted));

            if (!allPermissionsGranted)
            {
                // Solicitar permisos, este proceso es síncrono por naturaleza en Android
                ActivityCompat.RequestPermissions(this, permisos, 1);
            }
        }

        private void LoadSpinner1()
        {
            try
            {
                using (var thisConnection = new SqlConnection(cadenaConexion))
                {
                    thisConnection.Open();
                    using (var cmnd = thisConnection.CreateCommand())
                    {
                        cmnd.CommandText = "select inicio_campo from Tb_folio_campo";
                        foliocampo = Convert.ToInt32(cmnd.ExecuteScalar());
                    }

                    ds.Clear();
                    using (var da = new SqlDataAdapter("select * FROM tb_cat_vehiculos Where estatus = 'A'", thisConnection))
                    {
                        da.Fill(ds, "camionetas");
                        camionetas = ds.Tables["camionetas"];
                    }
                }

                // Prepara los datos para el Spinner
                string[] strFrutas = new string[camionetas.Rows.Count + 1];
                strFrutas[0] = "Seleccione un vehiculo";
                for (int i = 1; i <= camionetas.Rows.Count; i++)
                {
                    strFrutas[i] = camionetas.Rows[i - 1]["descripcion"].ToString();
                }

                var comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);

                Spinner spinner1 = FindViewById<Spinner>(Resource.Id.spinner2);
                spinner1.Adapter = comboAdapter;
            }
            catch (Exception ex)
            {
                // Manejar excepciones
                Toast.MakeText(this, "Error al cargar los datos del spinner.", ToastLength.Long).Show();
            }
        }

        private void LoadSpinner2()
        {
            try
            {
                using (var thisConnection = new SqlConnection(cadenaConexion))
                {
                    using (var da = new SqlDataAdapter("SELECT clave, usuario, password FROM Tb_Autoriza_OdeP WHERE clave = 'EMB' AND obs = 'PRESPLIT'", thisConnection))
                    {
                        ds.Clear(); // Asegúrate de limpiar el DataSet antes de llenarlo
                        da.Fill(ds, "responsables");
                        responsables = ds.Tables["responsables"];
                    }
                }

                // Prepara los datos para el Spinner
                string[] strFrutas = new string[responsables.Rows.Count + 1];
                strFrutas[0] = "Seleccione un Responsable";
                for (int i = 1; i <= responsables.Rows.Count; i++)
                {
                    strFrutas[i] = responsables.Rows[i - 1]["usuario"].ToString();
                }

                var comboAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, strFrutas);

                Spinner spinner2 = FindViewById<Spinner>(Resource.Id.spinner2);
                spinner2.Adapter = comboAdapter;

                //Agrega el evento ItemSelected para manejar la selección de un ítem en el spinner
                spinner2.ItemSelected += spinner_ItemSelected2;
            }
            catch (Exception ex)
            {
                //Manejar excepciones
                Toast.MakeText(this, "Error al cargar los datos del spinner.", ToastLength.Long).Show();
            }
        }

        private async Task ValidateAppUpdateAsync()
        {
            await Task.Run(() => getData()); // Ejecuta getData en un hilo separado si es necesario

            versionApp.Text = "Pre/Split Camionetas - Versión: " + currentVersionName;

            if (await Task.Run(() => isNewVersionAvailable())) // Verifica la nueva versión en un hilo separado
            {
                string msj = $"Nueva Versión: {isNewVersionAvailable()}\nCurrent Version: {currentVersionName}({currentVersionCode})\nLatest Version: {latestVersionName}({latestVersionCode})\nDesea Actualizar?";

                var alertDialog = new Android.App.AlertDialog.Builder(this)
                    .SetTitle(Html.FromHtml("<font color='#EC144C' size='10'>Actualización Disponible</font>"))
                    .SetIcon(Resource.Drawable.update)
                    .SetMessage(Html.FromHtml("<font color='#000000' size='10'>" + msj + "</font>"))
                    .SetPositiveButton(Html.FromHtml("<font face='Comic Sans MS, arial' color='#6CB43C' size='10'>Sí</font>"), SaveAction)
                    .SetNegativeButton(Html.FromHtml("<font face='Comic Sans MS, arial' color='#DF0101' size='10'>No</font>"), CancelaAction)
                    .SetCancelable(false)
                    .Create();

                alertDialog.Show();
            }
        }

        private void CancelaAction(object sender, DialogClickEventArgs e)
        {
            Finish();
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            downloadApp();
        }

        void Btnlogin_Click(object sender, EventArgs e)
        {
            if (responsablesplit == "Seleccione un Responsable")
            {
                Toast.MakeText(this, "Por favor, asegurese de seleccionar un responsable y volver a intentarlo", ToastLength.Long).Show();
                return;
            }

            //if (vehiculo == "Seleccione un vehiculo")
            //{
            //    Toast.MakeText(this, "Por favor, asegurese de seleccionar un vehiculo y volver a intentarlo", ToastLength.Long).Show();
            //    return;
            //}

            //var camioneta = "";
            //if (camionetas.Rows.Count != 0)
            //{
            //    for (int i = 0; i < camionetas.Rows.Count; i++)
            //    {
            //        if (camionetas.Rows[i]["descripcion"].ToString() == vehiculo)
            //        {
            //            camioneta = camionetas.Rows[i]["clave"].ToString();
            //        }
            //    }
            //}
            //else
            //{
            //    Toast.MakeText(this, "Por favor, Seleccione un vehiculo", ToastLength.Long).Show();
            //    return;
            //}

            var responsable = "";
            if (responsables.Rows.Count != 0)
            {
                for (int i = 0; i < responsables.Rows.Count; i++)
                {
                    if (responsables.Rows[i]["usuario"].ToString() == responsablesplit)
                    {
                        responsable = responsables.Rows[i]["usuario"].ToString();
                    }
                }
            }
            else
            {
                Toast.MakeText(this, "Por favor, Seleccione un responsable", ToastLength.Long).Show();
                return;
            }


            //******************************************************


            Intent intent = new Intent(this, typeof(SolicitarPed));
            //intent.PutExtra("cvcamioneta", camioneta.ToString());
            intent.PutExtra("cvresponsable", responsable.ToString());
            intent.PutExtra("camioneta", vehiculo.ToString());
            intent.PutExtra("responsable", responsablesplit.ToString());
            intent.PutExtra("currentVersionName", currentVersionName.ToString());
            //intent.PutExtra("cadenaConexion", cadenaConexion.ToString());
            StartActivity(intent);
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            vehiculo = spinner.GetItemAtPosition(e.Position).ToString();
        }

        private void spinner_ItemSelected2(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            responsablesplit = spinner.GetItemAtPosition(e.Position).ToString();
        }

        private void getData()
        {
            try
            {
                context = this;
                // Datos locales
                System.Console.WriteLine("AutoUpdater", "GetData");
                Android.Content.PM.PackageInfo pckginfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);

                currentVersionCode = pckginfo.VersionCode;
                currentVersionName = pckginfo.VersionName;

                // Datos remotos
                string data = downloadHttp(new URL(INFO_FILE));
                JSONObject json = new JSONObject(data.ToString());
                latestVersionCode = json.GetInt("versionCode");
                latestVersionName = json.OptString("versionName");
                downloadURL = json.GetString("downloadURL");
                System.Console.WriteLine("AutoUpdate", "Datos obtenidos con éxito");
            }
            catch (JSONException e)
            {
                System.Console.WriteLine("AutoUpdate", "Ha habido un error con el JSON", e);
            }
            catch (Android.Content.PM.PackageManager.NameNotFoundException e)
            {
                System.Console.WriteLine("AutoUpdate", "Ha habido un error con el packete :S", e);
            }
            catch (System.IO.IOException e)
            {
                System.Console.WriteLine("AutoUpdate", "Ha habido un error con la descarga", e);
            }
        }

        private static string downloadHttp(URL url)
        {
            // Codigo de coneccion, Irrelevante al tema.

            StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
            StrictMode.SetThreadPolicy(policy);
            HttpURLConnection c = (HttpURLConnection)url.OpenConnection();

            c.RequestMethod = "GET";
            c.ReadTimeout = (15 * 1000);
            c.UseCaches = false;
            c.Connect();
            Java.IO.BufferedReader reader = new Java.IO.BufferedReader(new Java.IO.InputStreamReader(c.InputStream));
            Java.Lang.StringBuilder stringBuilder = new Java.Lang.StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                stringBuilder.Append(line + "\n");
            }
            return stringBuilder.ToString();
        }
        public bool isNewVersionAvailable()
        {
            return latestVersionCode > currentVersionCode;
        }

        private string downloadApp()
        {
            var progressDialog = ProgressDialog.Show(this, "Espere Por Favor...", "Descargando Actualizacion", true);
            new System.Threading.Thread(new System.Threading.ThreadStart(delegate
            {//LOAD METHOD TO GET ACCOUNT INFO
                try
                {
                    var pathToNewFolder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/PreSplitCamionetas";
                    Directory.CreateDirectory(pathToNewFolder);

                    string archivo = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/PreSplitCamionetas/PreSplitCamionetas.apk";

                    var webClient = new WebClient();
                    webClient.DownloadFileCompleted += (s, ex) =>
                    {
                        Java.IO.File toInstall = new Java.IO.File(archivo);
                        Android.Net.Uri downloadUri = FileProvider.GetUriForFile(context, context.ApplicationContext.PackageName + ".provider", toInstall);

                        Intent intent = new Intent(Intent.ActionView);
                        intent.SetDataAndType(downloadUri, "application/vnd.android.package-archive");
                        intent.SetFlags(ActivityFlags.NewTask);
                        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                        StartActivity(intent);
                        Finish();

                        #region Actualizar APK OLD
                        /*RunOnUiThread(() => Toast.MakeText(this, "Aplicacion Actualizada.", ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                        RunOnUiThread(() => progressDialog.Hide());
                        Intent intentx = new Intent(Intent.ActionView);
                        intentx.SetDataAndType(Android.Net.Uri.FromFile(new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/PreSplitCamionetas/PreSplitCamionetas.apk")), "application/vnd.android.package-archive");
                        intentx.SetFlags(ActivityFlags.NewTask);
                        StartActivity(intentx);
                        Finish();*/
                        #endregion
                    };

                    var folder = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/PreSplitCamionetas";
                    //webClient.DownloadFileAsync(new System.Uri("http://192.168.123.4:81/EmbarquesApk/APK_PreSplitCamionetas/PreSplitCamionetas.apk"), folder + "/PreSplitCamionetas.apk");
                    webClient.DownloadFileAsync(new System.Uri("http://189.206.160.206:81/EmbarquesApk/APK_PreSplitCamionetas/PreSplitCamionetas.apk"), folder + "/PreSplitCamionetas.apk");
                }
                catch (System.IO.IOException e)
                {
                    RunOnUiThread(() => progressDialog.Hide());
                    RunOnUiThread(() => Toast.MakeText(this, e.ToString(), ToastLength.Long).Show()); //HIDE PROGRESS DIALOG 
                }
            })).Start();
            return "1";
        }

        private bool validaWiFi()
        {
            #region ValidaWiFi
            WifiManager wifi = (WifiManager)Android.App.Application.Context.GetSystemService(Context.WifiService);
            if (wifi.IsWifiEnabled == false)
            {
                PreSplitCamionetas.GuardaLocal GuardaError = new PreSplitCamionetas.GuardaLocal();
                GuardaError.creartxt("Wifi Deshabilitada");
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#FCEC70' size = 10>Error en el Adaptador WIFI</font>"));
                alertDialog.SetIcon(Resource.Drawable.warning);
                alertDialog.SetMessage(Html.FromHtml("<font color='#E0F1FA' size = 10>El Dispositivo no tiene la Wifi Activada, favor de activarlo</font>"));
                alertDialog.SetCancelable(false);
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                    Finish();

                });
                alertDialog.Show();
                return false;
            }
            else
            {
                return true;
            }
            #endregion
        }

        private bool validaConexionRed()
        {
            ConnectivityManager connectivityManager = (ConnectivityManager)GetSystemService(Context.ConnectivityService);
            NetworkInfo activeConnection = connectivityManager.ActiveNetworkInfo;
            bool isOnline = (activeConnection != null) && activeConnection.IsConnected;
            if (!isOnline || !validaservidores())
            {
                cadenaConexion = "Persist Security Info=False;user id=sa; password=Gabira2026$;Initial Catalog =GAB_Irapuato; server=tcp:189.206.160.206,2352; Connect Timeout = 0";
                INFO_FILE = "http://189.206.160.206:81/EmbarquesApk/APK_PreSplitCamionetas/version.txt";
                if (!isOnline)
                {
                    PreSplitCamionetas.GuardaLocal GuardaError = new PreSplitCamionetas.GuardaLocal();
                    GuardaError.creartxt("Error en la conexion de red, No esta conectado a ninguna red");
                    Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                    alertDialog.SetTitle(Html.FromHtml("<font color='#FCEC70' size = 10>Error en la Conexion a Internet</font>"));
                    alertDialog.SetIcon(Resource.Drawable.warning);
                    alertDialog.SetMessage(Html.FromHtml("<font color='#E0F1FA' size = 10>El Dispositivo no Esta conectado a ninguna Red, favor de verificarlo</font>"));
                    alertDialog.SetCancelable(false);
                    alertDialog.SetNeutralButton("Ok", delegate
                    {
                        alertDialog.Dispose();
                        Finish();

                    });
                    alertDialog.Show();
                    return false;
                }

                return true;
            }
            else
            {
                return true;
            }
        }

        private string getDeviceID()
        {

            Android.Telephony.TelephonyManager telephonyManager;
            telephonyManager = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //string deviceid=telephonyManager.DeviceId;
            string deviceid = CrossDeviceInfo.Current.Id;
            return deviceid;
        }
        public bool validaservidores()
        {
            bool online = true;
            string[] sitios = new string[1];
            sitios[0] = "http://189.206.160.206:81/EmbarquesApk/";
            //sitios[0] = "http://192.168.123.4:81/EmbarquesApk/";
            //sitios[1] = "http://192.168.123.6";

            for (int i = 0; i < sitios.Length; i++)
            {
                PreSplitCamionetas.GuardaLocal ValidarServidor = new PreSplitCamionetas.GuardaLocal();
                bool onlinex = ValidarServidor.HayConexion(sitios[i]);

                if (onlinex == false)
                {
                    ValidarServidor.creartxt("Error al Conectar a " + sitios[i]);
                }
            }
            return online;
        }

        private void ValidarErroresLocal()
        {
            Java.IO.File sdCard = Android.App.Application.Context.GetExternalFilesDir(null);

            Java.IO.File dir = new Java.IO.File(sdCard.AbsolutePath + "/MyFolder"); dir.Mkdirs();
            Java.IO.File file = new Java.IO.File(dir, "errores.txt");
            string FileToRead = file.ToString();

            // Creating string array  
            if (file.Exists())
            {
                string[] lines = System.IO.File.ReadAllLines(FileToRead);
                string correo = string.Join(System.Environment.NewLine, lines).Replace("\n", "<br>");
                var proxy = new WebServiceEmbarquesLocal.WebServiceEmbarques();
                proxy.SendMailPersonal("ricardo.cortes@mrlucky.com.mx;jgalvan@mrlucky.com.mx", correo, "Error Generado en PreSplitCamionetas Lectora: " + imei + "", "jgalvan", "mnK3a2aN@1|Q21VV", "jgalvan@mrlucky.com.mx");
                file.Delete();
            }
        }
    }
}

