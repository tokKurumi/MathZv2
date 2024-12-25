namespace MathZv2.Services.DocumentationScanner.Services.IServices;

using Qdrant.Client.Grpc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IRagSearchService
{
    Task<IReadOnlyList<ScoredPoint>> SearchAsync(string searchQuery, CancellationToken cancellationToken = default);
}