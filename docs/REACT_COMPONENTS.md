# Testify React Components Architecture

Component tree, data flow, and state management for the Testify frontend.
Built with React + TypeScript, using client-side router (React Router v6).

Generated during decomposition phase before coding, updated after M2/M3 API build to reflect actual endpoints and response shapes.

---

## Design Principles

- Each component handles its own loading, empty, and error states
- All data fetching happens in parent components (lift state up pattern)
- Form validation on client must match server validation
- No secrets or tokens exposed in bundle
- Access tokens stored in React state (memory), never localStorage
- Refresh tokens stored as httpOnly cookies (managed by browser automatically)
- Most API responses return objects directly (not wrapped in `{ data: {...} }`)

---

## Component Tree Structure

App (root)
  ├── Router (React Router v6)
  │   ├── Layout (header/nav wrapper)
  │   │   └── (page components below)
  │   ├── LoginPage (route: /login)
  │   ├── RegisterPage (route: /register)
  │   ├── ProjectListPage (route: /projects)
  │   ├── ProjectDetailPage (route: /projects/:id)
  │   │   ├── TabNav (Projects, Test Cases, Runs, Defects, Dashboard)
  │   │   ├── ProjectTestCasesTab
  │   │   ├── ProjectTestRunsTab
  │   │   ├── ProjectDefectsTab
  │   │   └── ProjectDashboardTab
  │   ├── TestCaseEditorPage (route: /projects/:id/test-cases/:caseId/edit)
  │   ├── TestRunExecutorPage (route: /projects/:id/test-runs/:runId/execute)
  │   └── NotFoundPage (route: *)

---

## Component Specifications

### App

**Purpose:** Root component, initializes authentication state, wraps entire app.

**Responsibilities:**
- Check if user is logged in on app load (attempt silent refresh)
- Store accessToken in React state (memory only)
- Provide auth context to all child components
- Handle logout (clear state, revoke refresh token)

**State:**
- accessToken (string or null, in memory)
- currentUser (User object { id, email, username } or null)
- isLoading (boolean during initial auth check)
- isAuthenticated (boolean)

**States to Handle:**
- Loading: Show splash screen during initial auth check
- Unauthenticated: Show LoginPage
- Authenticated: Show router with all pages
- Error: Show error message if auth check fails

**API Calls:**
- POST /api/v1/auth/refresh (on app load, using refresh token cookie — automatically sent by browser)
  - Success: { accessToken, expiresIn, user: { id, email, username } }
  - Failure: Redirect to login (refresh token expired or invalid)

**Auth Context Provider:**
Wrap router with AuthContext that provides:
- accessToken
- currentUser
- login(email, password)
- logout()
- isAuthenticated

**Child Components:**
- Router (if authenticated), or LoginPage (if not)

---

### LoginPage

**Purpose:** User login form.

**Route:** /login

**Props:** None (uses navigation to redirect after login)

**State:**
- email (string)
- password (string)
- isLoading (boolean during API call)
- error (string or null)

**Form Validation:**
- Email: required, valid format
- Password: required, min 8 chars

**States to Handle:**
- Default: Empty form, no errors
- Loading: Disable button, show spinner during login
- Error: Display error message from API (invalid credentials, rate limited, etc.)
- Success: Store accessToken in React state, update AuthContext, redirect to /projects

**API Calls:**
- POST /api/v1/auth/login with { email, password }
- Response: { accessToken, expiresIn, user: { id, email, username } }
- Side effect: Refresh token automatically set in httpOnly cookie by server

**Child Components:** None

---

### RegisterPage

**Purpose:** User registration form.

**Route:** /register

**Props:** None

**State:**
- email (string)
- username (string)
- password (string)
- passwordConfirm (string)
- isLoading (boolean)
- error (string or null)

**Form Validation:**
- Email: required, valid format
- Username: required, 3-50 chars, alphanumeric + underscore
- Password: required, min 8 chars, must include uppercase, lowercase, number, special char
- Password confirm: required, must match password

