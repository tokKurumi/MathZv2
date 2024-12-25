using MathZv2.Services.DocumentationScanner;
using MathZv2.Services.DocumentationScanner.Models;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<NugetConfig>(builder.Configuration.GetSection(nameof(NugetConfig)));
builder.Services.Configure<DocumentsConfig>(builder.Configuration.GetSection(nameof(DocumentsConfig)));

builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<NugetConfig>>().Value);
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<DocumentsConfig>>().Value);

builder.AddServiceDefaults();

builder.AddQdrantClient("qdrant");
builder.AddOllamaSharpEmbeddingGenerator("phi35");

builder.Services.AddHostedService<DocumentationScannerService>();

var host = builder.Build();

await host.RunAsync();