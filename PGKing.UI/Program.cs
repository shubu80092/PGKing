using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using PGKing.Infrastructure.Data;
using PGKing.UI.Services;
using PGKing.UI.Middlewares;
using PGKing.Application.Interfaces.Repositories;
using PGKing.Infrastructure.Repositories;
using PGKing.Application.Interfaces.Services;
using PGKing.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddTransient<IStorageService, StorageService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ISuperAdminRepository, SuperAdminRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<PGKing.Application.Interfaces.Services.IEmailService, PGKing.Infrastructure.Services.EmailService>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Configure Forwarded Headers for Render/Reverse Proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Added EnableRetryOnFailure to avoid timeout errors during startup
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31)), 
        mySqlOptions => mySqlOptions.EnableRetryOnFailure());
});

// Configure Authentication (Supporting both Cookies and JWT for migration phase)
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_FOR_JWT_THAT_IS_LONG_ENOUGH_123!";
builder.Services.AddAuthentication(options => {
    // Default to Cookies for existing web routes, but allow JWT explicitly
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
    
    // Return standard JSON for 401 and 403 instead of empty body
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse(); // Suppress default empty response
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(PGKing.Application.DTOs.ApiResponse<object>.Fail("Unauthorized: Invalid or missing Bearer token."), new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(result);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(PGKing.Application.DTOs.ApiResponse<object>.Fail("Forbidden: You do not have permission to access this resource."), new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(result);
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PGKing API", Version = "v1" });
    
    // Configure JWT for Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Failsafe copy of logo to favicon.ico if logo.png exists
try
{
    var webRootPath = app.Environment.WebRootPath;
    if (!string.IsNullOrEmpty(webRootPath))
    {
        var logoPath = Path.Combine(webRootPath, "images", "logo.png");
        var faviconPath = Path.Combine(webRootPath, "favicon.ico");
        if (File.Exists(logoPath))
        {
            File.Copy(logoPath, faviconPath, true);
        }
    }
}
catch { }

// Ensure invariant/en-US culture for number formatting (Latitude/Longitude floating point parsing)
// Fallback to InvariantCulture if running in Globalization Invariant Mode (DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true)
System.Globalization.CultureInfo defaultCulture;
try
{
    defaultCulture = new System.Globalization.CultureInfo("en-US");
}
catch (System.Globalization.CultureNotFoundException)
{
    defaultCulture = System.Globalization.CultureInfo.InvariantCulture;
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = new List<System.Globalization.CultureInfo> { defaultCulture },
    SupportedUICultures = new List<System.Globalization.CultureInfo> { defaultCulture }
});

app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PGKing API v1"));

// Automatically apply migrations on startup

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        try { context.Database.Migrate(); } catch { }

        try
        {
            context.Database.ExecuteSqlRaw(@"
DROP PROCEDURE IF EXISTS AddColumnIfNotExists;
CREATE PROCEDURE AddColumnIfNotExists(
    IN tableName VARCHAR(255),
    IN columnName VARCHAR(255),
    IN columnDefinition TEXT
)
BEGIN
    DECLARE colExists INT DEFAULT 0;
    SELECT COUNT(*) INTO colExists
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = tableName
      AND COLUMN_NAME = columnName;
    
    IF colExists = 0 THEN
        SET @sqlstmt = CONCAT('ALTER TABLE `', tableName, '` ADD COLUMN `', columnName, '` ', columnDefinition);
        PREPARE stmt FROM @sqlstmt;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END;
");
            context.Database.ExecuteSqlRaw("CALL AddColumnIfNotExists('Properties', 'Area', 'VARCHAR(100) NULL');");
            context.Database.ExecuteSqlRaw("CALL AddColumnIfNotExists('Properties', 'CityName', 'VARCHAR(100) NULL');");
            context.Database.ExecuteSqlRaw("CALL AddColumnIfNotExists('Properties', 'StateName', 'VARCHAR(100) NULL');");
            context.Database.ExecuteSqlRaw("CALL AddColumnIfNotExists('Properties', 'PropertySlug', 'VARCHAR(200) NULL');");
            context.Database.ExecuteSqlRaw("CALL AddColumnIfNotExists('Properties', 'LocationSlug', 'VARCHAR(200) NULL');");
            context.Database.ExecuteSqlRaw("CALL AddColumnIfNotExists('Properties', 'CanonicalUrl', 'VARCHAR(500) NULL');");

            context.Database.ExecuteSqlRaw(@"
UPDATE `Properties` SET `Area` = 'Bhandup West' WHERE `Id` = 1 AND (`Area` IS NULL OR `Area` = '');
UPDATE `Properties` SET `Area` = 'Powai' WHERE `Id` = 2 AND (`Area` IS NULL OR `Area` = '');
UPDATE `Properties` SET `Area` = 'Andheri East' WHERE `Id` = 3 AND (`Area` IS NULL OR `Area` = '');
UPDATE `Properties` SET `Area` = 'Bhandup West' WHERE (`Area` IS NULL OR `Area` = '');

UPDATE `Properties` SET `CityName` = 'Mumbai' WHERE (`CityName` IS NULL OR `CityName` = '');
UPDATE `Properties` SET `StateName` = 'Maharashtra' WHERE (`StateName` IS NULL OR `StateName` = '');

UPDATE `Properties`
SET 
    `LocationSlug` = CONCAT('pg-in-', LOWER(REPLACE(REPLACE(TRIM(`Area`), ' ', '-'), '--', '-')), '-', LOWER(REPLACE(REPLACE(TRIM(`CityName`), ' ', '-'), '--', '-'))),
    `PropertySlug` = LOWER(REPLACE(REPLACE(REPLACE(REPLACE(TRIM(`Title`), ' ', '-'), '.', ''), ',', ''), '--', '-'))
WHERE `PropertySlug` IS NULL OR `PropertySlug` = '';

UPDATE `Properties`
SET `CanonicalUrl` = CONCAT('https://pgking.in/', `LocationSlug`, '/', `PropertySlug`)
WHERE `CanonicalUrl` IS NULL OR `CanonicalUrl` = '';
");
        }
        catch (Exception sqlEx)
        {
            var log = services.GetRequiredService<ILogger<Program>>();
            log.LogWarning(sqlEx, "Failsafe schema verification note.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// IMPORTANT: Enable serving static files from wwwroot (needed for uploads)
app.UseStaticFiles();

app.UseRouting();

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Existing (old) property details route for HTTP 301 Permanent Redirect
app.MapControllerRoute(
    name: "propertyDetailsOld",
    pattern: "paying-guests/{slug}",
    defaults: new { controller = "Home", action = "PropertyDetailsOld" })
    .WithStaticAssets();

// SEO Location Listing page route: e.g. /pg-in-bhandup-west-mumbai
app.MapControllerRoute(
    name: "locationPropertiesSeo",
    pattern: "pg-in-{locationSlug}",
    defaults: new { controller = "Home", action = "LocationPropertiesSeo" })
    .WithStaticAssets();

// New SEO Property Details route: e.g. /pg-in-bhandup-west-mumbai/janteswar-society
app.MapControllerRoute(
    name: "propertyDetailsSeo",
    pattern: "{locationSlug:regex(^pg-in-.*)}/{propertySlug}",
    defaults: new { controller = "Home", action = "PropertyDetailsSeo" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();