**States to Handle:**
- Default: Empty form
- Loading: Disable button, show spinner
- Error: Display error (email exists, username taken, password too weak, validation failed)
- Success: Store accessToken in React state, update AuthContext, redirect to /projects

**API Calls:**
- POST /api/v1/auth/register with { email, username, password }
- Response: { accessToken, expiresIn, user: { id, email, username } }
- Side effect: Refresh token set in httpOnly cookie

**Child Components:** None

---

### ProjectListPage

**Purpose:** Display all projects user owns or is member of.

**Route:** /projects

**Props:** None (gets data from API)

**State:**
- projects (array of Project objects)
- isLoading (boolean)
- error (string or null)
- limit (number, default 10)
- offset (number, default 0)

**States to Handle:**
- Loading: Show skeleton loaders or spinner
- Empty: Show "No projects" message with "Create Project" button
- Error: Show error message, retry button
- Success: Show project list with cards

**API Calls:**
- GET /api/v1/projects (returns flat array of projects)
- Response: [{ id, name, description, ownerId, createdAt }, ...]

**User Actions:**
- Click project card → navigate to /projects/:id
- Click "Create Project" → show modal or navigate to create form
- Pagination (if list is long) → adjust offset/limit and refetch

**Child Components:**
- ProjectCard (repeating for each project)
- CreateProjectButton

---

### ProjectCard

**Purpose:** Display single project summary (reused in ProjectListPage).

**Props:**
- project (Project object)
- onClick (function to navigate or open detail)

**State:** None (stateless)

**Displays:**
- Project name
- Description (truncated if long)
- Owner name (if not current user)
- "View" button
- Created date

**Child Components:** None

---

### ProjectDetailPage

**Purpose:** Main hub for a single project. Tabs for test cases, runs, defects, dashboard.

**Route:** /projects/:id

**Props:**
- projectId (from route params)

**State:**
- project (Project object)
- activeTab (string: "cases" | "runs" | "defects" | "dashboard")
- isLoading (boolean)
- error (string or null)

**States to Handle:**
- Loading: Show spinner
- Forbidden: Show "You don't have access" if user not member/owner
- Not Found: Show 404 if project doesn't exist
- Error: Show error message, retry button
- Success: Show project name, tab navigation

**API Calls:**
- GET /api/v1/projects/:id (returns single Project object with basic info)
- Response: { id, name, description, ownerId, createdAt, ... }

**User Actions:**
- Click tab → update activeTab state
- Click "Create Test Case" → navigate to editor or show inline form
- Click "Create Test Run" → show modal to select test cases

**Child Components:**
- TabNav (selects between tabs)
- ProjectTestCasesTab
- ProjectTestRunsTab
- ProjectDefectsTab
- ProjectDashboardTab

---

### ProjectTestCasesTab

**Purpose:** List test cases for the project, allow CRUD.

**Props:**
- projectId (from parent)

**State:**
- cases (array of TestCase objects)
- isLoading (boolean)
- error (string or null)
- limit (number, default 10)
- offset (number, default 0)

**States to Handle:**
- Loading: Show skeleton loaders
- Empty: Show "No test cases" with "Create" button
- Error: Show error, retry button
- Success: Show list of test cases with Edit/Delete buttons

**API Calls:**
- GET /api/v1/projects/:id/test-cases (returns flat array)
- Response: [{ id, projectId, title, preconditions, steps, expectedResult, priority, createdAt }, ...]
- POST /api/v1/projects/:id/test-cases (create new)
- PATCH /api/v1/projects/:id/test-cases/:caseId (update)
- DELETE /api/v1/projects/:id/test-cases/:caseId (delete)

**User Actions:**
- Click "New Case" → show TestCaseEditor (inline or navigate to /projects/:id/test-cases/new/edit)
- Click Edit → navigate to /projects/:id/test-cases/:caseId/edit
- Click Delete → confirm, then delete
- Pagination → adjust offset/limit and refetch

