namespace Oryxen.Domain.Services;

/// <summary>
/// Domain service that derives a normalized plant Health Score (0-100) from the raw
/// metrics reported by a Sensor Lite device. The score weights soil moisture most
/// heavily, since it is the dominant driver of plant survival, followed by ambient
/// humidity and temperature comfort ranges.
/// </summary>
public static class PlantHealthCalculator
{
    // Ideal operating ranges for a generic indoor plant.
    private const double SoilMoistureMin = 40d;
    private const double SoilMoistureMax = 70d;
    private const double HumidityMin = 40d;
    private const double HumidityMax = 60d;
    private const double TemperatureMin = 18d;
    private const double TemperatureMax = 27d;

    public static int Compute(double soilMoisture, double humidity, double temperature)
    {
        var soilScore = RangeScore(soilMoisture, SoilMoistureMin, SoilMoistureMax);
        var humidityScore = RangeScore(humidity, HumidityMin, HumidityMax);
        var temperatureScore = RangeScore(temperature, TemperatureMin, TemperatureMax);

        var weighted = (soilScore * 0.50) + (humidityScore * 0.25) + (temperatureScore * 0.25);

        return (int)Math.Clamp(Math.Round(weighted * 100d), 0d, 100d);
    }

    /// <summary>
    /// Returns 1.0 when the value sits inside the ideal range and decays linearly
    /// towards 0.0 as it drifts away by up to one full range-width on either side.
    /// </summary>
    private static double RangeScore(double value, double min, double max)
    {
        if (value >= min && value <= max)
        {
            return 1d;
        }

        var width = Math.Max(max - min, 1d);
        var distance = value < min ? min - value : value - max;

        return Math.Clamp(1d - (distance / width), 0d, 1d);
    }
}
