# AI-Assisted Engineering Challenge

**Duration:** 2 weeks
**Deliverable:** A working, deployed web application plus the evidence of how you built it.

---

## 1. Why you are doing this

This is not a test of whether you can write code. It is a test of whether you can **direct** an AI to build something real, and then **stand behind what it produced**.

The industry has already moved. The junior developers who get hired now are the ones who can take a vague requirement, break it down, drive an AI through the build, and then defend every line in code review. The ones who paste prompts and hope are the ones who get filtered out.

By the end of this you will have:

- A production-shaped application you can show anyone.
- A public GitHub repository with real commit history and CI/CD.
- A clear picture of where your gaps are, so we can close them deliberately.

The gaps are the point. Do not hide them.

---

## 2. What you are building

**Testify** - a lightweight test run and defect tracker.

You know this domain already, which is deliberate. You should not be burning energy working out what the product does. You should be burning it on the engineering.

### Core functionality (all mandatory)

**Projects**
- Create a project with a name and description.
- List and view projects.

**Test cases**
- Belong to a project.
- Fields: title, preconditions, steps, expected result, priority (Low/Medium/High/Critical).
- Full create, read, update, delete.

**Test runs**
- Created against a project, and pull in a selected set of test cases.
- Each case in a run has a result: Not Run, Pass, Fail, Blocked.
- A run has a status: In Progress or Complete.
- A run shows a live pass rate.

**Defects**
- Raised from a failed test case in a run.
- Fields: title, description, severity, status (Open / In Progress / Resolved / Closed).
- A defect links back to the test case and run that produced it.

**Dashboard**
- Per project: total cases, runs completed, current pass rate, open defects by severity.

### Explicitly out of scope

Do not build: email notifications, file attachments, comment threads, real-time updates, mobile apps, or an integration with any real test tool. If you finish early, go to the stretch goals in section 8. Scope creep is a fail condition, not initiative.

---

## 3. Technical requirements

You may choose your API language from: **TypeScript (Node), C#/.NET, Go, or Ruby on Rails**. Pick the one you want to be interviewed on. Everything else below is fixed.

### 3.1 Web application
- React with TypeScript. No plain JavaScript.
- A client-side router with at least: project list, project detail, test case editor, test run execution view, defect list, dashboard.
- Loading, empty and error states on every view that fetches data. An empty table with no message is incomplete work.
- Form validation on the client that matches the validation on the server.
- No secrets, keys or tokens in the frontend bundle.

### 3.2 API
- RESTful, versioned under `/api/v1`.
- Correct HTTP verbs and status codes. A failed create returns 4xx, not 200 with an error body.
- Consistent error response shape across every endpoint.
- Server-side validation on every write. Never trust the client.
- Pagination on any endpoint that can return an unbounded list.
- OpenAPI/Swagger spec, generated or hand-written, served by the app.
- A relational database (PostgreSQL). Schema managed by migrations, not by hand.

### 3.3 Containerization
- A Dockerfile for the API and a Dockerfile for the web app.
- Multi-stage builds. Your final images must not contain build tooling or source.
- Containers run as a non-root user.
- `docker compose up` from a clean clone must give a working local environment: API, web, database, and migrations applied.
- A `.dockerignore` that actually excludes what it should.
- Image sizes recorded in your README. Be able to explain why they are what they are.

### 3.4 Infrastructure
- Infrastructure defined as code (Terraform preferred; Pulumi or a documented equivalent is acceptable).
- Environment configuration through environment variables only. Nothing environment-specific compiled into an image.
- At minimum two environments conceptually separated: local and deployed.
- A health check endpoint that reports application and database status, and is used by your deployment platform.
- Database backup or persistence strategy documented, even if you do not automate it.

### 3.5 Security
This section is not optional polish. Treat it as core.

- Authentication: users register and log in. Passwords hashed with bcrypt or argon2. Never plain text, never MD5 or SHA1.
- Session management via JWT or secure HTTP-only cookies. Justify your choice in writing.
- Authorization: a user can only see and modify data in projects they belong to. Prove this with a test.
- Input validation and output encoding on every user-supplied field.
- Parameterised queries or an ORM. No string-concatenated SQL anywhere.
- Rate limiting on authentication endpoints.
- Security headers set (CSP, HSTS, X-Content-Type-Options, X-Frame-Options).
- CORS configured to a known origin, not `*`.
- Secrets in GitHub Secrets and environment variables. If a secret ever lands in a commit, rotate it and write up what happened.
- Dependency scanning enabled (Dependabot or Trivy) with findings triaged, not ignored.
- A `SECURITY.md` listing which OWASP Top 10 risks you addressed, how, and which ones you knowingly did not.

### 3.6 GitHub and deployment
- Public repository, meaningful commit history. One giant "initial commit" is a fail.
- Trunk-based or short-lived feature branches. Every change reaches `main` through a pull request, including your own.
- Pull requests use a template and have a description that explains the change.
- GitHub Actions pipeline that on every PR: lints, type-checks, runs tests, builds both images.
- On merge to `main`: builds and pushes images, then deploys.
- Deployed and publicly reachable over HTTPS. Fly.io, Render, Railway or AWS free tier are all fine. Cheapest thing that works.
- A branch protection rule on `main` that requires the pipeline to pass.
- The README badge shows the pipeline status.

