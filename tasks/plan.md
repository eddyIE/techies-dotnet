# Implementation Plan: Techies (.NET)

Spec: [SPEC.md](../SPEC.md) · Modules: [CAPABILITY-MAP.md](../CAPABILITY-MAP.md) · Tasks: [todo.md](todo.md)

## Overview

Build the ASP.NET Core MVC monolith described in SPEC.md module by module, in the approved
build order (foundation → identity → catalog → promotions, loyalty → orders → shop, customers,
staff → dashboard). Each task is a vertical slice — entity + migration + service + tests + screen
— that leaves the app running, and gets its own commit.

## Architecture Decisions

- **One project, folders per concern** (`Domain/`, `Data/`, `Services/`, `Areas/Admin/`,
  `Controllers/`) — no Clean-Architecture layers; a university-sized app doesn't earn them.
- **Migrations grow per module.** Each module task adds its entities and one EF migration, so
  every commit has a schema that matches its code. FKs to tables that don't exist yet (e.g.
  `inventory_adjustments.order_id → orders`) are added in the migration that creates the target.
- **Seeding per module, idempotent.** `DbSeeder` runs module seed steps in order; each step
  skips if its table already has rows. Ported data lives in `Data/Seed/*.json` (converted once
  from `../techies` Flyway SQL with a throwaway script; the JSON is the committed source).
  Generated data (customers, orders, redemptions, points) uses a fixed `Random` seed and a fixed
  reference date so it is identical on every machine.
- **Business rules as services with real-Postgres tests.** Transactions, conditional stock
  updates and unique constraints can't be faked by InMemory/SQLite, so service tests use
  Testcontainers. Pure calculations get plain unit tests.
- **Sequential execution by the main agent.** Migrations and the shared DbContext make most
  tasks order-dependent; sub-agents would mostly conflict. A code-review sub-agent runs at the
  final checkpoint.

## Phases

| Phase | Tasks | Module(s) |
|---|---|---|
| 1. Foundation | 1–4 | foundation |
| 2. Identity | 5–7 | identity |
| 3. Catalog | 8–12 | catalog |
| 4. Promotions & loyalty | 13–16 | promotions, loyalty |
| 5. Orders | 17–21 | orders (+ reviews data for the shop) |
| 6. Shop | 22–26 | shop |
| 7. Customers & staff | 27–29 | customers, staff |
| 8. Dashboard & wrap-up | 30–32 | dashboard |

Checkpoints after each phase (see todo.md): build green, all tests green, app starts from an
empty DB and the phase's screens work by hand.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Testcontainers needs Docker running; Docker Desktop is currently stopped | High | Task 2 verifies it first; if Docker can't start, stop and ask the user |
| Porting ~130 products / ~230 specs / ~180 reviews from SQL by hand is error-prone | Med | Convert with a script, then assert counts in a seed test |
| Ported image URLs (cellphones, tgdd, picsum) may break or hotlink-block | Low | Keep them as-is (spec); fall back to a local placeholder image via `onerror` |
| Shop checkout race on stock | Med | Conditional `UPDATE … WHERE available >= qty` + CHECK constraint, covered by a concurrency test |
| Two cookie schemes mis-wired so one grants the other's access | High | Policies bind each scheme explicitly; identity tasks include a cross-access check |
| Scope creep from the BRD's Android-era features (vouchers, card payment) | Med | SPEC "out of scope" list; anything else → ask first |

## Open Questions

None blocking.
