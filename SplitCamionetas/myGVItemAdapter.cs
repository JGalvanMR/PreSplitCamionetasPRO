using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using PreSplitCamionetas.Modal;

namespace PreSplitCamionetas
{
    public class myGVItemAdapter : BaseAdapter<FlimStarInfo>
    {
        private readonly Activity _currentContext;

        // FIX: Copia defensiva inmutable — el adapter nunca comparte referencia
        // con la lista viva de la Activity. Así, modificaciones concurrentes a
        // listItem (Add/Remove durante escaneo o eliminación) no corrompen la
        // vista ni lanzan ArgumentOutOfRangeException en GetView().
        private readonly List<FlimStarInfo> _items;

        public myGVItemAdapter(Activity currentContext, List<FlimStarInfo> lstFlimInfo)
        {
            _currentContext = currentContext;

            // FIX: Copia defensiva. Si llega null se inicializa lista vacía.
            // Antes: el adapter guardaba la referencia original → cualquier
            // listItem.Add() desde otro hilo corrompía la iteración interna
            // del adapter y lanzaba IndexOutOfRangeException o
            // ArgumentOutOfRangeException.
            _items = lstFlimInfo != null
                ? new List<FlimStarInfo>(lstFlimInfo)
                : new List<FlimStarInfo>();
        }

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            // FIX: Guard de bounds. Antes: acceso directo sin validar →
            // ArgumentOutOfRangeException cuando el adapter recibía un position
            // inválido (p.ej. tras una eliminación concurrente, o cuando Android
            // llama GetView() con position = Count-1 justo después de un Clear).
            if (position < 0 || position >= _items.Count)
            {
                // Devuelve una vista vacía en vez de lanzar excepción.
                return convertView ?? _currentContext.LayoutInflater
                    .Inflate(Resource.Layout.custGridViewItem, null);
            }

            var item = _items[position];

            if (convertView == null)
                convertView = _currentContext.LayoutInflater
                    .Inflate(Resource.Layout.custGridViewItem, null);

            convertView.FindViewById<TextView>(Resource.Id.txtName).Text = item.Name ?? string.Empty;
            convertView.FindViewById<TextView>(Resource.Id.txtAge).Text = item.Age ?? string.Empty;
            convertView.FindViewById<ImageView>(Resource.Id.imgPers).SetImageResource(item.ImageID);

            return convertView;
        }

        // FIX: Retorna 0 cuando la lista es null/vacía en vez de -1.
        // Antes: Count devolvía -1 → Android llamaba GetView(-1) →
        // ArgumentOutOfRangeException inmediato. Con 0 el GridView
        // simplemente no dibuja ítems.
        public override int Count => _items.Count;

        public override FlimStarInfo this[int position]
            => (position >= 0 && position < _items.Count) ? _items[position] : null;
    }
}
