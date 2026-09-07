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


---

### Test Runs Endpoints

#### GET /projects/:id/runs

List all test runs for a project.

**Request:** No body

Query Parameters (optional):
- page: Page number (default: 1)
- limit: Results per page (default: 10)

Example:
GET /projects/uuid-here/runs?page=1&limit=10

**Response (200 OK):**
```json
{
  "data": {
    "runs": [
      {
        "id": "uuid-here",
        "project_id": "project-uuid",
        "status": "In Progress",
        "pass_rate": 75.5,
        "total_cases": 10,
        "passed_count": 7,
        "failed_count": 2,
        "blocked_count": 1,
        "not_run_count": 0,
        "created_at": "2024-01-02T10:00:00Z",
        "updated_at": "2024-01-02T10:30:00Z",
        "completed_at": null
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 10,
      "total": 12
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

#### POST /projects/:id/runs

Create a new test run and select which test cases to include.

**Request:**
```json
{
  "test_case_ids": ["case-uuid-1", "case-uuid-2", "case-uuid-3"]
}
```

**Response (201 Created):**
```json
{
  "data": {
    "id": "uuid-here",
    "project_id": "project-uuid",
    "status": "In Progress",
    "pass_rate": 0,
    "total_cases": 3,
    "passed_count": 0,
    "failed_count": 0,
    "blocked_count": 0,
    "not_run_count": 3,
    "created_at": "2024-01-02T10:00:00Z",
    "updated_at": "2024-01-02T10:00:00Z",
    "completed_at": null
  },
  "status": 201
}
```

**Error Responses:**
- 400: test_case_ids is required
- 400: test_case_ids must be non-empty array
- 400: Invalid test case IDs
- 404: Project not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

#### GET /projects/:id/runs/:run_id

Get a specific test run with all its test results.

**Request:** No body

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "project_id": "project-uuid",
    "status": "In Progress",
    "pass_rate": 75.5,
    "total_cases": 4,
    "passed_count": 3,
    "failed_count": 1,
    "blocked_count": 0,
    "not_run_count": 0,
    "test_results": [
      {
        "result_id": "result-uuid-1",
        "test_case_id": "case-uuid-1",
        "test_case_title": "Login with valid credentials",
        "result": "Pass",
        "notes": null
      },
      {
        "result_id": "result-uuid-2",
        "test_case_id": "case-uuid-2",
        "test_case_title": "Login with invalid password",
        "result": "Fail",
        "notes": "Password field not accepting special chars"
      }
    ],
    "created_at": "2024-01-02T10:00:00Z",
    "updated_at": "2024-01-02T10:30:00Z",
    "completed_at": null
  },
  "status": 200
}
```

**Error Responses:**
- 404: Test run not found
- 403: Forbidden (user not a member of project)
- 401: Unauthorized

---

#### PATCH /projects/:id/runs/:run_id

Update a test run result (mark test cases as Pass/Fail/Blocked/Not Run).

**Request:**
```json
{
  "status": "Complete",
  "test_results": [
    {
      "test_result_id": "result-uuid-1",
      "result": "Pass",
      "notes": null
    },
    {
      "test_result_id": "result-uuid-2",
      "result": "Fail",
      "notes": "Password field not accepting special chars"
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "project_id": "project-uuid",
    "status": "Complete",
    "pass_rate": 50.0,
    "total_cases": 2,
    "passed_count": 1,
    "failed_count": 1,
    "blocked_count": 0,
    "not_run_count": 0,
    "created_at": "2024-01-02T10:00:00Z",
    "updated_at": "2024-01-02T11:00:00Z",
    "completed_at": "2024-01-02T11:00:00Z"
  },
  "status": 200
}
```

**Error Responses:**
- 400: Invalid result (must be Pass/Fail/Blocked/Not Run)
- 400: Invalid status (must be In Progress or Complete)
- 404: Test run or result not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

#### DELETE /projects/:id/runs/:run_id

Delete a test run and all its results.

**Request:** No body

**Response (204 No Content):**

(no response body)

**Error Responses:**
- 404: Test run not found
- 403: Forbidden (user not a member)
- 401: Unauthorized


---

### Defects Endpoints

#### GET /projects/:id/defects

List all defects in a project.

**Request:** No body

Query Parameters (optional):
- status: Filter by status (Open/In Progress/Resolved/Closed)
- severity: Filter by severity (Low/Medium/High/Critical)
- page: Page number (default: 1)
- limit: Results per page (default: 10)

Example:
GET /projects/uuid-here/defects?status=Open&severity=High&page=1&limit=10

