# CheckOut API

Teacher Check-In System with device fingerprinting and approval workflow.

## Overview

CheckOut is an ASP.NET Core Web API designed for educational institutions to track teacher attendance using Google OAuth authentication and device fingerprinting. The system implements a device approval workflow to ensure secure check-ins.

## Technology Stack

- **.NET 10.0** - ASP.NET Core Web API
- **Database** - SQLite with Linq2DB ORM
- **Authentication** - Google OAuth 2.0 + JWT Bearer tokens
- **API Documentation** - Swagger/OpenAPI (Swashbuckle)

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Google OAuth 2.0 Client ID ([Create one here](https://console.cloud.google.com/))

### Configuration

Create or update `appsettings.json` or `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=checkout.db"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
    },
    "AllowedDomain": "your-school.edu",
    "Jwt": {
      "Issuer": "CheckOut-API",
      "Audience": "CheckOut-Client",
      "SecretKey": "your-secret-key-here-must-be-at-least-32-characters-long",
      "ExpirationHours": 10
    }
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
  }
}
```

**Required Configuration:**
- `Authentication:Google:ClientId` - Your Google OAuth 2.0 Client ID
- `Authentication:AllowedDomain` - Allowed email domain (e.g., "school.edu")
- `Authentication:Jwt:SecretKey` - **Must be at least 32 characters**

### Running the Application

```bash
# Restore dependencies
dotnet restore

# Run in development mode
dotnet run

# Build for production
dotnet build -c Release

# Publish
dotnet publish -c Release
```

**Default URL:** http://localhost:5105

**Swagger UI:** http://localhost:5105/swagger (Development mode only)

### Docker

```bash
# Build image
docker build -t checkout-api .

# Run container
docker run -p 5105:5105 checkout-api
```

## API Endpoints

All endpoints except authentication require a JWT Bearer token in the Authorization header:
```
Authorization: Bearer <jwt-token>
```

### Authentication

#### POST /api/auth/google
Authenticate using Google ID token and receive JWT token.

**Request Body:** (plain string, not JSON)
```
"google-id-token-string"
```

**Response:** (200 OK)
```
"jwt-token-string"
```

**Error Responses:**
- `400 Bad Request` - Invalid Google token
- `403 Forbidden` - Email domain not allowed

**Process:**
1. Validates Google ID token
2. Checks if email domain matches configured domain
3. Creates or updates teacher record
4. Returns JWT token for subsequent API calls

---

### Check-Ins

#### POST /api/check-ins
Create a check-in record with device fingerprint.

**Authentication:** Required

**Request Body:** (plain string, not JSON)
```
"device-fingerprint-string"
```

**Response Scenarios:**

**New Device (Pending Approval):**
```json
{
  "Message": "Check-in successful. Your device is pending approval.",
  "Status": "Pending",
  "RequiresApproval": true
}
```

**Approved Device:**
```json
{
  "Message": "Check-in successful.",
  "Status": "Approved"
}
```

**Error Responses:**
- `401 Unauthorized` - Invalid or missing token
- `403 Forbidden` - Device registered to different teacher or blocked
- `404 Not Found` - Teacher not found

#### GET /api/check-ins
Retrieve all check-in records with optional filtering.

**Authentication:** Required

**Query Parameters:**
- `deviceStatus` (optional) - Filter by status: "Pending", "Approved", or "Blocked"

**Examples:**
```
GET /api/check-ins
GET /api/check-ins?deviceStatus=Approved
GET /api/check-ins?deviceStatus=Pending
```

**Response:** (200 OK)
```json
[
  {
    "Id": 1,
    "Teacher": {
      "GoogleId": "123456789",
      "Email": "teacher@school.edu",
      "FullName": "John Doe"
    },
    "Device": {
      "Id": 1,
      "Fingerprint": "device-fingerprint-hash",
      "DeviceStatus": "Approved",
      "LastSeen": "2026-02-25T10:30:00Z"
    },
    "CheckInTime": "2026-02-25T10:30:00Z"
  }
]
```

---

### Device Management

#### GET /api/devices
Get all devices with associated teacher information.

**Authentication:** Required

**Response:** (200 OK)
```json
[
  {
    "Id": 1,
    "Fingerprint": "device-fingerprint-hash",
    "DeviceStatus": "Approved",
    "TeacherId": 1,
    "Teacher": {
      "Id": 1,
      "GoogleId": "123456789",
      "Email": "teacher@school.edu",
      "FullName": "John Doe",
      "UpdatedAt": "2026-02-25T10:00:00Z",
      "CreatedAt": "2026-02-20T08:00:00Z"
    },
    "LastSeen": "2026-02-25T10:30:00Z",
    "UpdatedAt": "2026-02-25T10:30:00Z",
    "CreatedAt": "2026-02-20T09:00:00Z"
  }
]
```

#### PATCH /api/devices/{deviceId}/approve
Approve a pending device.

**Authentication:** Required

**Path Parameters:**
- `deviceId` (integer) - ID of the device to approve

**Response:** (200 OK)
```json
"Device approved successfully."
```

**Error Responses:**
- `404 Not Found` - Device not found

#### PATCH /api/devices/{deviceId}/block
Block a device to prevent check-ins.

**Authentication:** Required

**Path Parameters:**
- `deviceId` (integer) - ID of the device to block

**Response:** (200 OK)
```json
"Device blocked successfully."
```

**Error Responses:**
- `404 Not Found` - Device not found

---

## Database Schema

**Database:** SQLite (file: `checkout.db`)

### Tables

#### Teachers
- `Id` (int, PK) - Auto-increment
- `GoogleId` (string) - Google user ID
- `Email` (string) - Teacher email
- `FullName` (string) - Teacher full name
- `UpdatedAt` (DateTime)
- `CreatedAt` (DateTime)

#### Devices
- `Id` (int, PK) - Auto-increment
- `Fingerprint` (string) - Device fingerprint hash
- `DeviceStatus` (string) - "Pending", "Approved", or "Blocked"
- `TeacherId` (int, FK) - References Teachers.Id
- `LastSeen` (DateTime)
- `UpdatedAt` (DateTime)
- `CreatedAt` (DateTime)

#### CheckIns
- `Id` (int, PK) - Auto-increment
- `TeacherId` (int, FK) - References Teachers.Id
- `DeviceId` (int, FK) - References Devices.Id
- `CheckInTime` (DateTime)

**Note:** Database is automatically created on first run. In development mode, it's recreated on each startup.

## Device Status Workflow

1. **Pending** - New device awaiting admin approval
2. **Approved** - Device can perform check-ins
3. **Blocked** - Device cannot perform check-ins (requires admin action)

## Security Features

- **JWT Authentication** - All endpoints (except auth) require valid JWT token
- **Google OAuth Verification** - Only users from configured domain can authenticate
- **Device Approval Workflow** - Prevents unauthorized device usage
- **CORS Protection** - Only configured origins can access the API
- **Device Binding** - Devices are bound to specific teachers

## JWT Token Claims

- `sub` - Teacher ID
- `email` - Teacher email
- `name` - Teacher full name
- `jti` - Unique token identifier
- `iat` - Issued at timestamp

## Development Notes

- **Database Reset:** In development mode, the database is deleted and recreated on each startup
- **Swagger UI:** Available at `/swagger` in development mode
- **HTTPS:** Disabled in development mode for easier local testing

## License

[Add your license here]
