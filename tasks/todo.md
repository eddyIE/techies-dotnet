# Tasks: Techies (.NET)

Each task: RED → GREEN → `dotnet test Techies.slnx` → `dotnet build Techies.slnx` → commit.
Standard verification for every task unless noted: **tests green, build 0 errors, app starts**.
`S` = 1–2 files, `M` = 3–5 files. Paths are relative to `src/Techies.Web/` unless shown otherwise.

## Phase 1 — Foundation

- [x] **Task 1: Repository and solution skeleton** (M) · deps: none
  - Acceptance: own git repo in `techies-dotnet/` with `.gitignore` (bin, obj, `wwwroot/uploads`,
    user secrets); `Techies.slnx` with `src/Techies.Web` (MVC, net10.0) and `tests/Techies.Tests`
    (xUnit); `docker-compose.yml` with Postgres 16 on 5433; app listens on 5080; existing
    `brd.md`, `db-design.docx`, SPEC, map and tasks committed as the baseline.
  - Verify: `dotnet build` passes; `dotnet test` runs (one placeholder test); `/` responds.
  - Files: `.gitignore`, `Techies.slnx`, `docker-compose.yml`, `Properties/launchSettings.json`, `Program.cs`

- [x] **Task 2: Database plumbing and test fixture** (M) · deps: 1
  - Acceptance: `AppDbContext` with Npgsql + snake_case naming; connection string in
    `appsettings.Development.json`; migrations applied and `DbSeeder` invoked on startup;
    `PostgresFixture` (Testcontainers) applies migrations for service tests.
  - Verify: fixture test opens a connection and sees the `__EFMigrationsHistory` table; app
    starts against `docker compose up -d db`.
  - Files: `Data/AppDbContext.cs`, `Data/Seed/DbSeeder.cs`, `Program.cs`, `tests/Techies.Tests/PostgresFixture.cs`

- [x] **Task 3: Layouts, culture and formatting** (M) · deps: 1
  - Acceptance: shop `_Layout` and admin `_AdminLayout` (Bootstrap 5, Bootstrap Icons, sidebar
    placeholder, flash messages from `TempData`); vi-VN culture; `Format.Money` → `1.234.000đ`,
    `Format.LocalTime` → `26/10/2026 14:05` in Asia/Ho_Chi_Minh; `DomainException`.
  - Verify: unit tests for both formatters; `/` and `/admin` render their layouts.
  - Files: `Views/Shared/_Layout.cshtml`, `Areas/Admin/Views/Shared/_AdminLayout.cshtml`, `Infrastructure/Format.cs`, `Domain/DomainException.cs`, `tests/.../Unit/FormatTests.cs`

- [ ] **Task 4: Slug and image upload helpers** (S) · deps: 1
  - Acceptance: `Slug.From("Điện thoại Samsung")` → `dien-thoai-samsung`; `ImageStorage`
    accepts jpg/png/webp ≤ 2 MB into `wwwroot/uploads/<yyyy>/<guid>.<ext>`, rejects others
    with a Vietnamese message.
  - Verify: unit tests for slug cases and upload accept/reject.
  - Files: `Infrastructure/Slug.cs`, `Infrastructure/ImageStorage.cs`, tests

### Checkpoint: Foundation
- [ ] Build + tests green; fresh `docker compose down -v && up` then `dotnet run` works.

## Phase 2 — Identity

- [ ] **Task 5: Identity schema and staff/tier seed** (M) · deps: 2
  - Acceptance: entities + migration for `users` (with status), `addresses`, `staff` (role,
    status), `membership_tiers`, `customer_loyalty`; seeder adds tiers 0–3 and the 3 demo staff
    with BCrypt hashes.
  - Verify: service test — seeding twice leaves exactly 3 staff and 4 tiers.
  - Files: `Domain/*.cs`, `Data/AppDbContext.cs`, `Data/Migrations/*`, `Data/Seed/DbSeeder.cs`, tests

- [ ] **Task 6: Staff login, roles and admin menus** (M) · deps: 3, 5
  - Acceptance: `Staff` cookie scheme; `/admin/login` + logout; policies `SuperAdmin`,
    `Catalog`, `Orders`; sidebar filtered by role; access-denied page; locked staff refused at
    login and signed out on next request (`AccountStatusService`); non-admin roles land on
    their first allowed menu.
  - Verify: service tests for login check (wrong password, locked, ok) and lock re-validation;
    manual: each seeded role sees only its menus.
  - Files: `Program.cs`, `Infrastructure/AuthSetup.cs`, `Services/AccountStatusService.cs`, `Areas/Admin/Controllers/AuthController.cs` + views, tests

