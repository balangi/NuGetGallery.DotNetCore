using Microsoft.Data.SqlClient; 

namespace NuGet.Services.Sql;

public interface ISqlConnectionFactory
{
    bool TryCreate(out SqlConnection sqlConnection);
    Task<SqlConnection> CreateAsync();
    Task<SqlConnection> OpenAsync();
}
