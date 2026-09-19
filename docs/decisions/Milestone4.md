# Milestone 4: Web Application

## Decision 20: Phase 1 Auth — Setup and Blockers Encountered

**Context:**
Started M4 (Frontend) with Phase 1: Auth setup. Goal was LoginPage, RegisterPage, AuthContext, routing — all wired and functional. Quickly hit multiple configuration and dependency issues that consumed ~2 hours of troubleshooting.

**What I asked the AI:**
Build Phase 1 auth from scratch — types, AuthContext (login/register/logout/refresh), LoginPage, RegisterPage, React Router setup, basic CSS styling. Copy-paste code, move fast.

**What it gave me:**
1. TypeScript types (User, AuthResponse, Project)
2. AuthContext with full auth flow (login, register, logout, silent refresh on app load)
3. LoginPage and RegisterPage forms with validation
4. App.tsx with Router and protected routes
5. Global CSS (forms, buttons, cards, utilities)

**What I changed and why:**
1. Attempted Tailwind CSS setup — hit PostCSS configuration issues immediately (Tailwind v4 doesn't ship CLI by default, PostCSS config errors). Abandoned Tailwind, reverted to plain CSS instead. Lesson: Tailwind adds complexity we don't need for MVP; plain CSS is faster.
2. Fixed import paths: `import { User, AuthResponse } from '../types'` → `import type { User, AuthResponse } from '../types/index'` (TypeScript verbatimModuleSyntax requirement + explicit index path).
3. Downgraded react-router-dom from v7.18.4 to v6 because v7 had incompatibility with React 19 hooks (BrowserRouter was throwing "Invalid hook call" errors).
4. Fixed Router/AuthProvider nesting: Initially wrapped Router inside AuthProvider (wrong), causing hook dispatcher to be null. Corrected to AuthProvider wrapping Router (hooks must be called in valid React component context).
5. Removed useAuth.ts hook initially, then recreated it when imports failed.
6. Deleted postcss.config.js and tailwind.config.js after abandoning Tailwind.

**What I did not understand at first:**
- That Tailwind v4 changed its distribution model away from CLI — wasted 30min trying to configure something that doesn't work the way it used to. Should have checked Tailwind v4 docs before attempting setup.
- That TypeScript's `verbatimModuleSyntax` flag requires explicit `import type` for interfaces, not just `import`. This is a strict mode that prevents runtime-only imports.
- That react-router-dom v7 has compatibility issues with React 19 in some configurations — v6 is the stable fallback.
- That component composition order matters with React hooks: hooks in child components can't execute before the parent's hook dispatcher is initialized. AuthProvider must wrap Router, not vice versa.

**Status:**
RESOLVED — Phase 1 auth fully functional. LoginPage renders, RegisterPage renders, routing works, protected routes redirect to /login. Frontend is ready for Phase 2 (Projects list/detail).

**Testing:**
Manual testing in browser: both login and register pages load and render correctly. Forms are interactive (not yet wired to API, but structure is correct).

**Commit:** 955d955 (feat/phase-1-auth PR merged to main)


---

### Decision 21: Phase 2 Projects — ProjectListPage and useApi Hook

**Context:**
Built ProjectListPage to list all projects from API. Created useApi hook as centralized fetch wrapper handling Bearer token auth, credentials, and error handling.

**What I asked:**
Build useApi hook (token auth wrapper) and ProjectListPage (fetches projects, grid layout, click to detail page).

**What it gave me:**
useApi hook with apiCall function, ProjectListPage with loading/error/empty states, grid layout, navigation.

**What I changed:**
Implemented as provided. Wired directly to API at http://localhost:5000.

**What I did not understand at first:**
None — this phase was straightforward.

**Status:**
RESOLVED — ProjectListPage fully functional, fetches from API, routes to detail page.

**Commit:** f161657

---

### Decision 22: Phase 3 Project Detail — ProjectDetailPage with Tab Navigation

**Context:**
Built ProjectDetailPage with tab navigation (TestCases, TestRuns, Defects, Dashboard). Created TabNav component and 4 tab skeleton components.

**What I asked:**
Build ProjectDetailPage, TabNav component, and 4 placeholder tab components. Wire detail page to API.

**What it gave me:**
ProjectDetailPage fetching single project from API, TabNav with active tab styling, 4 tab skeletons ready for content, back button to projects list.

**What I changed:**
Implemented as provided.

**What I did not understand at first:**
None — tab pattern is standard.

**Status:**
RESOLVED — ProjectDetailPage fully functional, fetches from API, tabs switch on click. Ready for Phase 4 (fill in tab content).

**Commit:** 0bdb39c

---

### Decision 23: Phase 4 Forms — TestCaseEditor Component Template

**Context:**
Started Phase 4 (Forms). Built TestCaseEditor as template component showing form pattern for all remaining forms (TestRunExecutor, CreateTestRunModal).

**What I asked:**
Build TestCaseEditor form component (create/edit test cases, fetch if caseId exists, POST/PATCH).

**What it gave me:**
Complete form with fields (title, preconditions, steps, expectedResult, priority), error/loading states, save/cancel buttons.

**What I changed:**
None — implemented as provided.

**Status:**
IN PROGRESS — TestCaseEditor built. Remaining: fill tab content (TestCasesTab with editor + list), TestRunExecutor, CreateTestRunModal, E2E tests.

**Commit:** d1cbcf8

---

### Decision 24: Phase 4 Forms Complete — TestCasesTab and TestCaseEditor

**Context:**
Completed Phase 4 forms. Built full TestCasesTab with list, add/edit/delete buttons. TestCaseEditor form component handles both create and edit flows.

**What I asked:**
Implement TestCasesTab (list + CRUD buttons), fill remaining placeholder tabs, fix TypeScript errors.

**What it gave me:**
TestCasesTab with useEffect fetch, TestCaseEditor integration, table display with actions, error/loading states.

**What I changed:**
Fixed TypeScript errors: type-only imports for Project interface, removed unused React imports, prefixed unused params with underscore.

**Status:**
RESOLVED — Phase 4 forms complete. TestCasesTab fully functional (pending API connection). Remaining tabs (TestRunsTab, DefectsTab, DashboardTab) filled with placeholders ready for content.

**Commit:** be6bc5e

---

**Last updated:** 2026-09-19
**Status:** M4 Phase 3 complete. Phase 1 (Auth), Phase 2 (Projects list), Phase 3 (Project detail with tabs) built and merged. Phases 4-5 in progress.
**Next:** Phase 4 (Forms: TestCaseEditor, TestRunExecutor, CreateTestRunModal).

---