using System;

namespace Oryxen.Domain.Services;

public record SimulatedReading(
    double Temperature, double Humidity, 
    double LightLevel, double SoilMoisture, 
    DateTime Timestamp);

public static class IoTMetricGenerator
{
    private static readonly Random Rng = new();

    // Random walk constraints
    private const double TempVariation = 0.5;
    private const double HumidityVariation = 1.0;
    private const double SoilMoistureVariation = 2.0;
    private const double LightVariation = 100.0;
    
    // Limits
    private const double TempMin = 10.0;
    private const double TempMax = 35.0;
    private const double HumMin = 20.0;
    private const double HumMax = 80.0;
    private const double SoilMin = 0.0;
    private const double SoilMax = 100.0;
    private const double LightMaxDay = 8000.0;
    private const double LightMinDay = 1000.0;
    private const double LightNightMax = 50.0;

    public static SimulatedReading SeedReading(DateTime targetTime)
    {
        bool isNight = IsNightTime(targetTime);
        return new SimulatedReading(
            Temperature: Math.Round(Rng.NextDouble() * (28.0 - 18.0) + 18.0, 1),
            Humidity: Math.Round(Rng.NextDouble() * (60.0 - 40.0) + 40.0, 1),
            LightLevel: Math.Round(isNight ? Rng.NextDouble() * LightNightMax : Rng.NextDouble() * (LightMaxDay - LightMinDay) + LightMinDay, 0),
            SoilMoisture: Math.Round(Rng.NextDouble() * (80.0 - 50.0) + 50.0, 1),
            Timestamp: targetTime
        );
    }

    public static SimulatedReading NextReading(SimulatedReading previous, DateTime targetTime)
    {
        bool isNight = IsNightTime(targetTime);

        double nextTemp = previous.Temperature + RandomVariation(TempVariation);
        if (isNight) nextTemp -= 0.1;
        else nextTemp += 0.1;
        nextTemp = Math.Clamp(nextTemp, TempMin, TempMax);

        double nextHum = previous.Humidity + RandomVariation(HumidityVariation);
        nextHum = Math.Clamp(nextHum, HumMin, HumMax);

        double nextLight;
        if (isNight)
        {
            nextLight = previous.LightLevel > LightNightMax 
                ? previous.LightLevel * 0.5
                : Rng.NextDouble() * LightNightMax;
        }
        else
        {
            nextLight = previous.LightLevel < LightMinDay
                ? LightMinDay + RandomVariation(LightVariation)
                : previous.LightLevel + RandomVariation(LightVariation);
            nextLight = Math.Clamp(nextLight, 0, LightMaxDay);
        }

        // Soil moisture decays slowly over time
        double timeDiffHours = (targetTime - previous.Timestamp).TotalHours;
        double decay = timeDiffHours > 0 ? timeDiffHours * 0.3 : 0;
        double nextSoil = previous.SoilMoisture - decay + RandomVariation(SoilMoistureVariation * 0.2);
        nextSoil = Math.Clamp(nextSoil, SoilMin, SoilMax);

        return new SimulatedReading(
            Math.Round(nextTemp, 1),
            Math.Round(nextHum, 1),
            Math.Round(nextLight, 0),
            Math.Round(nextSoil, 1),
            targetTime
        );
    }

    public static SimulatedReading PostWateringReading(SimulatedReading previous, DateTime targetTime)
    {
        var reading = NextReading(previous, targetTime);
        double highMoisture = Rng.NextDouble() * (95.0 - 85.0) + 85.0;
        return reading with { SoilMoisture = Math.Round(highMoisture, 1) };
    }

    private static double RandomVariation(double maxVariation)
    {
        return (Rng.NextDouble() * 2 - 1) * maxVariation;
    }

    private static bool IsNightTime(DateTime time)
    {
        return time.Hour >= 22 || time.Hour < 6;
    }
}
