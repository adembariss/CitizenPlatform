using CitizenPlatform.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCitizenPlatformApi(builder.Configuration);

var app = builder.Build();

app.UseGlobalExceptionHandling();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
