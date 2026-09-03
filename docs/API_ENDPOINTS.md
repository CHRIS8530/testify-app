# Testify API Endpoints

RESTful API specification for Testify, versioned under `/api/v1`.
All requests require proper authentication (JWT or session).
All responses follow a consistent error shape.

Generated during decomposition phase before coding.

---

## Base URL

http://localhost:5000/api/v1

## Authentication

All endpoints (except /auth/register and /auth/login) require:
- Authorization header with JWT token, OR
- Valid session cookie (to be decided during M3)

## Response Format

All responses follow this structure:

### Success Response
```json
{
  "data": { /* response body */ },
  "status": 200
}
```

### Error Response
```json
{
  "error": {
    "message": "User-friendly error message",
    "code": "ERROR_CODE",
    "details": { /* additional context */ }
  },
  "status": 400
}
```

---

## Endpoints by Resource


### Authentication Endpoints

#### POST /auth/register

Create a new user account.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (201 Created):**
```json
{
  "data": {
    "id": "uuid-here",
    "email": "user@example.com"
  },
  "status": 201
}
```

**Error Responses:**
- 400: Email already exists
- 400: Invalid email format
- 400: Password too weak (min 8 chars)

---

#### POST /auth/login

Authenticate user and return token/session.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "email": "user@example.com",
    "token": "jwt-token-here"
  },
  "status": 200
}
```

**Error Responses:**
- 401: Invalid email or password
- 429: Too many login attempts (rate limited)

---

#### POST /auth/logout

Invalidate current session/token.

**Request:** No body required (uses Authorization header)

**Response (200 OK):**
```json
{
  "data": { "message": "Logged out successfully" },
  "status": 200
}
```

**Error Responses:**
- 401: Unauthorized (no valid token)


### Projects Endpoints

#### GET /projects

List all projects the user owns or is a member of.

**Request:** No body (query params optional)

Query Parameters (optional):
- page: Page number (default: 1)
- limit: Results per page (default: 10)

Example:
GET /projects?page=1&limit=10


---

#### POST /projects

Create a new project (current user becomes owner).

**Request:**
```json
{
  "name": "WebApp Tests",
  "description": "Testing web app features"
}
```

**Response (201 Created):**
```json
{
  "data": {
    "id": "uuid-here",
    "name": "WebApp Tests",
    "description": "Testing web app features",
    "owner_id": "current-user-uuid",
    "created_at": "2024-01-01T00:00:00Z",
    "updated_at": "2024-01-01T00:00:00Z"
  },
  "status": 201
}
```

**Error Responses:**
- 400: Name is required
- 400: Name must be unique per user
- 401: Unauthorized

---

#### GET /projects/:id

Get a specific project (user must be owner or member).

**Request:** No body

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "name": "WebApp Tests",
    "description": "Testing web app features",
    "owner_id": "owner-uuid",
    "members": [
      {
        "user_id": "member-uuid",
        "email": "member@example.com",
        "role": null
      }
    ],
    "created_at": "2024-01-01T00:00:00Z",
    "updated_at": "2024-01-01T00:00:00Z"
  },
  "status": 200
}
```

**Error Responses:**
- 404: Project not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

#### PATCH /projects/:id

Update a project (owner only).

**Request:**
```json
{
  "name": "Updated Project Name",
  "description": "Updated description"
}
```

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "name": "Updated Project Name",
    "description": "Updated description",
    "owner_id": "owner-uuid",
    "created_at": "2024-01-01T00:00:00Z",
    "updated_at": "2024-01-02T00:00:00Z"
  },
  "status": 200
}
```

**Error Responses:**
- 400: Name must be unique per user
- 403: Forbidden (user is not owner)
- 404: Project not found
- 401: Unauthorized

---

#### DELETE /projects/:id

Delete a project and all related data (owner only).

**Request:** No body

**Response (204 No Content):**
(no response body)

**Error Responses:**
- 403: Forbidden (user is not owner)
- 404: Project not found
- 401: Unauthorized


---

### Test Cases Endpoints

#### GET /projects/:id/cases

List all test cases in a project.

**Request:** No body

Query Parameters (optional):
- page: Page number (default: 1)
- limit: Results per page (default: 10)

Example:
GET /projects/uuid-here/cases?page=1&limit=10

**Response (200 OK):**
```json
{
  "data": {
    "cases": [
      {
        "id": "uuid-here",
        "project_id": "project-uuid",
        "title": "Login with valid credentials",
        "preconditions": "User not logged in",
        "steps": "1. Enter email\n2. Enter password\n3. Click login",
        "expected_result": "User logged in successfully",
        "priority": "High",
        "created_at": "2024-01-01T00:00:00Z",
        "updated_at": "2024-01-01T00:00:00Z"
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 10,
      "total": 25
    }
  },
  "status": 200
}
```

**Error Responses:**
- 404: Project not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

#### POST /projects/:id/cases

Create a new test case in a project.

**Request:**
```json
{
  "title": "Login with valid credentials",
  "preconditions": "User not logged in",
  "steps": "1. Enter email\n2. Enter password\n3. Click login",
  "expected_result": "User logged in successfully",
  "priority": "High"
}
```

**Response (201 Created):**
```json
{
  "data": {
    "id": "uuid-here",
    "project_id": "project-uuid",
    "title": "Login with valid credentials",
    "preconditions": "User not logged in",
    "steps": "1. Enter email\n2. Enter password\n3. Click login",
    "expected_result": "User logged in successfully",
    "priority": "High",
    "created_at": "2024-01-01T00:00:00Z",
    "updated_at": "2024-01-01T00:00:00Z"
  },
  "status": 201
}
```

**Error Responses:**
- 400: Title is required
- 400: Steps and expected_result are required
- 400: Invalid priority (must be Low/Medium/High/Critical)
- 404: Project not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

#### GET /projects/:id/cases/:case_id

Get a specific test case.

**Request:** No body

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "project_id": "project-uuid",
    "title": "Login with valid credentials",
    "preconditions": "User not logged in",
    "steps": "1. Enter email\n2. Enter password\n3. Click login",
    "expected_result": "User logged in successfully",
    "priority": "High",
    "created_at": "2024-01-01T00:00:00Z",
    "updated_at": "2024-01-01T00:00:00Z"
  },
  "status": 200
}
```

**Error Responses:**
- 404: Test case not found
- 403: Forbidden (user not a member of project)
- 401: Unauthorized

---

#### PATCH /projects/:id/cases/:case_id

Update a test case.

**Request:**
```json
{
  "title": "Login with valid credentials",
  "preconditions": "User not logged in",
  "steps": "1. Enter email\n2. Enter password\n3. Click login",
  "expected_result": "User logged in successfully",
  "priority": "High"
}
```

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "project_id": "project-uuid",
    "title": "Login with valid credentials",
    "preconditions": "User not logged in",
    "steps": "1. Enter email\n2. Enter password\n3. Click login",
    "expected_result": "User logged in successfully",
    "priority": "High",
    "created_at": "2024-01-01T00:00:00Z",
    "updated_at": "2024-01-02T00:00:00Z"
  },
  "status": 200
}
```

**Error Responses:**
- 400: Invalid priority
- 404: Test case not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

#### DELETE /projects/:id/cases/:case_id

Delete a test case.

**Request:** No body

**Response (204 No Content):**

(no response body)

**Error Responses:**
- 404: Test case not found
- 403: Forbidden (user not a member)
- 401: Unauthorized