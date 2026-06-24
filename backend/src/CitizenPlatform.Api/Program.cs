using CitizenPlatform.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCitizenPlatformApi();

var app = builder.Build();

app.UseGlobalExceptionHandling();
app.MapControllers();

app.Run();

public partial class Program;
