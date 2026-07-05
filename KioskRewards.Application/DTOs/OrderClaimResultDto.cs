namespace KioskRewards.Application.DTOs;

/// What the member actually got for successfully claiming an order code.
public sealed record OrderClaimResultDto(string ProductDescription, int PointsAwarded);
