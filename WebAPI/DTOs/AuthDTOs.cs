namespace WebAPI.DTOs;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Accesstoken);
