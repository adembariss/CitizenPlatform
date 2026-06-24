# API Contract

## Create Public Complaint

Creates a citizen complaint from request metadata and the primary user/device coordinate. Photo upload is intentionally out of scope for this endpoint and will be handled separately.

```http
POST /api/public/complaints
Content-Type: application/json
```

### Request

```json
{
  "categoryId": "22222222-2222-2222-2222-222222222201",
  "title": "Kaldırım hasarı",
  "description": "Mahalle girişindeki kaldırım hasarlı.",
  "citizenFullName": "Ada Lovelace",
  "citizenPhoneNumber": "+905551112233",
  "citizenEmail": "ada@example.com",
  "latitude": 41.05,
  "longitude": 29.0,
  "addressText": "Demo adres",
  "isAnonymous": false,
  "source": "CitizenWeb"
}
```

Rules:

- `description` is required.
- `categoryId` must refer to an active category available for the resolved municipality.
- `latitude` must be between `-90` and `90`.
- `longitude` must be between `-180` and `180`.
- `source` must be `CitizenWeb` or `CitizenMobile`.
- Municipality is resolved from the primary complaint coordinate, not from photo EXIF data.
- If no municipality boundary contains the coordinate, no complaint is created.

### Success Response

```http
201 Created
```

```json
{
  "success": true,
  "data": {
    "complaintId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "trackingCode": "BLD-2026-A8F21C",
    "municipalityName": "Demo Belediyesi",
    "status": "New",
    "createdAt": "2026-06-24T13:46:57.0000000+00:00"
  },
  "message": null,
  "errors": []
}
```

### Failure Response

```http
400 Bad Request
```

```json
{
  "success": false,
  "data": null,
  "message": "No active municipality boundary contains the coordinate.",
  "errors": []
}
```

## Resolve Municipality By Coordinate

```http
GET /api/public/municipalities/resolve?lat=41.05&lng=29.0
```

Returns a standard `ApiResponse<MunicipalityResolveResult>`.