- [ ] **Task 7: Customer registration and login** (M) · deps: 5, 6
  - Acceptance: `Customer` cookie scheme; register (validation per spec, unique email
    case-insensitive, creates `customer_loyalty` tier 0), login, logout in the shop header;
    customer cookie can't open `/admin` and staff cookie isn't a shop login; seeder adds ~40
    demo customers with addresses.
  - Verify: service tests — duplicate email (different case) rejected; locked customer refused.
  - Files: `Services/CustomerAccountService.cs`, `Controllers/AccountController.cs` + views, `ViewModels/Account/*`, `Data/Seed/*`, tests

### Checkpoint: Identity
- [ ] Build + tests green; all 3 staff roles and a new customer log in correctly; no cross-access.

## Phase 3 — Catalog

- [ ] **Task 8: Catalog schema and categories admin** (M) · deps: 4, 6
  - Acceptance: entities + migration for categories, products, product_images, product_specs,
    stock_items (CHECK available ≥ 0), inventory_adjustments; admin categories list/create/edit
    with icon upload and display order (policy `Catalog`).
  - Verify: service test — category slug generated and unique; manual create/edit.
  - Files: `Domain/*`, `Data/*`, `Services/CategoryService.cs`, `Areas/Admin/Controllers/CategoriesController.cs` + views, tests

- [ ] **Task 9: Port catalog and stock seed** (M) · deps: 8
  - Acceptance: `Data/Seed/categories.json`, `products.json` (with images + specs),
    `stock.json` converted from `../techies` catalog/inventory migrations; seeder loads them;
    stock includes zero and ≤ 5 items.
  - Verify: seed test asserts counts match the source SQL and every product has a stock row.
  - Files: `Data/Seed/*.json`, `Data/Seed/CatalogSeed.cs`, tests

- [ ] **Task 10: Product list, search and filters** (S) · deps: 9
  - Acceptance: admin product list — name search, category filter, out-of-stock filter,
    active filter, 20/page; shows price, stock, active badge.
  - Verify: service test for the combined query filters; manual.
  - Files: `Services/ProductService.cs`, `Areas/Admin/Controllers/ProductsController.cs` + `Index.cshtml`, tests

- [ ] **Task 11: Product create/edit with images and specs** (M) · deps: 10
  - Acceptance: form with name, category, price > 0, description, thumbnail upload, gallery
    add/remove/reorder, spec rows add/delete (JS), active toggle; new product gets a stock row
    with 0; no delete.
  - Verify: service test — save replaces specs/images as submitted; manual create + edit.
  - Files: `Services/ProductService.cs`, `ViewModels/Catalog/ProductForm.cs`, `ProductsController.cs`, `Edit.cshtml`, `wwwroot/js/product-form.js`

- [ ] **Task 12: Inventory import, stock count and history** (M) · deps: 10
  - Acceptance: inventory list; Nhập hàng (+qty, `IMPORT`) and Kiểm kê (set qty, `COUNT`)
    each write an adjustment with before/after, reason, staff; per-product history.
  - Verify: service tests for both actions incl. invalid input (qty ≤ 0, count < 0).
  - Files: `Services/InventoryService.cs`, `Areas/Admin/Controllers/InventoryController.cs` + views, tests

### Checkpoint: Catalog
- [ ] Build + tests green; catalog staff can manage everything in BRD §2 against seeded data.

## Phase 4 — Promotions & Loyalty

- [ ] **Task 13: Coupons** (M) · deps: 6
  - Acceptance: entity + migration; `CouponService.StatusAt` and `Evaluate`; admin list with
    status badges, create/edit with validation, pause/activate (policy `SuperAdmin`); seed 5
    coupons from `V8__coupons.sql`.
  - Verify: unit tests for status and evaluate (paused, not started, expired, below minimum,
    discount capped at subtotal); manual.
  - Files: `Domain/Coupon.cs`, `Data/*`, `Services/CouponService.cs`, `Areas/Admin/Controllers/CouponsController.cs` + views, tests

- [ ] **Task 14: Membership tier policy** (S) · deps: 5
  - Acceptance: tier screen (SuperAdmin) edits name/threshold/% for tiers 1–3 with validation;
    save recomputes every customer's tier; `Tiers.For(points)` helper.
  - Verify: unit test for `Tiers.For`; service test — raising a threshold downgrades a customer.
  - Files: `Services/LoyaltyService.cs`, `Areas/Admin/Controllers/TiersController.cs` + view, tests

