# The Plan - 🛰️ PulseAlert
This plan is to be established before any code is written, to establish clear goals and direction. The content should remain largely unchanged to illustrate the ideas that existed before any issues of implementation arose. 

## 🎯 Vision & Goals
A server and dashboard for tracking GPS devices and responding to SOS signals. 

This project is inspired by Garmin inReach. This service can be critical preparation for outdoorsmen, explorers, and anyone who seeks to go into the untamed wild with a layer of protection from the tamed home. 

The idea is that explorers have devices which contain a GPS tracker, and some kind of SOS button. The device turns on, and it sends location data to a tracking service until the device turns off. In case of an emergency, the explorer can use the SOS button, which alerts the tracking service to an emergency. The tracking service can then call proper authorities to send assistance. 

The tracking service stores the location data on a server, where it can easily be monitored from a dashboard. When an SOS signal is sent, the dashboard alerts the user, who can then take proper action. 

### Core Components 
The project should consist of the following components: 
* A Backend Receiver and Processor: This service will receive incoming satellite-relayed messages. It will feature message validation, and SOS processing. It should be able to handle a large amount of incoming signals without impeding other functionality. 
* A Frontend Monitoring Dashboard: This React app will show the satellite-relayed messages on a convenient dashboard. In case of an emergency, it should provide response options. 
* A Relational Database: This will record satellite-relayed messages, devices, and ongoing emergencies. 
* Cloud platform hosting: The backend and database must be hosted, and operate on a cloud platform. This cloud platform should be configured using Terraform.

The project would need a number of GPS devices signaling to the receiver, but that is unrealistic. Instead a microservice, run by the backend will simulate these signals. 

### 🚫 Out of Scope and Practical Limitations
* Actual functionality of the dashboard is limited. Responding to an emergency should be limited to pressing a "Resolve" button. 
* Realistic GPS locations are of no concern. 
* A realistic system would work over a long time span, and process tens of thousands of signals per hour. For demonstration purposes this system will process much fewer signals and over a shorter period of time. 
* The project will not attempt to interact with actual satellites. The project scope assumes that satellite messages are delivered to the server via a webhook. 
* The project assumes that the technology supporting the app will be stable and functional. 
* Pings waiting in the in-memory queue are lost if the server crashes. Durable queuing is a stretch goal (see Crash Resistance and Recovery in Phase 6).
* Operator accounts and dashboard login are out of scope. Only the satellite webhook is protected, by a shared API key. 

## 🛠️ Architecture & Tech Stack
### System Architecture
![](images/DataFlowChart.png)

### Backend Server
* The backend framework will be .NET 10 and an ASP.NET Core Web API.
* Real time web functionality will be done through SignalR. 
* The React dashboard is built into static files and served by the API, so the whole app is one container with one URL.

#### Satellite Worker
The worker is a hosted service running in the background of the app. It simulates a number of explorers, using devices out in the world. Each explorer has a location, speed, battery life, SOS state, and flags to dictate behavior. The explorer will move around, while decreasing their battery life. There will be two things the explorer can do: 
* Deactivate their device, indicating the end of an adventure and no more need of service. 
* Send an SOS, indicating the need for assistance. 
Both of these actions are communicated via a HTTP webhook in the API. 

Explorers are kept in memory. Like a real device, the worker only talks to the API over HTTP. The webhook base URL is configurable, so the worker works both locally and in the cloud.

#### API
Endpoints are written with `async`/`await`. ASP.NET Core handles concurrent requests. 

##### Satellite Webhook
`POST /api/telemetry` receives pings from devices. Requests must include a shared secret in an `X-Api-Key` header. Devices identify themselves by IMEI.

```json
{
  "imei": "300234063918230",
  "modelName": "Explorer Alpha",
  "firmwareVersion": "2.1.0",
  "latitude": 35.779600,
  "longitude": -78.630000,
  "altitudeMeters": 120.5,
  "batteryPercentage": 84,
  "isSosActive": false,
  "powerOff": false,
  "recordedAtUtc": "2026-10-17T13:42:10Z"
}
```

Responses:
* `202 Accepted` - the ping is valid and has been queued.
* `400 Bad Request` - the ping failed validation (e.g. latitude out of range).
* `401 Unauthorized` - missing or wrong API key.
* `503 Service Unavailable` - the queue is full. The sender should retry later.

