# Identity Service

A standalone .NET 8 identity/authentication microservice, built with Clean
Architecture and Domain-Driven Design so it can be dropped into a larger
distributed system as the single source of truth for authentication.

## Layers

```
src/
  IdentityService.Domain          -> Entities, value objects, aggregates, domain events. No dependencies.
  IdentityService.Application     -> Use cases (CQRS via MediatR), interfaces Infrastructure implements.
  IdentityService.Infrastructure  -> EF Core (SQL Server), JWT, password hashing, repository implementations.
  IdentityService.Api             -> ASP.NET Core Web API: controllers, middleware, composition root.
```

Dependencies point inward only: Api -> Infrastructure -> Application -> Domain.
Domain has zero package references beyond MediatR.Contracts (for the
`IDomainEvent : INotification` marker) and never references EF Core, ASP.NET,
or any other framework.

## Key design decisions

- **User aggregate owns its invariants.** Failed-login lockout, refresh-token
  rotation/revocation, and "changing your password kills every other session"
  are all enforced on `User` itself, not in a command handler — so they can't
  be bypassed by a future use case that forgets to check.
- **Result<T>, not exceptions, for business outcomes.** Invalid credentials,
  duplicate email, locked account, etc. are `Result.Failure` values mapped to
  HTTP status codes in `ResultExtensions`. Exceptions are reserved for genuine
  bugs/infra failures and caught by `ExceptionHandlingMiddleware`.
- **Refresh-token rotation.** Every `/api/auth/refresh` call revokes the
  presented token and issues a new one (`ReplacedByTokenId` links them), which
  both limits a leaked token's lifetime and gives you a hook to detect reuse
  of an already-revoked token as a compromise signal.
- **Domain events dispatched post-commit.** `ApplicationDbContext.SaveChangesAsync`
  collects events from tracked aggregates, saves, and only *then* publishes via
  MediatR — so other parts of the system (or an outbox you bolt on later) never
  react to state that didn't actually persist.
- **JWT access token (short-lived, 15 min default) + opaque refresh token**
  (hashed at rest with SHA-256, 30-day default) is the standard pattern for
  letting other services validate the access token locally against the shared
  signing key, without calling back into this service on every request.

## Extending for a distributed system

This is deliberately a solid core, not everything a large system eventually
wants. Natural next additions, each isolated to one layer:

- **Outbox pattern** — add an `OutboxMessage` table + a background processor
  in Infrastructure that publishes domain events to a message broker (RabbitMQ/
  Kafka/Azure Service Bus) instead of (or in addition to) in-process MediatR,
  so other services get `UserRegistered`, `PasswordChanged`, etc. reliably.
- **Redis** for refresh-token/session lookups instead of SQL Server, if login
  volume needs it — swap the `IUserRepository` refresh-token query.
- **RS256 signing** (asymmetric) instead of the current HS256 shared secret,
  so downstream services can validate tokens with only a public key/JWKS
  endpoint rather than holding the signing secret.
- **Rate limiting** on `/api/auth/login` and `/api/auth/refresh` (ASP.NET
  Core's built-in `AddRateLimiter`) on top of the existing lockout.
- **Integration tests** via `WebApplicationFactory<Program>` + Testcontainers
  for SQL Server — `Program` is already exposed as `partial` for this.

## Running locally

Requires the .NET 8 SDK and a SQL Server instance (LocalDB, a full SQL Server
install, or the `mcr.microsoft.com/mssql/server` Docker image all work).

```bash
# 1. Point ConnectionStrings:Default in appsettings.Development.json (or user-secrets) at your DB
# 2. Create the initial migration (once EF Core tools are installed)
cd src/IdentityService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../IdentityService.Api

# 3. Run (migrations apply automatically in Development, see Program.cs)
cd ../IdentityService.Api
dotnet run
```

Quickest way to get a local SQL Server for development:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_password123" \
  -p 1433:1433 --name sql-server -d mcr.microsoft.com/mssql/server:2022-latest
```

Swagger UI is available at `http://localhost:5000/swagger` in Development.
`GET http://localhost:5000/health` reports database connectivity for
container orchestrators. The gRPC endpoint listens on port `5001`
(HTTP/2, no TLS by default — see `Program.cs` for the Kestrel setup).

### Or via Docker

```bash
docker build -t identity-service -f src/IdentityService.Api/Dockerfile .
docker run -p 8080:8080 --env-file .env identity-service
```

## API surface

### REST (port 5000)

| Method | Route                       | Auth   | Purpose                                   |
|--------|------------------------------|--------|--------------------------------------------|
| POST   | `/api/auth/register`         | Anon   | Create a new user                          |
| POST   | `/api/auth/login`             | Anon   | Exchange credentials for token pair        |
| POST   | `/api/auth/refresh`           | Anon   | Rotate a refresh token for a new pair      |
| POST   | `/api/auth/revoke`            | Anon   | Revoke a refresh token (logout)            |
| POST   | `/api/auth/change-password`   | Bearer | Change password, revokes other sessions    |
| GET    | `/api/users/{id}`             | Bearer | Fetch a user by id                         |
| GET    | `/api/users/me`               | Bearer | Fetch the caller's own profile             |

### gRPC (port 5001, internal only)

`Protos/identity.proto` defines `IdentityGrpc` with `GetUser` and `GetUsers`
(batch). This is a **service-to-service** contract, not for end-user auth —
see `OrdersService/README.md` for why. Every call must carry an
`x-internal-api-key` metadata header matching `Grpc:InternalApiKey`
(`InternalApiKeyInterceptor` rejects anything else with `Unauthenticated`).
Not exposed publicly in a real deployment — firewall port 5001 to the
internal network / service mesh only.

## Run both services together

See `docker-compose.yml` one level up (alongside this repo and
`OrdersService/`) to run Identity Service, Orders Service, and SQL Server
together — `docker compose up --build`.

## Note on this environment

This was generated without network access to NuGet, so the package
restore/build couldn't be verified here — double check package versions
against what's current when you first `dotnet restore`. The code itself
targets stable, well-known APIs (EF Core 8, MediatR 12, FluentValidation 11)
that haven't had breaking changes in this area.
