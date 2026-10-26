# Techies (.NET)

Ứng dụng web thương mại điện tử đồ công nghệ cho đồ án môn học, xây dựng bằng **ASP.NET Core MVC (.NET 10)**,
**EF Core** và **PostgreSQL 16**. Một ứng dụng duy nhất (monolith), hai nhóm người dùng:

- **Nhân viên** vận hành cửa hàng trên **trang quản trị (Admin CMS)** tại `/admin` — 7 phân hệ trong [brd.md](brd.md).
- **Khách hàng** dùng **cửa hàng** tại `/`: tìm kiếm (không phân biệt dấu, theo tên sản phẩm hoặc tên danh mục),
  giỏ hàng, đặt hàng thanh toán khi nhận hàng (COD) kèm mã giảm giá, theo dõi và hủy đơn, đánh giá sản phẩm
  của đơn đã hoàn tất, và trang điểm thưởng (`/diem-thuong`) để đổi quà lấy mã nhận tại quầy.

Yêu cầu chi tiết nằm trong [SPEC.md](SPEC.md); sơ đồ phân hệ ở [CAPABILITY-MAP.md](CAPABILITY-MAP.md); kế hoạch
triển khai ở [tasks/](tasks/plan.md). Dự án Spring Boot ở `../techies` chỉ được dùng làm tham khảo và làm nguồn
dữ liệu mẫu.

**Tài liệu tham chiếu để cập nhật BRD và thiết kế CSDL** (viết cho người và cho AI đọc):

- [docs/tham-chieu-brd.md](docs/tham-chieu-brd.md) — hệ thống đang làm gì, so với từng mục của `brd.md`.
- [docs/tham-chieu-csdl.md](docs/tham-chieu-csdl.md) — cấu trúc CSDL hiện tại, so với `db-design.docx`, kèm prompt vẽ ERD.

## Chạy ứng dụng

Cần có: .NET 10 SDK và Docker.

```bash
docker compose up -d db                      # PostgreSQL 16 tại localhost:5433
dotnet run --project src/Techies.Web         # http://localhost:5080
```

Khi khởi động, ứng dụng tự áp dụng migration và nạp dữ liệu mẫu (chỉ khi CSDL còn trống).
Muốn làm lại từ đầu: `docker compose down -v` rồi chạy lại.

### Tài khoản dùng thử

| Vai trò | Email | Mật khẩu | Vào được |
|---|---|---|---|
| Quản trị viên cao cấp (vai trò hệ thống) | `admin@techies.vn` | `Admin@123` | `/admin` — bảng điều khiển, mọi menu, kể cả Nhân sự và Vai trò & Phân quyền |
| Nhân viên Kho & Sản phẩm | `kho@techies.vn` | `Staff@123` | Sản phẩm, danh mục, kho |
| Nhân viên Điều phối & Quầy | `quay@techies.vn` | `Staff@123` | Đơn hàng, khách hàng, đánh giá, quầy nhận quà |
| Khách hàng | `khach01@example.com` … `khach40@example.com` | `Customer@123` | Cửa hàng |

Khách hàng có thể tự đăng ký tại `/tai-khoan/dang-ky`. Tài khoản nhân viên do Quản trị viên cao cấp tạo
(không có trang tự đăng ký cho nhân viên); Quản trị viên cao cấp cũng tạo vai trò mới và gán quyền tại
*Vai trò & Phân quyền*. Thử mã nhận quà **`GIFT-G53Y-ZDS3`** ở *Quầy nhận quà*, hoặc mã giảm giá
**`TECHIES50K`** khi đặt hàng từ 500.000đ.

## Phát triển

```bash
dotnet build Techies.slnx
dotnet test Techies.slnx                     # cần Docker: test dịch vụ chạy trên một Postgres tạm (Testcontainers)
dotnet ef migrations add <TenMigration> --project src/Techies.Web --output-dir Data/Migrations
dotnet format Techies.slnx --verify-no-changes
```

## Cấu trúc thư mục

