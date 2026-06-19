using System.Net.Http.Json;

// =============================================================================
// Oryxen Sensor Lite — IoT telemetry simulator
//
// Sends bursts of telemetry to POST /api/v1/telemetry, sweeping soil moisture /
// temperature across the health bands so the backend PlantHealthCalculator
// produces the full range of Health Scores (optimal -> critical) and the web /
// mobile dashboards light up accordingly.
//
// Usage:
//   dotnet run --project tools/Oryxen.SensorSimulator -- --plant <guid> --count 12
//   dotnet run --project tools/Oryxen.SensorSimulator                 (runs forever)
// =============================================================================

string baseUrl = GetArg("--url") ?? "http://localhost:5170/api/v1";
string plantId = GetArg("--plant") ?? "11111111-2222-3333-4444-555555555555";
int count = int.TryParse(GetArg("--count"), out var c) ? c : 0;       // 0 = run until Ctrl+C
int intervalMs = int.TryParse(GetArg("--interval"), out var i) ? i : 1500;

using var http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };

Console.WriteLine($"Oryxen Sensor Simulator -> {baseUrl}");
Console.WriteLine($"Plant: {plantId} | bursts: {(count == 0 ? "infinite" : count.ToString())} | interval: {intervalMs}ms");
Console.WriteLine(new string('-', 64));

// Four phases drive soil moisture and temperature towards each health band.
var phases = new (string Name, double Soil, double Humidity, double Temp, double Light)[]
{
    ("OPTIMAL", 62, 52, 22, 850),   // everything in the ideal range
    ("GOOD",    45, 45, 26, 650),   // slightly off
    ("WARNING", 30, 35, 30, 400),   // drying + warm
    ("CRITICAL", 14, 22, 34, 150),  // parched + hot
};

var random = new Random();
int sent = 0;
int burst = 0;

Console.CancelKeyPress += (_, e) => { e.Cancel = true; count = -1; };

while (count == 0 || sent < count)
{
    if (count == -1) break;

    var phase = phases[burst % phases.Length];
    var payload = new
    {
        deviceId = "SL-SIM-001",
        plantId,
        humidity = Jitter(phase.Humidity, 4),
        temperature = Jitter(phase.Temp, 2),
        lightLevel = Jitter(phase.Light, 80),
        soilMoisture = Jitter(phase.Soil, 5),
    };

    try
    {
        var response = await http.PostAsJsonAsync("telemetry", payload);
        if (response.IsSuccessStatusCode)
        {
            var reading = await response.Content.ReadFromJsonAsync<TelemetryReadingDto>();
            sent++;
            Console.WriteLine(
                $"[{sent,3}] {phase.Name,-8} soil={payload.soilMoisture,5:F1}% temp={payload.temperature,4:F1}C " +
                $"-> healthScore={reading?.HealthScore,3} ({Band(reading?.HealthScore ?? 0)})");
        }
        else
        {
            Console.WriteLine($"  ! HTTP {(int)response.StatusCode} from POST /telemetry");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"  ! Cannot reach backend at {baseUrl} ({ex.Message}). Is it running?");
        await Task.Delay(2000);
    }

    burst++;
    if (count == 0 || sent < count)
    {
        await Task.Delay(intervalMs);
    }
}

Console.WriteLine(new string('-', 64));
Console.WriteLine($"Done. {sent} readings sent for plant {plantId}.");
return 0;

double Jitter(double value, double spread) => Math.Round(value + (random.NextDouble() * 2 - 1) * spread, 1);

static string Band(int score) => score switch
{
    < 30 => "critical",
    < 60 => "warning",
    < 80 => "good",
    _ => "optimal",
};

string? GetArg(string name)
{
    var idx = Array.IndexOf(args, name);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

internal sealed record TelemetryReadingDto(int HealthScore, double SoilMoisture, double Temperature);
