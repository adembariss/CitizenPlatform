using CitizenPlatform.Domain.Enums;

namespace CitizenPlatform.Application.DTOs;

public sealed record PublicStatsDto(
    int TotalComplaints,
    int ResolvedComplaints,
    int ActiveMunicipalities,
    int Categories);

// Deliberately coarse: only a point, its category and status. No tracking code,
// no citizen data, no description — safe for a public transparency map.
public sealed record PublicComplaintMapPointDto(
    double Latitude,
    double Longitude,
    string CategoryName,
    ComplaintStatus Status,
    DateTimeOffset CreatedAt);
