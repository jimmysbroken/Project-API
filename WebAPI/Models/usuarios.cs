namespace Minimalapi.JWT.Models;

public record usuarios(
    int id,
    string username,
    string passwd,
    bool activo
);

public record usuariosDTO(
    string username,
    string passwd
);

public record usuariosUpdateDTO(
    string username,
    string passwd,
    bool activo
);