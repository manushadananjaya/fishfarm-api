# Fish Farm API — Frontend Integration Guide

## Overview

REST API for managing **fish farms** and their **workers**. Built on ASP.NET Core 9.

| Item | Value |
|------|--------|
| **Base URL (dev HTTP)** | `http://localhost:5235` |
| **Base URL (dev HTTPS)** | `https://localhost:7295` |
| **Swagger UI (dev only)** | `/` (root) |
| **OpenAPI spec** | `/swagger/v1/swagger.json` |
| **Auth** | None (open API, CORS enabled for all origins) |
| **Default response format** | `application/json` |
| **Error format** | RFC 7807 Problem Details (`application/problem+json`) |

---

## General Conventions

### IDs
- All resource IDs are **UUIDs** (`Guid`).
- Example: `"11111111-0000-0000-0000-000000000001"`

### Dates & Times
| Type | Format | Example |
|------|--------|---------|
| `DateOnly` | ISO date `YYYY-MM-DD` | `"2026-12-31"` |
| `DateTime` | ISO 8601 UTC | `"2024-01-01T00:00:00Z"` |

### Enums
`WorkerPosition` is serialized as a **string** in JSON responses:

| Value | Meaning |
|-------|---------|
| `"CEO"` | CEO (max 1 per farm) |
| `"Worker"` | Regular worker |
| `"Captain"` | Captain |

In **multipart/form-data** requests, send `Position` as the string name (`CEO`, `Worker`, `Captain`).

### Routing Note
Routes are **case-insensitive** in ASP.NET Core. Defined paths:

- Farms: `/api/FishFarms`
- Workers: `/api/fishfarms/{fishFarmId}/workers`

You can use either casing; recommend standardizing on lowercase in the frontend: `/api/fishfarms`.

### Soft Delete
- Deletes are **soft deletes** (records hidden, not physically removed).
- Deleted farms/workers return **404 Not Found** — not 410.
- Worker list and farm detail only return **active** (non-deleted) workers.

### Pictures
- Stored on **Cloudinary**; API returns full `pictureUrl`.
- `pictureUrl` may be `null` if no image was uploaded.
- Farm pictures: max **5 MB**
- Worker pictures: max **3 MB**
- Allowed types: **JPEG, PNG, WebP** (`image/jpeg`, `image/png`, `image/webp`)

---

## Pagination

Used on list endpoints.

### Query Parameters

| Param | Fish Farms Default | Workers Default | Min | Max |
|-------|-------------------|-----------------|-----|-----|
| `pageNumber` | `1` | `1` | `1` (clamped) | — |
| `pageSize` | `10` | `20` | `1` (clamped) | Farms: **50**, Workers: **100** |

Invalid values are silently clamped (e.g. `pageNumber=0` → `1`, `pageSize=200` → max).

### Response Shape

