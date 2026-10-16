using System.Text.Json.Serialization;

namespace Shared;

public record BuienradarFeed(
    [property: JsonPropertyName("actual")] ActualSection Actual);

public record ActualSection(
    [property: JsonPropertyName("stationmeasurements")] List<StationMeasurement> StationMeasurements);

public record StationMeasurement(
    [property: JsonPropertyName("stationid")] int StationId,
    [property: JsonPropertyName("stationname")] string StationName,
    [property: JsonPropertyName("regio")] string? Region,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("weatherdescription")] string? WeatherDescription);