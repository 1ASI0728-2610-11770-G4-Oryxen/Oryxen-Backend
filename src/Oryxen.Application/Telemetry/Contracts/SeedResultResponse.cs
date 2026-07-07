using System;

namespace Oryxen.Application.Telemetry.Contracts;

public sealed record SeedResultResponse(int PlantsProcessed, int TotalReadings, TimeSpan Duration);
