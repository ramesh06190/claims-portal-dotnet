# Claims Portal (.NET code sample)

[![Build and test](https://github.com/ramesh06190/claims-portal-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/ramesh06190/claims-portal-dotnet/actions/workflows/ci.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512bd4)
![.NET Framework 4.8 Web Forms](https://img.shields.io/badge/.NET%20Framework%204.8-Web%20Forms-0b5cad)
![WCAG 2.1 AA](https://img.shields.io/badge/WCAG-2.1%20AA-1e6b34)

A small weekly benefit claims application I wrote to show how I build accessible, secure
enterprise apps on .NET. It pairs with my front-end portfolio at
https://ramesh06190.github.io/portfolio/ and uses the same design system.

All data is made up.

## What is in it

| Project | What it shows |
|---|---|
| `ClaimsPortal.Core` | Domain model, business rules with claimant friendly messages, paging and sorting, `ClaimService` |
| `ClaimsPortal.Infrastructure` | EF Core on SQL Server with migrations and seed data, repository, transactional outbox, background dispatcher with retries |
| `ClaimsPortal.Web` | ASP.NET Core MVC with Razor, REST API with ProblemDetails and OpenAPI, custom middleware, accessibility tag helpers |
| `ClaimsPortal.WebForms` | ASP.NET Web Forms on .NET Framework 4.8, built to WCAG 2.1 AA |
| `ClaimsPortal.Tests` | 50 xUnit tests: unit tests with Moq and FluentAssertions, plus API and page tests through `WebApplicationFactory` |

```
Browser ──► MVC controllers ─┐
                             ├──► ClaimService ──► IClaimRepository ──► EF Core ──► SQL Server
API clients ─► API controller┘          (Core)         (Infrastructure)           │
                                                                                  ▼
                                               OutboxDispatcher ◄── OutboxMessages table
                                                     │
                                                     ▼
                                     IMessagePublisher (Azure Service Bus in production)
```

## Accessibility

The MVC form works without JavaScript and follows the same patterns as the portfolio's components:

- Errors are listed in a summary at the top that takes focus and links to each field.
- The page title starts with "Error:" when there are problems, so screen reader users hear it on load.
- A custom tag helper (`AccessibleFieldTagHelper`) adds `aria-invalid` and links the error with `aria-describedby`. Out of the box ASP.NET only adds a CSS class, which assistive technology never announces.
- Error messages start with "Error:" so they do not rely on color, and the input keeps what the person typed.
- Dates use the three field "memorable date" pattern, with messages that name the missing part.
- The claims list sorts on the server, marks the sorted column with `aria-sort`, and uses links with `aria-current="page"` for pagination.

### Web Forms specifics

The Web Forms page (`Default.aspx`) fixes the accessibility problems Web Forms controls have by default:

| Default behavior | Fix used here |
|---|---|
| `GridView` renders header cells as `<td>` in the body | `UseAccessibleHeader="true"` plus `HeaderRow.TableSection = TableHeader` for `<thead>` and `<th scope="col">` |
| `RadioButtonList` renders a layout table | `RepeatLayout="UnorderedList"` inside a `fieldset` with a `legend` |
| Validators show a red asterisk with no text | `Display="None"`, then text errors and an error summary built from `Page.Validators` |
| `ValidationSummary` cannot link to fields | Custom summary in a `Repeater` with links to each field |
| Generated ids like `ctl00_MainContent_ClaimantId` | `clientIDMode="Static"` so `<label for>` and `aria-describedby` match |
| `asp:Label` renders a `span` | `AssociatedControlID` so it renders a real `<label for>` |
| Focus is lost after a postback | Focus moves to the error summary or the results heading |

Both apps were checked with axe and HTML_CodeSniffer (0 errors, including the error and results states)
and by keyboard.

## Security

- Content Security Policy that only allows this site's scripts and styles, so the views have no inline script or style
- `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` and `Permissions-Policy` headers
- Antiforgery tokens on MVC form posts, and `ViewStateUserKey` on the Web Forms page for the same protection
- Correlation IDs on every request, only reused from the caller when they are safe to log
- API errors return ProblemDetails with a correlation ID and never an exception or stack trace
- The list API leaves out claimant IDs and contact details
- NuGet audit fails the build on vulnerable packages. A transitive `System.Security.Cryptography.Pkcs` from SqlClient is pinned to a patched version for that reason.
- Web Forms removes `X-Powered-By` and `X-AspNet-Version`, uses HttpOnly SameSite cookies and encrypted view state

## API

| Method | Route | Notes |
|---|---|---|
| GET | `/api/claims?page=1&pageSize=10&sort=Claimant&direction=Ascending` | Paged list. Sort by `Submitted`, `ClaimNumber`, `Claimant`, `Program`, `WeekEnding`, `Status` or `Amount`. |
| GET | `/api/claims/{claimNumber}` | One claim, or a 404 ProblemDetails |
| POST | `/api/claims` | Files a claim. Returns 201 with a Location header, or 400 ValidationProblemDetails with an error per field. |

The OpenAPI document is at `/openapi/v1.json`.

## Run it

Needs the .NET 10 SDK and SQL Server LocalDB (installed with Visual Studio).

```bash
dotnet run --project src/ClaimsPortal.Web
# http://localhost:5272 - the database is created and seeded on first run in Development
```

Tests use in-memory SQLite, so they need no database:

```bash
dotnet test ClaimsPortal.slnx
```

The Web Forms project needs Visual Studio's MSBuild (it is not part of `ClaimsPortal.slnx` for that reason).
Open `src/ClaimsPortal.WebForms/ClaimsPortal.WebForms.csproj` in Visual Studio and run it with IIS Express.

## CI

GitHub Actions builds with warnings as errors and the recommended .NET analyzers, fails on vulnerable
packages, checks that the EF Core model has no unapplied changes, runs the tests, and builds the
Web Forms project on Windows.
