using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using Android.App;
using Android.Text;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace PreSplitCamionetas.Models
{
    class Mensajes
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string titulo { get; set; }
        public string mensaje { get; set; }
    }
}