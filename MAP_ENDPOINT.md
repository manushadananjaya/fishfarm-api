# Map Endpoint — Fish Farm GPS Markers

> **This document covers one new endpoint only.**
> All other endpoints are documented in `SEARCH_AND_DELETE_PICTURE_ENDPOINTS.md`.

---

## `GET /api/fishfarms/map`

Returns a lightweight list of GPS coordinates for all active fish farms.
Use this endpoint exclusively for rendering map markers — it returns **only the fields
needed to place a pin** and is significantly faster than the full list endpoint because
workers, pictures, cage counts, and audit fields are never fetched from the database.

---

## Query Parameters

All parameters are **optional**. Omitting all four returns every active farm.

### Bounding-box filter

Supply any combination of `north`, `south`, `east`, `west` to restrict results to a
geographic region. All filtering happens in SQL before any data is returned — the full
table is never loaded into memory.

| Parameter | Type      | Description                              | Valid range          |
|-----------|-----------|------------------------------------------|----------------------|
| `north`   | `decimal` | Maximum latitude  (top of the box)       | −90 to 90            |
| `south`   | `decimal` | Minimum latitude  (bottom of the box)    | −90 to 90            |
| `east`    | `decimal` | Maximum longitude (right edge of the box)| −180 to 180          |
| `west`    | `decimal` | Minimum longitude (left edge of the box) | −180 to 180          |

> **Constraint:** `north` must be greater than `south`; `east` must be greater than `west`.
> Anti-meridian spanning (e.g. `west=170`, `east=-170`) is **not** supported.

---

## Example Requests

```
# All active farms — no filter
GET /api/fishfarms/map

# Farms inside a bounding box over Sri Lanka
GET /api/fishfarms/map?north=9.9&south=5.9&east=82.0&west=79.5

# Only latitude-filtered (south bound only)
GET /api/fishfarms/map?south=6.5
```

---

## Response

### `200 OK`

Returns a flat JSON array — **not paginated**.

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "farmCode": "FF-00001",
    "name": "Ocean Salmon Farm",
    "gpsLatitude": 6.9271,
    "gpsLongitude": 79.8612
  },
  {
    "id": "7cb12e89-4a3b-4f1e-8d2c-1a9b0c3d5e7f",
    "farmCode": "FF-00002",
    "name": "Blue Sea Farm",
    "gpsLatitude": 7.2906,
    "gpsLongitude": 80.6337
  }
]
```

> The array is sorted alphabetically by farm name.

### `400 Bad Request` — validation failure

Returned when any coordinate is out of the valid WGS-84 range, or when the bounding
box is logically invalid (e.g. `north` ≤ `south`).

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "North": ["North must be greater than South."],
    "East":  ["East must be greater than West (anti-meridian spanning is not supported)."]
  }
}
```

**All possible validation error keys and messages**

| `errors` key | Trigger                              | Message                                                               |
|--------------|--------------------------------------|-----------------------------------------------------------------------|
| `North`      | `north` outside −90…90              | "North must be a valid latitude between -90 and 90."                 |
| `South`      | `south` outside −90…90              | "South must be a valid latitude between -90 and 90."                 |
| `East`       | `east` outside −180…180             | "East must be a valid longitude between -180 and 180."               |
| `West`       | `west` outside −180…180             | "West must be a valid longitude between -180 and 180."               |
| `North`      | `north` ≤ `south` (both provided)   | "North must be greater than South."                                   |
| `East`       | `east` ≤ `west` (both provided)     | "East must be greater than West (anti-meridian spanning is not supported)." |

---

## TypeScript Types

```typescript
// ── Response item ─────────────────────────────────────────────────────────
interface FishFarmMapDto {
  id: string;           // UUID — stable identifier, use as map marker key
  farmCode: string;     // Human-readable display ID, e.g. "FF-00001"
  name: string;
  gpsLatitude: number;  // WGS-84 decimal degrees
  gpsLongitude: number; // WGS-84 decimal degrees
}

// ── Query params ──────────────────────────────────────────────────────────
interface FishFarmMapParams {
  north?: number;
  south?: number;
  east?:  number;
  west?:  number;
}

// ── Full response type ────────────────────────────────────────────────────
type FishFarmMapResponse = FishFarmMapDto[];
```

---

## Usage Notes

| Concern | Guidance |
|---|---|
| **Use for map only** | Do not use this endpoint to populate lists, tables, or cards — use `GET /api/fishfarms` for those |
| **Not paginated** | All matching farms are returned in a single array. If you need to handle thousands of farms, discuss server-side clustering with the backend team |
| **Soft-deleted farms** | Never returned — excluded automatically by the server |
| **`farmCode` on markers** | Display `farmCode` (e.g. "FF-00001") in popups/tooltips, not the raw UUID |
| **Deep-link on click** | Use `id` (UUID) to navigate to `GET /api/fishfarms/{id}` for the full farm detail |
| **Coordinates** | WGS-84 decimal degrees — pass directly to Leaflet, Mapbox, Google Maps, etc. |
| **Bbox partial supply** | You can supply any 1–4 of the bbox params; you do not have to supply all four |
