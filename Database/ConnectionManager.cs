using System.Data.Common;
using System.Data.SqlClient;

namespace Michael.Database
{
    public class ConnectionManager
    {
        public static DbConnection GetMssqlConnection(string connectionString)
        {
            SqlConnection connection = new SqlConnection
            {
                ConnectionString = connectionString
            };

            return connection;
        }
    }
}
