using System.Data;
using Dapper;
using WebAPI.Models;

namespace WebAPI.Services;

public class DatabaseUserService
{
    private readonly IDbConnection _connection;

    public DatabaseUserService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Usuario?> FindByUsernameAsync(string username)
    {
        const string sql = "SELECT id, username, passwd, rol FROM usuarios WHERE username = @Username";
        return await _connection.QueryFirstOrDefaultAsync<Usuario>(sql, new { Username = username });
    }
}
