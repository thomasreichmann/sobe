using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sobe.Api.Data;
using Sobe.Api.Infra;
using Sobe.Api.Repositories;
using Sobe.Api.Security;
using Sobe.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>().BindConfiguration("Jwt")
    .Validate(o => Encoding.UTF8.GetByteCount(o.Chave) >= 32, "Configure Jwt:Chave com pelo menos 32 bytes.")
    .ValidateOnStart();
builder.Services.AddOptions<DispositivoOptions>().BindConfiguration("Dispositivos")
    .Validate(o => o.Chave.Length >= 16, "Configure Dispositivos:Chave com pelo menos 16 caracteres.")
    .ValidateOnStart();

builder.Services.AddDbContext<SobeDbContext>((servicos, opcoes) =>
    opcoes.UseOracle(servicos.GetRequiredService<IConfiguration>().GetConnectionString("Oracle")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IEstacaoRepository, EstacaoRepository>();
builder.Services.AddScoped<IEquipamentoRepository, EquipamentoRepository>();
builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<IAlertaRepository, AlertaRepository>();
builder.Services.AddScoped<IFavoritoRepository, FavoritoRepository>();
builder.Services.AddScoped<IRelatoRepository, RelatoRepository>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EstacaoService>();
builder.Services.AddScoped<EquipamentoService>();
builder.Services.AddScoped<SensorService>();
builder.Services.AddScoped<AlertaService>();
builder.Services.AddScoped<FavoritoService>();
builder.Services.AddScoped<RelatoService>();
builder.Services.AddScoped<PainelService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer()
    .AddScheme<AuthenticationSchemeOptions, DispositivoAuthHandler>(DispositivoAuthHandler.Esquema, null);
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((opcoes, jwt) =>
    {
        opcoes.MapInboundClaims = false;
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Value.Emissor,
            ValidAudience = jwt.Value.Emissor,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Chave)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.AddPolicy("login", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("Seguranca:LoginsPorMinuto", 10),
            Window = TimeSpan.FromMinutes(1),
        }));
});

builder.Services.AddProblemDetails();
builder.Services.AddControllers(opcoes => opcoes.Filters.Add<FiltroDeErros>()).AddJsonOptions(opcoes =>
    opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sobe API",
        Version = "v1",
        Description = "Situação dos elevadores, escadas rolantes e plataformas das estações, com alertas gerados por sensores.",
    });
    opcoes.EnableAnnotations();
    opcoes.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token devolvido por /api/auth/login.",
    });
    opcoes.AddSecurityDefinition("Dispositivo", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = DispositivoAuthHandler.Cabecalho,
        Description = "Chave usada pelos sensores para enviar leituras.",
    });
    foreach (var esquema in new[] { "Bearer", "Dispositivo" })
    {
        opcoes.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = esquema } }] = [],
        });
    }
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (contexto, proximo) =>
{
    contexto.Response.Headers.XContentTypeOptions = "nosniff";
    contexto.Response.Headers.XFrameOptions = "DENY";
    contexto.Response.Headers["Referrer-Policy"] = "no-referrer";
    await proximo();
});
app.UseSwagger();
app.UseSwaggerUI(opcoes => opcoes.DocumentTitle = "Sobe API");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

public partial class Program;
