# New Endpoints — Search & Picture Delete

> **Scope of this document:** Only the endpoints that are new or changed in this release.
> All other endpoints remain unchanged from the main API documentation.

---

## Table of Contents

1. [Filtered Farm List (Search)](#1-filtered-farm-list-search)
2. [Filtered Worker List (Search)](#2-filtered-worker-list-search)
3. [Delete Farm Picture](#3-delete-farm-picture)
4. [Delete Worker Picture](#4-delete-worker-picture)
5. [TypeScript Types](#5-typescript-types)
6. [Common Error Shapes](#6-common-error-shapes)

---

## 1. Filtered Farm List (Search)

### `GET /api/fishfarms`

Returns a paginated list of fish farms. All filter parameters are optional and fully composable — any combination works.

**Query Parameters**

| Parameter    | Type      | Default | Constraints              | Description                                        |
|--------------|-----------|---------|-------------------------|----------------------------------------------------|
| `pageNumber` | `integer` | `1`     | ≥ 1                     | 1-based page number                                |
| `pageSize`   | `integer` | `10`    | 1–50 (server-clamped)   | Items per page                                     |
| `search`     | `string`  | —       | max 200 chars           | Case-insensitive name contains match               |
| `hasBarge`   | `boolean` | —       | `true` / `false`        | Filter farms with or without a barge               |
| `minCages`   | `integer` | —       | > 0, ≤ maxCages if both | Minimum number of cages (inclusive)                |
| `maxCages`   | `integer` | —       | > 0, ≥ minCages if both | Maximum number of cages (inclusive)                |

> **Optimization note:** All filters are applied at the SQL level before `COUNT` runs,
> so `totalCount` always reflects the filtered result set, not the full table.

**Example requests**

```
# All farms whose name contains "ocean"
GET /api/fishfarms?search=ocean

# Farms with a barge, 10–30 cages
GET /api/fishfarms?hasBarge=true&minCages=10&maxCages=30

# Combined — search + filter + page 2
GET /api/fishfarms?search=bay&hasBarge=false&minCages=5&pageNumber=2&pageSize=5
```

**Success response — `200 OK`**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Ocean Bay Farm",
      "gpsLatitude": 7.2906,
      "gpsLongitude": 80.6337,
      "numberOfCages": 12,
      "hasBarge": true,
      "pictureUrl": "https://res.cloudinary.com/.../sample.jpg",
      "workerCount": 5
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

**Validation errors — `400 Bad Request`**

| Trigger                          | `errors` key   | Message                                          |
|----------------------------------|----------------|--------------------------------------------------|
| `search` longer than 200 chars   | `Search`       | "Search term must not exceed 200 characters."    |
| `minCages` ≤ 0                   | `MinCages`     | "MinCages must be greater than 0."               |
| `maxCages` ≤ 0                   | `MaxCages`     | "MaxCages must be greater than 0."               |
| `minCages` > `maxCages`          | `MinCages`     | "MinCages must not be greater than MaxCages."    |

**What has not changed**

- Response shape is identical to the previous `GET /api/fishfarms` — no breaking changes.
- Omitting all filter params behaves exactly as before.

---

## 2. Filtered Worker List (Search)

### `GET /api/fishfarms/{farmId}/workers`

Returns a paginated list of active (non-deleted) workers for a farm. All filter parameters are optional.

**Path Parameters**

| Parameter  | Type   | Description       |
|------------|--------|-------------------|
| `farmId`   | `uuid` | The farm's GUID   |

**Query Parameters**

| Parameter     | Type      | Default | Constraints            | Description                                                     |
|---------------|-----------|---------|------------------------|-----------------------------------------------------------------|
| `pageNumber`  | `integer` | `1`     | ≥ 1                    | 1-based page number                                             |
| `pageSize`    | `integer` | `20`    | 1–100 (server-clamped) | Items per page                                                  |
| `search`      | `string`  | —       | max 200 chars          | Case-insensitive contains match on **name OR email**            |
| `position`    | `string`  | —       | See values below       | Filter by worker position                                       |
| `certExpired` | `boolean` | —       | `true` / `false`       | `true` = only expired certs; `false` = only valid certs         |

**`position` valid values** (case-insensitive in the query string)

| Value    | Meaning          |
|----------|-----------------|
| `CEO`    | Farm CEO         |
| `Worker` | Regular worker   |

**Example requests**

```
# Workers whose name or email contains "john"
GET /api/fishfarms/{farmId}/workers?search=john

# Only workers with expired certification
GET /api/fishfarms/{farmId}/workers?certExpired=true

# CEOs only
GET /api/fishfarms/{farmId}/workers?position=CEO

# Combined — search + expired certs + page 1
GET /api/fishfarms/{farmId}/workers?search=doe&certExpired=true&pageNumber=1&pageSize=20
```

**Success response — `200 OK`**

```json
{
  "items": [
    {
      "id": "d1a2b3c4-5e6f-7890-abcd-ef1234567890",
      "fishFarmId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "John Fisher",
      "age": 34,
      "email": "john@example.com",
      "position": "Worker",
      "certifiedUntil": "2027-06-15",
      "isExpired": false,
      "pictureUrl": "https://res.cloudinary.com/.../worker.jpg",
      "createdAt": "2026-01-10T08:00:00Z",
      "updatedAt": "2026-06-01T12:30:00Z"
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

**Validation errors — `400 Bad Request`**

| Trigger                         | `errors` key | Message                                       |
|---------------------------------|--------------|-----------------------------------------------|
| `search` longer than 200 chars  | `Search`     | "Search term must not exceed 200 characters." |
| Invalid `position` string value | —            | `400` from ASP.NET model binding              |

**`404 Not Found`** — returned when the `farmId` does not exist or is soft-deleted.

**What has not changed**

- Response shape is identical to the previous `GET /api/fishfarms/{farmId}/workers`.
- `isExpired` is calculated server-side on every response: `certifiedUntil < today (UTC)`.

---

## 3. Delete Farm Picture

### `DELETE /api/fishfarms/{id}/picture`

Removes the picture from a fish farm. The farm record itself is **not** deleted.

**Idempotent** — calling this endpoint on a farm that already has no picture returns `204` without error.

**Path Parameters**

| Parameter | Type   | Description     |
|-----------|--------|-----------------|
| `id`      | `uuid` | The farm's GUID |

**Request body** — none

**Responses**

| Status | Meaning                                     |
|--------|---------------------------------------------|
| `204 No Content` | Picture removed (or farm had no picture — idempotent) |
| `404 Not Found`  | Farm does not exist or is soft-deleted      |

**Example request**

```http
DELETE /api/fishfarms/3fa85f64-5717-4562-b3fc-2c963f66afa6/picture
```

**Example success response**

```
HTTP/1.1 204 No Content
```

**Behaviour details**

1. Loads the farm by id.
2. If `picturePublicId` is `null`, returns `204` immediately (no-op).
3. Sets `pictureUrl` and `picturePublicId` to `null` on the farm record.
4. Commits the database change first.
5. Deletes the Cloudinary asset as a best-effort operation after the DB commit (failure is logged but does not surface as an error to the caller).

> After a successful `204`, `GET /api/fishfarms/{id}` will return `"pictureUrl": null` for this farm.

---

## 4. Delete Worker Picture

### `DELETE /api/fishfarms/{farmId}/workers/{workerId}/picture`

Removes the profile picture from a worker. The worker record itself is **not** deleted.

**Idempotent** — calling this on a worker that already has no picture returns `204` without error.

**Path Parameters**

| Parameter  | Type   | Description        |
|------------|--------|--------------------|
| `farmId`   | `uuid` | The farm's GUID    |
| `workerId` | `uuid` | The worker's GUID  |

**Request body** — none

**Responses**

| Status | Meaning                                        |
|--------|------------------------------------------------|
| `204 No Content` | Picture removed (or worker had no picture — idempotent) |
| `404 Not Found`  | Worker not found in this farm, or farm does not exist   |

**Example request**

```http
DELETE /api/fishfarms/3fa85f64-5717-4562-b3fc-2c963f66afa6/workers/d1a2b3c4-5e6f-7890-abcd-ef1234567890/picture
```

**Example success response**

```
HTTP/1.1 204 No Content
```

**Behaviour details**

1. Loads the worker scoped to the given `farmId` (returns `404` if the worker belongs to a different farm).
2. If `picturePublicId` is `null`, returns `204` immediately (no-op).
3. Sets `pictureUrl` and `picturePublicId` to `null` on the worker record.
4. Commits the database change first.
5. Deletes the Cloudinary asset best-effort after the DB commit.

> After a successful `204`, `GET /api/fishfarms/{farmId}/workers/{workerId}` will return `"pictureUrl": null` for this worker.

---

## 5. TypeScript Types

```typescript
// ── Shared pagination wrapper ────────────────────────────────────────────────
interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ── Farm summary (list view) ─────────────────────────────────────────────────
interface FishFarmSummaryDto {
  id: string;           // UUID
  name: string;
  gpsLatitude: number;
  gpsLongitude: number;
  numberOfCages: number;
  hasBarge: boolean;
  pictureUrl: string | null;
  workerCount: number;
  // NOTE: createdAt / updatedAt are NOT included in the list view
}

// ── Worker DTO ───────────────────────────────────────────────────────────────
type WorkerPosition = "CEO" | "Worker";

interface WorkerDto {
  id: string;           // UUID
  fishFarmId: string;   // UUID
  name: string;
  age: number;
  email: string;
  position: WorkerPosition;
  certifiedUntil: string;   // "YYYY-MM-DD" (DateOnly)
  isExpired: boolean;       // certifiedUntil < today (UTC), computed server-side
  pictureUrl: string | null;
  createdAt: string;        // ISO 8601 UTC
  updatedAt: string;        // ISO 8601 UTC
}

// ── Query param helpers ──────────────────────────────────────────────────────
interface FishFarmSearchParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  hasBarge?: boolean;
  minCages?: number;
  maxCages?: number;
}

interface WorkerSearchParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  position?: WorkerPosition;
  certExpired?: boolean;
}
```

---

## 6. Common Error Shapes

All `400` and `404` responses follow RFC 7807 Problem Details.

**`400` — Validation failure**

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "MinCages": ["MinCages must not be greater than MaxCages."],
    "Search": ["Search term must not exceed 200 characters."]
  }
}
```

**`404` — Resource not found**

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "FishFarm with id '3fa85f64-...' was not found."
}
```

---

## Quick Reference — All Affected Endpoints

| Method   | Path                                             | Change          |
|----------|--------------------------------------------------|-----------------|
| `GET`    | `/api/fishfarms`                                 | Added `search`, `hasBarge`, `minCages`, `maxCages` query params |
| `GET`    | `/api/fishfarms/{farmId}/workers`                | Added `search`, `position`, `certExpired` query params          |
| `DELETE` | `/api/fishfarms/{id}/picture`                    | **New endpoint** |
| `DELETE` | `/api/fishfarms/{farmId}/workers/{workerId}/picture` | **New endpoint** |
