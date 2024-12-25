namespace MathZv2.Services.DocumentationScanner.Services;

using MathZv2.Services.DocumentationScanner.Models;
using MathZv2.Services.DocumentationScanner.Services.IServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

public class RagSearchService(
    ILogger<RagSearchService> logger,
    IOptions<DocumentsConfig> documentsConfig,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    QdrantClient qdrantClient) : IRagSearchService
{
    private readonly ILogger<RagSearchService> _logger = logger;
    private readonly IOptions<DocumentsConfig> _documentsConfig = documentsConfig;
    private readonly QdrantClient _qdrantClient = qdrantClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;

    public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(string searchQuery, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateEmbeddingVectorAsync(searchQuery, cancellationToken: cancellationToken);

        return await _qdrantClient.SearchAsync(_documentsConfig.Value.CollectionName, embedding, cancellationToken: cancellationToken);
    }
}