```json
{
  "items": [],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## Error Handling

All errors use **Problem Details** (RFC 7807).

### 404 Not Found

```json
{
  "type": null,
  "title": "Resource not found",
  "status": 404,
  "detail": "'FishFarm' with key 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx' was not found.",
  "instance": "/api/FishFarms/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```

### 400 Validation Failed

```json
{
  "type": null,
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation failures have occurred.",
  "instance": "/api/fishfarms/.../workers",
  "errors": {
    "Request.Email": ["A valid email address is required."],
    "Email": ["Email 'john@example.com' is already in use."]
  }
}
```

**Important for form binding:** validation error keys come from two sources:

| Source | Key Format | Example Keys |
|--------|------------|--------------|
| FluentValidation (schema rules) | `Request.{FieldName}` | `Request.Name`, `Request.Email`, `Request.CertifiedUntil` |
| FluentValidation (nested — picture) | `Request.Picture` / `Request.Picture.Length` / `Request.Picture.ContentType` | File missing, file too large, wrong mime type |
| Business rules (handler) | `{FieldName}` | `Email`, `Position`, `CertifiedUntil` |

Map both when displaying field errors. Example: for the email field, check `errors.Email` **and** `errors["Request.Email"]`.

**Picture upload error keys (applies to all picture endpoints):**

| Scenario | Error Key | Message |
|----------|-----------|---------|
| No file sent | `Request.Picture` | `A picture file is required.` |
| File exceeds size limit | `Request.Picture.Length` | `Picture must not exceed 5 MB.` (farms) / `Picture must not exceed 3 MB.` (workers) |
| Wrong file type | `Request.Picture.ContentType` | `Picture must be a JPEG, PNG, or WebP image.` |

### 500 Internal Server Error

```json
{
  "title": "An unexpected error occurred",
  "status": 500,
  "detail": "..."
}
```

---

## Data Models

### FishFarmSummaryDto (list view)

> **Note:** The summary DTO has **no** `createdAt`/`updatedAt` timestamps. If you need timestamps, call `GET /api/FishFarms/{id}` (detail endpoint).

```typescript
interface FishFarmSummary {
  id: string;
  name: string;
  gpsLatitude: number;    // decimal, max 4 decimal places
  gpsLongitude: number;   // decimal, max 4 decimal places
  numberOfCages: number;
  hasBarge: boolean;
  pictureUrl: string | null;
  workerCount: number;    // active workers only — no createdAt/updatedAt
}
```

### FishFarmDto (detail view)

```typescript
interface FishFarm {
  id: string;
  name: string;
  gpsLatitude: number;
  gpsLongitude: number;
  numberOfCages: number;
  hasBarge: boolean;
  pictureUrl: string | null;
  createdAt: string;      // ISO datetime UTC
  updatedAt: string;
  workers: Worker[];      // all active workers (not paginated)
}
```

### WorkerDto

```typescript
interface Worker {
  id: string;
  fishFarmId: string;
  name: string;
  age: number;
  email: string;
  position: "CEO" | "Worker" | "Captain";
  certifiedUntil: string;  // "YYYY-MM-DD"
  isExpired: boolean;      // computed: certifiedUntil < today (UTC)
  pictureUrl: string | null;
  createdAt: string;
  updatedAt: string;
}
```

**`isExpired` rules:**
- `true` when `certifiedUntil < today` (UTC date).
- Set consistently on: worker list, worker detail, and farm detail embedded workers.
- Use this flag in the UI for badges/warnings — do not recompute unless you also use UTC.

---

# Fish Farms API

Base path: `/api/FishFarms`

---

## 1. List Fish Farms

```
GET /api/FishFarms?pageNumber=1&pageSize=10
```

**Response:** `200 OK` — `PaginatedResult<FishFarmSummary>`

**Notes:**
- Returns summary only (no embedded workers).
- Use `workerCount` for list cards.
- Full workers: `GET /api/FishFarms/{id}`.

---

## 2. Get Fish Farm by ID

```
GET /api/FishFarms/{id}
```

**Response:** `200 OK` — `FishFarm` (with `workers[]`)

**Errors:** `404` if farm not found or soft-deleted.

---

## 3. Create Fish Farm

```
POST /api/FishFarms
Content-Type: multipart/form-data
```

### Form Fields

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `Name` | string | Yes | Not empty, max **200** chars |
| `GpsLatitude` | decimal | Yes | -90 to 90, max **4 decimal places** |
| `GpsLongitude` | decimal | Yes | -180 to 180, max **4 decimal places** |
| `NumberOfCages` | int | Yes | Must be **> 0** |
| `HasBarge` | bool | Yes | `true` / `false` |
| `Picture` | file | No | If provided: max 5 MB, JPEG/PNG/WebP |

**Response:** `201 Created`

```json
{ "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" }
```

`Location` header points to `GET /api/FishFarms/{id}`.

**Design note:** picture is optional on create. You can create the farm first, then upload via `PATCH /picture`.

### Example (JavaScript)

```javascript
const form = new FormData();
form.append("Name", "Atlantic Salmon Farm");
form.append("GpsLatitude", "60.3913");
form.append("GpsLongitude", "5.3221");
form.append("NumberOfCages", "12");
form.append("HasBarge", "true");
// form.append("Picture", fileInput.files[0]); // optional

await fetch("http://localhost:5235/api/FishFarms", {
  method: "POST",
  body: form,
});
```

---

## 4. Update Fish Farm Metadata

```
PUT /api/FishFarms/{id}
Content-Type: application/json
```

Picture is **not** updated here.

### Request Body

```json
{
  "name": "Updated Farm Name",
  "gpsLatitude": 61.0000,
  "gpsLongitude": 6.0000,
  "numberOfCages": 10,
  "hasBarge": true
}
```

### Validation (same as create, except no picture)

| Field | Rules |
|-------|-------|
| `name` | Required, max 200 chars |
| `gpsLatitude` | -90 to 90, max 4 decimal places |
| `gpsLongitude` | -180 to 180, max 4 decimal places |
| `numberOfCages` | > 0 |

**Response:** `204 No Content`

**Errors:** `400`, `404`

---

## 5. Update Fish Farm Picture

```
PATCH /api/FishFarms/{id}/picture
Content-Type: multipart/form-data
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `Picture` | file | **Yes** | Max 5 MB, JPEG/PNG/WebP |

**Response:** `200 OK`

```json
{ "pictureUrl": "https://res.cloudinary.com/..." }
```

Replaces the old image on Cloudinary.

> **Trade-off to be aware of:** The old Cloudinary image is deleted *before* the DB save. If the DB save fails, the newly uploaded image is cleaned up automatically — but the old image is already gone at that point. On a DB save failure the API returns 500 and the farm will have no picture until a new one is uploaded. This is an accepted trade-off in the current implementation.

**Errors:** `400` (missing/invalid file), `404`

---

## 6. Delete Fish Farm

```
DELETE /api/FishFarms/{id}
```

**Response:** `204 No Content`

**Behavior:**
- Soft-deletes the farm **and all its active workers** (cascade).
- Cloudinary images cleaned up after DB commit (best-effort).

**Errors:** `404`

---

# Workers API

Base path: `/api/fishfarms/{fishFarmId}/workers`

Workers are always scoped to a parent fish farm.

---

## 1. List Workers for a Farm

```
GET /api/fishfarms/{fishFarmId}/workers?pageNumber=1&pageSize=20
```

**Response:** `200 OK` — `PaginatedResult<Worker>`

**Errors:** `404` if parent farm not found/deleted.

**Not available yet (planned):**
- `?position=`
- `?certifiedOnly=true`
- `?search=`

---

## 2. Get Worker by ID

```
GET /api/fishfarms/{fishFarmId}/workers/{workerId}
```

**Response:** `200 OK` — `Worker`

**Errors:** `404` if farm or worker not found, or worker belongs to another farm.

---

## 3. Create Worker

```
POST /api/fishfarms/{fishFarmId}/workers
Content-Type: multipart/form-data
```

### Form Fields

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `Name` | string | Yes | Not empty, max **150** chars |
| `Age` | int | Yes | **18–80** inclusive |
| `Email` | string | Yes | Valid email, max **256** chars, **globally unique** among active workers |
| `Position` | string | Yes | `CEO`, `Worker`, or `Captain` |
| `CertifiedUntil` | date | Yes | Must be **strictly in the future** (UTC today excluded) |
| `Picture` | file | No | If provided: max 3 MB, JPEG/PNG/WebP |

### Business Rules (400)

| Rule | Error Key | Message |
|------|-----------|---------|
| Email already exists | `Email` | `Email '{email}' is already in use.` |
| Farm already has a CEO | `Position` | `This farm already has a CEO.` |

**Response:** `201 Created`

```json
{ "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" }
```

**Errors:** `400`, `404` (farm not found)

### Example

```javascript
const form = new FormData();
form.append("Name", "John Fisher");
form.append("Age", "30");
form.append("Email", "john.fisher@example.com");
form.append("Position", "Worker");
form.append("CertifiedUntil", "2027-06-30");
// form.append("Picture", file);

await fetch(`http://localhost:5235/api/fishfarms/${farmId}/workers`, {
  method: "POST",
  body: form,
});
```

---

## 4. Update Worker

```
PUT /api/fishfarms/{fishFarmId}/workers/{workerId}
Content-Type: application/json
```

Picture is **not** updated here.

### Request Body

```json
{
  "name": "John Fisher",
  "age": 31,
  "email": "john.fisher@example.com",
  "position": "Captain",
  "certifiedUntil": "2028-01-01"
}
```

### Validation

| Field | Rules |
|-------|-------|
| `name` | Required, max 150 chars |
| `age` | 18–80 |
| `email` | Valid email, max 256, unique (excluding current worker) |
| `position` | `CEO`, `Worker`, `Captain` |
| `certifiedUntil` | **Strictly future date** (not today, not past) |

### Business Rules (400)

| Scenario | Error Key | Message |
|----------|-----------|---------|
| `certifiedUntil` is today or past | `CertifiedUntil` | `CertifiedUntil must be a future date.` |
| Worker already expired + no renewal | `CertifiedUntil` | `This worker's certification has expired. Provide a future CertifiedUntil date to renew it before making other changes.` |
| Email taken by another worker | `Email` | `Email '{email}' is already in use.` |
| Promoting to CEO when one exists | `Position` | `This farm already has a CEO.` |

**Certification UX guidance:**
- If `isExpired === true` on load, require the user to pick a **future** `certifiedUntil` before saving.
- You cannot submit today's date — API rejects it.
- To renew an expired worker: send a future date (e.g. tomorrow or later).

**Response:** `204 No Content`

**Errors:** `400`, `404`

---

## 5. Update Worker Picture

```
PATCH /api/fishfarms/{fishFarmId}/workers/{workerId}/picture
Content-Type: multipart/form-data
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `Picture` | file | **Yes** | Max 3 MB, JPEG/PNG/WebP |

**Response:** `200 OK`

```json
{ "pictureUrl": "https://res.cloudinary.com/..." }
```

> **Trade-off to be aware of:** Same as the farm picture endpoint — the old worker image is deleted from Cloudinary *before* the DB save. If the DB save fails, the new image is cleaned up but the old one is already gone. The API returns 500 and the worker will have no picture until a new one is uploaded.

**Errors:** `400`, `404`

---

## 6. Delete Worker

```
DELETE /api/fishfarms/{fishFarmId}/workers/{workerId}
```

**Response:** `204 No Content`

**Behavior:** soft-delete; Cloudinary cleanup after DB commit.

**Errors:** `404`

---

# Recommended Frontend Flows

## Farm List Page
1. `GET /api/FishFarms?pageNumber=&pageSize=`
2. Show `name`, `pictureUrl`, `workerCount`, `hasBarge`, coordinates.
3. Paginate with `hasNextPage` / `hasPreviousPage`.

## Farm Detail Page
1. `GET /api/FishFarms/{id}` — farm + all workers in one call.
2. Show workers with `isExpired` badge.
3. For a paginated worker table, prefer `GET /api/fishfarms/{id}/workers` (supports larger lists).

## Create Farm
1. `POST /api/FishFarms` (multipart) — picture optional.
2. Redirect to detail using returned `id`.
3. Optional: `PATCH /api/FishFarms/{id}/picture` if image added later.

## Edit Farm
1. Metadata: `PUT /api/FishFarms/{id}` (JSON).
2. Image only: `PATCH /api/FishFarms/{id}/picture` (multipart).

## Worker Form
1. Validate client-side using rules above (especially age, email, future cert date).
2. Create: `POST` multipart.
3. Update: `PUT` JSON.
4. Handle CEO constraint: only one CEO per farm — show error on `Position` field.

## Delete Confirmations
- Deleting a **farm** deletes **all workers** — warn the user clearly.
- Deleted resources disappear from all GET endpoints (404).

---

# TypeScript Types (copy-paste)

```typescript
export type WorkerPosition = "CEO" | "Worker" | "Captain";

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface FishFarmSummary {
  id: string;
  name: string;
  gpsLatitude: number;
  gpsLongitude: number;
  numberOfCages: number;
  hasBarge: boolean;
  pictureUrl: string | null;
  workerCount: number;
}

export interface FishFarm {
  id: string;
  name: string;
  gpsLatitude: number;
  gpsLongitude: number;
  numberOfCages: number;
  hasBarge: boolean;
  pictureUrl: string | null;
  createdAt: string;
  updatedAt: string;
  workers: Worker[];
}

export interface Worker {
  id: string;
  fishFarmId: string;
  name: string;
  age: number;
  email: string;
  position: WorkerPosition;
  certifiedUntil: string;
  isExpired: boolean;
  pictureUrl: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateFishFarmBody {
  name: string;
  gpsLatitude: number;
  gpsLongitude: number;
  numberOfCages: number;
  hasBarge: boolean;
  picture?: File;
}

export interface UpdateFishFarmBody {
  name: string;
  gpsLatitude: number;
  gpsLongitude: number;
  numberOfCages: number;
  hasBarge: boolean;
}

export interface CreateWorkerBody {
  name: string;
  age: number;
  email: string;
  position: WorkerPosition;
  certifiedUntil: string;
  picture?: File;
}

export interface UpdateWorkerBody {
  name: string;
  age: number;
  email: string;
  position: WorkerPosition;
  certifiedUntil: string;
}

export interface ProblemDetails {
  title: string;
  status: number;
  detail: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
```

---

# Quick Reference — All Endpoints

| Method | Endpoint | Body | Success |
|--------|----------|------|---------|
| `GET` | `/api/FishFarms` | — | `200` paginated summaries |
| `GET` | `/api/FishFarms/{id}` | — | `200` farm + workers |
| `POST` | `/api/FishFarms` | `multipart/form-data` | `201` `{ id }` |
| `PUT` | `/api/FishFarms/{id}` | JSON | `204` |
| `PATCH` | `/api/FishFarms/{id}/picture` | `multipart/form-data` | `200` `{ pictureUrl }` |
| `DELETE` | `/api/FishFarms/{id}` | — | `204` |
| `GET` | `/api/fishfarms/{farmId}/workers` | — | `200` paginated workers |
| `GET` | `/api/fishfarms/{farmId}/workers/{workerId}` | — | `200` worker |
| `POST` | `/api/fishfarms/{farmId}/workers` | `multipart/form-data` | `201` `{ id }` |
| `PUT` | `/api/fishfarms/{farmId}/workers/{workerId}` | JSON | `204` |
| `PATCH` | `/api/fishfarms/{farmId}/workers/{workerId}/picture` | `multipart/form-data` | `200` `{ pictureUrl }` |
| `DELETE` | `/api/fishfarms/{farmId}/workers/{workerId}` | — | `204` |

---

# Important Gotchas for Frontend

1. **No auth** — no tokens/headers needed today; CORS is open.
2. **Multipart vs JSON** — create endpoints and picture patches use `FormData`; metadata updates use JSON.
3. **`CertifiedUntil` must be tomorrow or later** — today is rejected on create and update.
4. **One CEO per farm** — enforce in UI; API returns 400 on violation.
5. **Email is globally unique** across all active workers (not just per farm).
6. **Validation error keys differ** — check both `Request.Field` and `Field` in `errors`.
7. **GPS precision** — max 4 decimal places; more will fail validation.
8. **Soft delete = 404** — don't expect deleted items in lists or 410 responses.
9. **Farm delete is destructive** — cascades to all workers.
10. **Swagger in dev** — run the API and open `http://localhost:5235` to explore live.