### 3.7 Testing
You are a QA. This is your strength, so I am setting the bar higher here than elsewhere.

- Unit tests on business logic (pass rate calculation, status transitions, authorization rules).
- Integration tests against the API hitting a real database in a container.
- At least six end-to-end tests (Playwright or Cypress) covering the critical paths: register, log in, create a project, create a case, execute a run, raise a defect.
- Tests run in CI and block the merge when they fail.
- A short written test strategy: what you chose to test at each level and why.

---

## 4. How you must work with AI

Use AI aggressively. That is the whole point. But these rules are non-negotiable.

**Rule 1: You own every line.**
If it is in your repository, you can explain it. "The AI wrote it" is not an answer in a code review, an interview, or an incident.

**Rule 2: Keep a decision log.**
Maintain `docs/DECISIONS.md`. For every significant decision (not every prompt), record:

```
## [Date] - [Decision]
**Context:** what I was trying to do
**What I asked the AI:** the substance of the prompt
**What it gave me:** summarised
**What I changed and why:** the important part
**What I did not understand at first:** be honest here
```

I expect between fifteen and thirty entries. The "what I did not understand" field is the most valuable thing in this whole exercise. Do not sanitise it.

**Rule 3: Never accept a security or infrastructure suggestion without verifying it.**
AI models are confidently wrong about auth, CORS, container permissions and IAM more often than anywhere else. Every time you take AI advice in section 3.4 or 3.5, verify it against primary documentation and note the source in your decision log.

**Rule 4: Prompt small.**
"Build me a test management app" produces something you cannot maintain. Work at the level of a single endpoint, a single component, a single migration. If a response is longer than you can review carefully, your prompt was too big.

**Rule 5: Log where the AI cost you time.**
Keep a short section in `DECISIONS.md` for the times AI sent you down a wrong path: hallucinated APIs, outdated patterns, code that looked right and was not. This is not a criticism of the tooling. Knowing its failure modes is the skill.

---

## 5. Milestones

| Milestone | Due | Must be true |
|---|---|---|
| **M1: Foundations** | End of day 2 | Repo created, compose file runs API + database locally, health endpoint responds, first migration applied, CI runs lint on PRs |
| **M2: API complete** | End of day 5 | All entities have working CRUD, validation, pagination, OpenAPI spec served, integration tests green |
| **M3: Auth and security** | End of day 7 | Registration and login working, authorization enforced and tested, security headers set, `SECURITY.md` written |
| **M4: Web application** | End of day 11 | All views built and wired to the API, all states handled, E2E tests green |
| **M5: Deployed** | End of day 14 | Infrastructure as code applied, deployed over HTTPS, pipeline deploys on merge, README and decision log complete |

At each milestone, push and tell me. I will review the code and the log, not just the running app.

---

## 6. How you will be assessed

Nine areas, each scored 1 to 5. I am looking for the shape of the profile, not the total.

| Area | What I am looking at |
|---|---|
| Problem decomposition | Did you break work down before prompting, or prompt and then react |
| AI direction | Prompt quality, iteration, knowing when to stop asking and start reading docs |
| Code comprehension | Can you explain any file I point at, cold |
| API design | Resource modelling, status codes, consistency, error handling |
| Frontend | State management, component structure, handling of loading and failure |
| Security | Depth of understanding versus checklist compliance |
| Infrastructure and containers | Whether you understand what you deployed or just got it working |
| Testing | Given your background, I expect this to be your strongest column |
| Communication | README, decision log, PR descriptions, how you explain trade-offs |

### The walkthrough

On completion we do a forty-five minute session. I will:

1. Pick three files at random and ask you to walk me through them.
2. Ask you to make a small change live, without AI assistance.
3. Ask you to explain one security decision and one infrastructure decision in depth.
4. Ask what you would do differently with another two weeks.

**You cannot pass this exercise on a working application alone.** A deployed app you cannot explain scores lower than a partially finished one you can.

---

## 7. Definition of done

- [ ] `git clone` then `docker compose up` gives a working local environment with no manual steps beyond copying `.env.example`
- [ ] Deployed application reachable over HTTPS
- [ ] Every core feature in section 2 works end to end
- [ ] Pipeline green on `main`, branch protection enabled
- [ ] All test levels present and running in CI
- [ ] `README.md`: what it is, architecture diagram, how to run it, how to deploy it, known limitations
- [ ] `SECURITY.md` complete
- [ ] `docs/DECISIONS.md` complete and honest
- [ ] No secrets anywhere in the git history
- [ ] You can explain any file in the repository

---

## 8. Stretch goals

Only after section 7 is fully signed off.

- Import test cases from CSV.
- Role-based access control (admin, tester, viewer) with tests proving each boundary.
- Structured logging with correlation IDs traced from browser to database.
- A second deployed environment (staging) with promotion through the pipeline.
- Test run history and trend chart over time.
- Ephemeral preview environment per pull request.

---

## 9. Getting unstuck

Ask me anything, at any point, with one condition: tell me what you already tried and what the AI told you. "This does not work" gets you a question back. "I asked for X, got Y, expected Z, and here is what I checked" gets you a real answer.

Being stuck is not the failure. Being stuck quietly for two days is.
