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
    class XLoteSug
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string recibosug { get; set; }
        public string fecrecsug { get; set; }
        public string cveprod { get; set; }
        public string Tarima { get; set; }
        public int Cajasdis { get; set; }
        public int Cajasusadas { get; set; }
        public string foliomens { get; set; }

    }
}