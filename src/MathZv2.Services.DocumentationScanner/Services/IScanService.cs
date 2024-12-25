namespace MathZv2.Services.DocumentationScanner.Services;

public interface IScanService
{
    Task ScanAsync(CancellationToken cancellationToken = default);
}
