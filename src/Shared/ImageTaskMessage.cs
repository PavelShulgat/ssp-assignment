namespace Shared;

public record ImageTaskMessage(
    Guid JobId,
    int StationId,
    string StationName,
    double? Temperature,
    string? Description);