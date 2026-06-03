using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sanalink.API.Data;
using Sanalink.API.Infrastructure;
using Sanalink.API.Middleware;
using Sanalink.API.Models;
using Sanalink.API.Services;
using Sanalink.API.Services.Fhir;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
{
    var betterStackToken = context.Configuration["BetterStack:SourceToken"];

    config.ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/sanalink-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14
        );

    if (!string.IsNullOrWhiteSpace(betterStackToken))
        config.WriteTo.BetterStack(sourceToken: betterStackToken);
});

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// ── Compliance feature flags (read from config directly — IFeatureManager not available pre-Build) ──
var flags = builder.Configuration.GetSection("FeatureManagement");
bool httpsEnabled           = flags.GetValue<bool>(FeatureFlags.ISO27001_Https);
bool swaggerGated           = flags.GetValue<bool>(FeatureFlags.ISO27001_SwaggerGate);
bool rateLimitingEnabled    = flags.GetValue<bool>(FeatureFlags.ISO27001_RateLimiting);
bool lockoutEnabled         = flags.GetValue<bool>(FeatureFlags.ISO27001_AccountLockout);
bool tokenRevocationEnabled = flags.GetValue<bool>(FeatureFlags.ISO27001_TokenRevocation);
bool dataRetentionEnabled   = flags.GetValue<bool>(FeatureFlags.ISO27001_DataRetention);
bool dhis2Enabled           = flags.GetValue<bool>(FeatureFlags.DHIS2);
bool fhirEnabled            = flags.GetValue<bool>(FeatureFlags.FHIR);

// ── Feature management (IFeatureManager for runtime checks inside controllers/services) ──
builder.Services.AddFeatureManagement();

// ── Domain services ──
builder.Services.AddScoped<IRoleSeeder, RoleSeeder>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IEncounterService, EncounterService>();
builder.Services.AddScoped<IFacilityService, FacilityService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ILabOrderService, LabOrderService>();
builder.Services.AddScoped<IPharmacyDispenseService, PharmacyDispenseService>();

// ── ISO 27001: Token Revocation (service always registered; middleware added conditionally below) ──
builder.Services.AddScoped<ITokenRevocationService, TokenRevocationService>();

// ── ISO 27001: Data Retention ──
builder.Services.AddScoped<IDataRetentionService, DataRetentionService>();
if (dataRetentionEnabled)
    builder.Services.AddHostedService<DataRetentionBackgroundService>();

// ── DHIS2 export ──
builder.Services.AddHttpClient<Dhis2ExportService>();
builder.Services.AddScoped<IDhis2ExportService, Dhis2ExportService>();
if (dhis2Enabled)
    builder.Services.AddHostedService<Dhis2ExportBackgroundService>();

// ── FHIR mapping ──
if (fhirEnabled)
    builder.Services.AddScoped<FhirMappingService>();

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                new Uri(origin).Host.EndsWith(".vercel.app"))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ── Database ──
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

Log.Information("Current Environment: {Environment}", builder.Environment.EnvironmentName);
Log.Information("Connection string configured for: {Database}", connectionString?.Split(';').FirstOrDefault());

// ── Identity — lockout options driven by feature flag ──
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit          = true;
    options.Password.RequiredLength        = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase      = false;
    options.Password.RequireLowercase      = false;

    // ISO 27001 A.9: Account lockout
    options.Lockout.AllowedForNewUsers        = lockoutEnabled;
    options.Lockout.MaxFailedAccessAttempts   = lockoutEnabled ? 5 : int.MaxValue;
    options.Lockout.DefaultLockoutTimeSpan    = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT authentication ──
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken              = true;
    options.RequireHttpsMetadata   = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = false,
        ValidateAudience         = false,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ── ISO 27001 A.14: Rate Limiting ──
// Policy is always registered; PermitLimit is set very high (≈ off) when the flag is disabled.
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter(FeatureFlags.RatePolicy_AuthLogin, opt =>
    {
        opt.PermitLimit            = rateLimitingEnabled ? 10 : 1_000_000;
        opt.Window                 = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow      = 6;
        opt.QueueProcessingOrder   = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit             = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Swagger / OpenAPI ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Sanalink.API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ──
var app = builder.Build();

// ── ISO 27001 A.14: HTTPS enforcement ──
if (httpsEnabled)
{
    app.UseHttpsRedirection();
    if (!app.Environment.IsDevelopment())
        app.UseHsts();
}

// ── ISO 27001 A.14: Swagger gated to development only ──
if (!swaggerGated || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(app.Environment.IsDevelopment() ? "AllowLocalhost" : "AllowVercel");
app.UseAuthentication();
app.UseMiddleware<AuditLoggingMiddleware>();

// ── ISO 27001 A.9: Token revocation check (runs after UseAuthentication, before UseAuthorization) ──
if (tokenRevocationEnabled)
    app.UseMiddleware<TokenRevocationMiddleware>();

app.UseRateLimiter();
app.UseAuthorization();
app.UseSerilogRequestLogging();
app.MapControllers();

// ── Startup: migrate (prod only) + seed ──
using (var scope = app.Services.CreateScope())
{
    var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

    if (env.IsProduction())
        db.Database.Migrate();

    var seeder = scope.ServiceProvider.GetRequiredService<IRoleSeeder>();
    await seeder.SeedRolesAsync();
    await seeder.SeedAdminUserAsync();
}

await app.RunAsync();
