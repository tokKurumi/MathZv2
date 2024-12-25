using Asp.Versioning;
using MathZv2.ServiceDefaults;
using MathZv2.Services.DocumentationScanner.Models;
using MathZv2.Services.DocumentationScanner.Services;
using MathZv2.Services.DocumentationScanner.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCors();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddApiVersioning(settings =>
{
    settings.DefaultApiVersion = new ApiVersion(1, 0);
    settings.ApiVersionReader = new UrlSegmentApiVersionReader();
    settings.AssumeDefaultVersionWhenUnspecified = true;
    settings.ReportApiVersions = true;
})
.AddApiExplorer(settings =>
{
    settings.GroupNameFormat = "'v'VVV";
    settings.SubstituteApiVersionInUrl = true;
});

builder.Services.Configure<NugetConfig>(builder.Configuration.GetSection(nameof(NugetConfig)));
builder.Services.Configure<DocumentsConfig>(builder.Configuration.GetSection(nameof(DocumentsConfig)));

builder.AddQdrantClient("qdrant");
builder.AddOllamaSharpEmbeddingGenerator("phi35");

builder.Services.AddScoped<IScanDocumentsService, ScanDocumentsService>();
builder.Services.AddScoped<IRagSearchService, RagSearchService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors(settings =>
    {
        settings.AllowAnyHeader();
        settings.AllowAnyMethod();
        settings.AllowAnyOrigin();
    });
}

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler();

app.UseRequestTimeouts();
app.UseOutputCache();

app.MapDefaultEndpoints();

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .Build();

var api = app.MapGroup("api/v{version:apiVersion}").WithApiVersionSet(versionSet);

api.MapPost("scan", async (IScanDocumentsService scanService, CancellationToken cancellationToken) =>
{
    await scanService.ScanAsync(cancellationToken);
    return Results.Ok();
});

api.MapGet("search", async (IRagSearchService ragSearchService, [FromQuery] string query, CancellationToken cancellationToken) =>
{
    var results = await ragSearchService.SearchAsync(query, cancellationToken);
    return Results.Ok(results);
});

api.MapGet("ping", () =>
{
    return Results.Ok("pong");
});

await app.RunAsync();