# Spec: Techies (.NET)

Status: draft for review · 2026-10-26 · Module index: [CAPABILITY-MAP.md](CAPABILITY-MAP.md)

## Objective

A university project: an electronics e-commerce web app ("Techies" — phones, laptops, TVs…)
with **two kinds of end user**:

- **Staff** use the **Admin CMS** (`/admin`) to run the business — the 7 modules of `brd.md`.
- **Customers** use a **minimal shop** (`/`) to browse, order (COD only) and track orders.

It is a standalone re-implementation inspired by the Spring Boot microservice project in
`../techies` (used only as a reference and as a seed-data source). It is deliberately a
**single ASP.NET Core MVC monolith with one database** — simple to run, explain and grade.

Source documents: `brd.md` (admin requirements, authoritative for behaviour) and
`db-design.docx` (data model, adapted for a monolith as described in [Data Model](#data-model)).
Where they conflict with this spec, this spec wins.

Success = a grader runs two commands, logs in as each kind of user, and can exercise every
BRD feature against realistic seeded data.

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 (LTS), C# 14, `net10.0` |
| Web | ASP.NET Core MVC + Razor views, Areas (`Admin`), server-rendered |
| Data | PostgreSQL 16 (docker-compose), EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions` (snake_case) |
| Auth | ASP.NET Core cookie authentication, two schemes; `BCrypt.Net-Next` for hashing (no ASP.NET Identity) |
| UI | Bootstrap 5 (bundled in `wwwroot/lib` by the MVC template), Bootstrap Icons, Chart.js 4 (jsDelivr, pinned version) |
| Tests | xUnit, `Testcontainers.PostgreSql` for service tests against a real Postgres |

Approved NuGet packages are exactly those above plus `Microsoft.EntityFrameworkCore.Design`
and the xUnit/test SDK packages. Anything else → ask first.

## Commands

```bash
docker compose up -d db                                   # Postgres 16 on localhost:5433
dotnet build Techies.slnx                                 # build everything
dotnet test Techies.slnx                                  # all tests (needs Docker running)
dotnet run --project src/Techies.Web                      # http://localhost:5080 — migrates + seeds on start
dotnet ef migrations add <Name> --project src/Techies.Web --output-dir Data/Migrations
dotnet format Techies.slnx --verify-no-changes            # style check
docker compose down -v                                    # wipe the database (re-seeds on next run)
```

Host port **5433** (not 5432) so it can run alongside `../techies`' Postgres.

Seeded logins (Development):

| Who | Email | Password |
|---|---|---|
| Super Admin | `admin@techies.vn` | `Admin@123` |
| Catalog staff | `kho@techies.vn` | `Staff@123` |
| Order/counter staff | `quay@techies.vn` | `Staff@123` |
| Demo customers | `khach01@example.com` … | `Customer@123` |

## Project Structure

```
Techies.slnx
docker-compose.yml
src/Techies.Web/
  Program.cs                     → composition root: DB, auth schemes, policies, seed on start
  Areas/Admin/Controllers/       → one controller per admin screen group (ProductsController, OrdersController…)
  Areas/Admin/Views/             → admin Razor views + _AdminLayout (sidebar filtered by role)
  Controllers/                   → shop controllers (Home, Products, Cart, Checkout, Account, MyOrders)
  Views/                         → shop Razor views + _Layout
  Domain/                        → EF entities + enums + DomainException (no EF attributes; config in Data/)
  Data/AppDbContext.cs           → DbContext + Fluent configuration
  Data/Migrations/               → EF migrations
  Data/Seed/                     → DbSeeder + JSON files ported from ../techies
  Services/                      → business logic per module (OrderService, LoyaltyService, CouponService…)
  ViewModels/                    → form/list models, grouped by module folder
  Infrastructure/                → auth helpers, money/date formatting, file upload, slug helper
  wwwroot/uploads/               → uploaded images (git-ignored)
tests/Techies.Tests/
  Unit/                          → pure rules (points, COD amount, shipping, coupon status, tier)
  Services/                      → service tests on a Testcontainers Postgres
```

Controllers stay thin: validate the view model, call one service method, redirect or render.
All business rules from the BRD live in `Services/` so they can be tested without HTTP.

## Code Style

```csharp
namespace Techies.Web.Services;

public sealed class OrderService(AppDbContext db, LoyaltyService loyalty, TimeProvider clock)
{
    // BRD §3: CONFIRMED -> COMPLETED, then award 1 point per 1.000đ of (subtotal - discount).
    public async Task CompleteAsync(Guid orderId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new DomainException("Không tìm thấy đơn hàng.");
        if (order.Status != OrderStatus.Confirmed)
            throw new DomainException("Chỉ hoàn tất được đơn đang ở trạng thái Đã xác nhận.");

        order.Status = OrderStatus.Completed;
        order.CompletedAt = clock.GetUtcNow();
        if (order.PaymentMethod == PaymentMethod.Cod)
            order.PaymentStatus = PaymentStatus.Paid;

        await loyalty.AwardForOrderAsync(order, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
```

- File-scoped namespaces, primary constructors for DI, `sealed` by default, nullable enabled,
  `async` all the way with `CancellationToken`.
- English identifiers, code and comments; **Vietnamese** for all user-facing text (views,
  validation messages, `DomainException` messages).
- Rule violations throw `DomainException`; controllers catch it, put the message in
  `TempData["Error"]` and redirect back. No exceptions for normal control flow elsewhere.
- Enums stored as strings matching the BRD values (`AWAITING_PAYMENT`, `CONFIRMED`, …).
- Money: `decimal`, `numeric(19,2)`, displayed as `1.234.000đ`. Times: stored UTC
  (`timestamptz`), displayed in `Asia/Ho_Chi_Minh` as `dd/MM/yyyy HH:mm`.
- Inject `TimeProvider` wherever "now" matters (coupons, revenue periods) so tests can fix time.
- Comments explain *why* (the BRD rule), not *what*.

## Testing Strategy

- **Scope (agreed): core business rules only.** No controller, UI or browser tests.
- `tests/Techies.Tests/Unit` — pure functions: points for an order, COD amount, shipping fee,
  coupon status at a given time, tier for a points total, revenue period ranges + % change.
- `tests/Techies.Tests/Services` — service methods against a real Postgres via
  Testcontainers (one container per test class fixture, schema from migrations): complete
  order, cancel order (admin and customer), checkout stock reservation, stock import/count,
  gift claim, tier threshold update, lock checks.
- Each rule in a module's acceptance criteria tagged **[test]** must have at least one test.
- `dotnet test` must be green before every commit. Manual run-through of each module's screens
  is the verification for UI criteria.

## Boundaries

- **Always:** keep business rules in `Services/` with tests; wrap multi-table state changes in
  one transaction; use `[Authorize]` policies on every admin controller; keep antiforgery
  validation on all POSTs (global `AutoValidateAntiforgeryToken`); generate a migration for
  every model change; run `dotnet build` + `dotnet test` before each commit; commit per task.
- **Ask first:** adding a NuGet package or JS library not listed in Tech Stack; changing an
  approved data-model rule; anything that changes scope (new features, dropping a BRD item);
  touching anything outside `techies-dotnet/` (the `../techies` repo is read-only).
- **Never:** commit secrets beyond the documented dev seed passwords; store plaintext
  passwords; hard-delete products, staff, customers, orders or reviews; let customers edit
  data the BRD makes read-only; skip or delete a failing test to get green; write to the
  `../techies` database or repo.

## Data Model

One PostgreSQL schema (`public`), snake_case, **real foreign keys everywhere**. Adapted from
`db-design.docx`; the order tables come from `../techies/order-service` migrations.

| Table | Key columns / notes |
|---|---|
| `users` | id, email (unique, lower-cased), password_hash, full_name, phone, **status** (`ACTIVE`/`LOCKED`, added — BRD §6), created_at, updated_at |
| `addresses` | id, user_id → users, recipient_name, phone, line1, ward, district, province, is_default |
| `staff` | id, full_name, email (unique), phone, password_hash, **role** (`SUPER_ADMIN`/`CATALOG_STAFF`/`ORDER_STAFF`), status (`ACTIVE`/`LOCKED`), created_at, updated_at |
| `categories` | id, name, slug (unique), image_url, display_order |
| `products` | id, category_id → categories, name, slug (unique), description, price, thumbnail_url, active, created_at |
| `product_images` | id, product_id → products, url, display_order |
| `product_specs` | id, product_id → products, spec_name, spec_value, display_order |
| `stock_items` | product_id (PK, → products), available (≥ 0, CHECK), updated_at |
| `inventory_adjustments` | id, product_id → products, adjustment_type (`IMPORT`/`COUNT`/`ORDER`/`CANCEL`), quantity_delta, quantity_before, quantity_after, reason, order_id → orders (nullable), staff_id → staff (nullable), created_at |
| `coupons` | id, code (unique, upper-case), description, discount_amount, min_subtotal, active, starts_at, expires_at, created_by → staff, created_at |
| `coupon_redemptions` | id, coupon_id → coupons, order_id → orders, customer_id → users, discount_amount, status (`APPLIED`/`RESTORED`), redeemed_at, restored_at |
| `membership_tiers` | tier (0–3, PK), name, threshold_points, discount_percent |
| `customer_loyalty` | customer_id (PK, → users), lifetime_points, balance, tier → membership_tiers, updated_at |
| `point_transactions` | id, customer_id → users, transaction_type (`EARN`/`REDEEM`), points_delta, order_id → orders, gift_redemption_id → gift_redemptions, note, created_at |
| `gifts` | id, name, description, image_url, points_cost, min_tier → membership_tiers, stock_available, active, created_at, updated_at |
| `gift_redemptions` | id, code (unique, `GIFT-XXXX-XXXX`), gift_id → gifts, customer_id → users, points_spent, status (`ISSUED`/`CLAIMED`), redeemed_at, claimed_at, claimed_by → staff |
| `orders` | id, order_ref (unique, `ORD-` + sequence from 100001), user_id → users, status, failure_code, subtotal, shipping_fee, coupon_code, discount, total, ship_recipient/phone/line1/ward/district/province (snapshot), payment_method (`COD`/`MOCK_CARD`), payment_status (`PENDING`/`PAID`/`DECLINED`/`REFUNDED`), created_at, updated_at, **completed_at** (added — revenue is dated by it) |
| `order_items` | id, order_id → orders, product_id → products, product_name + unit_price + thumbnail_url (snapshots), quantity (≥ 1), line_total |
| `reviews` | id, customer_id → users, product_id → products, order_id → orders, rating (1–5, CHECK), comment, created_at. No update/delete paths in code. |

Dropped from `db-design.docx` (microservice-only or unused): `user_avatars`, `roles`,
`permissions`, `role_permissions` (3 fixed roles instead), `stock_movements`,
`stock_movement_items`, `stock_items.version`, `carts`/`cart_items` (session cart),
`saga_steps`, the SQL functions (replaced by tested C# services), the views and `unaccent`.

`total = subtotal + shipping_fee − discount`. Shipping fee: 30.000đ, waived when
subtotal ≥ 500.000đ (same as `../techies` `ShippingPolicy`).

---

## Module: foundation

Objective: an empty but runnable skeleton every other module plugs into.

Acceptance:
- Own git repository at `techies-dotnet/` (it currently sits, ignored, inside Homebrew's repo).
- `docker compose up -d db` + `dotnet run --project src/Techies.Web` starts the app on
  `http://localhost:5080`, applies migrations and runs the seeder on startup; re-running is
  idempotent (seeder skips when data exists).
- `/` renders the shop layout; `/admin` renders the admin layout (sidebar, top bar, flash
  messages from `TempData`).
- Display uses vi-VN via explicit helpers (`Format.Money` → `1.234.000đ`, `Format.LocalTime` →
  `26/10/2026 14:05`); the request culture stays invariant so numeric form fields bind reliably. **[test]**
- `dotnet test` runs a Testcontainers Postgres fixture with migrations applied.
- Image upload helper: accepts jpg/png/webp ≤ 2 MB, stores under `wwwroot/uploads/<yyyy>/<guid>.<ext>`,
  returns the URL; anything else is rejected with a Vietnamese message.

## Module: identity

Objective: two separate kinds of login with no crossover.

Acceptance:
- Two cookie schemes: `Customer` (shop, login at `/tai-khoan/dang-nhap`) and `Staff`
  (admin, login at `/admin/login`). A customer cookie never grants admin access and vice versa.
- Customer registration: full name, email (unique, case-insensitive), phone (10 digits,
  starts with 0), password ≥ 8 chars, confirm password. Creates `users` + `customer_loyalty`
  at tier 0. Staff have **no** registration page.
- Passwords hashed with BCrypt; plaintext never stored or logged.
- Login rejects a `LOCKED` account with "Tài khoản đã bị khóa". A session belonging to an
  account locked later is rejected on its next request (cookie `OnValidatePrincipal` checks
  status) — the user is signed out. **[test]** (lock check service)
- Authorization policies: `SuperAdmin`, `Catalog` (SUPER_ADMIN or CATALOG_STAFF),
  `Orders` (SUPER_ADMIN or ORDER_STAFF). Admin sidebar shows only the menus the role can use;
  direct URL access to a forbidden screen returns an "access denied" page.
- Seeder creates the Super Admin and two demo staff (see Commands).

Role → menu access:

| Menu | SUPER_ADMIN | CATALOG_STAFF | ORDER_STAFF |
|---|:-:|:-:|:-:|
| Dashboard (with revenue) | ✓ | | |
| Products, categories, inventory | ✓ | ✓ | |
| Orders, delivery notes | ✓ | | ✓ |
| Gift counter (claim lookup) | ✓ | | ✓ |
| Customers, reviews | ✓ | | ✓ |
| Coupons | ✓ | | |
| Gifts, membership tiers | ✓ | | |
| Staff | ✓ | | |

Non-Super-Admin staff land on their first allowed menu after login.

## Module: catalog

Objective: BRD §2 — staff edit what customers see and keep stock accurate.

Acceptance:
- Categories: list, create, edit (name, icon image upload, display order). Slug generated from
  the name with Vietnamese diacritics removed (`Điện thoại` → `dien-thoai`). **[test]** (slug)
- Products list: search by name (case-insensitive), filter by category, filter "Hết hàng"
  (available = 0), filter active/inactive, paginated (20/page).
- Product create/edit: name, category, price (> 0), description, thumbnail upload, multiple
  gallery image uploads (add, remove, reorder by display order), and a spec table with
  "Thêm dòng thông số" / delete row (name + value pairs, client-side add/remove, saved together).
- Active toggle hides an inactive product from the shop (not listed, detail page 404, cannot be
  added to cart or checked out); past orders keep displaying it from their snapshots.
- No product hard-delete.
- Inventory screen: list of products with current `available`, searchable. Two actions, each
  recording an `inventory_adjustments` row with before/after, reason and the acting staff:
  - **Nhập hàng**: add a positive quantity (`IMPORT`). **[test]**
  - **Kiểm kê**: set the counted quantity ≥ 0; delta is computed (`COUNT`). **[test]**
- Per-product adjustment history (newest first).
- Seed: categories, products, images, specs and stock ported from
  `../techies/{catalog,inventory}-service` migrations into `Data/Seed/*.json`, keeping their
  image URLs; stock includes in-stock, low (≤ 5) and zero items.

## Module: promotions

Objective: BRD §4 — fixed-amount coupons.

Acceptance:
- Coupon list with computed status badge: `Đang chạy`, `Tạm dừng` (active = false),
  `Chưa bắt đầu` (now < starts_at), `Hết hạn` (now ≥ expires_at). **[test]**
- Create/edit: code (A–Z, 0–9, upper-cased, unique), description, discount amount (> 0 VND),
  minimum subtotal (≥ 0), optional start and expiry (expiry after start).
- Pause / re-activate button toggles `active`.
- `CouponService.Evaluate(coupon, subtotal, now)` returns applicable/not and the discount
  (`min(discount_amount, subtotal)`), with reason `COUPON_NOT_APPLICABLE` when paused, not
  started, expired or below minimum. **[test]** (Used by seed and order totals; the minimal
  shop does not take coupon codes.)
- Seed: the 5 coupons from `../techies/order-service/V8__coupons.sql`.

## Module: loyalty

Objective: BRD §5 — tiers, points and counter gift claims.

Acceptance:
- Tier policy screen: edit name, threshold points and discount % for tiers 1–3 (tier 0 is fixed
  at 0 points). Thresholds must strictly increase; % between 0 and 100. Saving recomputes every
  customer's tier from `lifetime_points`. **[test]**
- `LoyaltyService.AwardForOrderAsync(order)`: points = `floor((subtotal − discount) / 1000)`;
  adds to `lifetime_points` and `balance`, writes an `EARN` point transaction linked to the
  order, and upgrades the tier when a threshold is crossed. Never awards twice for one order.
  **[test]**
- Gifts: list, create/edit (name, description, image upload, points cost > 0, minimum tier
  0–3, stock available ≥ 0), active toggle. List shows "Hết quà" when stock is 0.
- Gift counter: staff enter a code (e.g. `GIFT-G53Y-ZDS3`, case-insensitive). Shows code, gift
  name, points spent, customer name + phone, redeemed time, status and — if claimed — claimed
  time and staff. For an `ISSUED` redemption a **Xác nhận giao quà** button sets `CLAIMED`,
  `claimed_at`, `claimed_by`; claiming twice is rejected. Unknown code → "Không tìm thấy mã".
  **[test]**
- Seed: tiers (0 Thành viên/0, 1 Bạc/10.000, 2 Vàng/30.000, 3 Kim cương/60.000 points with
  0/10/30/50 %) and gifts from `../techies/loyalty-service/V1__init.sql`.

## Module: orders

Objective: BRD §3 — staff run each order to completion or cancellation.

Acceptance:
- Order list with 5 status tabs (AWAITING_PAYMENT, CONFIRMED, COMPLETED, CANCELLED, FAILED)
  showing counts, plus "Tất cả"; the tab is a query string (`?status=CONFIRMED`) so other pages
  can deep-link. Search by order ref (`ORD-100117`) or recipient phone. Paginated, newest first.
- Detail page: shipping snapshot, items, subtotal, shipping fee, coupon code + discount, total,
  payment method + status, failure code (FAILED), timestamps, and **Số tiền cần thu hộ**:
  `total` when COD and payment PENDING, `0đ` when MOCK_CARD and PAID. **[test]**
- **Hoàn tất đơn hàng** (CONFIRMED only): status COMPLETED, `completed_at` set, COD payment
  becomes PAID, loyalty points awarded — one transaction. **[test]**
- **Hủy đơn** (CONFIRMED only): status CANCELLED; each item's quantity returned to stock with
  a `CANCEL` adjustment; an `APPLIED` coupon redemption becomes `RESTORED`; payment PAID becomes
  REFUNDED (COD PENDING stays PENDING) — one transaction. **[test]**
- Complete/cancel on any other status is rejected with a Vietnamese message. **[test]**
- **In phiếu giao hàng**: a print-friendly page (order ref, recipient, address, phone, items,
  total, amount to collect) with a Print button; no admin chrome when printed.
- Seed (generated, deterministic): ~270 orders over the past 15 months from demo customers (one
  COMPLETED order per ported review plus ~100 others),
  covering all 5 statuses, COD and MOCK_CARD, every payment status, failure codes
  (`PAYMENT_FAILED`, `OUT_OF_STOCK`, `SERVICE_UNAVAILABLE`), some with coupons (+ redemptions),
  with consistent loyalty points for COMPLETED orders.

## Module: shop

Objective: the bare-minimum customer side (scope "A").

Acceptance:
- Home: categories + latest products. Product listing: by category, search by name, paginated;
  shows price and "Hết hàng" when available = 0. Inactive products never appear.
- Product detail: gallery, price, description, spec table, stock state, reviews (read-only)
  with average rating; "Thêm vào giỏ" with quantity (disabled when out of stock).
- Cart in session: add, change quantity (1–99, ≤ available), remove; shows subtotal, shipping
  fee and total. Prices always re-read from the catalog (not trusted from the session).
- Checkout (login required): choose a saved address or enter a new one (saved, optionally
  default); payment fixed to COD. Placing the order, in one transaction: re-checks each product
  is active and has enough stock (otherwise nothing is created and the cart page shows which
  item failed), decrements stock with `ORDER` adjustments, creates the order as CONFIRMED /
  payment PENDING with snapshots, clears the cart. **[test]**
- Concurrent checkouts can never take `available` below 0 (conditional update /
  CHECK constraint). **[test]**
- My orders: the customer's own orders only, newest first, with a detail page. Another
  customer's order id returns 404. **[test]**
- Cancel own order: allowed only while CONFIRMED; uses the same cancel rule as admin. **[test]**
- Register / login / logout from the header. No coupons, card payment, reviews, gift
  redemption or profile editing in the shop.

## Module: customers

Objective: BRD §6 — read-only customer support and review monitoring.

Acceptance:
- Customer list: search by email, name or phone; shows tier, balance, status; paginated.
- Profile page (read-only): contact info, addresses, loyalty (lifetime points, balance, tier),
  point transactions, order history linking to the admin order detail. No create/edit forms.
- **Khóa / Mở khóa** toggles `users.status`; a locked customer cannot log in and is signed out
  on the next request (identity rule).
- Reviews list: product, customer, rating stars, comment, date; filter by rating, by product,
  and a "1–2 sao" quick filter; newest first. No edit, delete or reply.
- Seed: the 182 reviews from `../techies/order-service` V7/V12, attached to seeded COMPLETED
  orders of the same product (ratings 2–5; the source has no 1★ reviews).

## Module: staff

Objective: BRD §7 — Super Admin manages the team.

Acceptance:
- Staff list (Super Admin only): name, email, phone, role, status.
- Add staff: full name, work email (unique), phone, initial password (≥ 8), role dropdown —
  created ACTIVE.
- Edit: change name, phone and role. Reset password: Super Admin sets a new password.
- Lock / unlock: a locked staff member cannot log in and is signed out on the next request;
  their history (adjustments, claims, coupons created) is kept.
- A Super Admin cannot lock or demote themselves, and the last active Super Admin cannot be
  locked or demoted. **[test]**
- No hard delete.

## Module: dashboard

Objective: BRD §1 — the Super Admin's overview on opening `/admin`.

Acceptance:
- Pending orders: two cards — AWAITING_PAYMENT count and CONFIRMED count — each linking to the
  order list filtered to that status.
- Revenue: sum of `total` of COMPLETED orders by `completed_at`, with filters 7 ngày qua (daily
  points), Tháng này (daily), Quý này (weekly), Năm nay (monthly); Chart.js line chart, total
  for the period and % change vs. the same elapsed span of the previous period (shows "—" when
  the previous span is 0). Time zone Asia/Ho_Chi_Minh. **[test]** (period ranges + % change)
- Recent orders: the 10 newest orders (ref, recipient, time, total, payment method, status
  badge); ref links to the order detail. Refreshed on page load (no websockets).
- Best-sellers: top 10 products by quantity sold in COMPLETED orders for tuần / tháng / quý /
  năm, with revenue and current available stock; red "Sắp hết" badge when available ≤ 5.
- Visible to Super Admin only.

---

## Success Criteria

1. From a fresh clone with Docker running: `docker compose up -d db` then
   `dotnet run --project src/Techies.Web` gives a working, fully seeded app at
   `http://localhost:5080`.
2. Each seeded staff role sees exactly its menus (table in identity) and is refused elsewhere.
3. A customer can register, add to cart, check out COD, see the order in "Đơn hàng của tôi",
   and cancel it; stock goes down then back up accordingly.
4. A staff member can complete that same flow's order and the customer's points/tier update.
5. Every acceptance criterion above is demonstrable in the UI; every **[test]** criterion has
   a passing test; `dotnet test` and `dotnet build` (0 errors) are green.
6. No `../techies` file or database was modified.

## Open Questions

None blocking. Nice-to-haves explicitly **out of scope** unless asked: diacritic-insensitive
search (`unaccent`), coupon entry and card payment in the shop, customer reviews and gift
redemption from the shop, tier voucher usage, email notifications, deployment.
