using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace net5_webapi.Engines
{
    /// <summary>
    /// Use DBEngine as a template to your own database interface.
    /// In my case: Dapper and SQL Server.
    /// </summary>
    public interface IDBEngine
    {
        /// <summary>
        /// Use Value for SQL statements that return a single row with one or more columns.
        /// </summary>
        Task<T> Value<T>(string query, object parameters = null);

        /// <summary>
        /// Use Query for SQL statements that return a single row with one or more columns.
        /// </summary>
        Task<List<T>> Query<T>(string query, object parameters = null);

        /// <summary>
        /// Use Json to get the result of an SQL statement as JSON (JObject).
        /// </summary>
        Task<JObject> Json(string query, object parameters = null);

        /// <summary>
        /// Use JsonArray to get the result of an SQL statement as a JSON Array (JArray).
        /// </summary>
        Task<JArray> JsonArray(string query, object parameters = null);

        /// <summary>
        /// Use Execute for SQL statements that don't return results: INSERT, UPDATE, DELETE, etc.
        /// </summary>
        Task<int> Execute(string query, object parameters = null);
    }
}
