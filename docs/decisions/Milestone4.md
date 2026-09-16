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