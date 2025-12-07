using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Microsoft.OpenApi.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Minimalapi.JWT.Models;   // Asegúrate de que estos namespaces existan en tu proyecto
using Minimalapi.JWT.Services; // Asegúrate de que estos namespaces existan en tu proyecto
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ----- Swagger ----- //
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Práctica 6 - JWT + Roles",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Escribe: Bearer {tu token JWT}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    
    options.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    };
    options.AddSecurityRequirement(securityRequirement);
});

// IDbConnection para Dapper
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'");
    return new SqlConnection(connectionString);
});

// --- HealthChecks básicos --- //
builder.Services.AddHealthChecks();

// 1) Auth JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

// RATE LIMITER (Configuración Correcta)
// --- BORRA EL BLOQUE ANTERIOR DE AddRateLimiter Y PEGA ESTE ---

builder.Services.AddRateLimiter(options =>
{
    // Código de error cuando te bloquean (429)
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // DEFINIR UN LÍMITE GLOBAL PARTICIONADO POR IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Obtener la IP del cliente (si es localhost puede ser ::1 o 127.0.0.1)
        var userIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Crear una partición (regla) única para esa IP
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,       // Máximo 10 peticiones...
                Window = TimeSpan.FromSeconds(10), // ...cada 10 segundos
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // No dejar a nadie en espera, rechazar directo
            });
    });
});

// 2) Roles / Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Admin");
    });
});

// 3) Dependencias para usuarios
builder.Services.AddScoped<DatabaseUserService>();
builder.Services.AddScoped<ProductoService>();

var app = builder.Build();

// --- Swagger UI --- //
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- Prometheus metrics --- //
app.UseHttpMetrics(); 

app.UseAuthentication();
app.UseAuthorization();

// IMPORTANTE: Activar el middleware de Rate Limiting antes de los endpoints
app.UseRateLimiter(); 

// 4) Endpoint de login: POST /auth/login
// CORRECCIÓN: .RequireRateLimiting va AL FINAL
app.MapPost("/auth/login", async (LoginRequest request, DatabaseUserService userService, IConfiguration config) =>
{
    var usuario = await userService.FindByUsernameAsync(request.Username);

    if (usuario is null)
    {
        return Results.Unauthorized();
    }

    if (usuario.Passwd != request.Password)
    {
        return Results.Unauthorized();
    }

    var user = new User(usuario.Id, usuario.Username, usuario.Passwd, new[] { usuario.Rol });
    var token = JwtTokenService.GenerateJwtToken(user, config);

    return Results.Ok(new LoginResponse(token));
})
.RequireRateLimiting("fixed") // <--- AQUÍ VA
.WithTags("Auth");

// 5) Endpoint público
app.MapGet("/public/ping", () => Results.Ok("pong"))
   .RequireRateLimiting("fixed")
   .AllowAnonymous()
   .WithTags("Public");

// 6) Endpoint protegido (cualquier usuario autenticado)
app.MapGet("/api/me", (ClaimsPrincipal user) =>
{
    var username = user.Identity?.Name;
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);

    return Results.Ok(new
    {
        username,
        roles
    });
})
.RequireRateLimiting("fixed")
.RequireAuthorization()
.WithTags("User");

// 7) Endpoint protegido por rol Admin
app.MapGet("/admin/secret", () =>
{
    return Results.Ok("Sólo los admins pueden ver esto.");
})
.RequireRateLimiting("fixed")
.RequireAuthorization("AdminOnly")
.WithTags("Admin");

// --- ENDPOINTS DE PRODUCTOS ---

// GET All
app.MapGet("/api/productos", async (ProductoService productoService) =>
{
    var productos = await productoService.GetAllAsync();
    return Results.Ok(productos);
})
.RequireRateLimiting("fixed")
.RequireAuthorization()
.WithTags("Productos");

// GET por ID
app.MapGet("/api/productos/{id}", async (int id, ProductoService productoService) =>
{
    var producto = await productoService.GetByIdAsync(id);
    return producto is not null ? Results.Ok(producto) : Results.NotFound();
})
.RequireRateLimiting("fixed")
.RequireAuthorization()
.WithTags("Productos");

// POST - Crear producto (solo Admin)
app.MapPost("/api/productos", async (ProductoDTO dto, ProductoService productoService) =>
{
    var id = await productoService.CreateAsync(dto);
    return Results.Created($"/api/productos/{id}", new { id });
})
.RequireRateLimiting("fixed")
.RequireAuthorization("AdminOnly")
.WithTags("Productos");

// PUT - Actualizar producto (solo Admin)
app.MapPut("/api/productos/{id}", async (int id, ProductoDTO dto, ProductoService productoService) =>
{
    var updated = await productoService.UpdateAsync(id, dto);
    return updated ? Results.NoContent() : Results.NotFound();
})
.RequireRateLimiting("fixed")
.RequireAuthorization("AdminOnly")
.WithTags("Productos");

// DELETE - Eliminar producto (solo Admin)
app.MapDelete("/api/productos/{id}", async (int id, ProductoService productoService) =>
{
    var deleted = await productoService.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.RequireRateLimiting("fixed")
.RequireAuthorization("AdminOnly")
.WithTags("Productos");


// 8) Endpoint para ver entorno
app.MapGet("/environment", (IHostEnvironment env, IConfiguration cfg) =>
{
    return Results.Ok(new
    {
        Environment = env.EnvironmentName,
        ApplicationName = env.ApplicationName,
        MachineName = Environment.MachineName
    });
})
.RequireRateLimiting("fixed")
.AllowAnonymous()
.WithTags("Info");

// 9) HealthChecks
app.MapHealthChecks("/health")
   .RequireRateLimiting("fixed")
   .WithTags("Health");

// 10) Endpoint de métricas de Prometheus
app.MapMetrics("/metrics")
   .WithTags("Metrics");

app.Run();

// ======= TIPOS Y SERVICIOS ======== //

// DTOs básicos
public record LoginRequest(string Username, string Password);
public record LoginResponse(string Accesstoken);

// Modelo para la tabla usuarios existente
public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Passwd { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

// User para JWT
public record User(int Id, string Username, string PasswordHash, string[] Roles);

// Servicio de Usuario
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

// Servicio JWT
public static class JwtTokenService
{
    public static string GenerateJwtToken(User user, IConfiguration configuration)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role)); 
        }

        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}