**Child Components:**
- TestCaseCard (repeating)
- TestCaseEditor (inline form or separate page)

---

### TestCaseEditor

**Purpose:** Form to create or edit a test case.

**Props:**
- projectId (from route or parent)
- caseId (if editing, from route; null if creating)
- onSave (callback after successful save)
- onCancel (callback to cancel)

**State:**
- title (string)
- preconditions (string)
- steps (string)
- expectedResult (string)
- priority (string: "Low" | "Medium" | "High" | "Critical")
- isLoading (boolean)
- error (string or null)
- isDirty (boolean)

**Form Validation:**
- Title: required, 1-200 chars
- Steps: required, 1-5000 chars
- Expected Result: required, 1-5000 chars
- Priority: required, one of enum
- Preconditions: optional

**States to Handle:**
- Default: Empty form (create) or prefilled (edit)
- Loading: Disable save button, show spinner
- Error: Show validation errors or API error
- Unsaved Changes: Warn on cancel if isDirty
- Success: Show success message, redirect or call onSave

**API Calls:**
- POST /api/v1/projects/:id/test-cases (create)
- PATCH /api/v1/projects/:id/test-cases/:caseId (update)

**Child Components:** None (form inputs only)

---

### ProjectTestRunsTab

**Purpose:** List test runs, allow creation and execution.

**Props:**
- projectId (from parent)

**State:**
- runs (array of TestRun objects)
- isLoading (boolean)
- error (string or null)
- limit (number, default 10)
- offset (number, default 0)

**States to Handle:**
- Loading: Show skeleton loaders
- Empty: Show "No test runs" with "Create Run" button
- Error: Show error, retry
- Success: Show list of runs with status, pass rate, date

**API Calls:**
- GET /api/v1/projects/:id/test-runs (returns flat array)
- Response: [{ id, projectId, status, createdAt, ... }, ...]
- POST /api/v1/projects/:id/test-runs (create empty run, no body)
  - Response: { id, projectId, status: "pending", ... }
- DELETE /api/v1/projects/:id/test-runs/:runId (delete)

**User Actions:**
- Click "New Run" → POST to create run, then show SelectTestCasesModal or navigate to step 2
- Click run → navigate to /projects/:id/test-runs/:runId/execute
- Click Delete → confirm, then delete

**Child Components:**
- TestRunCard (repeating)
- SelectTestCasesModal (or inline form to add cases after run creation)

---

### SelectTestCasesModal (or step 2 of run creation)

**Purpose:** After creating empty test run, select which test cases to include.

**Props:**
- projectId (from parent)
- runId (the newly created run ID)
- onConfirm (callback after cases added)
- onCancel (close or go back)

**State:**
- cases (array of TestCase objects from API)
- selectedCaseIds (array of case IDs)
- isLoading (boolean during cases fetch)
- isSaving (boolean during POST results)
- error (string or null)

**States to Handle:**
- Loading: Show spinner while fetching cases
- Loaded: Show checkboxes for all cases, "Select All" button, "Add Cases" button
- Error: Show error
- Saving: Disable buttons, show spinner

**API Calls:**
- GET /api/v1/projects/:id/test-cases (to list all cases)
- POST /api/v1/projects/:id/test-runs/:runId/results with { testCaseIds: [...] }
  - Creates TestRunResult records with status "Not Run" for each case

**Child Components:** None (checkboxes only)

---

### TestRunExecutorPage

**Purpose:** Execute a test run — mark each test case result as Pass/Fail/Blocked/Not Run.

**Route:** /projects/:id/test-runs/:runId/execute

**Props:**
- projectId (from route)
- runId (from route)

**State:**
- results (array of TestRunResult objects)
- isLoading (boolean)
- error (string or null)
- isDraft (boolean; if true, show "Save Draft" button; if false, run is complete)
- isSubmitting (boolean during PATCH)