**Response (200 OK):**
```json
{
  "data": {
    "defects": [
      {
        "id": "uuid-here",
        "title": "Password field rejects special chars",
        "description": "Users cannot log in if password contains ! or @",
        "severity": "High",
        "status": "Open",
        "test_case_id": "case-uuid-2",
        "test_case_title": "Login with invalid password",
        "test_run_result_id": "result-uuid-2",
        "created_at": "2024-01-02T10:10:00Z",
        "updated_at": "2024-01-02T10:10:00Z",
        "resolved_at": null
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 10,
      "total": 5
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

#### POST /projects/:id/defects

Create a new defect from a failed test result.

**Request:**
```json
{
  "title": "Password field rejects special chars",
  "description": "Users cannot log in if password contains ! or @",
  "severity": "High",
  "test_case_id": "case-uuid-2",
  "test_run_result_id": "result-uuid-2"
}
```

**Response (201 Created):**
```json
{
  "data": {
    "id": "uuid-here",
    "title": "Password field rejects special chars",
    "description": "Users cannot log in if password contains ! or @",
    "severity": "High",
    "status": "Open",
    "test_case_id": "case-uuid-2",
    "test_case_title": "Login with invalid password",
    "test_run_result_id": "result-uuid-2",
    "created_at": "2024-01-02T10:10:00Z",
    "updated_at": "2024-01-02T10:10:00Z",
    "resolved_at": null
  },
  "status": 201
}
```

**Error Responses:**
- 400: Title is required
- 400: Description is required
- 400: Severity is required (must be Low/Medium/High/Critical)
- 400: test_case_id and test_run_result_id are required
- 404: Test case or test run result not found
- 404: Project not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

#### GET /projects/:id/defects/:defect_id

Get a specific defect.

**Request:** No body

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "title": "Password field rejects special chars",
    "description": "Users cannot log in if password contains ! or @",
    "severity": "High",
    "status": "Open",
    "test_case_id": "case-uuid-2",
    "test_case_title": "Login with invalid password",
    "test_run_result_id": "result-uuid-2",
    "created_at": "2024-01-02T10:10:00Z",
    "updated_at": "2024-01-02T10:10:00Z",
    "resolved_at": null
  },
  "status": 200
}
```

**Error Responses:**
- 404: Defect not found
- 403: Forbidden (user not a member of project)
- 401: Unauthorized

---

#### PATCH /projects/:id/defects/:defect_id

Update a defect status (user can change Open → In Progress → Resolved → Closed).

**Request:**
```json
{
  "status": "In Progress",
  "severity": "High"
}
```

**Response (200 OK):**
```json
{
  "data": {
    "id": "uuid-here",
    "title": "Password field rejects special chars",
    "description": "Users cannot log in if password contains ! or @",
    "severity": "High",
    "status": "In Progress",
    "test_case_id": "case-uuid-2",
    "test_case_title": "Login with invalid password",
    "test_run_result_id": "result-uuid-2",
    "created_at": "2024-01-02T10:10:00Z",
    "updated_at": "2024-01-02T11:00:00Z",
    "resolved_at": null
  },
  "status": 200
}
```

**Error Responses:**
- 400: Invalid status (must be Open/In Progress/Resolved/Closed)
- 400: Invalid severity (must be Low/Medium/High/Critical)
- 404: Defect not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

#### DELETE /projects/:id/defects/:defect_id

Delete a defect.

**Request:** No body

**Response (204 No Content):**

(no response body)

**Error Responses:**
- 404: Defect not found
- 403: Forbidden (user not a member)
- 401: Unauthorized


---

### Dashboard Endpoint

#### GET /projects/:id/dashboard

Get project dashboard with summary statistics.

**Request:** No body

**Response (200 OK):**
```json
{
  "data": {
    "project_id": "uuid-here",
    "project_name": "WebApp Tests",
    "total_test_cases": 10,
    "total_test_runs": 5,
    "latest_run_id": "run-uuid-1",
    "latest_run_status": "Complete",
    "latest_run_pass_rate": 85.0,
    "overall_pass_rate": 78.0,
    "open_defects_by_severity": {
      "Critical": 1,
      "High": 2,
      "Medium": 3,
      "Low": 1
    },
    "total_open_defects": 7,
    "defects_by_status": {
      "Open": 5,
      "In Progress": 2,
      "Resolved": 0,
      "Closed": 0
    },
    "recent_defects": [
      {
        "id": "defect-uuid-1",
        "title": "Password field rejects special chars",
        "severity": "High",
        "status": "Open",
        "created_at": "2024-01-02T10:10:00Z"
      }
    ]
  },
  "status": 200
}
```

**Error Responses:**
- 404: Project not found
- 403: Forbidden (user not a member)
- 401: Unauthorized

---

## Summary of All Endpoints

Total: 18 endpoints across 5 resource groups

| Resource | Endpoints |
|----------|-----------|
| Authentication | 3 (register, login, logout) |
| Projects | 5 (GET list, POST create, GET/:id, PATCH, DELETE) |
| Test Cases | 5 (GET list, POST create, GET/:id, PATCH, DELETE) |
| Test Runs | 5 (GET list, POST create, GET/:id, PATCH, DELETE) |
| Defects | 5 (GET list, POST create, GET/:id, PATCH, DELETE) |
| Dashboard | 1 (GET summary) |

All endpoints follow consistent patterns:
- Success responses include `{ "data": {...}, "status": 200 }`
- Error responses include `{ "error": {...}, "status": 4xx }`
- Pagination: `?page=1&limit=10`
- All timestamps in UTC ISO format
- Authorization enforced on all endpoints except /auth/register and /auth/login