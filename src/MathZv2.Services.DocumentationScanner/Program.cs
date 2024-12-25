using MathZv2.ServiceDefaults;
using MathZv2.Services.DocumentationScanner.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<NugetConfig>(builder.Configuration.GetSection(nameof(NugetConfig)));
builder.Services.Configure<DocumentsConfig>(builder.Configuration.GetSection(nameof(DocumentsConfig)));

builder.AddQdrantClient("qdrant");
builder.AddOllamaSharpEmbeddingGenerator("phi35");

builder.Services.AddProblemDetails();

//builder.Services.AddHostedService<DocumentationScannerService>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseRequestTimeouts();
app.UseOutputCache();

app.MapDefaultEndpoints();

await app.RunAsync();