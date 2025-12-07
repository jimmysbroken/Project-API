using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Microsoft.OpenApi.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using Minimalapi.JWT.Models;
using Minimalapi.JWT.Services;
using WebAPI.Services;
using WebAPI.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ----- Controllers ----- //
builder.Services.AddControllers();

// ----- Swagger ----- //
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Práctica 6 - JWT + Roles",
        Version = "v1"
    });

    //Config para que Swagger permita enviar el JWT
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
// --- HealtChecks básicos --- //
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
builder.Services.AddScoped<MovimientoService>();
var app = builder.Build();

// --- Swwagger UI --- //
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- Prometheus metrics --- //
app.UseHttpMetrics(); // mide las requests HTTP

app.UseAuthentication();
app.UseAuthorization();

// Mapear Controllers
app.MapControllers();

// HealthChecks
app.MapHealthChecks("/health");

// Métricas de Prometheus
app.MapMetrics("/metrics");

app.Run();
