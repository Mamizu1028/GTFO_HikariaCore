using Hikaria.Core.Contracts;
using Hikaria.Core.EntityFramework;
using Hikaria.Core.WebAPI.BackgroundServices;
using Hikaria.Core.WebAPI.Entitites;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Hikaria.Core.WebAPI.Extensions;

internal static class ServicesExtensions
{
    public static void ConfigureSqlServerContext(this IServiceCollection services, IConfiguration config)
    {
        var connectString = config.GetConnectionString("GTFODb");
        services.AddDbContext<GTFODbContext>(builder =>
        {
            builder.UseSqlServer(connectString);
        });
    }

    public static void ConfigureRepositoryWrapper(this IServiceCollection services)
    {
        services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
    }

    public static void ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        options.AddPolicy("AllowAnyPolicy", builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()));
    }

    public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JWTTokenOptions>(config.GetSection("JWT"));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtOpt = config.GetSection("JWT").Get<JWTTokenOptions>();
            byte[] keyBytes = Encoding.UTF8.GetBytes(jwtOpt.SecurityKey);
            var secKey = new SymmetricSecurityKey(keyBytes);
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true, //是否验证Issuer
                ValidateAudience = true, //是否验证Audience
                ValidateIssuerSigningKey = true, //是否验证SecurityKey
                ValidIssuer = jwtOpt.Issuer,
                ValidAudience = jwtOpt.Audience,
                IssuerSigningKey = secKey,
                ValidateLifetime = true, //是否验证失效时间
                ClockSkew = TimeSpan.FromSeconds(5)
            };
        });
    }

    public static void ConfigureBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<LiveLobbyWatchdog>();
    }
}
