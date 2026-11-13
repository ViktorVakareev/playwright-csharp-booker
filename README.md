# Playwright C# reference project: Restful-Booker

A small but complete test project to refer to in interviews: **unit, component, API and UI tests**, each in its own folder, built with **.NET 10, NUnit, Playwright for .NET and RestSharp**.

| Layer | What's tested | Target | Tools | Speed |
|---|---|---|---|---|
| **Unit** | Booking rules (`BookingValidator`) and the test-data builder | Pure C# code | NUnit | ms |
| **Component** | Our RestSharp `BookerApiClient` in isolation | A **fake HTTP handler**, no network | NUnit, RestSharp | ms |
| **API** | The Restful-Booker REST API: auth, CRUD, errors | **Local Restful-Booker** `http://localhost:3001` | Playwright `APIRequestContext`, plus the RestSharp client | 10s of ms per call |
| **UI** | Contact form journey, through a page object | **automationintesting.online** (Restful Booker Platform) | Playwright, NUnit | seconds |

```
playwright-csharp-booker/
├─ Booker.slnx                      solution (.NET 10 format)
├─ booker.runsettings               browser, channel, workers, URLs
├─ src/Booker.Core/                 "production-like" code the lower layers test
│  ├─ Models/Booking.cs             JSON model (lowercase API field names)
│  ├─ BookingValidator.cs           business rules → unit tests
│  └─ BookerApiClient.cs            RestSharp client → component + integration tests
└─ tests/Booker.Tests/
   ├─ Unit/                         BookingValidatorTests, BookingBuilderTests
   ├─ Component/                    BookerApiClientTests + StubHttpHandler
   ├─ Api/                          HealthAndAuthTests, BookingApiTests, BookerApiClientIntegrationTests,
   │                                BookingNegativeTests, BookingFlowTests, KnownDefectTests (extended version)
   ├─ Ui/Pages/HomePage.cs          page object
   ├─ Ui/ContactFormTests.cs        UI test
   ├─ Support/                      Settings, ApiTestBase, UiTestBase (tracing on failure), parallel config
   └─ TestData/BookingBuilder.cs    builder pattern for test data
```

---

## Versions: base first, then extended

The project is kept in two versions, so you can show **a working base first**, and only then the extended coverage built on top of it.

