# Techies (.NET)

Electronics e-commerce web app for a university project, built with **ASP.NET Core MVC (.NET 10)**,
**EF Core** and **PostgreSQL 16**. One monolith, two kinds of user:

- **Staff** run the business in the **Admin CMS** at `/admin` (the 7 modules of [brd.md](brd.md)).
- **Customers** use the **shop** at `/`: search (accent-insensitive, by product or category name),
  cart, cash-on-delivery checkout with coupon codes, order tracking and cancelling, reviews of
  completed orders, and a points page (`/diem-thuong`) to redeem gifts for a counter pickup code.

The requirements live in [SPEC.md](SPEC.md); the module map is [CAPABILITY-MAP.md](CAPABILITY-MAP.md)
and the build plan is in [tasks/](tasks/plan.md). The Spring Boot project in `../techies` was used
only as a reference and as the source of the seed data.

## Run it

Requirements: .NET 10 SDK and Docker.

```bash
docker compose up -d db                      # PostgreSQL 16 on localhost:5433
dotnet run --project src/Techies.Web         # http://localhost:5080
```

On startup the app applies migrations and seeds demo data (only into an empty database).
To start over: `docker compose down -v`, then run again.

### Demo logins

| Who | Email | Password | Lands on |
|---|---|---|---|
| Super Admin (system role) | `admin@techies.vn` | `Admin@123` | `/admin` dashboard, every menu incl. Nhân sự and Vai trò & Phân quyền |
| Catalog & inventory staff | `kho@techies.vn` | `Staff@123` | Products, categories, inventory |
| Order & counter staff | `quay@techies.vn` | `Staff@123` | Orders, customers, reviews, gift counter |
| Customers | `khach01@example.com` … `khach40@example.com` | `Customer@123` | Shop |

Customers can also register at `/tai-khoan/dang-ky`. Staff accounts are created by the Super Admin
(there is no staff sign-up), who can also define new roles and their permissions under
*Vai trò & Phân quyền*. Try gift code **`GIFT-G53Y-ZDS3`** at *Quầy nhận quà*.

## Develop

```bash
dotnet build Techies.slnx
dotnet test Techies.slnx                     # needs Docker: service tests use a throwaway Postgres
dotnet ef migrations add <Name> --project src/Techies.Web --output-dir Data/Migrations
dotnet format Techies.slnx --verify-no-changes
```

## Layout

```
src/Techies.Web/
  Areas/Admin/      Admin CMS controllers + views (one controller per screen group)
  Controllers/      Shop controllers (Home, Products, Cart, Checkout, Account, MyOrders)
  Domain/           Entities and enums
  Data/             DbContext, Fluent configuration, migrations, seed (Data/Seed/*.json ported from ../techies)
  Services/         All business rules — order complete/cancel, checkout, stock ledger, loyalty, coupons, …
  Infrastructure/   Auth schemes, permissions and policies, admin menu, grid, formatting, uploads, labels
tests/Techies.Tests/
  Unit/             Pure rules (order math, coupons, tiers, revenue periods, cart, formatting)
  Services/         Service tests against a real Postgres (Testcontainers)
```

## Design decisions

- **Monolith, not microservices.** The BRD's key flows cross modules (cancelling an order touches
  orders, stock, coupons and payment; completing one awards loyalty points). In one app each is a
  single database transaction instead of a saga.
- **Two cookie schemes** (`Staff`, `Customer`), each policy bound to one scheme, so a customer login
  never opens `/admin`. Sessions are re-validated per request: locking an account or changing a
  staff role takes effect immediately.
- **Roles and permissions** (`roles`, `permissions`, `role_permissions`). The permission catalogue
  lives in code (`Infrastructure/Permissions.cs`) and is seeded by migration; every admin action
  carries `[RequirePermission(...)]`, and permissions are read once per request so role edits apply
  immediately. The built-in Super Admin role has everything and alone manages staff and roles.
- **One shared admin grid** (`Infrastructure/Grid`, `_Grid.cshtml`, `wwwroot/js/admin-grid.js`) for every
  listing, Magento-style: sortable columns, a *Bộ lọc* panel, filter chips, page size, mass actions
  and a per-row *Chọn* menu. The server renders the grid; the script fetches just the grid partial
  and keeps the state in the URL (back button and bookmarks work, and it degrades to full pages).
- **`StockLedger` is the only writer of stock.** It locks the row, refuses negative stock and writes
  an `inventory_adjustments` audit row for every import, stock count, order and cancellation.
  Concurrent checkouts for the last unit sell it once (covered by a test).
- **No hard deletes** of products, staff, customers, orders or reviews; products are switched off,
  people are locked.
- UI text is Vietnamese; code, identifiers and the database are English.
