# Testify Database Schema

This document defines all tables, fields, and relationships for the Testify application.
Generated during decomposition phase before coding.

---

## Entities & Relationships


### Users

Purpose: Store application users who can register, log in, and own/participate in projects.

Fields:
- id (UUID or integer) - Primary key
- email (string, unique) - Email for login
- password_hash (string) - Bcrypt hashed password, never plain text
- created_at (timestamp) - When user registered
- updated_at (timestamp) - Last profile update

Constraints:
- email must be unique (can't have two users with same email)
- email must be valid format
- password_hash must never be null

Design Decision: We do NOT track last_login because:
- Not required for core Testify features
- Adds database writes on every login (performance cost)
- Can be added later via migration if needed

Example row:
| id | email | password_hash | created_at | updated_at |
| 1 | alice@example.com | $2b$12$... | 2024-01-01 | 2024-01-01 |


### Projects

Purpose: Containers for test cases, runs, and defects. Users can own or participate in projects.

Fields:
- id (UUID or integer) - Primary key
- name (string, required) - Project name
- description (text, optional) - Project details
- owner_id (foreign key → Users.id) - Who created/owns this project
- created_at (timestamp) - When project was created
- updated_at (timestamp) - Last modification

Constraints:
- owner_id must reference a valid user
- name must be unique per owner (two users can have projects named "WebApp", but one user can't have two)
- name and description are required on creation

Design Decision: Projects do NOT have a status field because:
- Not required for core features (create, list, view)
- Can be added later if archive/soft-delete needed
- Keeps schema lean for MVP

Relationships:
- Each Project has ONE owner (a User)
- Each Project can have MANY members (multiple Users, via ProjectMembers table)
- Each Project can have MANY TestCases

Example:
| id | name | description | owner_id | created_at | updated_at |
| 1 | WebApp Tests | Testing web app | 1 | 2024-01-01 | 2024-01-01 |


### ProjectMembers

Purpose: Links Users to Projects (many-to-many relationship).
A user can be a member of multiple projects.
A project can have multiple users.

Fields:
- id (UUID or integer) - Primary key
- project_id (foreign key → Projects.id) - Which project
- user_id (foreign key → Users.id) - Which user
- role (string, optional) - For future RBAC: "admin", "tester", "viewer" (out of scope for MVP)
- created_at (timestamp) - When user was added to project

Constraints:
- Combination of (project_id, user_id) must be unique (can't add same user twice to same project)
- project_id and user_id must reference valid records
- Deleting a User or Project should cascade-delete memberships

Relationships:
- Many ProjectMembers link to one Project
- Many ProjectMembers link to one User
- The owner_id in Projects is also a member in ProjectMembers (owner is always a member)

Design Decision: Role field exists but is unused in MVP because:
- RBAC is a stretch goal (section 8 of challenge)
- Can be implemented later without schema migration
- Keeps MVP focused on core features

Example:
| id | project_id | user_id | role | created_at |
| 1 | 1 | 1 | (null) | 2024-01-01 |
| 2 | 1 | 2 | (null) | 2024-01-01 |


### TestCases

Purpose: Define individual test cases within a project.
Each test case describes what to test, under what conditions, and what the expected result is.

Fields:
- id (UUID or integer) - Primary key
- project_id (foreign key → Projects.id) - Which project this case belongs to
- title (string, required) - Test case name
- preconditions (text, optional) - Setup required before running test
- steps (text, required) - Step-by-step instructions
- expected_result (text, required) - What should happen
- priority (enum: Low/Medium/High/Critical) - Test priority
- created_at (timestamp) - When created
- updated_at (timestamp) - Last modification

Constraints:
- project_id must reference a valid project
- title, steps, and expected_result are required
- priority must be one of: Low, Medium, High, Critical
- Deleting a Project should cascade-delete all its TestCases

Relationships:
- Many TestCases belong to one Project
- One TestCase can appear in MANY TestRuns (via TestRunResults)
- One TestCase can have MANY Defects

Example:
| id | project_id | title | priority | created_at | updated_at |
| 1 | 1 | Login with valid credentials | High | 2024-01-01 | 2024-01-01 |
| 2 | 1 | Login with invalid password | Medium | 2024-01-01 | 2024-01-01 |


### TestRuns

Purpose: Record instances of running test cases.
A test run captures when you executed a specific set of test cases, and the result of each.

Fields:
- id (UUID or integer) - Primary key
- project_id (foreign key → Projects.id) - Which project this run is for
- status (enum: In Progress/Complete) - Current state of the run
- created_at (timestamp) - When run was started
- updated_at (timestamp) - Last modification
- completed_at (timestamp, nullable) - When run was marked complete

Constraints:
- project_id must reference a valid project
- status must be one of: In Progress, Complete
- Deleting a Project should cascade-delete all its TestRuns

Relationships:
- Many TestRuns belong to one Project
- One TestRun can include MANY TestCases (via TestRunResults)
- One TestRun can have MANY Defects

Design Decision: Pass rate is CALCULATED, not stored in database because:
- Derived from TestRunResults (count passed / total cases)
- Changes when results change, so storing it creates sync problems
- Query calculated on-demand in API

Example:
| id | project_id | status | created_at | updated_at | completed_at |
| 1 | 1 | In Progress | 2024-01-02 10:00 | 2024-01-02 10:30 | (null) |
| 2 | 1 | Complete | 2024-01-01 09:00 | 2024-01-01 11:00 | 2024-01-01 11:00 |


### TestRunResults

Purpose: Junction table linking TestRuns to TestCases.
Records the result (Not Run/Pass/Fail/Blocked) of each test case within a specific run.

Fields:
- id (UUID or integer) - Primary key
- test_run_id (foreign key → TestRuns.id) - Which run this result belongs to
- test_case_id (foreign key → TestCases.id) - Which test case was executed
- result (enum: Not Run/Pass/Fail/Blocked) - Outcome of this test case in this run
- notes (text, optional) - Any notes about the result (why it failed, why blocked, etc.)
- created_at (timestamp) - When result was recorded
- updated_at (timestamp) - Last modification

Constraints:
- Combination of (test_run_id, test_case_id) must be unique (can't run same case twice in same run)
- test_run_id and test_case_id must reference valid records
- result must be one of: Not Run, Pass, Fail, Blocked
- Deleting a TestRun should cascade-delete all its results

Relationships:
- Many TestRunResults belong to one TestRun
- Many TestRunResults reference one TestCase
- One TestRunResult can trigger ONE Defect (if result is Fail)

Example:
| id | test_run_id | test_case_id | result | notes | created_at | updated_at |
| 1 | 1 | 1 | Pass | (null) | 2024-01-02 10:05 | 2024-01-02 10:05 |
| 2 | 1 | 2 | Fail | Password field not accepting special chars | 2024-01-02 10:10 | 2024-01-02 10:10 |
| 3 | 1 | 3 | Blocked | Bug DEFECT-42 blocks this test | 2024-01-02 10:15 | 2024-01-02 10:15 |


### Defects

Purpose: Track bugs/issues discovered during test runs.
A defect is raised when a test case fails, and links back to that failure.

Fields:
- id (UUID or integer) - Primary key
- title (string, required) - Bug title
- description (text, required) - Detailed description of the issue
- severity (enum: Low/Medium/High/Critical) - How severe the bug is
- status (enum: Open/In Progress/Resolved/Closed) - Current state of the defect
- test_case_id (foreign key → TestCases.id) - Which test case found this bug
- test_run_result_id (foreign key → TestRunResults.id) - Which specific run result raised this defect
- created_at (timestamp) - When defect was created
- updated_at (timestamp) - Last modification
- resolved_at (timestamp, nullable) - When defect was resolved

Constraints:
- title, description, severity, and status are required
- status must be one of: Open, In Progress, Resolved, Closed
- severity must be one of: Low, Medium, High, Critical
- test_case_id and test_run_result_id must reference valid records
- Deleting a TestCase or TestRunResult cascades to defects

Relationships:
- Many Defects reference one TestCase
- Many Defects reference one TestRunResult
- One Defect can have MANY historical status changes (tracked via created_at/updated_at)

Design Decision: Defects do NOT have comments/threads because:
- Challenge explicitly excludes comment threads (section 2, out of scope)
- Can be added later without schema changes
- Keeps MVP focused on core defect tracking

Example:
| id | title | severity | status | test_case_id | test_run_result_id | created_at | updated_at | resolved_at |
| 1 | Password field rejects special chars | High | Open | 2 | 2 | 2024-01-02 10:10 | 2024-01-02 10:10 | (null) |
| 2 | Login button unresponsive on slow networks | Medium | In Progress | 1 | 1 | 2024-01-01 09:30 | 2024-01-02 09:00 | (null) |


---

## Relationship Diagram (Text Format)


## Foreign Key Summary

| Table | Foreign Key | References | Behavior |
|-------|-------------|------------|----------|
| Projects | owner_id | Users.id | Cascade delete |
| ProjectMembers | project_id | Projects.id | Cascade delete |
| ProjectMembers | user_id | Users.id | Cascade delete |
| TestCases | project_id | Projects.id | Cascade delete |
| TestRuns | project_id | Projects.id | Cascade delete |
| TestRunResults | test_run_id | TestRuns.id | Cascade delete |
| TestRunResults | test_case_id | TestCases.id | Cascade delete |
| Defects | test_case_id | TestCases.id | Cascade delete |
| Defects | test_run_result_id | TestRunResults.id | Cascade delete |

## Index Strategy

For performance (to be implemented during M1):
- Users: index on email (unique)
- Projects: index on owner_id
- ProjectMembers: unique index on (project_id, user_id)
- TestCases: index on project_id
- TestRuns: index on project_id
- TestRunResults: unique index on (test_run_id, test_case_id)
- Defects: index on (test_case_id, status)

## Notes

- All timestamps use UTC
- All UUIDs use UUID v4 (will confirm during M1 implementation)
- All enums stored as strings (more readable in database than integers)
- Cascade deletes ensure referential integrity (deleting a project deletes all related data)