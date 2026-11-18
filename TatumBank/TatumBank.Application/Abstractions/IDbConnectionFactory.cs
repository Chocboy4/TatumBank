using MySqlConnector;

namespace TatumBank.Application.Abstractions
{
    public interface IDbConnectionFactory
    {
        MySqlConnection CreateConnection();
    }
}