- [ ] **Task 15: Gifts catalogue** (S) · deps: 4, 14
  - Acceptance: gift entity + migration; list ("Hết quà" when stock 0), create/edit with image
    upload, active toggle (SuperAdmin); seed gifts from `loyalty-service/V1__init.sql`.
  - Verify: service test for validation (points cost > 0, min tier 0–3); manual.
  - Files: `Domain/Gift.cs`, `Data/*`, `Services/GiftService.cs`, `Areas/Admin/Controllers/GiftsController.cs` + views

- [ ] **Task 16: Gift counter claim** (S) · deps: 15, 7
  - Acceptance: `gift_redemptions` + migration; counter screen (policy `Orders`) looks up a code
    case-insensitively, shows details, confirms hand-over once; unknown code message.
  - Verify: service tests — claim sets status/time/staff; second claim rejected; unknown code.
  - Files: `Domain/GiftRedemption.cs`, `Data/*`, `Services/GiftService.cs`, `Areas/Admin/Controllers/GiftCounterController.cs` + view, tests

### Checkpoint: Promotions & loyalty
- [ ] Build + tests green; coupon, tier, gift and counter screens work.

## Phase 5 — Orders

- [ ] **Task 17: Order schema and pure order rules** (M) · deps: 12, 13, 16
  - Acceptance: `orders` (ref sequence from 100001), `order_items`, `coupon_redemptions`,
    `point_transactions` + migration (incl. deferred FKs); `OrderMath`: shipping fee, total,
    amount to collect, points for an order.
  - Verify: unit tests for each `OrderMath` rule; service test — two orders get consecutive refs.
  - Files: `Domain/Order*.cs`, `Domain/PointTransaction.cs`, `Data/*`, `Services/OrderMath.cs`, tests

- [ ] **Task 18: Complete and cancel services** (M) · deps: 17
  - Acceptance: `LoyaltyService.AwardForOrderAsync` (idempotent, tier upgrade);
    `OrderService.CompleteAsync` and `CancelAsync` exactly as in SPEC orders module, each one
    transaction; wrong-status calls rejected.
  - Verify: service tests — complete (status, completed_at, COD → PAID, points, tier up, no
    double award); cancel (stock restored with `CANCEL` rows, coupon `RESTORED`, PAID →
    REFUNDED, COD stays PENDING); both reject non-CONFIRMED.
  - Files: `Services/OrderService.cs`, `Services/LoyaltyService.cs`, tests

- [ ] **Task 19: Admin order list and detail** (M) · deps: 18
  - Acceptance: status tabs with counts + "Tất cả" via `?status=`; search by ref or phone;
    paging; detail page with all fields and amount to collect; complete/cancel buttons only
    when CONFIRMED (policy `Orders`).
  - Verify: service test for list query (status + search); manual complete and cancel.
  - Files: `Services/OrderQueries.cs`, `Areas/Admin/Controllers/OrdersController.cs`, `Index.cshtml`, `Details.cshtml`, tests

- [ ] **Task 20: Delivery note print view** (S) · deps: 19
  - Acceptance: print page with ref, recipient, address, phone, items, total, amount to collect,
    Print button; `@media print` hides chrome.
  - Verify: manual print preview.
  - Files: `OrdersController.cs`, `Areas/Admin/Views/Orders/DeliveryNote.cshtml`, `wwwroot/css/print.css`

- [ ] **Task 21: Generated order, redemption and review seed** (M) · deps: 18, 9
  - Acceptance: `reviews` entity + migration (rating CHECK); deterministic seed of ~150 orders
    over 15 months covering every status/payment/failure case, coupon redemptions, points +
    tiers consistent with COMPLETED orders, gift redemptions (ISSUED and CLAIMED), and ~180
    reviews ported from `order-service` V7/V12 attached to COMPLETED orders.
  - Verify: seed test — every status present; each customer's lifetime points equal the sum of
    their EARN transactions; every review's order is COMPLETED and contains the product.
  - Files: `Domain/Review.cs`, `Data/*`, `Data/Seed/OrderSeed.cs`, `Data/Seed/reviews.json`, tests

### Checkpoint: Orders
- [ ] Build + tests green; order staff can run the BRD §3 flow on seeded orders; points update.

## Phase 6 — Shop

- [ ] **Task 22: Shop home, listing and product detail** (M) · deps: 21
  - Acceptance: home (categories + latest), listing by category + search + paging, detail with
    gallery, specs, stock state, reviews + average; inactive products 404 / hidden.
  - Verify: service test — shop query excludes inactive products; manual.
  - Files: `Services/ShopQueries.cs`, `Controllers/HomeController.cs`, `Controllers/ProductsController.cs` + views, tests

