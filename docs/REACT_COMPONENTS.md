# Testify React Components Architecture

Component tree, data flow, and state management for the Testify frontend.
Built with React + TypeScript, using client-side router (React Router v6).

Generated during decomposition phase before coding.

---

## Design Principles

- Each component handles its own loading, empty, and error states
- All data fetching happens in parent components (lift state up pattern)
- Form validation on client must match server validation
- No secrets or tokens exposed in bundle
- All API calls return consistent response shape: { data: {...}, status: 200 }

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
  │   ├── TestCaseEditorPage (route: /projects/:id/cases/:caseId/edit)
  │   ├── TestRunExecutorPage (route: /projects/:id/runs/:runId/execute)
  │   └── NotFoundPage (route: *)

  
---

## Component Specifications

### App

**Purpose:** Root component, initializes authentication state, wraps entire app.

**Responsibilities:**
- Check if user is logged in (token in localStorage or session cookie)
- Fetch user profile on app load
- Provide auth context to all child components
- Handle logout

**State:**
- isAuthenticated (boolean)
- currentUser (User object or null)
- isLoading (boolean during initial auth check)

**States to Handle:**
- Loading: Show splash screen during auth check
- Unauthenticated: Redirect to login
- Authenticated: Show router with all pages
- Error: Show error message if auth check fails

**API Calls:**
- GET /api/v1/health (on app load, to verify API is running)
- GET /api/v1/auth/me (or similar, to verify current user — optional, depends on session approach)

**Child Components:**
- Router (only if authenticated)

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
- showPassword (boolean to toggle password visibility)

**Form Validation:**
- Email: required, valid format
- Password: required, min 8 chars

**States to Handle:**
- Default: Empty form
- Loading: Disable button, show spinner during login
- Error: Display error message from API (invalid email/password, rate limited)
- Success: Store token/session, redirect to /projects

**API Calls:**
- POST /api/v1/auth/login with { email, password }
- Response: { data: { id, email, token }, status: 200 }

**Child Components:** None

---

### RegisterPage

**Purpose:** User registration form.

**Route:** /register

**Props:** None

**State:**
- email (string)
- password (string)
- passwordConfirm (string)
- isLoading (boolean)
- error (string or null)
- showPassword (boolean)

**Form Validation:**
- Email: required, valid format
- Password: required, min 8 chars, strong (uppercase, lowercase, number, special char)
- Password confirm: required, must match password

**States to Handle:**
- Default: Empty form
- Loading: Disable button, show spinner
- Error: Display error (email exists, password too weak, validation failed)
- Success: Store token/session, redirect to /projects

**API Calls:**
- POST /api/v1/auth/register with { email, password }
- Response: { data: { id, email }, status: 201 }

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
- page (number, for pagination)
- limit (number, default 10)
- totalProjects (number, from pagination response)

**States to Handle:**
- Loading: Show skeleton loaders or spinner
- Empty: Show "No projects" message with "Create Project" button
- Error: Show error message, retry button
- Success: Show project list with cards

**API Calls:**
- GET /api/v1/projects?page=1&limit=10
- Response: { data: { projects: [...], pagination: {...} }, status: 200 }

**User Actions:**
- Click project card → navigate to /projects/:id
- Click "Create Project" → navigate to create form or show modal
- Click pagination buttons → fetch next/prev page

**Child Components:**
- ProjectCard (repeating for each project)
- CreateProjectButton/Modal

---

### ProjectCard

**Purpose:** Display single project summary (reused in ProjectListPage).

**Props:**
- project (Project object with id, name, description, owner_id)
- onClick (function to navigate to project detail)

**State:** None (stateless)

**Displays:**
- Project name
- Description (truncated if long)
- "View" button
- Owner indicator (if current user is not owner)

**Child Components:** None

---

### ProjectDetailPage

**Purpose:** Main hub for a single project. Tabs for test cases, runs, defects, dashboard.

**Route:** /projects/:id

**Props:**
- projectId (from route params)

**State:**
- project (Project object with members list)
- activeTab (string: "cases" | "runs" | "defects" | "dashboard")
- isLoading (boolean)
- error (string or null)

**States to Handle:**
- Loading: Show spinner
- Forbidden: Show "You don't have access" if user not member
- Not Found: Show 404 if project doesn't exist
- Error: Show error message
- Success: Show project name, members, tab navigation

**API Calls:**
- GET /api/v1/projects/:id
- Response: { data: { id, name, description, owner_id, members: [...] }, status: 200 }

