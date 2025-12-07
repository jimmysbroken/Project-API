using System.Data;
using Dapper;
using Minimalapi.JWT.Models;
using Microsoft.Data.SqlClient;

namespace Minimalapi.JWT.Services;

public class MovimientoService
{
    private readonly IDbConnection _connection;

    public MovimientoService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<int> RegistrarMovimientoAsync(RegistrarMovimientoDTO dto, int usuarioId)
    {
        // Usar transacción para garantizar consistencia
        using var connection = _connection as SqlConnection;
        await connection!.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Calcular el total
            decimal total = dto.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

            // 2. Insertar movimiento (cabecera)
            const string sqlMovimiento = @"
                INSERT INTO movimientos (tipo, usuario_id, observaciones, total)
                VALUES (@Tipo, @UsuarioId, @Observaciones, @Total);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            var movimientoId = await connection.ExecuteScalarAsync<int>(
                sqlMovimiento,
                new { dto.Tipo, UsuarioId = usuarioId, dto.Observaciones, Total = total },
                transaction
            );

            // 3. Insertar detalles y actualizar stock
            foreach (var detalle in dto.Detalles)
            {
                // Insertar detalle
                const string sqlDetalle = @"
                    INSERT INTO movimiento_detalles (movimiento_id, producto_id, cantidad, precio_unitario, subtotal)
                    VALUES (@MovimientoId, @ProductoId, @Cantidad, @PrecioUnitario, @Subtotal)";

                await connection.ExecuteAsync(
                    sqlDetalle,
                    new
                    {
                        MovimientoId = movimientoId,
                        detalle.ProductoId,
                        detalle.Cantidad,
                        detalle.PrecioUnitario,
                        Subtotal = detalle.Cantidad * detalle.PrecioUnitario
                    },
                    transaction
                );

                // Actualizar stock según el tipo de movimiento
                int cantidadCambio = dto.Tipo == "Entrada" ? detalle.Cantidad : -detalle.Cantidad;

                const string sqlUpdateStock = @"
                    UPDATE productos 
                    SET stock_actual = stock_actual + @Cantidad 
                    WHERE id = @ProductoId";

                await connection.ExecuteAsync(
                    sqlUpdateStock,
                    new { Cantidad = cantidadCambio, detalle.ProductoId },
                    transaction
                );
            }

            transaction.Commit();
            return movimientoId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<Movimiento>> GetAllAsync()
    {
        const string sql = "SELECT * FROM movimientos ORDER BY fecha_movimiento DESC";
        return await _connection.QueryAsync<Movimiento>(sql);
    }

    public async Task<Movimiento?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM movimientos WHERE id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<Movimiento>(sql, new { Id = id });
    }

    public async Task<IEnumerable<MovimientoDetalle>> GetDetallesAsync(int movimientoId)
    {
        const string sql = "SELECT * FROM movimiento_detalles WHERE movimiento_id = @MovimientoId";
        return await _connection.QueryAsync<MovimientoDetalle>(sql, new { MovimientoId = movimientoId });
    }
}