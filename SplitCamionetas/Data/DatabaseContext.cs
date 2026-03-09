using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using PreSplitCamionetas.Models;

namespace PreSplitCamionetas.Data
{
    internal class DatabaseContext
    {
        private readonly SQLiteAsyncConnection connection;

        public DatabaseContext(string dbPath)
        {
            connection = new SQLiteAsyncConnection(dbPath);

            connection.CreateTableAsync<ConPedidos>().Wait();
            connection.CreateTableAsync<Pedidos>().Wait();
            connection.CreateTableAsync<xLote>().Wait();
            connection.CreateTableAsync<xLoteFinal>().Wait();
            connection.CreateTableAsync<XLoteSug>().Wait();
            connection.CreateTableAsync<xprod>().Wait();
            connection.CreateTableAsync<Mensajes>().Wait();
        }

        public async Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            return await connection.Table<T>().ToListAsync();
        }

        public async Task<List<T>> FilterItemsAsync<T>(string table, string condition) where T : new()
        {
            return await connection.QueryAsync<T>($"SELECT * FROM {table} WHERE {condition}");
        }

        public async Task<T> GetItemAsync<T>(string id) where T : new()
        {
            return await connection.FindAsync<T>(id);
        }

        public async Task<int> SaveItemAsync<T>(T item, bool isInsert) where T : new()
        {
            return (isInsert)
                ? await connection.InsertAsync(item)
                : await connection.UpdateAsync(item);
        }

        public async Task<int> DeleteItemAsync<T>(T item) where T : new()
        {
            return await connection.DeleteAsync(item);
        }
    }
}