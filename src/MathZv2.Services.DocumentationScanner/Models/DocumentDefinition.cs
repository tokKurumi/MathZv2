namespace MathZv2.Services.DocumentationScanner.Models;

public class DocumentDefinition
{
    public string PackageName { get; set; } = string.Empty;
    public string PackageFolder { get; set; } = string.Empty;
    public string PackageVersion { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
}
