using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PainelControlador.Api.Configuration;
using PainelControlador.Api.Middlewares;
using PainelControlador.Api.Services;
using N8nOptions = PainelControlador.Api.Configuration.N8nOptions;
using Serilog;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ---
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// --- CORS ---
const string CorsPolicyName = "DefaultCors";
var corsOpts = new CorsOptions();
builder.Configuration.GetSection(CorsOptions.SectionName).Bind(corsOpts);
var allowedOrigins = corsOpts.GetOrigins();

builder.Services.AddCors(o => o.AddPolicy(CorsPolicyName, p =>
{
    if (allowedOrigins.Length == 0)
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));

// --- N8n Postgres ---
var n8nOpts = new N8nOptions();
builder.Configuration.GetSection(N8nOptions.SectionName).Bind(n8nOpts);
// Fallback: lê direto de env var N8n__ConnectionString
if (!n8nOpts.IsConfigured)
    n8nOpts.ConnectionString = builder.Configuration["N8n__ConnectionString"] ?? "";
builder.Services.AddSingleton(n8nOpts);
builder.Services.AddScoped<IN8nDbService, N8nDbService>();

// --- Supabase REST ---
var sbOpts = builder.Configuration.GetSection(SupabaseOptions.SectionName).Get<SupabaseOptions>()
    ?? new SupabaseOptions();

builder.Services.AddHttpClient<SupabaseRestClient>(client =>
{
    client.BaseAddress = new Uri(sbOpts.Url.TrimEnd('/') + "/rest/v1/");
    client.DefaultRequestHeaders.Add("apikey", sbOpts.Key);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {sbOpts.Key}");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// --- JWT ---
var jwtOpts = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? new JwtOptions();
builder.Services.AddSingleton(jwtOpts);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidAudience = jwtOpts.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Secret)),
        };
    });

builder.Services.AddAuthorization();

// --- Health checks ---
builder.Services.AddHealthChecks();

// --- Services ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IInformationTypeService, InformationTypeService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IHollerithService, HollerithService>();

// --- API ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Log N8n config status at startup so it appears in EasyPanel logs
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("N8n IsConfigured={IsConfigured} | CS={ConnectionString}",
    n8nOpts.IsConfigured,
    string.IsNullOrWhiteSpace(n8nOpts.ConnectionString) ? "(empty)" : n8nOpts.ConnectionString[..Math.Min(40, n8nOpts.ConnectionString.Length)] + "…");

app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health/ready");

app.Run();

public partial class Program { }