**States to Handle:**
- Loading: Show spinner
- Not Found: Show 404
- Loaded: Show each test case result with radio buttons and notes textarea
- Submitting: Disable buttons, show spinner
- Error: Show error, retry
- Success: Show success message, navigate back to /projects/:id

**API Calls:**
- GET /api/v1/projects/:id/test-runs/:runId/results (fetch all results for this run)
  - Response: [{ id, testRunId, testCaseId, testCaseTitle, result, notes, ... }, ...]
- PATCH /api/v1/projects/:id/test-runs/:runId/results/:resultId (update single result)
  - Body: { result: "Pass"|"Fail"|"Blocked"|"Not Run", notes: "..." }

**User Actions:**
- Click radio buttons to select result for each case
- Type notes for each case
- Click "Save Draft" → PATCH each changed result (keep run status "In Progress")
- Click "Complete Run" → PATCH each changed result, then mark run status "Complete"

**Child Components:**
- TestResultCard (repeating for each result)

---

### TestResultCard

**Purpose:** Single test case result in TestRunExecutor.

**Props:**
- result (TestRunResult object: { id, testCaseId, testCaseTitle, result, notes, ... })
- onUpdate (callback with { resultId, result, notes })

**State:** None (all state in parent)

**Displays:**
- Test case title
- Radio buttons: Not Run | Pass | Fail | Blocked
- Textarea for notes (optional)
- Save button or auto-save on change

**Child Components:** None

---

### ProjectDefectsTab

**Purpose:** List defects in project, allow filtering.

**Props:**
- projectId (from parent)

**State:**
- defects (array of Defect objects)
- isLoading (boolean)
- error (string or null)
- filterStatus (string or null: "Open" | "In Progress" | "Resolved" | "Closed")
- filterSeverity (string or null: "Low" | "Medium" | "High" | "Critical")
- limit (number, default 10)
- offset (number, default 0)

**States to Handle:**
- Loading: Show skeleton loaders
- Empty: Show "No defects" message
- Error: Show error, retry
- Success: Show defect list with status/severity badges

**API Calls:**
- GET /api/v1/projects/:id/defects?status=Open&severity=High (with query params for filters)
- Response: [{ id, title, description, severity, status, testCaseId, testRunResultId, createdAt }, ...]

**User Actions:**
- Change filter dropdowns → refetch with updated query params
- Click defect → show detail or modal
- Click "Create Defect" → show form (linked to failed test result)

**Child Components:**
- DefectCard (repeating)
- DefectFilters
- CreateDefectButton

---

### DefectCard

**Purpose:** Display single defect summary.

**Props:**
- defect (Defect object)
- onClick (navigate or show detail)

**State:** None

**Displays:**
- Title
- Severity badge (colored: Critical=red, High=orange, Medium=yellow, Low=gray)
- Status badge
- Related test case title (if available)
- Created date

**Child Components:** None

---

### ProjectDashboardTab

**Purpose:** Show project summary metrics and trends.

**Props:**
- projectId (from parent)

**State:**
- dashboard (object with all metrics)
- isLoading (boolean)
- error (string or null)

**States to Handle:**
- Loading: Show skeleton loaders for all cards
- Error: Show error, retry
- Success: Show dashboard cards with metrics

**API Calls:**
- GET /api/v1/projects/:id/dashboard
- Response: {
    total_cases: number,
    total_runs: number,
    pass_rate: number (0-100),
    total_results: number,
    passed: number,
    failed: number,
    blocked: number,
    not_run: number,
    open_defects: number,
    defects_by_severity: [{ severity, count }, ...],
    defects_by_status: [{ status, count }, ...],
    recent_defects: [{ id, title, severity, status, created_at }, ...]
  }

**Displays:**
- Total test cases (card)
- Total test runs completed (card)
- Current pass rate (card with percentage and progress bar)
- Results breakdown (cards: passed, failed, blocked, not run)
- Open defects by severity (card with counts and colors)
- Recent defects (list of last 5)

