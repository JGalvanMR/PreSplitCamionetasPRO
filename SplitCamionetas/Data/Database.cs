using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using PreSplitCamionetas.Models;

namespace PreSplitCamionetas.Data
{
    internal class Database
    {
        private static readonly object _lock = new object();
        private readonly SQLiteAsyncConnection _connection;

        private static Database _instance;

        public static Database Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Database();
                    }
                    return _instance;
                }
            }
        }

        private Database()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string dbPath = Path.Combine(folder, "Pre_Split_Camionetas.db3");
            _connection = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeDatabaseAsync()
        {
            await _connection.CreateTableAsync<ConPedidos>();
            await _connection.CreateTableAsync<Pedidos>();
            await _connection.CreateTableAsync<xLote>();
            await _connection.CreateTableAsync<xLoteFinal>();
            await _connection.CreateTableAsync<XLoteSug>();
            await _connection.CreateTableAsync<xprod>();
            await _connection.CreateTableAsync<Mensajes>();
        }

        public Task<bool> DatabaseExistsAsync()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string dbPath = Path.Combine(folder, "Pre_Split_Camionetas.db3");
            return Task.FromResult(File.Exists(dbPath));
        }

        public Task<List<T>> Table<T>() where T : new()
        {
            return _connection.Table<T>().ToListAsync();
        }

        public Task<int> InsertAsync<T>(T item) where T : new()
        {
            return _connection.InsertAsync(item);
        }

        public Task<int> UpdateAsync<T>(T item) where T : new()
        {
            return _connection.UpdateAsync(item);
        }

        public Task<int> DeleteAsync<T>(T item) where T : new()
        {
            return _connection.DeleteAsync(item);
        }

        public Task<List<T>> GetItemsAsync<T>() where T : new()
        {
            return _connection.Table<T>().ToListAsync();
        }

        public async Task<T> ExecuteScalarAsync<T>(string query, params object[] args)
        {
            var result = await _connection.ExecuteScalarAsync<object>(query, args);
            return (T)Convert.ChangeType(result, typeof(T));
        }

        public Task<List<T>> QueryAsync<T>(string query, params object[] args) where T : new()
        {
            return _connection.QueryAsync<T>(query, args);
        }

        public Task<int> ExecuteAsync(string query, params object[] args)
        {
            return _connection.ExecuteAsync(query, args);
        }

        public Task<List<ConPedidos>> GetConPedidosAsync(string mcod)
        {
            return _connection.Table<ConPedidos>().Where(c => c.prod_clave == mcod).ToListAsync();
        }
    }
}
