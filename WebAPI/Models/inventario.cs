namespace Minimalapi.JWT.Models;

// Modelo para productos
public class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

// DTO para crear/actualizar producto
public record ProductoDTO(
    string Codigo,
    string Nombre,
    decimal PrecioUnitario,
    int StockMinimo
);

// Modelo para movimientos
public class Movimiento
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Entrada" o "Salida"
    public DateTime FechaMovimiento { get; set; }
    public int UsuarioId { get; set; }
    public string? Observaciones { get; set; }
    public decimal Total { get; set; }
}

// Modelo para detalles de movimiento
public class MovimientoDetalle
{
    public int Id { get; set; }
    public int MovimientoId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

// DTO para registrar un movimiento completo
public record RegistrarMovimientoDTO(
    string Tipo,
    string? Observaciones,
    List<DetalleMovimientoDTO> Detalles
);

public record DetalleMovimientoDTO(
    int ProductoId,
    int Cantidad,
    decimal PrecioUnitario
);
