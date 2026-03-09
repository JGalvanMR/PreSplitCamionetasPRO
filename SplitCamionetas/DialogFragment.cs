using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Data;
using System.Data.SqlClient;

namespace PreSplitCamionetas
{
    public class DialogFragmentSample : DialogFragment
    {
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        public static DialogFragmentSample NewInstace(Bundle bundle)
        {
            var fragment = new DialogFragmentSample();
            fragment.Arguments = bundle;
            return fragment;
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            //return customview for the fragment
            View viewer = inflater.Inflate(Resource.Layout.CapturarSplit, container, false);
            Button Guardarx = viewer.FindViewById<Button>(Resource.Id.GuardarCapturado);
            View view = inflater.Inflate(Resource.Layout.frmsupervisor, container, false);
            Button button = view.FindViewById<Button>(Resource.Id.btnClearLL);
            Button buttonAceptar = view.FindViewById<Button>(Resource.Id.btnLoginLL);
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle); //remove title area
            Dialog.SetCanceledOnTouchOutside(false); //dismiss window on touch outside


            button.Click += delegate
            {
                Dismiss();
                Toast.MakeText(Activity, "Dialog fragment dismissed!", ToastLength.Short).Show();
            };

            buttonAceptar.Click += delegate
            {
                string Res = "N";
                thisConnection.Open();
                string cadena = "SELECT PASSWORD FROM TB_AUTORIZA_ODEP WHERE CLAVE = 'PEDID' AND USUARIO = 'CAMIONETAS' ";
                SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
                DataSet ds = new DataSet();
                //MessageBox.Show(cadena); 
                da.Fill(ds, "Info");
                DataTable Info = ds.Tables["Info"];
                //MessageBox.Show(Info.Rows.Count.ToString()); 
                if (Info.Rows.Count == 0)
                    foreach (DataRow row in Info.Rows)
                        if (row["PASSWORD"].ToString().Trim().Length > 0)
                        {
                            Guardarx.Enabled = true;
                            capturar_split.AutoPed = "S";
                        }
                thisConnection.Close();








                Dismiss();
            };

            return view;
        }
    }
}