**Child Components:** None (or chart library if adding charts in future)

---

## Authentication & Token Management

### Access Token Lifecycle

1. **Login:** User submits email/password, receives `{ accessToken, expiresIn, user }`
2. **Storage:** accessToken stored in React state (memory, not localStorage)
3. **Use:** Included in every API request as `Authorization: Bearer <accessToken>`
4. **Expiry:** After 15 minutes, next API call will return 401

### Refresh Token Lifecycle

1. **Set by Server:** After login/register, server sets refresh token as httpOnly cookie
2. **Auto-sent:** Browser automatically includes refresh token in subsequent requests
3. **Silent Refresh:** On app load, call POST /api/v1/auth/refresh to get new accessToken
4. **On 401:** If API returns 401, app should attempt refresh; if refresh fails, redirect to login

### Implementation Pattern

```javascript
// On app load
useEffect(() => {
  const initAuth = async () => {
    try {
      const response = await fetch('/api/v1/auth/refresh', {
        method: 'POST',
        credentials: 'include' // important: send cookies
      });
      if (response.ok) {
        const { accessToken, user } = await response.json();
        setAccessToken(accessToken);
        setCurrentUser(user);
      } else {
        // Refresh failed, user not logged in
        setAccessToken(null);
        setCurrentUser(null);
      }
    } catch (err) {
      console.error('Auth check failed', err);
    } finally {
      setIsLoading(false);
    }
  };
  initAuth();
}, []);

// In fetch wrapper
const apiCall = async (url, options = {}) => {
  const response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${accessToken}`
    },
    credentials: 'include' // important: send cookies
  });
  
  if (response.status === 401) {
    // Try refresh
    const refreshResponse = await fetch('/api/v1/auth/refresh', {
      method: 'POST',
      credentials: 'include'
    });
    if (refreshResponse.ok) {
      const { accessToken: newToken } = await refreshResponse.json();
      setAccessToken(newToken);
      // Retry original request with new token
      return apiCall(url, options);
    } else {
      // Refresh failed, redirect to login
      logout();
    }
  }
  return response;
};
```

---

## State Management Strategy

**For MVP:** React hooks (useState, useContext) only.

**AuthContext:**
```javascript
const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [accessToken, setAccessToken] = useState(null);
  const [currentUser, setCurrentUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  const login = async (email, password) => { /* ... */ };
  const logout = async () => { /* ... */ };

  return (
    <AuthContext.Provider value={{ accessToken, currentUser, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
```

**Data Fetching Pattern (per component):**
```javascript
const [data, setData] = useState(null);
const [isLoading, setIsLoading] = useState(false);
const [error, setError] = useState(null);

useEffect(() => {
  fetchData();
}, [dependencies]);

const fetchData = async () => {
  setIsLoading(true);
  setError(null);
  try {
    const response = await fetch('/api/v1/...', {
      headers: { 'Authorization': `Bearer ${accessToken}` },
      credentials: 'include'
    });
    if (!response.ok) throw new Error(response.statusText);
    const json = await response.json();
    setData(json); // Most responses are unwrapped objects
  } catch (err) {
    setError(err.message);
  } finally {
    setIsLoading(false);
  }
};
```

---

## Error Handling

**API Error Response Shape:**
All errors return: `{ error: { message, code }, status: 4xx|5xx }`

**Error States in UI:**
- Show user-friendly message (do not expose technical details)
- Include "Retry" button for transient errors
- Log full error to console for debugging

**Error Boundaries:**
- One at App level (catches fatal errors)
- One around each major page (catches page-level errors, shows fallback)

---

## Loading & Empty States

**Loading State:**
- Show skeleton loaders matching content shape, or
- Centered spinner with "Loading..." text

**Empty State:**
- Show friendly message: "No projects yet" or "No test cases"
- Include action button: "Create Project", "Create Test Case", etc.

**Error State:**
- Show error message with "Retry" button
- Do NOT expose API stack traces or sensitive data