- [ ] **Task 23: Session cart** (S) · deps: 22
  - Acceptance: add/update (1–99, ≤ available)/remove; totals with shipping from `OrderMath`;
    prices re-read from DB; header cart count.
  - Verify: unit test for cart line rules; manual.
  - Files: `Services/CartService.cs`, `Controllers/CartController.cs` + view, tests

- [ ] **Task 24: Checkout service** (M) · deps: 23
  - Acceptance: `CheckoutService.PlaceOrderAsync` — one transaction: re-check active + stock,
    conditional stock decrement with `ORDER` adjustments, CONFIRMED/PENDING COD order with
    snapshots; failure reports the offending product and creates nothing.
  - Verify: service tests — success path; insufficient stock creates nothing; concurrent
    checkouts for the last unit → exactly one succeeds, stock never < 0.
  - Files: `Services/CheckoutService.cs`, tests

- [ ] **Task 25: Checkout page and addresses** (S) · deps: 24, 7
  - Acceptance: login-required checkout; pick saved address or add new (optionally default);
    COD only; success page with the order ref; cart cleared.
  - Verify: manual end-to-end as a seeded customer.
  - Files: `Controllers/CheckoutController.cs` + views, `ViewModels/Shop/CheckoutForm.cs`

- [ ] **Task 26: My orders and customer cancel** (S) · deps: 25, 18
  - Acceptance: own orders list + detail; another customer's order → 404; cancel button while
    CONFIRMED uses `OrderService.CancelAsync` after an ownership check.
  - Verify: service tests — ownership check; cancel by owner restores stock.
  - Files: `Controllers/MyOrdersController.cs` + views, `Services/OrderService.cs`, tests

### Checkpoint: Shop
- [ ] Build + tests green; Success Criteria 3 and 4 pass by hand.

## Phase 7 — Customers & Staff

- [ ] **Task 27: Customer directory, profile and lock** (M) · deps: 21
  - Acceptance: search by email/name/phone; read-only profile (contact, addresses, loyalty,
    point history, orders → admin detail); lock/unlock (policy `Orders`).
  - Verify: service test — search matches each field; lock then login refused; manual.
  - Files: `Services/CustomerQueries.cs`, `Areas/Admin/Controllers/CustomersController.cs` + views, tests

- [ ] **Task 28: Reviews monitoring** (S) · deps: 21
  - Acceptance: list with rating, product and "1–2 sao" filters, newest first; no edit/delete.
  - Verify: service test for the low-rating filter; manual.
  - Files: `Services/ReviewQueries.cs`, `Areas/Admin/Controllers/ReviewsController.cs` + view, tests

- [ ] **Task 29: Staff management** (M) · deps: 6
  - Acceptance: list, add (role dropdown, ACTIVE), edit name/phone/role, reset password,
    lock/unlock (SuperAdmin); guards: no self lock/demote, last active Super Admin protected.
  - Verify: service tests for every guard and duplicate email; manual lock → staff signed out.
  - Files: `Services/StaffService.cs`, `Areas/Admin/Controllers/StaffController.cs` + views, tests

### Checkpoint: Customers & staff
- [ ] Build + tests green; BRD §6 and §7 work by hand.

## Phase 8 — Dashboard & Wrap-up

- [ ] **Task 30: Revenue periods and chart** (M) · deps: 21
  - Acceptance: `RevenuePeriods` (7 days/month/quarter/year ranges, buckets, previous span, %
    change with "—" for zero); revenue query by `completed_at`; Chart.js line chart with filter.
  - Verify: unit tests for each period's range/buckets/% change at a fixed date; service test
    for the sum.
  - Files: `Services/RevenuePeriods.cs`, `Services/DashboardQueries.cs`, `Areas/Admin/Controllers/DashboardController.cs`, `Index.cshtml`, tests

- [ ] **Task 31: Pending cards, recent orders and best-sellers** (S) · deps: 30
  - Acceptance: AWAITING_PAYMENT and CONFIRMED cards linking to filtered order list; 10 recent
    orders; top 10 best-sellers per period with revenue, stock and "Sắp hết" (≤ 5).
  - Verify: service test for best-seller ranking; manual.
  - Files: `Services/DashboardQueries.cs`, `Index.cshtml`, tests

- [ ] **Task 32: README and final verification** (S) · deps: all
  - Acceptance: `README.md` with setup, commands, logins, module overview; fresh-DB run-through
    of every Success Criterion; code-review sub-agent pass, fixes applied.
  - Verify: Success Criteria 1–6 in SPEC.md checked off.
  - Files: `README.md`

### Checkpoint: Complete
- [ ] All SPEC acceptance criteria met; all **[test]** criteria covered; ready for review.
