using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;

namespace PreSplitCamionetas.Models
{
    class xLoteFinal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Tipo { get; set; }
        public string Pedido { get; set; }
        public string Folio { get; set; }
        public string Codigo { get; set; }
        public string Tarima { get; set; }
        public string Cajas { get; set; }
        public string nombre { get; set; }
        public string diacad { get; set; }
        public string mescad { get; set; }
    }
}