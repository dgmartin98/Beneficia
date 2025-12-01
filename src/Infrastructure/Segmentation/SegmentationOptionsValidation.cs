using Microsoft.Extensions.Options;

namespace Infrastructure.Segmentation;

public class SegmentationOptionsValidation : IValidateOptions<SegmentationOptions>
{
    public ValidateOptionsResult Validate(string? name, SegmentationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Segmentation connection string is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
