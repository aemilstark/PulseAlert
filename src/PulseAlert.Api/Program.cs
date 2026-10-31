using PulseAlert.Api.Filters;
using PulseAlert.Api.Workers;
using PulseAlert.Core.Telemetry;
using PulseAlert.Infrastructure;
using PulseAlert.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<TelemetryOptions>()
    .Bind(builder.Configuration.GetSection(TelemetryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ApiKeyFilter>();
builder.Services.AddHostedService<TelemetryWorker>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PulseAlertDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
