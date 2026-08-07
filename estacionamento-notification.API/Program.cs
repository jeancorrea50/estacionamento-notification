using System.Text;
using EstacionamentoNotification.API.Hubs;
using EstacionamentoNotification.API.Realtime;
using EstacionamentoNotification.Application.Abstractions;
using EstacionamentoNotification.Application.Commands.CriarNotificacao;
using EstacionamentoNotification.Domain.Interfaces;
using EstacionamentoNotification.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicy = "NotificationCors";

var pathBase = builder.Configuration["PathBase"];

builder.Services.AddControllers();
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
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
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken)
                    && (path.StartsWithSegments("/hubs")
                        || path.StartsWithSegments($"{pathBase}/hubs")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);

app.UseCors(CorsPolicy);
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificacaoHub>(NotificacaoHub.HubPath);

app.Run();