##### Dashboard Endpoints
| Method | Route | Purpose |
| :--- | :--- | :--- |
| GET | `/api/dashboard` | Metrics: queue depth, ingest rate (pings/min), active SOS count. |
| GET | `/api/telemetry/recent?count=25` | Most recent pings, for the initial load of the telemetry stream. |
| GET | `/api/devices/{imei}` | Device details and latest ping, for the Focus View. |
| GET | `/api/incidents?status=Active` | Active incidents, for the emergency banner. |
| POST | `/api/incidents/{id}/resolve` | Resolve one incident. |
| POST | `/api/incidents/resolve-all` | Resolve all active incidents. |
| POST | `/api/simulator/explorers` | Create or reset a simulated explorer. |

##### SignalR Hub
`/hubs/telemetry` pushes `PingReceived`, `IncidentOpened` and `IncidentResolved` events to the dashboard as they happen.

#### Signal Processing and SOS Evaluation
When a signal is received it is first added to the TelemetryPings table, and if the device's IMEI is unknown, it is also added to the Devices table. Signal Processing checks for two situations: If an Emergency Incident needs to be created and if a device needs to be activated or deactivated. 

When a device reports telemetry data, a device is set to active. This indicates that the device has been turned on, and the explorer is on their way. This includes a Deactivated device that sends a new ping: turning the device back on starts a new adventure. Devices need to be deactivated in order to indicate that the explorer no longer needs service. 

A ping with `PowerOff` set deactivates the device. If that device has an active Emergency Incident, the incident stays open until an operator resolves it. A device shutting down mid-emergency (for example, a dead battery) is the worst case, not a reason to close the incident. 

A signal reporting an SOS indicates that the device user has asked for help. If a signal comes in reporting an SOS, the system first checks if there is already an Emergency Incident in progress. If not, it creates a new incident. If an incident is already open, the new SOS ping is stored but the incident is not changed. It is important to note that if a signal comes in, not reporting an SOS, while an active Emergency Incident exists, the incident is not affected. Users cannot "undo" an SOS signal, in the same way they can't un-call 911 or un-pull a fire alarm. 

If an Incident is created, the proper entry is added to the database table and the UI is immediately updated. Note that incidents are only resolved via a button on the dashboard. 

Since signals arrive asynchronously, signal processing also has to be asynchronous. The webhook validates the pings and writes them to a bounded in-memory queue (`System.Threading.Channels`, capacity around 10,000), then returns immediately. If the queue is full, the webhook returns `503`.

Signal processing is done by a Telemetry Worker, a single background consumer that reads pings off the queue in batches. For each batch it opens a fresh `DbContext`, saves the pings, updates device status, evaluates SOS, and then publishes SignalR events. A single consumer is a deliberate choice: `DbContext` is not thread-safe, and processing pings in order removes race conditions such as two SOS pings both creating an incident. The partial unique index on EmergencyIncidents is a second line of defense. 

### Database
Entity Relationship Diagram (ERD)
![](images/DatabaseSchema.png)
* The database will be made in PostgreSQL and managed with Entity Framework Core.

#### Devices Table
| Column Name | Data Type | Constraints / Attributes | Description |
| :--- | :--- | :--- | :--- |
| **DeviceId** | Guid | Primary Key, Non-null | Unique internal identifier for the device. |
| **IMEI** | String | Unique Index, Max length: 15 | International Mobile Equipment Identity used for satellite network routing. |
| **ModelName** | String | Max length: 100 | Hardware model name (e.g., Explorer Alpha). |
| **FirmwareVersion** | String | Max length: 50 | Active software version on the physical unit. |
| **Status** | String | Max length: 20 | Operating status (Active, Deactivated). |
| **CreatedAtUtc** | DateTime | Non-null | UTC timestamp when the device was registered. |

#### TelemetryPings Table
| Column Name | Data Type | Constraints / Attributes | Description |
| :--- | :--- | :--- | :--- |
| **PingId** | Guid | Primary Key, Non-null | Unique identifier for the telemetry entry. |
| **DeviceId** | Guid | Foreign Key (Devices.DeviceId) | Reference to the associated hardware device. |
| **Latitude** | Decimal | Precision (9,6) | Latitude geographic coordinate (-90.000000 to 90.000000). |
| **Longitude** | Decimal | Precision (9,6) | Longitude geographic coordinate (-180.000000 to 180.000000). |
| **AltitudeMeters** | Double | Nullable | Elevation above sea level in meters. |
| **BatteryPercentage** | Integer | Range (0–100) | Remaining battery percentage reported by the device. |
| **IsSosActive** | Boolean | Non-null | Flag indicating whether the ping was sent during an active emergency. |
| **PowerOff** | Boolean | Non-null | Flag indicating whether the device is being turned off. |
| **RecordedAtUtc** | DateTime | Non-null | UTC timestamp generated by the device's GPS hardware. |
| **ReceivedAtUtc** | DateTime | Non-null | UTC timestamp recorded when the server processed the request. |

