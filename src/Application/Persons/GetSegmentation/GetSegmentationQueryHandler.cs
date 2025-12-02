using Application.Persons.Dtos;
using Application.Services;
using Gss.Mediator;
using Gss.Results;
using Microsoft.Extensions.Logging;

namespace Application.Persons.GetSegmentation;

public class GetSegmentationQueryHandler : IQueryHandler<GetSegmentationQuery, SegmentationDto>
{
    private readonly ISegmentationService _segmentationService;
    private readonly ILogger<GetSegmentationQueryHandler> _logger;

    public GetSegmentationQueryHandler(ISegmentationService segmentationService, ILogger<GetSegmentationQueryHandler> logger)
    {
        _segmentationService = segmentationService ?? throw new ArgumentNullException(nameof(segmentationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<SegmentationDto>> Handle(GetSegmentationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Obteniendo segmentación para persona {PersonId}", request.PersonId);

            var segmentation = await _segmentationService.GetSegmentationAsync(request.PersonId, cancellationToken);

            return Result<SegmentationDto>.Success(segmentation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo segmentación para persona {PersonId}", request.PersonId);
            throw;
        }
    }
}
