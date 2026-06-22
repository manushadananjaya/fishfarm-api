# New Endpoints — Search, Sort & Picture Delete

> **Scope of this document:** Only the endpoints that are new or changed in this release.
> All other endpoints remain unchanged from the main API documentation.

---

## Table of Contents

1. [Filtered + Sorted Farm List](#1-filtered--sorted-farm-list)
2. [Filtered Worker List (Search)](#2-filtered-worker-list-search)
3. [Delete Farm Picture](#3-delete-farm-picture)
4. [Delete Worker Picture](#4-delete-worker-picture)
5. [TypeScript Types](#5-typescript-types)
6. [Common Error Shapes](#6-common-error-shapes)
7. [Quick Reference](#7-quick-reference)

---

## 1. Filtered + Sorted Farm List

### `GET /api/fishfarms`

Returns a paginated, filterable, **server-side sorted** list of fish farms.
All parameters are optional — omitting all of them behaves exactly as before.

> **Why server-side sort?**
> Client-side sort only reorders the current page. Server-side sort applies `ORDER BY`
> in SQL before `SKIP/TAKE`, so every page is a correct slice of the globally sorted set.

---

### Query Parameters

**Pagination**

| Parameter    | Type      | Default | Constraints           | Description             |
|--------------|-----------|---------|-----------------------|-------------------------|
| `pageNumber` | `integer` | `1`     | ≥ 1                   | 1-based page number     |
| `pageSize`   | `integer` | `10`    | 1–50 (server-clamped) | Items per page          |

**Filtering** — all optional, fully composable

| Parameter  | Type      | Constraints              | Description                                  |
|------------|-----------|--------------------------|----------------------------------------------|
| `search`   | `string`  | max 200 chars            | Case-insensitive farm **name** contains match |
| `hasBarge` | `boolean` | `true` / `false`         | Filter farms with or without a barge         |
| `minCages` | `integer` | > 0, ≤ maxCages if both  | Minimum number of cages (inclusive)          |
| `maxCages` | `integer` | > 0, ≥ minCages if both  | Maximum number of cages (inclusive)          |

**Sorting**

| Parameter | Type     | Default      | Allowed values                                               | Description        |
|-----------|----------|--------------|--------------------------------------------------------------|--------------------|
| `sortBy`  | `string` | `name`       | `name`, `createdAt`, `updatedAt`, `numberOfCages`, `workerCount` | Field to sort by   |
| `sortDir` | `string` | `asc`        | `asc`, `desc`                                                | Sort direction     |

> All five `sortBy` values match the frontend `SORT_OPTIONS` exactly.
> Sorting is applied at the **SQL level** before `SKIP/TAKE` so pagination is always correct.

---

### Example Requests

```
# Newest farms first (Date Added ↓)
GET /api/fishfarms?sortBy=createdAt&sortDir=desc

# Most workers first
GET /api/fishfarms?sortBy=workerCount&sortDir=desc

# Farms with a barge, name A→Z, page 2
GET /api/fishfarms?hasBarge=true&sortBy=name&sortDir=asc&pageNumber=2

# Search "ocean" + most cages first
GET /api/fishfarms?search=ocean&sortBy=numberOfCages&sortDir=desc
```

---

### Success Response — `200 OK`

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "farmCode": "FF-00001",
      "name": "Ocean Bay Farm",
      "gpsLatitude": 7.2906,
      "gpsLongitude": 80.6337,
      "numberOfCages": 12,
      "hasBarge": true,
      "pictureUrl": "https://res.cloudinary.com/.../sample.jpg",
      "workerCount": 5,
      "createdAt": "2026-01-10T08:00:00Z",
      "updatedAt": "2026-06-01T12:30:00Z"
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

> `createdAt` and `updatedAt` are now included in the list view (previously missing).

---

### Validation Errors — `400 Bad Request`

| Trigger                        | `errors` key | Message                                                                              |
|--------------------------------|--------------|--------------------------------------------------------------------------------------|
| `search` > 200 chars           | `Search`     | "Search term must not exceed 200 characters."                                        |
| `minCages` ≤ 0                 | `MinCages`   | "MinCages must be greater than 0."                                                   |
| `maxCages` ≤ 0                 | `MaxCages`   | "MaxCages must be greater than 0."                                                   |
| `minCages` > `maxCages`        | `MinCages`   | "MinCages must not be greater than MaxCages."                                        |
| Unknown `sortBy` value         | `SortBy`     | "SortBy must be one of: name, createdAt, updatedAt, numberOfCages, workerCount."     |
| `sortDir` not `asc` / `desc`   | `SortDir`    | "SortDir must be 'asc' or 'desc'."                                                   |

---

## 2. Filtered Worker List (Search)

### `GET /api/fishfarms/{farmId}/workers`

Returns a paginated list of active (non-deleted) workers for a farm.
All filter parameters are optional.

**Path Parameters**

| Parameter | Type   | Description     |
|-----------|--------|-----------------|
| `farmId`  | `uuid` | The farm's GUID |

**Query Parameters**

| Parameter     | Type      | Default | Constraints             | Description                                             |
|---------------|-----------|---------|-------------------------|---------------------------------------------------------|
| `pageNumber`  | `integer` | `1`     | ≥ 1                     | 1-based page number                                     |
| `pageSize`    | `integer` | `20`    | 1–100 (server-clamped)  | Items per page                                          |
| `search`      | `string`  | —       | max 200 chars           | Case-insensitive contains match on **name OR email**    |
| `position`    | `string`  | —       | `CEO` or `Worker`       | Filter by worker position (case-insensitive)            |
| `certExpired` | `boolean` | —       | `true` / `false`        | `true` = only expired certs; `false` = only valid certs |

**Example Requests**

```
# Workers whose name or email contains "john"
GET /api/fishfarms/{farmId}/workers?search=john

# Only workers with expired certification
GET /api/fishfarms/{farmId}/workers?certExpired=true

# CEOs only
GET /api/fishfarms/{farmId}/workers?position=CEO

# Combined
GET /api/fishfarms/{farmId}/workers?search=doe&certExpired=true
```

**Success Response — `200 OK`**

```json
{
  "items": [
    {
      "id": "d1a2b3c4-5e6f-7890-abcd-ef1234567890",
      "workerCode": "WK-00001",
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

**Validation Errors — `400 Bad Request`**

| Trigger                        | `errors` key | Message                                       |
|--------------------------------|--------------|-----------------------------------------------|
| `search` > 200 chars           | `Search`     | "Search term must not exceed 200 characters." |
| Invalid `position` string      | —            | `400` from ASP.NET model binding              |

**`404 Not Found`** — `farmId` does not exist or is soft-deleted.

---

## 3. Delete Farm Picture

### `DELETE /api/fishfarms/{id}/picture`

Removes the picture from a fish farm. The farm itself is **not** deleted.

**Idempotent** — returns `204` even if the farm already has no picture.

**Path Parameters**

| Parameter | Type   | Description     |
|-----------|--------|-----------------|
| `id`      | `uuid` | The farm's GUID |

**Request body** — none

| Status           | Meaning                                              |
|------------------|------------------------------------------------------|
| `204 No Content` | Picture removed (or farm had no picture — no-op)     |
| `404 Not Found`  | Farm does not exist or is soft-deleted               |

```http
DELETE /api/fishfarms/3fa85f64-5717-4562-b3fc-2c963f66afa6/picture
→ 204 No Content
```

**Sequence:** load farm → null out `pictureUrl` + `picturePublicId` → commit DB → delete Cloudinary asset (best-effort).

> After `204`, `GET /api/fishfarms/{id}` returns `"pictureUrl": null`.

---

## 4. Delete Worker Picture

### `DELETE /api/fishfarms/{farmId}/workers/{workerId}/picture`

Removes the profile picture from a worker. The worker record itself is **not** deleted.

**Idempotent** — returns `204` even if the worker already has no picture.

**Path Parameters**

| Parameter  | Type   | Description       |
|------------|--------|-------------------|
| `farmId`   | `uuid` | The farm's GUID   |
| `workerId` | `uuid` | The worker's GUID |

**Request body** — none

| Status           | Meaning                                                    |
|------------------|------------------------------------------------------------|
| `204 No Content` | Picture removed (or worker had no picture — no-op)         |
| `404 Not Found`  | Worker not found in this farm, or farm does not exist      |

```http
DELETE /api/fishfarms/3fa85f64-5717-4562-b3fc-2c963f66afa6/workers/d1a2b3c4-.../picture
→ 204 No Content
```

**Sequence:** load worker scoped to `farmId` → null out `pictureUrl` + `picturePublicId` → commit DB → delete Cloudinary asset (best-effort).

> After `204`, `GET .../workers/{workerId}` returns `"pictureUrl": null`.

---

## 5. TypeScript Types

```typescript
// ── Shared pagination wrapper ─────────────────────────────────────────────
interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ── Farm summary (list view) ──────────────────────────────────────────────
interface FishFarmSummaryDto {
  id: string;           // UUID — use for API routing
  farmCode: string;     // Human-readable display ID, e.g. "FF-00001"
  name: string;
  gpsLatitude: number;
  gpsLongitude: number;
  numberOfCages: number;
  hasBarge: boolean;
  pictureUrl: string | null;
  workerCount: number;
  createdAt: string;    // ISO 8601 UTC
  updatedAt: string;    // ISO 8601 UTC
}

// ── Farm detail (single farm view) ───────────────────────────────────────
interface FishFarmDto extends Omit<FishFarmSummaryDto, 'workerCount'> {
  workers: WorkerDto[];
}

// ── Worker DTO ────────────────────────────────────────────────────────────
type WorkerPosition = 'CEO' | 'Worker';

interface WorkerDto {
  id: string;           // UUID — use for API routing
  workerCode: string;   // Human-readable display ID, e.g. "WK-00001"
  fishFarmId: string;
  name: string;
  age: number;
  email: string;
  position: WorkerPosition;
  certifiedUntil: string;  // "YYYY-MM-DD"
  isExpired: boolean;      // computed server-side: certifiedUntil < today (UTC)
  pictureUrl: string | null;
  createdAt: string;       // ISO 8601 UTC
  updatedAt: string;       // ISO 8601 UTC
}

// ── Sort helpers (match your SORT_OPTIONS values exactly) ─────────────────
type FarmSortField = 'name' | 'createdAt' | 'updatedAt' | 'numberOfCages' | 'workerCount';
type SortDir       = 'asc' | 'desc';

// ── Query param builders ──────────────────────────────────────────────────
interface FishFarmSearchParams {
  pageNumber?: number;
  pageSize?:   number;    // server clamps to 50
  search?:     string;    // max 200 chars
  hasBarge?:   boolean;
  minCages?:   number;
  maxCages?:   number;
  sortBy?:     FarmSortField;  // default: "name"
  sortDir?:    SortDir;        // default: "asc"
}

interface WorkerSearchParams {
  pageNumber?:   number;
  pageSize?:     number;          // server clamps to 100
  search?:       string;          // max 200 chars — matches name OR email
  position?:     WorkerPosition;
  certExpired?:  boolean;
}
```

---

## 6. Common Error Shapes

**`400` — Validation failure**

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "SortBy": ["SortBy must be one of: name, createdAt, updatedAt, numberOfCages, workerCount."],
    "SortDir": ["SortDir must be 'asc' or 'desc'."]
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

## 7. Quick Reference

| Method   | Path                                                   | What changed                                                                                                    |
|----------|--------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------|
| `GET`    | `/api/fishfarms`                                       | Added `search`, `hasBarge`, `minCages`, `maxCages`, **`sortBy`**, **`sortDir`**; response now includes `createdAt` + `updatedAt` |
| `GET`    | `/api/fishfarms/{farmId}/workers`                      | Added `search`, `position`, `certExpired`                                                                       |
| `DELETE` | `/api/fishfarms/{id}/picture`                          | **New endpoint**                                                                                                |
| `DELETE` | `/api/fishfarms/{farmId}/workers/{workerId}/picture`   | **New endpoint**                                                                                                |
