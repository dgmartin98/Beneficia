namespace Infrastructure.Segmentation;

public class SegmentationOptions
{
    public const string SectionName = "Segmentation";

    public string? ConnectionString { get; set; }
    public int CommandTimeoutSeconds { get; set; } = 30;
}
