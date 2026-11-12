using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace TatumBank.Infrastructure
{
    public interface IDbConnectionFactory
    {
        MySqlConnection CreateConnection();
    }
}
