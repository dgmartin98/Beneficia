using Application.Persons.Dtos;

namespace Application.Persons.GetSegmentation;

public sealed record GetSegmentationQuery(int PersonId) : IQuery<SegmentationDto>;
