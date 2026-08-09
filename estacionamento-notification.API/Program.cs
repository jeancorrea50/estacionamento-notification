using System.Text;
using System.Security.Claims;
using EstacionamentoNotification.API.Hubs;
using EstacionamentoNotification.API.Realtime;
using EstacionamentoNotification.Application.Abstractions;
using EstacionamentoNotification.Application.Commands.CriarNotificacao;
using EstacionamentoNotification.Domain.Interfaces;
using EstacionamentoNotification.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicy = "NotificationCors";

var pathBase = builder.Configuration["PathBase"]?.Trim();
if (!string.IsNullOrWhiteSpace(pathBase) && !pathBase.StartsWith('/'))
    pathBase = "/" + pathBase;
pathBase = string.IsNullOrWhiteSpace(pathBase) ? null : pathBase.TrimEnd('/');

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Estacionamento Notification API",
        Version = "v1",
        Description =
            "API de notificações em tempo real (SignalR hub `/hubs/notificacao`, evento `notificacaoRecebida`). " +
            "Endpoints Admin usam JWT Bearer; endpoints internal usam header `X-Internal-Key`."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT do login. Formato: Bearer {seu token}"
    });

    c.AddSecurityDefinition("X-Internal-Key", new OpenApiSecurityScheme
    {
        Name = "X-Internal-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Chave interna (Notification:InternalKey) para POST /api/internal/notificacoes"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "X-Internal-Key"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CentralConnection")));

builder.Services.AddScoped<INotificacaoRepository, NotificacaoRepository>();
builder.Services.AddScoped<INotificacaoRealtimePublisher, NotificacaoRealtimePublisher>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CriarNotificacaoCommand).Assembly));
builder.Services.AddSignalR();

var jwtSection = builder.Configuration.GetSection("BearerTokenSettings");
var secret = jwtSection["Secret"] ?? string.Empty;
var key = Encoding.ASCII.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    // Igual ao estacionamento-backend: sem remap inbound.
    // Com MapInboundClaims=true, "role" vira URI longa e [Authorize(Roles="Admin")] falha.
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["ValidOn"],
        RoleClaimType = "role",
        NameClaimType = "unique_name"
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            // Normaliza URI longa → "role" caso o token traga ClaimTypes.Role sem outbound map.
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                foreach (var claim in identity.FindAll(ClaimTypes.Role).ToList())
                {
                    if (!identity.HasClaim("role", claim.Value))
                        identity.AddClaim(new Claim("role", claim.Value));
                }

                var nameId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(nameId) && !identity.HasClaim("nameid", nameId))
                    identity.AddClaim(new Claim("nameid", nameId));
            }

            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Após UsePathBase, Path já vem sem o prefixo (/hubs/...).
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    var swaggerJson = string.IsNullOrWhiteSpace(pathBase)
        ? "/swagger/v1/swagger.json"
        : $"{pathBase}/swagger/v1/swagger.json";
    c.SwaggerEndpoint(swaggerJson, "Estacionamento Notification API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors(CorsPolicy);
// HTTP atrás de porta publicada / gateway — não redirecionar para HTTPS.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificacaoHub>(NotificacaoHub.HubPath);

app.Run();
