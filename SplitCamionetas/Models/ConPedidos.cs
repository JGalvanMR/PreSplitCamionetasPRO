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
    class ConPedidos
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string prod_clave { get; set; }
        public string nombre { get; set; }
        public int pedido { get; set; }
        public int surtido { get; set; }
    }
}