```
src/Techies.Web/
  Areas/Admin/      Controller + view của trang quản trị (mỗi nhóm màn hình một controller)
  Controllers/      Controller của cửa hàng (Home, Products, Cart, Checkout, Account, MyOrders, Points)
  Domain/           Thực thể (entity) và enum
  Data/             DbContext, cấu hình Fluent, migration, dữ liệu mẫu (Data/Seed/*.json chuyển từ ../techies)
  Services/         Toàn bộ quy tắc nghiệp vụ — hoàn tất/hủy đơn, đặt hàng, sổ kho, điểm thưởng, mã giảm giá, …
  Infrastructure/   Cơ chế đăng nhập, quyền và policy, menu quản trị, lưới dữ liệu (grid), định dạng, upload, nhãn
tests/Techies.Tests/
  Unit/             Quy tắc thuần (tính tiền đơn hàng, mã giảm giá, hạng, kỳ doanh thu, giỏ hàng, định dạng)
  Services/         Test dịch vụ trên Postgres thật (Testcontainers)
docs/               Tài liệu tham chiếu cho BRD và thiết kế CSDL
```

## Các quyết định thiết kế

- **Monolith, không dùng microservice.** Các luồng chính của BRD đi qua nhiều phân hệ (hủy đơn chạm tới đơn
  hàng, kho, mã giảm giá và thanh toán; hoàn tất đơn cộng điểm thưởng). Trong một ứng dụng, mỗi luồng là một
  giao dịch (transaction) CSDL thay vì một saga.
- **Hai cơ chế cookie đăng nhập riêng** (`Staff`, `Customer`); mỗi policy gắn với đúng một cơ chế, nên đăng
  nhập khách hàng không bao giờ mở được `/admin`. Phiên được kiểm tra lại ở mỗi request: khóa tài khoản hoặc
  đổi vai trò nhân viên có hiệu lực ngay.
- **Vai trò và quyền** (`roles`, `permissions`, `role_permissions`). Danh mục quyền nằm trong mã nguồn
  (`Infrastructure/Permissions.cs`) và được nạp bằng migration; mọi thao tác quản trị đều gắn
  `[RequirePermission(...)]`, quyền được đọc một lần mỗi request nên sửa vai trò có hiệu lực ngay. Vai trò hệ
  thống Quản trị viên cao cấp luôn có toàn quyền và là vai trò duy nhất quản lý nhân sự và phân quyền.
- **Một lưới dữ liệu dùng chung** (`Infrastructure/Grid`, `_Grid.cshtml`, `wwwroot/js/admin-grid.js`) cho mọi
  danh sách quản trị, kiểu Magento: cột sắp xếp được, nút *Bộ lọc* mở bảng lọc, nhãn bộ lọc đang áp dụng, số
  dòng mỗi trang, thao tác hàng loạt và menu *Chọn* cho từng dòng. Máy chủ dựng lưới; script chỉ tải lại phần
  lưới bằng AJAX và giữ trạng thái trên URL (nút Back và bookmark hoạt động, tắt JavaScript vẫn dùng được).
- **Tìm kiếm không phân biệt dấu** bằng extension `unaccent` của PostgreSQL: mỗi từ khóa phải xuất hiện trong
  tên sản phẩm hoặc tên danh mục ("dien thoai samsung" ra điện thoại Samsung).
- **`StockLedger` là nơi duy nhất thay đổi tồn kho.** Nó khóa dòng tồn kho, không cho tồn âm và ghi một dòng
  `inventory_adjustments` cho mọi lần nhập hàng, kiểm kê, đặt hàng và hủy đơn. Hai người cùng đặt món hàng
  cuối cùng thì chỉ một người mua được (có test kiểm chứng).
- **Không xóa cứng** sản phẩm, nhân viên, khách hàng, đơn hàng hay đánh giá; sản phẩm thì tắt kinh doanh,
  người dùng thì khóa tài khoản.
- Giao diện bằng tiếng Việt; mã nguồn, tên định danh và CSDL bằng tiếng Anh.
