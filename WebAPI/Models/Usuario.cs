namespace WebAPI.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Passwd { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

public record User(int Id, string Username, string PasswordHash, string[] Roles);
