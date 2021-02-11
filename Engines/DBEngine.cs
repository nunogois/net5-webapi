using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace net5_webapi.Engines
{
    public class DBEngine : IDBEngine
    {
        private readonly string _connectionString;

        public DBEngine(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DBConnection");
        }

        public async Task<T> Value<T>(string query, object parameters = null)
        {
            using var conx = new SqlConnection(_connectionString);
            return await conx.QueryFirstOrDefaultAsync<T>(query, parameters);
        }

        public async Task<List<T>> Query<T>(string query, object parameters = null)
        {
            using var conx = new SqlConnection(_connectionString);
            var results = await conx.QueryAsync<T>(query, parameters);
            return results.ToList();
        }

        public async Task<JObject> Json(string query, object parameters = null)
        {
            try
            {
                return JObject.FromObject(await Value<dynamic>(query, parameters));
            }
            catch { }

            return new JObject();
        }

        public async Task<JArray> JsonArray(string query, object parameters = null)
        {
            try
            {
                return JArray.FromObject(await Query<dynamic>(query, parameters));
            }
            catch { }

            return new JArray();
        }

        public async Task<int> Execute(string query, object parameters = null)
        {
            using var conx = new SqlConnection(_connectionString);
            return await conx.ExecuteAsync(query, parameters);
        }
    }
}
