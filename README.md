# Longa Server

API server for the Longa mobile client. Built with .NET 8 and Clean Architecture.

## Structure

```
longa-server/
├── src/
│   ├── Longa.API/          # Web API, endpoints, presentation
│   ├── Longa.Application/  # Use cases, interfaces, services
│   ├── Longa.Domain/       # Entities, domain logic
│   └── Longa.Infrastructure/ # External concerns, implementations
└── tests/
    ├── Longa.UnitTests/
    └── Longa.IntegrationTests/
```

## Configuration

### Mapbox (Search API)

- **Local dev:** `appsettings.Development.json` → `Mapbox:AccessToken`
- **Production:** Environment variable `Mapbox__AccessToken` or User Secrets

### Database (Neon Postgres)

Connection string in `appsettings.Development.json` → `ConnectionStrings:Default`. Supports `postgresql://` URLs; these are automatically converted to Npgsql format (avoids URL parsing issues).

To override via environment variable, **use single quotes** so the shell does not interpret `&`:

```bash
export ConnectionStrings__Default='postgresql://user:pass@host/db?sslmode=require'
```

Or use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):
```bash
dotnet user-secrets set "ConnectionStrings:Default" "postgresql://user:pass@host/db?sslmode=require"
```

## Run

```bash
dotnet run --project src/Longa.API
```

### Expose via localtunnel (for mobile testing)

With the server running, in another terminal:

```bash
lt --port 5194
```

Copy the HTTPS URL (e.g. `https://xxx.loca.lt`) and add to `longa-mobile/.env`:

```
EXPO_PUBLIC_LONGA_API_URL=https://xxx.loca.lt
```

The search box will then use the Longa API instead of calling Mapbox directly.

## Test

```bash
dotnet test
```

## Search API

Location search via Mapbox Search Box API.

### Suggest locations (autocomplete)

```
GET /search/suggest?q={query}&session_token={uuid}
```

| Query param     | Required | Description                                |
|-----------------|----------|--------------------------------------------|
| q               | Yes      | Search query                               |
| session_token   | Yes      | UUID v4 to group requests for billing      |
| proximity       | No       | `lon,lat` to bias results near a location  |
| country         | No       | ISO 3166 alpha-2 code (e.g. `ZA`)          |
| limit           | No       | Max results (default 5, max 10)            |

**Response:** `[{ "mapboxId", "name", "fullAddress", "placeFormatted", "featureType" }, ...]`

### Retrieve location details (coordinates)

```
GET /search/retrieve/{mapboxId}?session_token={uuid}
```

| Query param     | Required | Description                    |
|-----------------|----------|--------------------------------|
| session_token   | Yes      | Same UUID used in suggest call |

**Response:** `{ "id", "name", "address", "coordinates": { "latitude", "longitude" } }`

## Database & Seed

The server uses Neon Postgres. Run the migration first:

```bash
# In Neon SQL Editor, run: migrations/001_initial_schema.sql
```

Seed driver trips for testing (client does not yet support posting trips):

```bash
cd longa-server
npm install
DATABASE_URL="postgresql://user:pass@host/db?sslmode=require" npm run seed
# Or: npm run seed -- --url "postgresql://..."
```
