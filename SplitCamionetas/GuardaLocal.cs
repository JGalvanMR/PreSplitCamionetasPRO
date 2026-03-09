using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;

namespace PreSplitCamionetas
{
    public class GuardaLocal
    {
        public bool HayConexion(string direccionweb)
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead(direccionweb))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }



        public void creartxt(string error)
        {
            //Java.IO.File sdCard = Android.OS.Environment.ExternalStorageDirectory; Java.IO.File dir = new Java.IO.File(sdCard.AbsolutePath + "/MyFolder"); dir.Mkdirs();
            //Java.IO.File file = new Java.IO.File(dir, "habilitar.txt");

            Java.IO.File sdCard = Android.App.Application.Context.GetExternalFilesDir(null);

            Java.IO.File dir = new Java.IO.File(sdCard.AbsolutePath + "/MyFolder"); dir.Mkdirs();
            Java.IO.File file = new Java.IO.File(dir, "errores.txt");
            string FileToRead = file.ToString();
            if (!file.Exists())
            {
                file.CreateNewFile();
                file.Mkdir();
                FileWriter writer = new FileWriter(file); // Writes the content to the file 
                writer.Write(error + System.Environment.NewLine);
                writer.Flush();
                writer.Close();
            }
            else
            {
                string rutaarchivo = file.ToString();
                System.IO.File.AppendAllText(rutaarchivo, error + System.Environment.NewLine);
            }

        }
    }
}