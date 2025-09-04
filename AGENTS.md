# AGENTS.md — Backend Guidelines

This repository is set up for experimenting with Codex on backend tasks only.  
Frontend code, browser automation, and deployment configuration are **out of scope**.

---

## Project Layout
- **AppHost (Aspire):** `./backend/TheGame.AppHost`
- **API (Minimal API):** `./backend/TheGame.Api`
- **Tests (xUnit):** `./backend/TheGame.Tests`
  - Integration tests live under `./backend/TheGame.Tests/ItnegrationTests`

---

## Toolchain
- .NET SDK **9.x** (pinned by `global.json`)
- Node.js 22 exists in repo but is not required for backend tasks
- Aspire orchestrates backend services through AppHost

---

## Commands
```bash
# Build solution
dotnet build .

# Build API
dotnet build ./backend/TheGame.Api -c Debug

# Run all tests (unit + integration)
dotnet test ./backend/TheGame.Tests --verbosity=detailed  --filter=Category=Unit & Category=Integration

# Optional: run Aspire locally
dotnet run --project ./backend/TheGame.AppHost
```

Integration tests use WebApplicationFactory<TEntryPoint> and Testcontainers (MsSqlFixture) to spin up SQL and apply migrations automatically. No manual DB setup is required.

## Branching & PRs

Working branch: `experiment/codex-agent`

Target branch for PRs: `feature/save-prompt-queries`

PRs: must be Draft

Do not commit directly to main

## Permissions

### Allowed

1. Modify existing backend code
1. Add or update endpoints
1. Adjust DI wiring
1. Create EF Core migrations
1. Add or modify tests in ./backend/TheGame.Tests
1. Run dotnet cli commands to build and test solutions.

### Not Allowed

1. Creating new projects or solutions
1. Frontend or Playwright work
1. Changing authentication provider setup
1. Modifying production secrets or deployment configs

## Testing

Framework: `xUnit`

Unit tests: no DB.

Integration tests: in-memory test host + Testcontainers (MsSqlFixture)

Auth in tests: use JWT bearer tokens as shown in
IntegrationTests/UserEndpointTests.CanGetPlayerDataForAuthenticatedUser

Each new or changed endpoint must include:

1. At least one happy-path test
1. At least one unhappy-path test (validation, auth, or error flow)

## Coding Style

1. Match existing files in this repository for naming, structure, and layout
1. Follow vertical-slice pattern (endpoints, handlers, validators grouped logically)
1. Use `System.Text.Json` (not Newtonsoft)
1. Nullable reference types enabled
1. Always use braces { } even for single-line statements
1. Use descriptive identifiers (no 1–2 letter names)
1. Keep consistency with existing logging, validation, and test patterns
1. Invert `if` statements to flatten the nested structure.
1. Use LINQ instead of nested for loops whenever possible.
1. Solution must build/compile successfully and all test report success. If there's an issue with either, fix these before calling it done. If it still fails after 3 attempts, stop and seek help from me.

## Do-Not-Touch Areas

1. Do not add new projects
1. Do not modify frontend
1. Do not alter deployment pipeline files
1. Do not introduce external dependencies without clear justification
1. Do not commit into `main` or make PRs against `main`