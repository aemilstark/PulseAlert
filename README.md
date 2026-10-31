# PulseAlert 🛰️
A server and dashboard for tracking GPS devices and responding to SOS signals. This is a toy project, designed to illustrate, test, and grow my ability as a programmer.  

The project itself is inspired by Garmin inReach. This service can be critical preparation for outdoorsmen, explorers, and anyone who seeks to go into the untamed wild with a layer of protection from the tamed home.

The idea is that explorers have devices which contain a GPS tracker, and some kind of SOS button. The device turns on, and it sends location data to a tracking service until the device turns off. In case of an emergency, the explorer can use the SOS button, which alerts the tracking service to an emergency. The tracking service can then call proper authorities to send assistance.

The tracking service stores the location data on a server, where it can easily be monitored from a dashboard. When an SOS signal is sent, the dashboard alerts the user, who can then take proper action.

## The Plan - Project Philosophy
> "Plans are worthless, but planning is everything" 
>
> -Dwight D. Eisenhower

In the age of AI, software development is being reimagined. Projects are less about producing code, and more about foreseeing the code that will be produced. To that end, a lot of time and effort has been put into the planning of this project, resulting in [a central PLAN](docs/PLAN.md). In it you will find the project scope, a detailed project description, data structures, and a roadmap. 

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
