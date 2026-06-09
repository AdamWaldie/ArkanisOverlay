namespace Arkanis.Overlay.External.FleetYards.Abstractions;

using System.Text.Json.Serialization;

public class FleetYardsHangarVehicleDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("serial")]
    public string? Serial { get; set; }

    [JsonPropertyName("flagship")]
    public bool Flagship { get; set; }

    [JsonPropertyName("purchased")]
    public bool Purchased { get; set; }

    [JsonPropertyName("loaner")]
    public bool Loaner { get; set; }

    [JsonPropertyName("public")]
    public bool Public { get; set; }

    [JsonPropertyName("model")]
    public FleetYardsModelDto? Model { get; set; }

    [JsonPropertyName("groups")]
    public List<FleetYardsGroupDto>? Groups { get; set; }
}

public class FleetYardsModelDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("production_status")]
    public string? ProductionStatus { get; set; }

    [JsonPropertyName("classification")]
    public string? Classification { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("cargo")]
    public double? Cargo { get; set; }

    [JsonPropertyName("min_crew")]
    public int? MinCrew { get; set; }

    [JsonPropertyName("max_crew")]
    public int? MaxCrew { get; set; }

    [JsonPropertyName("manufacturer")]
    public FleetYardsManufacturerDto? Manufacturer { get; set; }

    [JsonPropertyName("media")]
    public List<FleetYardsMediaDto>? Media { get; set; }
}

public class FleetYardsManufacturerDto
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class FleetYardsGroupDto
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class FleetYardsMediaDto
{
    [JsonPropertyName("source")]
    public FleetYardsMediaSourceDto? Source { get; set; }
}

public class FleetYardsMediaSourceDto
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }
}

public class FleetYardsFleetDto
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("member_count")]
    public int? MemberCount { get; set; }

    [JsonPropertyName("vehicles_count")]
    public int? VehiclesCount { get; set; }
}

public class FleetYardsMyFleetDto
{
    [JsonPropertyName("fleet")]
    public FleetYardsFleetDto? Fleet { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
}

public class FleetYardsHangarStatsDto
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("model_count")]
    public int ModelCount { get; set; }
}
