using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CitizenPlatform.Api.Authorization;
using CitizenPlatform.Api.Filters;
using CitizenPlatform.Api.HealthChecks;
using CitizenPlatform.Application.Features.AdminCategories;
using CitizenPlatform.Application.Features.AdminComplaints;
using CitizenPlatform.Application.Features.AdminDashboard;
using CitizenPlatform.Application.Features.AdminDepartments;
using CitizenPlatform.Application.Features.Auth;
using CitizenPlatform.Application.Features.CitizenAccounts;
using CitizenPlatform.Application.Features.Complaints;
using CitizenPlatform.Application.Features.PublicCategories;
using CitizenPlatform.Application.Features.PublicInsights;
using CitizenPlatform.Application.Features.PublicDirectory;
using CitizenPlatform.Infrastructure;
using CitizenPlatform.Infrastructure.Identity;
using CitizenPlatform.Infrastructure.Storage;
using CitizenPlatform.Integrations;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace CitizenPlatform.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCitizenPlatformApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.Filters.Add<ValidationProblemFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

        var maxRequestBodySizeBytes = configuration
            .GetSection(ObjectStorageOptions.SectionName)
            .GetValue<long?>(nameof(ObjectStorageOptions.MaxRequestBodySizeBytes))
            ?? 50L * 1024 * 1024;

        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxRequestBodySizeBytes;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
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
            });
        });

        RegisterComplaintFeature(services);
        RegisterAuthFeature(services);
        RegisterCitizenAccountFeature(services);
        RegisterAdminComplaintFeature(services);
        RegisterAdminDashboardFeature(services);
        RegisterAdminCategoryFeature(services);
        RegisterAdminDepartmentFeature(services);

        services.AddInfrastructure(configuration);
        services.AddIntegrations();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        AddJwtAuthentication(services, configuration);
        AddCors(services, configuration);
        AddRateLimiting(services);

        return services;
    }

    private static void RegisterComplaintFeature(IServiceCollection services)
    {
        services.AddScoped<CreateComplaintCommandHandler>();
        services.AddScoped<AddComplaintAttachmentsCommandHandler>();
        services.AddScoped<TrackComplaintQueryHandler>();
        services.AddScoped<PublicCategoryListQueryHandler>();
        services.AddScoped<PublicStatsQueryHandler>();
        services.AddScoped<PublicComplaintMapQueryHandler>();
        services.AddScoped<ProvinceListQueryHandler>();
        services.AddScoped<DistrictListQueryHandler>();
        services.AddScoped<ComplaintAttachmentUploadService>();
        services.AddScoped<IValidator<CreateComplaintCommand>, CreateComplaintCommandValidator>();
        services.AddScoped<IValidator<AddComplaintAttachmentsCommand>, AddComplaintAttachmentsCommandValidator>();
    }

    private static void RegisterAuthFeature(IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
    }

    private static void RegisterCitizenAccountFeature(IServiceCollection services)
    {
        services.AddScoped<RegisterCitizenCommandHandler>();
        services.AddScoped<CitizenComplaintListQueryHandler>();
        services.AddScoped<CitizenProfileQueryHandler>();
        services.AddScoped<IValidator<RegisterCitizenCommand>, RegisterCitizenCommandValidator>();
    }

    private static void RegisterAdminComplaintFeature(IServiceCollection services)
    {
        services.AddScoped<AdminComplaintListQueryHandler>();
        services.AddScoped<AdminComplaintDetailQueryHandler>();
        services.AddScoped<AdminComplaintHistoryQueryHandler>();
        services.AddScoped<UpdateComplaintStatusCommandHandler>();
        services.AddScoped<AssignComplaintCommandHandler>();
        services.AddScoped<AddAdminCommentCommandHandler>();
    }

    private static void RegisterAdminDashboardFeature(IServiceCollection services)
    {
        services.AddScoped<DashboardSummaryQueryHandler>();
    }

    private static void RegisterAdminCategoryFeature(IServiceCollection services)
    {
        services.AddScoped<AdminCategoryListQueryHandler>();
        services.AddScoped<CreateCategoryCommandHandler>();
        services.AddScoped<UpdateCategoryCommandHandler>();
        services.AddScoped<IValidator<CreateCategoryCommand>, CreateCategoryCommandValidator>();
    }

    private static void RegisterAdminDepartmentFeature(IServiceCollection services)
    {
        services.AddScoped<AdminDepartmentListQueryHandler>();
        services.AddScoped<CreateDepartmentCommandHandler>();
        services.AddScoped<UpdateDepartmentCommandHandler>();
        services.AddScoped<IValidator<CreateDepartmentCommand>, CreateDepartmentCommandValidator>();
    }

    private static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = JwtOptions.Resolve(configuration);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(AuthorizationPolicySetup.Configure);
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = CorsOptionsResolver.ResolveAllowedOrigins(configuration);

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptionsResolver.PolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }

    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partitioned per client IP so one noisy client cannot exhaust the global budget.
            AddPerClientFixedWindowPolicy(options, RateLimitingPolicyNames.AuthLogin, permitLimit: 5);
            AddPerClientFixedWindowPolicy(options, RateLimitingPolicyNames.PublicWrite, permitLimit: 30);
            AddPerClientFixedWindowPolicy(options, RateLimitingPolicyNames.PublicRead, permitLimit: 60);
        });
    }

    private static void AddPerClientFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit)
    {
        options.AddPolicy(policyName, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    }
}
