using CitizenPlatform.Api.Extensions;
using CitizenPlatform.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

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
