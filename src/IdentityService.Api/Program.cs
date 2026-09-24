using IdentityService.Api.Grpc;
using IdentityService.Api.Middleware;
using IdentityService.Api.Services;
using IdentityService.Application;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Two ports, two protocols: 5000 stays plain HTTP/1.1 for REST/Swagger (and
// is what browsers and simple HTTP clients hit); 5001 is HTTP/2-only for
// gRPC. Splitting them avoids the ALPN/ports gotchas of serving both
// protocols off one Kestrel endpoint and keeps the gRPC port easy to firewall
// off from public traffic in deployment, since only other internal services
// should ever call it.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000, o => o.Protocols = HttpProtocols.Http1);
    options.ListenAnyIP(5001, o => o.Protocols = HttpProtocols.Http2);
});

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.Configure<GrpcSecurityOptions>(builder.Configuration.GetSection(GrpcSecurityOptions.SectionName));
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<InternalApiKeyInterceptor>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity Service", Version = "v1" });

    // JWT support in Swagger UI so a caller can paste a token and try
    // [Authorize] endpoints directly from the docs.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT access token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default")!);

// Other services in the distributed system are expected to sit behind their
// own gateway/BFF; CORS here is deliberately permissive-by-config rather than
// hardcoded, so it can be locked down per environment via appsettings.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Convenience only: applies EF Core migrations automatically on startup
    // in Development so a fresh clone can `dotnet run` against a local
    // SQL Server without a manual `dotnet ef database update` step. Production
    // deployments should run migrations as an explicit release step instead.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGrpcService<UserGrpcService>();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
