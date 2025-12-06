using System.Data;
using Dapper;
using Minimalapi.JWT.Models;

namespace Minimalapi.JWT.Services;

public class ProductoService
{
    private readonly IDbConnection _connection;

    public ProductoService(IDbConnection connection)
    {
        _connection = connection;
    }

    // TODO: Implementar estos métodos
    public async Task<IEnumerable<Producto>> GetAllAsync()
    {
        // Consulta SQL para obtener todos los productos activo
        const string sql = "SELECT * FROM productos WHERE activo = 1";
        return await _connection.QueryAsync<Producto>(sql);

    }

    public async Task<Producto?> GetByIdAsync(int id)
    {
        // Consulta SQL para obtener un producto por ID
        const string sql = "SELECT * FROM productos WHERE id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<Producto>(sql, new { Id = id });

    }

    public async Task<int> CreateAsync(ProductoDTO dto)
    {
        // INSERT y retornar el ID del nuevo producto
        const string sql = @"
            INSERT INTO productos (codigo, nombre, precio_unitario, stock_minimo, activo)
            VALUES (@Codigo, @Nombre, @PrecioUnitario, @StockMinimo, 1);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        return await _connection.ExecuteScalarAsync<int>(sql, dto);

    }

    public async Task<bool> UpdateAsync(int id, ProductoDTO dto)
    {
        // UPDATE del producto
        const string sql = @"
            UPDATE productos 
            SET codigo = @Codigo, nombre = @Nombre, 
                precio_unitario = @PrecioUnitario, stock_minimo = @StockMinimo
            WHERE id = @Id";
        var rows = await _connection.ExecuteAsync(sql, new { Id = id, dto.Codigo, dto.Nombre, dto.PrecioUnitario, dto.StockMinimo });
        return rows > 0;

    }

    public async Task<bool> DeleteAsync(int id)
    {
        // Marcar como inactivo (soft delete)
        const string sql = "UPDATE productos SET activo = 0 WHERE id = @Id";
        var rows = await _connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;

    }
}
