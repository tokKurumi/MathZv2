namespace MathZv2.Services.DocumentationScanner.Services.IServices;

public interface IScanDocumentsService
{
    Task ScanAsync(CancellationToken cancellationToken = default);
}
