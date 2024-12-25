namespace MathZv2.Services.DocumentationScanner.Services;

using MathZv2.Services.DocumentationScanner.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;

public class ScanService(
    ILogger<ScanService> logger,
    QdrantClient qdrantClient,
    IOptions<NugetConfig> nugetConfig,
    IOptions<DocumentsConfig> documentsConfig,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    : IScanService
{
    private const string COLLECTION_NAME = "documents";

    private readonly ILogger<ScanService> _logger = logger;
    private readonly QdrantClient _qdrantClient = qdrantClient;
    private readonly IOptions<NugetConfig> _nugetConfig = nugetConfig;
    private readonly IOptions<DocumentsConfig> _documentsConfig = documentsConfig;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;

    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ExecuteAsync method.");

        await EnsureQdrantCollectionAsync(COLLECTION_NAME, cancellationToken);

        foreach (var documentDefinition in _documentsConfig.Value.DocumentationDefinitions)
        {
            var pathToScan = Path.Combine(
                _nugetConfig.Value.PackageFolder,
                documentDefinition.PackageFolder,
                documentDefinition.PackageVersion,
                "lib",
                documentDefinition.Framework
            );

            _logger.LogInformation("Scanning path: {PathToScan}", pathToScan);

            var allXmlFiles = Directory.GetFiles(pathToScan, "*.xml", SearchOption.AllDirectories);

            _logger.LogInformation("Found {FileCount} XML files in path: {PathToScan}", allXmlFiles.Length, pathToScan);

            var members = allXmlFiles.SelectMany(GetMemberElements).ToList();
            var points = await GenerateEmbeddingsPointsAsync(members, cancellationToken);

            _logger.LogInformation("Generated {PointCount} embedding points.", points.Count);

            await _qdrantClient.UpsertAsync(COLLECTION_NAME, points, cancellationToken: cancellationToken);

            _logger.LogInformation("Upserted points to Qdrant collection: {CollectionName}", COLLECTION_NAME);
        }

        _logger.LogInformation("Finished ExecuteAsync method.");
    }

    private async Task EnsureQdrantCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        if (!await _qdrantClient.CollectionExistsAsync(collectionName, cancellationToken))
        {
            await _qdrantClient.CreateCollectionAsync(collectionName, new VectorParams()
            {
                Size = 3072,
                Distance = Distance.Cosine,
            }, cancellationToken: cancellationToken);
        }
    }

    private async Task<List<PointStruct>> GenerateEmbeddingsPointsAsync(List<string> members, CancellationToken cancellationToken = default)
    {
        var allPoints = new List<PointStruct>();
        var batchSize = 500;
        var totalBatches = (int)Math.Ceiling((double)members.Count / batchSize);
        var stopwatch = new Stopwatch();

        for (int i = 0; i < members.Count; i += batchSize)
        {
            stopwatch.Restart();
            var batch = members.GetRange(i, Math.Min(batchSize, members.Count - i));
            var embeddedResults = await _embeddingGenerator.GenerateAndZipAsync(batch, cancellationToken: cancellationToken);

            var points = embeddedResults.Select(embedding =>
            {
                return new PointStruct()
                {
                    Id = Guid.NewGuid(),
                    Vectors = embedding.Embedding.Vector.ToArray(),
                    Payload =
                    {
                        ["Member"] = embedding.Value,
                    }
                };
            }).ToList();

            allPoints.AddRange(points);
            stopwatch.Stop();

            _logger.LogInformation("Processed batch {CurrentBatch}/{TotalBatches} in {ElapsedMilliseconds} ms. {BatchesRemaining} batches remaining.",
                (i / batchSize) + 1, totalBatches, stopwatch.ElapsedMilliseconds, totalBatches - ((i / batchSize) + 1));
        }

        return allPoints;
    }

    private static List<string> GetMemberElements(string xmlFilePath)
    {
        var memberList = new List<string>();

        var xdoc = XDocument.Load(xmlFilePath);

        var members = xdoc.Descendants("member");

        foreach (var member in members)
        {
            memberList.Add(member.ToString());
        }

        return memberList;
    }
}
