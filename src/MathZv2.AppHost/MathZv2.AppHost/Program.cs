var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder
    .AddOllama("ollama")
    .WithGPUSupport()
    .WithOpenWebUI()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsContainer()
    .WithContainerRuntimeArgs("--gpus=all");

var aiModel = ollama
    .AddModel("phi35", "phi3.5");

var qdrant = builder.AddQdrant("qdrant")
    .WithDataVolume()
    .WithHttpEndpoint(6333, 6333, "dashboard");

builder.AddProject<Projects.MathZv2_Services_DocumentationScanner>("mathzv2-services-documentationscanner")
    .WithReference(aiModel)
    .WithReference(qdrant)
    .WithExternalHttpEndpoints();

builder.Build().Run();
