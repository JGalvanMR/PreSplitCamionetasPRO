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
namespace PreSplitCamionetas
{
    [Activity(Label = "Cancelar Split")]
    public partial class CancelarSplit : Activity
    {
        public static string crcancelar, split, pedidocancelar;
        public static SQLiteConnection db;
        SqlConnection thisConnection = new SqlConnection(MainActivity.cadenaConexion);
        SqlDataAdapter da;
        DataSet ds = new DataSet();
        SqlCommand cmnd = new SqlCommand();
        SqlCommand cmnd1 = new SqlCommand();

        EditText pedidocan;
        TextView cansplit;
        TextView usuario;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            crcancelar = Intent.GetStringExtra("respcancel");
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CancelarSplit);

            pedidocan = FindViewById<EditText>(Resource.Id.pedidocancelar);
            usuario = FindViewById<TextView>(Resource.Id.usercancel);
            cansplit = FindViewById<TextView>(Resource.Id.SplitCargados);

            pedidocan.EditorAction += (sender, e) =>
            {


                if (e.ActionId == ImeAction.Done || e.ActionId == ImeAction.Next)
                {
                    List<FlimStarInfo> lstFlimStar = ConsSplit();
                    var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                    gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);
                    gvObject.ItemClick += new EventHandler<AdapterView.ItemClickEventArgs>(OnGridView_ItemClicked); ; //detalle_pedido

                    pedidocan.SetSelection(0, pedidocan.Text.Length);
                    pedidocan.RequestFocus();

                }
            };

        }

        private void OnGridView_ItemClicked(object sender, AdapterView.ItemClickEventArgs e)
        {
            split = e.View.FindViewById<TextView>(Resource.Id.txtName).Text;
            split = split.Replace("Split Numero: ", "");

            pedidocancelar = pedidocan.Text.Trim();

            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
            alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Cancelar Split</font>"));
            alertDialog.SetIcon(Resource.Drawable.question);
            alertDialog.SetMessage(Html.FromHtml("<font color='#000000' size = 10>¿Desea Cancelar el Splir Numero " + split + "?</font>"));
            alertDialog.SetPositiveButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>Sí</font>"), SaveAction);
            alertDialog.SetNegativeButton(Html.FromHtml("<font face = 'Comic Sans MS, arial' color='#dc3545' size = '10'>No</font>"), CancelaAction);
            alertDialog.Create();
            alertDialog.Show();
        }

        private void SaveAction(object sender, DialogClickEventArgs e)
        {
            thisConnection.Open();
            string cadena = "UPDATE tb_det_Etiqueta SET Estatus = 'C' WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND Split = '" + split.ToString() + "'";
            SqlCommand cmd = new SqlCommand(cadena, thisConnection);
            cmd.ExecuteNonQuery();

            string cadenados = "UPDATE tb_det_split SET estatus = 'C' WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND tarima = '" + split.ToString() + "'";
            SqlCommand cmddos = new SqlCommand(cadenados, thisConnection);
            cmddos.ExecuteNonQuery();

            string Cadena = "Select * From tb_det_split WHERE emb_folio = '" + pedidocancelar.ToString() + "' AND tarima = '" + split.ToString() + "' ";
            SqlDataAdapter da = new SqlDataAdapter(Cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "Ped");
            DataTable Ped = ds.Tables["Ped"];
            foreach (DataRow row in Ped.Rows)
            {
                if (row["tipo_rec"].ToString().Trim() == "PTC")
                    cadena = "UPDATE TB_DET_TRAZABILIDAD SET SURTIDO = SURTIDO - " + row["cajas"].ToString().Trim() + " WHERE PROD_CLAVE = '" + row["prod_clave"].ToString().Trim() + "' AND RECIBO = '" + row["no_lote"].ToString().Trim() + "' " +
                        "AND TIPO = 'PTC' AND TARIMA = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";

                else
                    cadena = "UPDATE TB_DET_ETI_FINAL SET CAJAS_SUR = CAJAS_SUR - " + row["cajas"].ToString().Trim() + " WHERE CVE_PROD = '" + row["prod_clave"].ToString().Trim() + "' AND FOLIO = '" + row["no_lote"].ToString().Trim() + "' " +
                        "AND TARIMA = '" + Convert.ToInt32(row["TARINI"].ToString().Trim()).ToString() + "' ";
                cmd = new SqlCommand(cadena, thisConnection);
                cmd.ExecuteNonQuery();


            }

            Android.Telephony.TelephonyManager mTelephonyMgr;
            mTelephonyMgr = (Android.Telephony.TelephonyManager)GetSystemService(TelephonyService);
            //IMEI number  
            string imei = mTelephonyMgr.DeviceId;


            string cadenas = "INSERT INTO TB_REGISTRO_MOVIMIENTOS(FECHA,NOM_COMPU,NOM_USU,TIPO_MOV,OP_CLAVE,FOLIO,DETALLE,SISTEMA,MOV_FOLIO) " +
                            "VALUES('" + System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "','CEL " + imei + "','" + crcancelar.Trim() + "','C','7.10','" +
                            pedidocancelar.ToString().Trim() + "','Cancelacion Split " + split.ToString() + "','SPLIT','" + pedidocancelar.ToString().Trim() + "')";
            //MessageBox.Show(cadena);
            SqlCommand cmds = new SqlCommand(cadenas, thisConnection);
            cmds.ExecuteNonQuery();


            thisConnection.Close();


            Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
            alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Split Cancelado</font>"));
            alertDialog.SetIcon(Resource.Drawable.exito);
            alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>Split Cancelado Correctamente!!! </font>"));
            alertDialog.SetCancelable(false);
            alertDialog.SetNeutralButton("Ok", delegate
            {
                alertDialog.Dispose();
                pedidocan.Text = "";
                cansplit.Text = "000|000";
                List<FlimStarInfo> lstFlimStar = ConsSplit();
                lstFlimStar.Clear();
                var gvObject = FindViewById<GridView>(Resource.Id.gvCtrCancel);
                gvObject.Adapter = new myGVItemAdapter(this, null);
                gvObject.Adapter = null;
                gvObject.Adapter = new myGVItemAdapter(this, lstFlimStar);

            });
            alertDialog.Show();

        }

        private void CancelaAction(object sender, DialogClickEventArgs e)
        {
            return;
        }

        List<FlimStarInfo> listItem = new List<FlimStarInfo>();

        List<FlimStarInfo> GetFlimStarInformation()
        {
            throw new NotImplementedException();
        }

        List<FlimStarInfo> ConsSplit()
        {
            string Existe = "N";
            int cantidadsplit = 0;
            thisConnection.Open();
            listItem.Clear();
            string contenido = "";
            //thisConnection.Open();
            string cadena = "Select DISTINCT(tarima) AS NoSplit from tb_det_split where emb_folio = '" + pedidocan.Text.Trim() + "' AND estatus != 'C'";
            SqlDataAdapter da = new SqlDataAdapter(cadena, thisConnection);
            DataSet ds = new DataSet();
            da.Fill(ds, "ConsPed");
            DataTable ConsPed = ds.Tables["ConsPed"];

            foreach (DataRow Row in ConsPed.Rows)
            {
                Existe = "S";
                listItem.Add(new FlimStarInfo()
                {
                    Name = "Split Numero: " + Row["NoSplit"].ToString().Trim(),
                    Age = "Para Cancelar de Clic Aqui",
                    ImageID = Resource.Drawable.producto
                });
                cantidadsplit++;
            }

            cansplit.Text = cantidadsplit.ToString();

            if (Existe != "S")
            {
                Android.App.AlertDialog.Builder alertDialog = new Android.App.AlertDialog.Builder(this);
                alertDialog.SetTitle(Html.FromHtml("<font color='#dc3545' size = 10>Pedido Sin Split/font>"));
                alertDialog.SetIcon(Resource.Drawable.no);
                alertDialog.SetCancelable(false);
                alertDialog.SetMessage(Html.FromHtml("<font color='#FFFFFF' size = 10>El pedido: " + pedidocan.Text.Trim() + " No cuenta con split disponible</font>"));
                alertDialog.SetNeutralButton("Ok", delegate
                {
                    alertDialog.Dispose();
                });
                alertDialog.Show();
            }

            //LbxCons.Font = new Font(LbxCons.Font.Name, 7);   ;
            thisConnection.Close();

            return listItem;
        }

    }
}