**User Actions:**
- Click tab → update activeTab state
- Click "Create Test Case" → navigate to create form or show modal
- Click "Create Test Run" → show modal to select test cases

**Child Components:**
- TabNav (tabs for Cases, Runs, Defects, Dashboard)
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
- page (number, pagination)
- limit (number, default 10)
- selectedCaseId (for edit mode, or null)

**States to Handle:**
- Loading: Show skeleton loaders
- Empty: Show "No test cases" with "Create" button
- Error: Show error, retry button
- Success: Show list of test cases with Edit/Delete buttons

**API Calls:**
- GET /api/v1/projects/:id/cases?page=1&limit=10
- POST /api/v1/projects/:id/cases (create new)
- PATCH /api/v1/projects/:id/cases/:caseId (update)
- DELETE /api/v1/projects/:id/cases/:caseId (delete)

**User Actions:**
- Click "New Case" → show TestCaseEditor (inline modal or new page)
- Click Edit → show TestCaseEditor with prefilled data
- Click Delete → confirm, then delete
- Click case → maybe show details view

**Child Components:**
- TestCaseCard (repeating)
- TestCaseEditor (modal or inline)
- CreateButton

---

### TestCaseEditor

**Purpose:** Form to create or edit a test case.

**Props:**
- projectId (from parent)
- caseId (if editing, otherwise null)
- initialData (TestCase object if editing, or empty defaults)
- onSave (callback after successful save)
- onCancel (callback to close editor)

**State:**
- title (string)
- preconditions (string)
- steps (string)
- expectedResult (string)
- priority (enum: Low/Medium/High/Critical)
- isLoading (boolean)
- error (string or null)
- isDirty (boolean, form has unsaved changes)

**Form Validation:**
- Title: required, 1-200 chars
- Steps: required, 1-2000 chars
- Expected Result: required, 1-2000 chars
- Priority: required, one of enum values
- Preconditions: optional

**States to Handle:**
- Default: Empty form (create) or prefilled (edit)
- Loading: Disable save button, show spinner
- Error: Show validation errors or API error
- Unsaved Changes: Show warning on cancel if isDirty
- Success: Show success message, call onSave callback

**API Calls:**
- POST /api/v1/projects/:id/cases (create)
- PATCH /api/v1/projects/:id/cases/:caseId (update)

**Child Components:** None (just form inputs)

---

### ProjectTestRunsTab

**Purpose:** List test runs, allow creation and execution.

**Props:**
- projectId (from parent)

**State:**
- runs (array of TestRun objects)
- isLoading (boolean)
- error (string or null)
- page (number, pagination)
- limit (number, default 10)

**States to Handle:**
- Loading: Show skeleton loaders
- Empty: Show "No test runs" with "Create Run" button
- Error: Show error, retry
- Success: Show list of runs with status, pass rate, date

**API Calls:**
- GET /api/v1/projects/:id/runs?page=1&limit=10
- POST /api/v1/projects/:id/runs (create new run)
- DELETE /api/v1/projects/:id/runs/:runId (delete)

**User Actions:**
- Click "New Run" → show modal to select test cases
- Click run → navigate to /projects/:id/runs/:runId/execute
- Click Delete → confirm, then delete

**Child Components:**
- TestRunCard (repeating)
- CreateTestRunModal
- ConfirmDeleteModal

---

### CreateTestRunModal

**Purpose:** Let user select which test cases to include in a new run.

**Props:**
- projectId (from parent)
- onConfirm (callback with selected case IDs)
- onCancel (close modal)

**State:**
- cases (array of TestCase objects from API)
- selectedCaseIds (array of UUIDs)
- isLoading (boolean during cases fetch)
- isSaving (boolean during POST run)
- error (string or null)

**States to Handle:**
- Loading: Show spinner while fetching cases
- Loaded: Show checkboxes for all cases, "Select All" button, "Create" button
- Error: Show error
- Saving: Disable buttons, show spinner

**API Calls:**
- GET /api/v1/projects/:id/cases (to list all cases)
- POST /api/v1/projects/:id/runs with { test_case_ids: [...] }

**Child Components:** None (just checkboxes)

---

### TestRunExecutorPage

**Purpose:** Execute a test run — mark each test case as Pass/Fail/Blocked/Not Run.

**Route:** /projects/:id/runs/:runId/execute

**Props:**
- projectId (from route)
- runId (from route)

**State:**
- run (TestRun object with test_results array)
- isLoading (boolean)
- error (string or null)
- results (object mapping result_id to { result: "Pass"|"Fail"|"Blocked"|"Not Run", notes: string })
- isSubmitting (boolean during PATCH)

