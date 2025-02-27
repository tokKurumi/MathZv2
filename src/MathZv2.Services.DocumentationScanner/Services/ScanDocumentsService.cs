namespace MathZv2.Services.DocumentationScanner.Services;

using MathZv2.Services.DocumentationScanner.Models;
using MathZv2.Services.DocumentationScanner.Services.IServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Threading.Tasks;
using System.Xml.Linq;

public class ScanDocumentsService(
    ILogger<ScanDocumentsService> logger,
    IOptions<NugetConfig> nugetConfig,
    IOptions<DocumentsConfig> documentsConfig,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    QdrantClient qdrantClient)
    : IScanDocumentsService
{
    private readonly ILogger<ScanDocumentsService> _logger = logger;
    private readonly IOptions<NugetConfig> _nugetConfig = nugetConfig;
    private readonly IOptions<DocumentsConfig> _documentsConfig = documentsConfig;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;
    private readonly QdrantClient _qdrantClient = qdrantClient;

    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ExecuteAsync method.");

        await EnsureQdrantCollectionAsync(_documentsConfig.Value.CollectionName, cancellationToken);

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
            await ProcessBatchesAsync(members, cancellationToken);
        }

        _logger.LogInformation("Finished ExecuteAsync method.");
    }

    private async Task EnsureQdrantCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        if (!await _qdrantClient.CollectionExistsAsync(collectionName, cancellationToken))
        {
            await _qdrantClient.CreateCollectionAsync(collectionName, new VectorParams()
            {
                Size = _documentsConfig.Value.VectorSize,
                Distance = Distance.Cosine,
            }, cancellationToken: cancellationToken);
        }
    }

    private async Task ProcessBatchesAsync(List<string> members, CancellationToken cancellationToken)
    {
        var batchSize = 500;
        var totalBatches = (int)Math.Ceiling((double)members.Count / batchSize);

        for (int i = 0; i < members.Count; i += batchSize)
        {
            var batch = members.GetRange(i, Math.Min(batchSize, members.Count - i));
            var currentBatch = (i / batchSize) + 1;

            try
            {
                var embeddedResults = await _embeddingGenerator.GenerateAndZipAsync(batch, cancellationToken: cancellationToken);

                var points = embeddedResults.Select(embedding => new PointStruct
                {
                    Id = Guid.NewGuid(),
                    Vectors = embedding.Embedding.Vector.ToArray(),
                    Payload =
                    {
                        ["Member"] = embedding.Value,
                    }
                }).ToList();

                await _qdrantClient.UpsertAsync(_documentsConfig.Value.CollectionName, points, cancellationToken: cancellationToken);

                _logger.LogInformation("Successfully processed and upserted batch {CurrentBatch}/{TotalBatches}.",
                    currentBatch, totalBatches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {CurrentBatch}/{TotalBatches}. Skipping this batch.", currentBatch, totalBatches);
            }
        }
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
