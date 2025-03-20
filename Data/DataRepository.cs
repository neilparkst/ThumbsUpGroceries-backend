using Dapper;
using Microsoft.Data.SqlClient;

namespace ThumbsUpGroceries_backend.Data
{
    public class DataRepository
    {
        private readonly string _connectionString;

        public DataRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
        }
    }
}
