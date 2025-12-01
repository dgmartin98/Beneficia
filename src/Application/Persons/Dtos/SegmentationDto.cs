namespace Application.Persons.Dtos;

public sealed class SegmentationDto
{
    public string Indicator { get; set; } = string.Empty;
    public int Value { get; set; }
    public string? AditionalData { get; set; }
    public int ProcessOk { get; set; }
}