| Version | Where | What it contains |
|---|---|---|
| **Base** | branch `main`, frozen as tag `v1.0-base` | The reference suite described in this README: **30 tests** (14 unit, 5 component, 10 API, 1 UI), all green |
| **Extended** | branch `extended-coverage` | The base plus business flows, negative API cases and known-defect tests: **68 tests** (14 unit, 10 component, 43 API, 1 UI) plus 6 known-defect tests run on demand. See [What the extended version adds](#what-the-extended-version-adds) |

The tag `v1.0-base` never moves, so the base can always be shown exactly as it was, even after `main` changes later.

### Demo order (interview walkthrough)

**1. Show the base, run it and validate it**

```powershell
cd C:\LAB\playwright-csharp-booker
git switch main                    # or: git switch --detach v1.0-base   (the frozen snapshot)
dotnet build
dotnet test                        # expected: Passed 30, Failed 0
dotnet test --filter "TestCategory=Unit"   # then one layer at a time: Unit | Component | Api | Ui
```

What to say: "This is the base: one project, four layers of the pyramid, all green. Here is how it is structured, and how I run and debug it."

**2. Only then, show the extension**

```powershell
git switch extended-coverage
dotnet build
dotnet test
git diff main --stat               # exactly what was added on top of the base
```

What to say: "Once the base was stable, I extended it in a branch: more business flows, negative cases and known defects. The base never depended on the extension."

### Working on the versions

- New work goes only into `extended-coverage` (or a feature branch from it). `main` stays the base.
- Before a demo, run `dotnet test` on **both** branches.
- When the extension is finished and reviewed, it can be merged into `main`. The tag `v1.0-base` still points at the original base.

---

## What the extended version adds

**From 30 to 68 tests**, all green, plus 6 known-defect tests that run only on demand.

| File | Category | Tests | What it covers |
|---|---|---|---|
| `Api/BookingNegativeTests.cs` | `Api`, `Negative` | 20 | **Auth:** invalid or empty token, PATCH/DELETE without a token, wrong credentials table. **Ids:** unknown or non-numeric id → 404; PUT/DELETE on an unknown id → 405. **Bodies:** partial PUT → 400, malformed JSON → 400, empty body not accepted. **Headers:** `Accept` table (JSON/XML → 200, others → 418). Every "rejected" test also checks the booking was **not changed**. |
| `Api/BookingFlowTests.cs` | `Api`, `Flow` | 12 | PATCH changes only the fields sent; update and delete with **cookie or Basic auth**; one token for several changes; **journey:** book 3 nights → extend to 5 → cancel → 404; search by first + last name, unknown name, two guests with the same name, date range, and the date-filter boundary quirk. |
| `Api/KnownDefectTests.cs` | `Api`, `KnownDefect` | 6 (explicit) | What the API **should** do but doesn't: check-out before check-in, negative price, invalid date, non-numeric price (all accepted with 200), empty body → 500, unknown id → 405 instead of 404. |
| `Api/BookerApiClientIntegrationTests.cs` | `Api` | +1 | Partial update through our RestSharp client against the real service. |
| `Component/BookerApiClientTests.cs` | `Component` | +5 | New `PatchBookingAsync` sends only the changed fields with the cookie; delete treats 201 as success; 403 on delete → exception with status; 500 on GET throws (not `null`); **no response at all** (network down) → clear exception. |

Supporting changes:
- **`BookerApiClient.PatchBookingAsync`**: partial update (PATCH) in the client.
- **`ApiTestBase` cleans up after itself**: every booking a test creates is tracked and deleted in `[TearDown]` (with Basic auth), so a run leaves no data on the server. Verified: same booking count before and after two full runs.
- New helpers in `ApiTestBase`: `PostRawBookingAsync` (send any body, still cleaned up), `SearchAsync`, `BasicAuth`.
- `Component/StubHttpHandler.cs` also has `UnreachableHttpHandler`, which simulates a network that is down.

### Known defects: tests that are allowed to fail

```powershell
dotnet test                                          # normal run: 68 passed, known defects not run
dotnet test --filter "TestCategory=KnownDefect"      # expected today: 6 failed
```

The `KnownDefectTests` fixture is `[Explicit]`, so a normal run (and CI) stays green, but the defects are written down as executable tests, not just in a bug tracker. When one is fixed, its test turns green: remove `[Explicit]` and it becomes a normal regression test.

What to say: "Our own `BookingValidator` already rejects a negative price and check-out before check-in; the unit tests prove it. The API accepts both. So the rule exists, but the service doesn't apply it. I wrote that down as tests rather than ignoring it."

### More filters

```powershell
dotnet test --filter "TestCategory=Negative"         # 20 negative API tests
dotnet test --filter "TestCategory=Flow"             # 12 business-flow tests
```

---

## Run it

```powershell
cd C:\LAB\playwright-csharp-booker
dotnet build
dotnet test                                        # everything
dotnet test --filter "TestCategory=Unit"           # one layer: Unit | Component | Api | Ui
dotnet test --filter "TestCategory!=Ui"            # all but the browser tests
$env:BOOKER_URL="https://restful-booker.herokuapp.com"; dotnet test --filter "TestCategory=Api"   # public instance instead of local
$env:HEADED="1"; dotnet test --filter "TestCategory=Ui"                                           # watch the browser
```

**Prerequisites:**
- Your local Restful-Booker is running on port 3001; check with `curl.exe http://localhost:3001/ping`, which should return `Created`.
- Google Chrome is installed: the runsettings use `channel=chrome`, because Windows Smart App Control blocks Playwright's own Chromium.
- Internet access for the UI test.

Expected on `main` (base): **30 tests**: 14 unit, 5 component, 10 API, 1 UI. On `extended-coverage`: **68 tests**: 14 unit, 10 component, 43 API, 1 UI. (`[TestCase]` rows count separately.)

---

## Build it from scratch, step by step (what to say you did)

**1. Create the solution and projects**

```powershell
mkdir C:\LAB\playwright-csharp-booker; cd C:\LAB\playwright-csharp-booker
dotnet new sln -n Booker                       # .NET 10 creates Booker.slnx
dotnet new classlib -n Booker.Core -o src/Booker.Core
dotnet new nunit -n Booker.Tests -o tests/Booker.Tests
dotnet sln add src/Booker.Core tests/Booker.Tests
dotnet add tests/Booker.Tests reference src/Booker.Core
```

**2. Add the packages**

```powershell
dotnet add src/Booker.Core package RestSharp
dotnet add tests/Booker.Tests package Microsoft.Playwright.NUnit
dotnet build
pwsh tests/Booker.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
#   No PowerShell 7? Use: powershell -ExecutionPolicy Bypass -File tests\Booker.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
```

**3. Configuration:** `booker.runsettings` holds the browser, `channel=chrome`, headless mode, workers and URLs. The test project points at it with `<RunSettingsFilePath>`, so `dotnet test` and Test Explorer use the same settings. `Settings.cs` lets environment variables override them (`BOOKER_URL`, `UI_URL`, `HEADED`), which is how CI points the same tests at a different environment.

**4. Bottom of the pyramid first:**
- **Unit:** `BookingValidator` rules with `[TestCase]` boundaries (price 0 and -1; 1 night and 0 nights; a stay across a year boundary).
- **Component:** `BookerApiClient` with `StubHttpHandler` injected through RestSharp's `ConfigureMessageHandler`. These check the request *we* build (method, path, `Accept` header, lowercase JSON, auth cookie) and *our* error handling (404 → null, "Bad credentials" → exception, 500 → status in the exception).

**5. API tests:** `ApiTestBase : PlaywrightTest` creates an `APIRequestContext` per test with the base URL and JSON headers. Each test **creates its own booking first**, so it knows the expected data and can run in parallel. The tests cover:
- ping 201;
- valid and invalid auth;
- create → read back;
- search by name;
- update with and without a token (200 / 403);
- delete → 201 then 404;
- invalid body is not 2xx;
- one full lifecycle through our RestSharp client.

**6. UI test:** `UiTestBase : PageTest` gives each test a fresh context with the base URL and keeps a trace on failure. `HomePage` (page object) holds the locators: `data-testid` hooks for the form fields, and a role locator for the button and the confirmation. The test reads like the user's journey, and its web-first assertions wait automatically.

**7. Run, then debug failures from the trace:** `playwright.ps1 show-trace <path from the test output>`.

---

## Restful-Booker quirks the tests document (good interview stories)

| Behaviour | Why it's interesting |
|---|---|
| `GET /ping` returns **201**, `DELETE` returns **201** | Assert the API's actual contract, not textbook codes, and raise it as a finding |
| Bad credentials → **200** + `{"reason":"Bad credentials"}` | Checking only the status code would pass a failed login: always check the body |
| Missing `Accept: application/json` → **418 I'm a teapot** | Headers are part of the contract; the client sets them in one place |
| Invalid body → **500**, not 400 | A 500 on bad input is a bug; the test asserts "not 2xx" and notes it |
| PUT/DELETE need `Cookie: token=...` (or Basic auth) | Auth tests: with a token 200, without 403 |
| The data resets periodically | Tests create their own data and never rely on fixed ids |
| PUT/DELETE on an unknown id → **405**, not 404 | Documented in `BookingNegativeTests` (actual) and `KnownDefectTests` (expected) |
| PUT with some fields missing → **400**; PATCH changes only what you send | The difference between replacing and partially updating a resource |
| Accepts check-out before check-in, a negative price, `"not-a-date"` (stored as `0NaN-aN-aN`) and `"abc"` as price (stored as `null`) | No server-side validation: the known-defect tests |
| Search: `checkin` filter is **exclusive**, `checkout` filter is **inclusive** | A booking starting on the given day is *not* found, one ending on it *is*. Boundary testing found it |

---

## Interview talking points

- **The test pyramid:** "Most checks are unit and component tests, fast and stable. The API tests cover the contract and the business flows. **One** UI test proves the critical journey. Here: 14 unit, 5 component, 10 API, 1 UI."
- **Component vs API test:** "A component test isolates *our* code by faking its dependency, here the network through an `HttpMessageHandler` stub, so it's fast and deterministic. An API test hits the real service and checks *its* behaviour."
- **Why Playwright *and* RestSharp?** "Playwright's `APIRequestContext` keeps UI and API in one tool and one report. RestSharp is a typical framework-level client, which I use at work inside Bellatrix. The project shows both, and the same client is tested with a stub and against the real service."
- **Test data:** "Builder pattern with valid defaults; each test changes only what it's about; unique names; no shared records, so parallel runs are safe."
- **Stability:** "No sleeps: Playwright auto-waits and web-first assertions retry. A fresh context per test; traces on failure; config from runsettings or environment variables, never hard-coded."
- **Design patterns used:**
  - Page Object;
  - Builder (test data);
  - Template Method (the `ApiTestBase` and `UiTestBase` base classes);
  - dependency injection of the HTTP handler (testability).
- **What I'd add next:** auth once with storage state, contract or schema checks against OpenAPI, a GitHub Actions pipeline with test results and traces as artifacts, and tags for smoke vs regression.
