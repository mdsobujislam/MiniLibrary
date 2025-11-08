using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Infrastructure.Data
{
    public class SqlServerConnectionFactory : IDbConnectionFactory
    {
        private readonly string _conn;
        public SqlServerConnectionFactory(IConfiguration cfg) => _conn = cfg.GetConnectionString("DefaultConnection")!;
        public DbConnection CreateConnection() => new SqlConnection(_conn);
    }
}
