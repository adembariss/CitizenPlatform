# API Contract

## Create Public Complaint

Creates a citizen complaint from request metadata and the primary user/device coordinate. Municipality resolution always uses the submitted `latitude` and `longitude`; photo EXIF GPS is stored only as auxiliary evidence.

### JSON Request

```http
POST /api/public/complaints
Content-Type: application/json
```

```json
{
  "categoryId": "22222222-2222-2222-2222-222222222201",
  "title": "Kaldirim hasari",
  "description": "Mahalle girisindeki kaldirim hasarli.",
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

### Multipart Request

```http
POST /api/public/complaints
Content-Type: multipart/form-data
```

Form fields:

- `categoryId`
- `title`
- `description`
- `citizenFullName`
- `citizenPhoneNumber`
- `citizenEmail`
- `latitude`
- `longitude`
- `addressText`
- `isAnonymous`
- `source`: `CitizenWeb` or `CitizenMobile`
- `files` or `files[]`: one or more image files

Allowed attachment content types:

- `image/jpeg`
- `image/png`
- `image/webp`

Default per-file size limit: `10 MB`.

### Rules

- `description` is required.
- `categoryId` must refer to an active category available for the resolved municipality.
- `latitude` must be between `-90` and `90`.
- `longitude` must be between `-180` and `180`.
- `source` must be `CitizenWeb` or `CitizenMobile`.
- Municipality is resolved from the primary complaint coordinate, not from photo EXIF data.
- If no municipality boundary contains the coordinate, no complaint is created.
- Attachment files are stored outside `wwwroot` under the configured local storage root.

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

## Add Complaint Attachments

Adds one or more photos to an existing complaint by tracking code. The current policy allows this only within a short upload window after complaint creation.

```http
POST /api/public/complaints/{trackingCode}/attachments
Content-Type: multipart/form-data
```

Form fields:

- `files` or `files[]`: one or more image files

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
    "attachments": [
      {
        "attachmentId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        "fileName": "e4d909c290d0fb1ca068ffaddf22cbd0.jpg",
        "originalFileName": "photo.jpg",
        "contentType": "image/jpeg",
        "sizeInBytes": 104857,
        "sha256Hash": "64-character-lowercase-sha256",
        "photoExifLatitude": null,
        "photoExifLongitude": null,
        "photoTakenAt": null
      }
    ]
  },
  "message": null,
  "errors": []
}
```

## Resolve Municipality By Coordinate

```http
GET /api/public/municipalities/resolve?lat=41.05&lng=29.0
```

Returns a standard `ApiResponse<MunicipalityResolveResult>`.
