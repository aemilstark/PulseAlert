# PulseAlert 🛰️
A server and dashboard for tracking GPS devices and responding to SOS signals. This project is inspired by Garmin inReach. 

[A full understanding of the project can be found in the central plan.](docs/PLAN.md) 

> "Plans are useless but planning is critical"

In the age of AI, software development is being reimagined. Projects are less about producing code, and more about foreseeing the code that will be produced. To that end, a lot of time and effort has been put into this plan. 

## Running locally
Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
docker compose up -d
dotnet tool restore
dotnet ef database update --project src/PulseAlert.Infrastructure --startup-project src/PulseAlert.Api
dotnet run --project src/PulseAlert.Api
```

The API listens on http://localhost:5229. `GET /health` reports whether the API can reach the database.

Devices send pings to `POST /api/telemetry` with an `X-Api-Key` header. In Development the key is `local-dev-key` (set in `appsettings.Development.json`); elsewhere set `Telemetry__ApiKey`. `src/PulseAlert.Api/PulseAlert.Api.http` has a sample request.