**States to Handle:**
- Loading: Show spinner
- Not Found: Show 404
- Loaded: Show each test case with radio buttons (Pass/Fail/Blocked/Not Run) and notes textarea
- Submitting: Disable buttons, show spinner
- Error: Show error, retry
- Success: Show success message, navigate back to /projects/:id/runs

**API Calls:**
- GET /api/v1/projects/:id/runs/:runId (fetch run with current results)
- PATCH /api/v1/projects/:id/runs/:runId with { status: "Complete", test_results: [...] }

**User Actions:**
- Click radio buttons to select result for each case
- Type notes for each case
- Click "Complete Run" → PATCH to API with status="Complete"
- Click "Save Draft" → PATCH with status="In Progress"

**Child Components:**
- TestResultCard (repeating for each case)

---

### TestResultCard

**Purpose:** Single test case result in TestRunExecutor (radio buttons + notes).

**Props:**
- testCase (TestCase object)
- currentResult (string: Pass/Fail/Blocked/Not Run or null)
- currentNotes (string)
- onChange (callback with { resultId, result, notes })

**State:** None (all state in parent)

**Displays:**
- Test case title and priority
- Radio buttons for result
- Textarea for notes
- Shows "This result is linked to defect #X" if applicable

**Child Components:** None

---

### ProjectDefectsTab

**Purpose:** List defects in project, allow filtering by status/severity.

**Props:**
- projectId (from parent)

**State:**
- defects (array of Defect objects)
- isLoading (boolean)
- error (string or null)
- filterStatus (enum or null: Open/In Progress/Resolved/Closed)
- filterSeverity (enum or null: Low/Medium/High/Critical)
- page (number, pagination)
- limit (number, default 10)

**States to Handle:**
- Loading: Show skeleton loaders
- Empty: Show "No defects" message
- Error: Show error, retry
- Success: Show defect list with status/severity badges

**API Calls:**
- GET /api/v1/projects/:id/defects?status=Open&severity=High&page=1&limit=10

**User Actions:**
- Change filter dropdowns → refetch with new filters
- Click defect → show DefectDetail or open modal
- Click "Create Defect" → show CreateDefectForm

**Child Components:**
- DefectCard (repeating)
- FilterBar (dropdowns for status/severity)
- CreateDefectButton

---

### DefectCard

**Purpose:** Display single defect summary (reused in ProjectDefectsTab).

**Props:**
- defect (Defect object)
- onClick (navigate or show detail)

**State:** None

**Displays:**
- Title
- Severity badge (colored: Critical=red, High=orange, etc.)
- Status badge
- Related test case title
- Created date

**Child Components:** None

---

### ProjectDashboardTab

**Purpose:** Show project summary metrics.

**Props:**
- projectId (from parent)

**State:**
- dashboard (object with stats)
- isLoading (boolean)
- error (string or null)

**States to Handle:**
- Loading: Show skeleton loaders for all cards
- Error: Show error, retry
- Success: Show dashboard cards

**API Calls:**
- GET /api/v1/projects/:id/dashboard

**Displays:**
- Total test cases (card)
- Total test runs completed (card)
- Current pass rate (card with percentage and bar chart)
- Open defects by severity (card with counts: Critical, High, Medium, Low)
- Recent defects (small list with 5-10 most recent)
- Maybe: pass rate trend (chart if time allows in MVP)

**Child Components:** None (or chart component if adding charts)

---

## State Management Strategy

**For MVP:** React hooks only (useState, useContext for auth).

**Context API:**
- AuthContext: currentUser, isAuthenticated, login(), logout()
- (Can add more contexts later if needed)

**API State Pattern:**
Each page/tab component follows this pattern:
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
const response = await fetch('/api/v1/...');
if (!response.ok) throw new Error(response.statusText);
const json = await response.json();
setData(json.data);
} catch (err) {
setError(err.message);
} finally {
setIsLoading(false);
}
};


---

## Error Boundaries

Recommended error boundary components:
- One at App level (catches any fatal errors)
- One around each page (catches page-level errors, shows fallback UI)

---

## Loading & Empty States

**Loading State:**
- Show skeleton loaders (gray placeholder boxes) matching content shape
- Alternative: centered spinner with "Loading..." text

**Empty State:**
- Show friendly message: "No projects yet" or "No test cases"
- Include action button: "Create Project", "Create Test Case"

**Error State:**
- Show error message (API error or user-friendly message)
- Include "Retry" button to refetch
- Do NOT show technical error details to user