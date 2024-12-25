namespace MathZv2.Services.DocumentationScanner.Models;

public class DocumentsConfig
{
    public string CollectionName { get; set; } = string.Empty;
    public ulong VectorSize { get; set; }
    public IList<DocumentDefinition> DocumentationDefinitions { get; set; } = [];
}
