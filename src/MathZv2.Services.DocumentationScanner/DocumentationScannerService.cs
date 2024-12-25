namespace MathZv2.Services.DocumentationScanner;

using MathZv2.Services.DocumentationScanner.Models;
using Microsoft.Extensions.AI;
using Qdrant.Client;
using Qdrant.Client.Grpc;

public class DocumentationScannerService(
    ILogger<DocumentationScannerService> logger,
    QdrantClient qdrantClient,
    NugetConfig nugetConfig,
    DocumentsConfig documentsConfig,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator
    ) : BackgroundService
{
    private readonly ILogger<DocumentationScannerService> _logger = logger;
    private readonly QdrantClient _qdrantClient = qdrantClient;
    private readonly NugetConfig _nugetConfig = nugetConfig;
    private readonly DocumentsConfig _documentsConfig = documentsConfig;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await _qdrantClient.CollectionExistsAsync("documents", stoppingToken))
        {
            await _qdrantClient.CreateCollectionAsync("documents", new VectorParams()
            {
                Size = 3072,
                Distance = Distance.Cosine,
            }, cancellationToken: stoppingToken);
        }

        var feedData = new List<string>
        {
            "Qdrant is a vector database & vector similarity search engine. It deploys as an API service providing search for the nearest high-dimensional vectors. With Qdrant, embeddings or neural network encoders can be turned into full-fledged applications for matching, searching, recommending, and much more!",
            "Docker helps developers build, share, and run applications anywhere — without tedious environment configuration or management.",
            "PyTorch is a machine learning framework based on the Torch library, used for applications such as computer vision and natural language processing.",
            "MySQL is an open-source relational database management system (RDBMS). A relational database organizes data into one or more data tables in which data may be related to each other; these relations help structure the data. SQL is a language that programmers use to create, modify and extract data from the relational database, as well as control user access to the database.",
            "NGINX is a free, open-source, high-performance HTTP server and reverse proxy, as well as an IMAP/POP3 proxy server. NGINX is known for its high performance, stability, rich feature set, simple configuration, and low resource consumption.",
            "FastAPI is a modern, fast (high-performance), web framework for building APIs with Python 3.7+ based on standard Python type hints.",
            "SentenceTransformers is a Python framework for state-of-the-art sentence, text and image embeddings. You can use this framework to compute sentence / text embeddings for more than 100 languages. These embeddings can then be compared e.g. with cosine-similarity to find sentences with a similar meaning. This can be useful for semantic textual similar, semantic search, or paraphrase mining.",
            "The cron command-line utility is a job scheduler on Unix-like operating systems. Users who set up and maintain software environments use cron to schedule jobs (commands or shell scripts), also known as cron jobs, to run periodically at fixed times, dates, or intervals."
        };

        var embeddedResults = await _embeddingGenerator.GenerateAndZipAsync(feedData, cancellationToken: stoppingToken);
        var points = embeddedResults.Select(embedding =>
        {
            return new PointStruct()
            {
                Id = Guid.NewGuid(),
                Vectors = embedding.Embedding.Vector.ToArray(),
                Payload =
                {
                    ["information"] = embedding.Value
                }
            };
        }).ToList();

        await _qdrantClient.UpsertAsync("documents", points, cancellationToken: stoppingToken);
    }
}