#### EmergencyIncidents Table
| Column Name | Data Type | Constraints / Attributes | Description |
| :--- | :--- | :--- | :--- |
| **IncidentId** | Guid | Primary Key, Non-null | Unique identifier for the emergency ticket. |
| **DeviceId** | Guid | Foreign Key (Devices.DeviceId) | Hardware unit associated with the emergency alert. |
| **TriggerPingId** | Guid | Foreign Key (TelemetryPings.PingId) | Telemetry ping that originally initiated the alert. |
| **Status** | String | Max length: 30 | Lifecycle state (Active, Resolved). |
| **Notes** | String | Text / Unlimited | Operator logs and dispatch notes recorded during response coordination. |
| **OpenedAtUtc** | DateTime | Non-null | UTC timestamp when the emergency was registered. |
| **ResolvedAtUtc** | DateTime | Nullable | UTC timestamp when the incident was marked resolved. | 

* Should have a Postgres partial unique index allowing for only one non-resolved incident per DeviceId 
* TelemetryPings should have an index on (DeviceId, RecordedAtUtc DESC), so that the latest ping for a device can be found quickly as the table grows. 
* Status columns are stored as strings and mapped to C# enums using EF Core value conversions. 

### Frontend Dashboard
Wireframe
![](images/FrontendWireframe.jpg)
* The frontend is written in React and uses TypeScript for type safety. 
* State management and data fetching will be done with React Query. 
* Live updates are pushed over SignalR: new pings, opened incidents and resolved incidents. React Query handles initial loads, device lookups, and polls the metrics endpoint every ~2 seconds. 

#### Header & Branding Bar
The top bar running across the dashboard. Establishes application branding (PulseAlert) and displays global navigation or quick status indicators.

#### System Health & Metrics Panel (Top-Left)
Displays metrics of real-time system performance. It includes: 
* Queue Depth: Shows how many incoming pings are currently waiting in the .NET background queue.
* Ingest Rate: Measures the throughput (e.g., pings per minute) being processed by the backend.
* SOS Status: Shows the current total count of active emergency alerts requiring operator intervention.
* API Latency: Displays the round-trip response time (in milliseconds) of HTTP requests to track backend responsiveness. This is measured by the browser as a rolling average, not reported by the server.

#### Selected Device Focus View (Top-Right)
A dedicated inspector panel for deep telemetry inspection of a specific device. At the top is a text box and button for setting the panel to display a specific Device ID. Alternatively the panel can be set by clicking any row of the Telemetry Stream or the Active Emergency Incidents Banner. The panel shows model, IMEI number, exact coordinates, last signal timestamp, battery life, and current status. It also includes a button to Resolve an active alert, if applicable.

If a map is added, it will be here. 

#### Active Emergency Incidents Banner
A high-visibility alert bar highlighted in red, displaying data of an active Emergency Incident. It displays critical high-level information (Device ID, latest coordinates, battery). It also has a button to Resolve the incident. At the top it has navigation arrows to view other active emergencies. Clicking the bar loads the Device of the current emergency into the Focus View. 

#### Recent Telemetry Stream (Live Table)
A real-time table displaying incoming telemetry fixes. Lists recent GPS updates (Timestamp, Device ID, Model, Coordinates, Battery %, and SOS Status). To keep the browser DOM performant under high-frequency data streams, it operates as a circular buffer (showing only the top ~20–25 most recent pings).

#### Simulator Control Panel
A developer/demo tool to allow for testing the life cycle of a single Explorer/Device. Operators can input a Device ID, Device Model, latitude/longitude coordinates, battery %, and an SOS emergency checkbox and then click a button. This creates a new Explorer or resets an existing Explorer, if the Device ID has already been used. The Explorer acts normally; however, if the SOS emergency checkbox has been checked, they will soon trigger an SOS emergency. 

This panel also includes a "Resolve All Incidents" button, which resolves all current emergency incidents. 

### DevOps & Cloud Infrastructure
* Terraform will be used to provision cloud resources.
* Hosting will be done on Azure: the API container runs on Azure Container Apps, and the database on Azure Database for PostgreSQL (Flexible Server). Container Apps supports WebSockets for SignalR and can scale to zero to keep demo costs low. 
* Docker will be used to containerize the .NET backend, which also serves the built React frontend. 
* Docker Compose will be used to start a local PostgreSQL database (and later the API) for local development. 
* GitHub Actions will be used for builds, tests, and automated code quality checks.

