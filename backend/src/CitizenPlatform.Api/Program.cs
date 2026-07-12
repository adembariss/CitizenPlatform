using System.Globalization;
using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Infrastructure.Identity;
using CitizenPlatform.Infrastructure.Seeding;

// Form/query model binding is culture-sensitive; pin to invariant so "41.05" parses the
// same regardless of the host OS locale (tr-TR parses it as 4105 otherwise).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    JwtOptions.EnsureProductionSecret(builder.Configuration);
}

builder.Services.AddCitizenPlatformApi(builder.Configuration);

var app = builder.Build();

app.UseGlobalExceptionHandling();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsOptionsResolver.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    using var seedScope = app.Services.CreateScope();
    var seeder = seedScope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

app.Run();

public partial class Program;
