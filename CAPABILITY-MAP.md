# Capability Map: Techies (.NET)

Approved 2026-10-26. Module ids are stable — specs, plans and commits refer to them.
Each module's requirements live in its section of [SPEC.md](SPEC.md) (`## Module: <id>`).

| Module id | Responsibility | Depends on |
|---|---|---|
| `foundation` | Solution + MVC project, Admin area + shop root, docker-compose Postgres 16, EF Core DbContext + migrations, seed runner, Bootstrap layouts, vi-VN formatting, test project | — |
| `identity` | Customers (`users`, `addresses`) and staff (`staff`, 3 fixed roles), two cookie schemes, BCrypt, login/logout, customer registration, seeded Super Admin, locked accounts rejected | foundation |
| `catalog` | Categories, products, images, specs, active toggle, search/filter, stock (`stock_items`) + `inventory_adjustments`, seed ported from Spring | identity |
| `promotions` | Fixed-amount coupons: CRUD, pause/resume, schedule, validity rule, `coupon_redemptions` | identity |
| `loyalty` | Tiers, `customer_loyalty`, `point_transactions`, gifts, `gift_redemptions` + counter claim, point-award service | identity |
| `orders` | `orders`/`order_items`, 5 statuses, admin list/search/detail, COD amount, delivery note, complete (→ points) and cancel (→ restock, coupon restore, refund) | catalog, promotions, loyalty |
| `shop` | Browse/search, product detail, session cart, COD checkout, my orders, cancel own CONFIRMED order | catalog, orders |
| `customers` | Admin customer directory + read-only profile, lock/unlock, reviews list + low-rating filter, reviews seed | orders, loyalty |
| `staff` | Super Admin staff management: create with role, lock/unlock, change role, reset password | identity, rbac |
| `rbac` | Roles + permission catalogue, role management screen, permission-based authorization (replaces fixed roles) | identity |
| `admin-grid` | Shared Magento-style grid: AJAX sort/filter/paging, filter panel, mass actions, row action dropdown | rbac |
| `shop-extras` | Coupon at checkout, reviews from completed orders, points page + gift redemption; accent/category-aware search | shop, promotions, loyalty, customers |
| `dashboard` | Pending counts + quick links, revenue chart + period filter + % change, recent orders, best-sellers + low stock | orders, catalog |

Build order: foundation → identity → catalog → promotions, loyalty → orders → shop, customers, staff → dashboard

Change 2026-10-26 (admin feedback): rbac → admin-grid, then each listing moves onto the grid.

Dependencies point one way: `orders` calls `loyalty` and `promotions`; neither calls back.

Change 2026-10-26 #2 (shop gaps): search → shop-extras (coupons, reviews, points & gifts).
