using Application.Persons.Dtos;

namespace Application.Services;

public interface ISegmentationService
{
    Task<SegmentationDto> GetSegmentationAsync(int personId, CancellationToken cancellationToken);
}
