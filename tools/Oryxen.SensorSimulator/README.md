# Oryxen Sensor Lite — IoT Simulator

Lightweight .NET console that emulates a Sensor Lite device, pushing telemetry bursts
to `POST /api/v1/telemetry`. It sweeps soil moisture and temperature through four phases
(OPTIMAL → GOOD → WARNING → CRITICAL) so the backend `PlantHealthCalculator` returns the
full Health Score range and the dashboards change color in real time.

## Run

```bash
# from the Oryxen-Backend folder, with the API running:
dotnet run --project tools/Oryxen.SensorSimulator -- --count 12

# target the same demo plant the web/mobile dashboards poll:
dotnet run --project tools/Oryxen.SensorSimulator -- --plant 11111111-2222-3333-4444-555555555555 --count 16

# run continuously (Ctrl+C to stop):
dotnet run --project tools/Oryxen.SensorSimulator
```

## Arguments

| Flag | Default | Description |
|------|---------|-------------|
| `--url` | `http://localhost:5170/api/v1` | Backend base URL |
| `--plant` | `11111111-2222-3333-4444-555555555555` | Target plant id |
| `--count` | `0` (infinite) | Number of bursts to send |
| `--interval` | `1500` | Milliseconds between bursts |