### Quality & Testing
* xUnit & Moq will be used for Unit testing of .NET business logic and alert handlers.
* Testcontainers will be used for integration tests against a real PostgreSQL database, to verify migrations and the partial unique index. 
* React Testing Library will be used for Unit and UI component testing.

## 🗺️ Roadmap
* Phase 0: Planning
  * **Goal:** Flesh out idea as much as possible 
  * Establish project scope
  * Write PLAN.md
  * Create visuals
  * Write clear instructions
* Phase 1: Architecture & Data Model
  * **Goal:** Establish the C# solution and database schema.
  * Scaffold .NET 10 Web API Solution:
    * Create a clean layer structure: Core (domain models/interfaces), Infrastructure (EF Core/DB context), API (controllers/endpoints).
  * Design EF Core Data Model:
    * Device: Device ID, model, status.
    * TelemetryPing: GPS coordinates (lat/long/altitude), battery level, timestamp.
    * EmergencyIncident: Status (Active, Resolved), timestamp.
  * Database Setup:
    * Add a docker-compose.yml that starts a local PostgreSQL.
    * Configure EF Core migrations with PostgreSQL.
  * Continuous Integration:
    * Add a GitHub Actions workflow that builds the solution and runs tests on every push and pull request.
* Phase 2: Ingestion Pipeline & Background Processing
  * **Goal:** Build the engine that simulates and processes incoming satellite data.
  * Satellite Ingestion Endpoint:
    * Build a POST /api/telemetry controller endpoint that accepts incoming GPS pings.
    * Validate the payload and require the X-Api-Key header.
  * Channel / In-Memory Queue:
    * Use a bounded .NET System.Threading.Channels queue to handle incoming pings asynchronously without blocking HTTP requests. Return 503 when it is full.
  * Background Worker (IHostedService):
    * Write a single-consumer background worker that reads pings off the channel and writes them to the database in batches using EF Core.
  * Emergency Trigger Logic:
    * If an incoming ping has IsSosActive = true and the device has no active incident, create an EmergencyIncident record.
    * Apply device activation and deactivation rules.
  * Satellite Worker:
    * Build the simulated explorers that send pings to the webhook.
  * Tests:
    * xUnit tests for the SOS and activation rules, written alongside the logic.
    * Testcontainers integration tests for migrations and the partial unique index.
* Phase 3: Simple Monitoring Endpoint / Dashboard
  * **Goal:** Provide visibility into system metrics.
  * API Telemetry Status Endpoint:
    * GET /api/dashboard: Returns total pings processed per minute, queue depth, and active SOS count.
    * The remaining dashboard endpoints and the SignalR hub listed in the API section.
  * Lightweight React / TypeScript Front End:
    * A 1-page dashboard using React and TypeScript displaying:
      * Live metrics counters 
      * A detailed device view 
      * A list of active emergencies 
      * A live stream of incoming telemetry pings 
      * A demo control panel
* Phase 4: Infrastructure as Code (Terraform)
  * **Goal:** Use Terraform in order to manage a cloud deployment.
  * Create terraform/ Directory:
    * Write a modular main.tf file that provisions:
      * An Azure Database for PostgreSQL (Flexible Server) instance.
      * An Azure Container App hosting the .NET API and frontend.
    * Local Validation:
      * Ensure terraform plan runs successfully against local environment variables.
* Phase 5: Testing, Documentation & Polish
  * **Goal:** Packaging for interview discussion.
  * Unit Tests:
    * Fill remaining gaps in xUnit tests covering the ingestion service logic and validation handlers.
    * Add React Testing Library tests for key dashboard components.
  * Comprehensive README.md:
    * Write a clear overview explaining:
      * Decisions made.
      * Instructions to run locally via dotnet run.
* Phase 6: Leaving the Project Scope
  * **Goal:** Implement stretch goals
  * Front End Map - A map on the device focus view to see where the device is.
  * Deadman's Switch - A background process which creates an emergency if a device suddenly stops sending pings. 
  * A More Impressive Explorer - Something with more meaningful locations and more interesting behaviors. 
  * Crash Resistance and Recovery - Making the system more stable in case of platform instability. 
  * Simple Validation - Checking for corrupted data or duplicate pings. 
  * A more interesting incident resolution process 
