# Tài liệu tham chiếu để cập nhật BRD — Techies (.NET)

> **Mục đích:** mô tả chính xác hệ thống **đang làm gì** (tính đến 26/10/2026), đối chiếu với từng mục của
> `brd.md`, để nhóm viết BRD (và AI hỗ trợ họ) cập nhật tài liệu. Nguồn sự thật là mã nguồn và `SPEC.md`;
> tài liệu này là bản tóm tắt nghiệp vụ của chúng, bằng tiếng Việt.

## 0. Hướng dẫn dành cho AI (đọc trước khi sửa BRD)

Bạn sẽ nhận `brd.md` (bản cũ) và tài liệu này. Hãy cập nhật `brd.md` theo các quy tắc:

1. **Giữ cấu trúc và văn phong của BRD cũ** (các mục 1–7, gạch đầu dòng, tiếng Việt, tên trường kỹ thuật để
   trong ngoặc như `status`, `paymentStatus`). Thêm mục mới ở cuối (mục 8, 9, 10) như phần "Bổ sung" bên dưới.
2. Mỗi mục trong tài liệu này có nhãn **TRẠNG THÁI**:
   - `GIỮ NGUYÊN` — BRD đã đúng, không sửa (có thể làm rõ câu chữ).
   - `THAY ĐỔI` — BRD mô tả khác với hệ thống; **thay** câu cũ (được trích trong khối "BRD cũ") bằng nội dung mới.
   - `BỔ SUNG` — chức năng có trong hệ thống nhưng BRD chưa có; **thêm** vào đúng mục.
   - `CHƯA TRIỂN KHAI` — BRD có nhắc nhưng hệ thống chưa làm; **giữ** trong BRD nhưng ghi rõ "(chưa triển khai)".
3. Mọi con số, giá trị trạng thái, tên quyền trong tài liệu này là **chính xác** — chép nguyên, không làm tròn,
   không đổi tên.
4. Không bịa thêm chức năng không có ở đây. Nếu tài liệu này không nói tới, giữ nguyên BRD.
5. Thay mọi chỗ nói khách hàng thao tác "trên App Android / ứng dụng di động" bằng "trên cửa hàng web (website
   của hệ thống)"; hệ thống hiện **không có** app Android hay backend Spring Boot — đây là một ứng dụng web
   ASP.NET Core MVC duy nhất gồm trang quản trị và cửa hàng.

## Bảng đối chiếu nhanh

| Mục BRD | Nội dung | Trạng thái |
|---|---|---|
| Phạm vi chung | Hệ thống gồm trang quản trị **và** cửa hàng web cho khách | THAY ĐỔI |
| §1 Dashboard | Thẻ đơn chờ, biểu đồ doanh thu, đơn gần đây, sản phẩm bán chạy | GIỮ NGUYÊN (+ làm rõ quy tắc) |
| §2 Sản phẩm & kho | Lọc "hết hàng" là khoảng tồn kho; nhật ký kho | THAY ĐỔI nhỏ + BỔ SUNG |
| §3 Đơn hàng | Bỏ 5 tab → bộ lọc trạng thái trong lưới; thao tác hàng loạt | THAY ĐỔI |
| §3 Thanh toán | MOCK_CARD/VNPay chưa có ở cửa hàng; chỉ COD | CHƯA TRIỂN KHAI (một phần) |
| §4 Mã giảm giá | Khách nhập mã khi đặt hàng | GIỮ NGUYÊN + BỔ SUNG |
| §5 Quà tặng & hạng | Khách đổi quà trên web; "Hết quà" = số lượng 0; voucher % theo hạng chưa dùng | THAY ĐỔI + CHƯA TRIỂN KHAI |
| §6 Khách hàng & đánh giá | Lưới, khóa hàng loạt, khách tự viết đánh giá | GIỮ NGUYÊN + BỔ SUNG |
| §7 Nhân sự & phân quyền | Vai trò do Quản trị viên cao cấp tự tạo, quyền theo từng thao tác | THAY ĐỔI |
| (mới) §8 Cửa hàng web | Toàn bộ chức năng phía khách hàng | BỔ SUNG |
| (mới) §9 Lưới dữ liệu quản trị | Cách mọi danh sách quản trị hoạt động | BỔ SUNG |
| (mới) §10 Chưa triển khai | Danh sách những gì BRD nhắc nhưng chưa làm | BỔ SUNG |

---

## Phạm vi chung — TRẠNG THÁI: THAY ĐỔI

**BRD cũ** (ngầm định khắp tài liệu): khách hàng dùng App Android; trang web chỉ là CMS cho admin; dữ liệu
"khớp 100% với Backend & App Android".

**Mới:** Hệ thống là **một ứng dụng web** gồm hai phần dùng chung một CSDL:
- **Trang quản trị** (`/admin`) cho nhân viên — các mục 1–7.
- **Cửa hàng web** (`/`) cho khách hàng — mục 8. Mọi thao tác của khách (đặt hàng, nhập mã giảm giá, hủy đơn,
  đánh giá, đổi quà) diễn ra trên cửa hàng web.

Hai loại tài khoản tách biệt hoàn toàn: tài khoản khách hàng không vào được trang quản trị và ngược lại.

---

## §1. Bảng điều khiển (Dashboard) — TRẠNG THÁI: GIỮ NGUYÊN (làm rõ quy tắc)

Chỉ nhân viên có quyền `dashboard.view` mới thấy (mặc định chỉ Quản trị viên cao cấp). Người không có quyền
này khi mở `/admin` được chuyển tới menu đầu tiên họ được phép.

- **Đơn chưa hoàn thành:** hai thẻ số lượng — `AWAITING_PAYMENT` (Chờ thanh toán) và `CONFIRMED` (Đã xác nhận /
  Chờ giao). Bấm vào thẻ mở lưới Đơn hàng đã lọc sẵn đúng trạng thái đó.
- **Doanh thu:** tổng `total` của các đơn `COMPLETED`, tính theo **thời điểm hoàn tất** (`completed_at`), múi giờ
  Việt Nam. Bộ lọc kỳ và độ chia của biểu đồ đường:
  - 7 ngày qua → theo ngày; Tháng này → theo ngày; Quý này → theo tuần; Năm nay → theo tháng.
  - % tăng/giảm so với **cùng khoảng thời gian đã trôi qua của kỳ trước** (VD: 1–26/10 so với 1–26/09).
    Nếu kỳ trước bằng 0 thì hiển thị "—".
- **Đơn hàng gần đây:** 10 đơn mới nhất (mã đơn, người nhận, thời gian đặt, tổng tiền, phương thức thanh toán,
  trạng thái); bấm mã đơn mở trang chi tiết. Cập nhật khi tải trang (không dùng thời gian thực/websocket).
- **Sản phẩm bán chạy:** top 10 theo **số lượng bán** trong các đơn `COMPLETED`, chọn kỳ Tuần (7 ngày qua) /
  Tháng / Quý / Năm; kèm doanh thu và tồn kho hiện tại; nhãn đỏ **"Sắp hết"** khi tồn kho **≤ 5**.

---

## §2. Quản lý sản phẩm & kho hàng — TRẠNG THÁI: THAY ĐỔI nhỏ + BỔ SUNG

**Danh mục:** tạo và sửa (tên, ảnh icon, thứ tự hiển thị). Đường dẫn thân thiện (`slug`) tự sinh từ tên, bỏ dấu
tiếng Việt (VD "Điện thoại" → `dien-thoai`). **Không có chức năng xóa danh mục.** — GIỮ NGUYÊN + làm rõ.

**Sản phẩm:**
- **THAY ĐỔI** — BRD cũ: *"lọc riêng các sản phẩm đang hết hàng (inStock = false)"*. Mới: không có trường
  `inStock`; lưới sản phẩm có bộ lọc **khoảng tồn kho** (từ–đến; "hết hàng" = tồn kho 0), kèm lọc theo danh
  mục, khoảng giá, trạng thái kinh doanh, và ô tìm kiếm.
- **BỔ SUNG** — Tìm kiếm không phân biệt hoa/thường và dấu tiếng Việt; mỗi từ khóa phải khớp **tên sản phẩm
  hoặc tên danh mục** (VD "dien thoai samsung" ra các điện thoại Samsung).
- Thêm/sửa: tên, danh mục, giá (> 0), mô tả, ảnh đại diện, thư viện nhiều ảnh (thêm, bỏ, sắp xếp lại thứ tự),
  bảng thông số "Thêm dòng thông số" / xóa dòng. — GIỮ NGUYÊN.
- Bật/Tắt kinh doanh: sản phẩm tắt thì không hiện trên cửa hàng, không thêm vào giỏ, không đặt được; đơn cũ vẫn
  giữ nguyên thông tin. Có thể bật/tắt **hàng loạt** từ lưới. **Không xóa cứng sản phẩm.** — GIỮ NGUYÊN + BỔ SUNG.
- Sản phẩm mới tạo có tồn kho 0 cho tới khi nhập hàng. — BỔ SUNG.
- Ảnh tải lên: jpg/png/webp, tối đa 2 MB, hệ thống kiểm tra nội dung tệp đúng là ảnh. — BỔ SUNG.

**Kiểm kho & nhập hàng:**
- **Nhập hàng:** cộng thêm số lượng (> 0). **Kiểm kê:** nhập số lượng thực tế (≥ 0), hệ thống tự tính chênh lệch.
  — GIỮ NGUYÊN.
- **BỔ SUNG — Nhật ký kho:** mọi thay đổi tồn kho đều được ghi lại với loại, số lượng trước/sau, chênh lệch, lý
  do, người thực hiện, thời gian. Có 4 loại: `IMPORT` (Nhập hàng), `COUNT` (Kiểm kê), `ORDER` (Xuất theo đơn khi
  khách đặt), `CANCEL` (Hoàn kho khi hủy đơn). Mỗi sản phẩm có trang lịch sử biến động kho.
- **BỔ SUNG — Quy tắc:** tồn kho không bao giờ âm; hai khách cùng đặt món cuối cùng thì chỉ một người đặt được.

---

## §3. Quản lý & điều phối đơn hàng — TRẠNG THÁI: THAY ĐỔI

**THAY ĐỔI — Danh sách đơn.** BRD cũ: *"Chuyển đổi qua lại giữa 5 Tab trạng thái đơn hàng (status)"*.
Mới: **không còn tab**. Danh sách đơn là một lưới dữ liệu (xem §9) với:
- Ô tìm kiếm theo **mã đơn** (VD `ORD-100117`) hoặc **số điện thoại người nhận** — GIỮ NGUYÊN.
- Bộ lọc: **Trạng thái** (5 giá trị, kèm số lượng mỗi trạng thái), phương thức thanh toán, trạng thái thanh
  toán, khoảng tổng tiền, khoảng ngày đặt.
- Sắp xếp theo mã đơn, ngày đặt, tổng tiền (mặc định mới nhất trước).
- **Thao tác hàng loạt:** Hoàn tất đơn, Hủy đơn, In phiếu giao hàng (nhiều phiếu, mỗi phiếu một trang in). Mỗi
  đơn được xử lý riêng; kết quả báo "Đã hoàn tất N / M" và lý do các đơn bị bỏ qua.

**5 trạng thái đơn — GIỮ NGUYÊN:** `AWAITING_PAYMENT`, `CONFIRMED`, `COMPLETED`, `CANCELLED`, `FAILED`
(mã lỗi `failure_code`: `PAYMENT_FAILED`, `OUT_OF_STOCK`, `SERVICE_UNAVAILABLE`).
Trạng thái thanh toán: `PENDING`, `PAID`, `DECLINED`, `REFUNDED`. Phương thức: `COD`, `MOCK_CARD`.

**CHƯA TRIỂN KHAI — thanh toán điện tử.** Cửa hàng web hiện **chỉ có COD**: đơn khách đặt luôn ở `CONFIRMED`,
thanh toán `PENDING`. Các trường hợp `AWAITING_PAYMENT`, `FAILED`, `MOCK_CARD`, `DECLINED` chỉ có trong **dữ
liệu mẫu**; **không có VNPay**. Trang quản trị vẫn xử lý đầy đủ các trạng thái này.

**Chi tiết đơn & tiền thu hộ — GIỮ NGUYÊN:** người nhận (`shippingAddress`), sản phẩm (`items`), tạm tính
(`subtotal`), phí vận chuyển (`shippingFee`), mã giảm giá (`couponCode`, `discount`), tổng (`total`), phương
thức và trạng thái thanh toán. **Số tiền cần thu hộ** = `total` nếu COD chưa thanh toán; = 0đ nếu đã thanh toán.
- **BỔ SUNG — Phí vận chuyển:** 30.000đ, **miễn phí khi tạm tính ≥ 500.000đ** (tính trên tạm tính trước giảm giá).
- **BỔ SUNG — Công thức:** `total = subtotal + shippingFee − discount`.

**Hoàn tất đơn (CONFIRMED → COMPLETED) — GIỮ NGUYÊN + làm rõ:** đơn COD chuyển thanh toán sang `PAID`; cộng
điểm **1 điểm cho mỗi 1.000đ của (subtotal − discount), làm tròn xuống**; tự lên hạng nếu đạt mốc; mỗi đơn chỉ
được cộng điểm một lần.

**Hủy đơn (CONFIRMED → CANCELLED) — THAY ĐỔI câu chữ:** BRD cũ *"hoàn lại voucher (nếu có)"* → Mới: **hoàn lại
lượt dùng mã giảm giá (coupon) nếu có** (hệ thống không có voucher theo hạng, xem §10). Trả hàng về kho (ghi
nhật ký `CANCEL`); thanh toán `PAID` → `REFUNDED`; COD chưa thanh toán giữ `PENDING`. Chỉ áp dụng cho đơn
`CONFIRMED`.
- **BỔ SUNG:** khách hàng cũng tự hủy được đơn của mình trên cửa hàng khi đơn còn `CONFIRMED` (cùng quy tắc).

**In phiếu giao hàng — GIỮ NGUYÊN:** trang in gồm mã đơn, người nhận, địa chỉ, SĐT, sản phẩm, tổng tiền, số tiền
cần thu hộ, chỗ ký của người giao và người nhận.

---

## §4. Quản lý mã giảm giá — TRẠNG THÁI: GIỮ NGUYÊN + BỔ SUNG

- Mã giảm **số tiền cố định** (VNĐ) trên tạm tính; điều kiện **tạm tính tối thiểu**; mã gồm 3–40 chữ cái/chữ
  số, tự viết hoa, không trùng. Giảm không vượt quá tạm tính. — GIỮ NGUYÊN.
- Tạm dừng / kích hoạt lại bất kỳ lúc nào (cả **hàng loạt** từ lưới); ngày bắt đầu và ngày hết hạn tùy chọn
  (hết hạn phải sau bắt đầu). — GIỮ NGUYÊN + BỔ SUNG.
- **BỔ SUNG — Trạng thái hiển thị** (tính tự động): `Đang chạy`, `Tạm dừng`, `Chưa bắt đầu`, `Hết hạn`; lọc
  được theo trạng thái, khoảng số tiền giảm, khoảng ngày hết hạn.
- **BỔ SUNG — Khách dùng mã:** ở trang đặt hàng có ô "Mã giảm giá" + nút **Áp dụng** để xem trước số tiền giảm,
  hoặc lý do không dùng được: *"Mã giảm giá đang tạm dừng."*, *"…chưa đến thời gian áp dụng."*, *"…đã hết
  hạn."*, *"Đơn hàng cần tạm tính tối thiểu X để dùng mã này."*, *"Mã giảm giá không tồn tại."* Khi đặt hàng,
  mã được kiểm tra lại; nếu không còn hợp lệ thì không tạo đơn. Mỗi đơn dùng tối đa **1 mã**; mã dùng lại được
  nhiều lần, không giới hạn theo khách.
- Mã thông báo kỹ thuật `COUPON_NOT_APPLICABLE` vẫn dùng nội bộ. — GIỮ NGUYÊN.

---

## §5. Quà tặng, đổi điểm & hạng thành viên — TRẠNG THÁI: THAY ĐỔI + CHƯA TRIỂN KHAI

**Danh mục quà:** tên, mô tả, ảnh, điểm cần đổi (`pointsCost` > 0), hạng tối thiểu (`minTier` 0–3), số lượng
còn, trạng thái mở đổi/tạm khóa (cả hàng loạt).
- **THAY ĐỔI** — BRD cũ: *"gạt nút báo Hết quà (inStock = false)"*. Mới: không có `inStock`; quà có **số lượng
  còn** (`stock_available`) — bằng 0 nghĩa là "Hết quà" — và một công tắc **Mở đổi / Tạm khóa** riêng.

**Quầy nhận quà — GIỮ NGUYÊN + làm rõ:** nhân viên nhập mã (VD `GIFT-G53Y-ZDS3`, không phân biệt hoa thường),
xem tên quà, điểm đã dùng, tên + SĐT khách, thời điểm đổi, trạng thái; bấm **Xác nhận giao quà** để chuyển
`ISSUED` → `CLAIMED` (lưu thời điểm và nhân viên giao). Mỗi mã chỉ nhận được một lần. Mã không có → "Không tìm
thấy mã".

**THAY ĐỔI — Nơi khách đổi quà:** BRD cũ *"mã nhận quà trên điện thoại"* (app). Mới: khách đổi quà trên trang
**Điểm thưởng** của cửa hàng web (xem §8.6) và nhận mã `GIFT-XXXX-XXXX`.

**Hạng thành viên:** hạng 0 cố định ở 0 điểm; hạng 1–3 sửa được tên, mốc điểm (`thresholdPoints`, phải tăng
dần) và % ưu đãi (`discountPercent`, 0–100). Khi lưu, hạng của **mọi khách** được tính lại theo tổng điểm tích
lũy. Giá trị mặc định: 0 Thành viên/0 điểm; 1 Bạc/10.000; 2 Vàng/30.000; 3 Kim cương/60.000 (ưu đãi 0/10/30/50%).
- **CHƯA TRIỂN KHAI** — % ưu đãi theo hạng (voucher hạng thành viên) **được lưu nhưng chưa áp dụng ở đâu**; cửa
  hàng chỉ có mã giảm giá cố định.

---

## §6. Khách hàng & đánh giá — TRẠNG THÁI: GIỮ NGUYÊN + BỔ SUNG

- Tìm khách theo email, họ tên, SĐT; lọc theo hạng, trạng thái; sắp xếp theo tên, email, điểm. — GIỮ NGUYÊN + BỔ SUNG.
- Hồ sơ chỉ xem: liên hệ, sổ địa chỉ, hạng, tổng điểm tích lũy (`lifetimePoints`), điểm khả dụng (`balance`),
  lịch sử điểm, lịch sử mua hàng. **Không tạo mới / sửa thông tin khách.** — GIỮ NGUYÊN.
- Khóa / Mở khóa (cả hàng loạt). **BỔ SUNG:** khách bị khóa không đăng nhập được và **bị đăng xuất ngay** ở thao
  tác tiếp theo.
- **Đánh giá:** lưới chỉ xem — lọc theo sản phẩm, **khoảng số sao** (VD 1→2 để xem đánh giá thấp), khoảng ngày,
  từ khóa trong nhận xét; dòng 1–2 sao được tô đỏ. Không sửa, xóa, trả lời. — THAY ĐỔI nhỏ (bỏ ô "chỉ 1–2 sao"
  riêng, thay bằng bộ lọc khoảng sao).
- **BỔ SUNG — Khách tự viết đánh giá** trên cửa hàng (xem §8.5).

---

## §7. Nhân sự & phân quyền (RBAC) — TRẠNG THÁI: THAY ĐỔI

**Giữ nguyên:**
- Không có trang tự đăng ký cho nhân viên; tài khoản Quản trị viên cao cấp đầu tiên được tạo sẵn khi cài đặt
  (`admin@techies.vn`).
- Tạo nhân viên: họ tên, email công việc (không trùng), SĐT, mật khẩu khởi tạo (≥ 8 ký tự), chọn vai trò →
  trạng thái `ACTIVE`.
- Khóa thay vì xóa (`LOCKED`; bị đăng xuất ngay; lịch sử thao tác được giữ); đổi vai trò; cấp lại mật khẩu.
  Khóa/mở khóa được cả hàng loạt.

**BỔ SUNG — Quy tắc an toàn:** Quản trị viên cao cấp không tự khóa hoặc tự hạ quyền chính mình; luôn phải còn
ít nhất một Quản trị viên cao cấp đang hoạt động. Đổi vai trò của ai thì phiên đăng nhập của người đó kết thúc.

**THAY ĐỔI — Vai trò.** BRD cũ: *"Định nghĩa 3 nhóm quyền hạn rõ ràng"* (cố định). Mới: **Quản trị viên cao cấp
tự tạo, sửa, xóa vai trò** tại màn hình **Vai trò & Phân quyền**, và chọn quyền cho từng vai trò theo danh sách
quyền dưới đây (chọn từng quyền hoặc "chọn cả nhóm"). Sửa quyền của vai trò có hiệu lực **ngay ở thao tác tiếp
theo** của nhân viên mang vai trò đó.
- Không xóa được vai trò đang gán cho nhân viên (báo "đang được gán cho N nhân viên").
- **Quản trị viên cao cấp** là **vai trò hệ thống**: luôn toàn quyền; đổi được tên/mô tả nhưng không đổi quyền,
  không xóa được. **Chỉ vai trò này** quản lý Nhân sự và Vai trò & Phân quyền (hai việc này không phải là quyền
  có thể gán cho vai trò khác).
- Ba nhóm của BRD cũ trở thành **ba vai trò có sẵn**: Quản trị viên cao cấp (hệ thống), Nhân viên Kho & Sản
  phẩm, Nhân viên Điều phối & Quầy — hai vai trò sau sửa được như mọi vai trò khác.

**Danh sách quyền (chính xác 22 quyền):**

| Nhóm | Mã quyền | Tên hiển thị | Nhân viên Kho & Sản phẩm | Nhân viên Điều phối & Quầy |
|---|---|---|:-:|:-:|
| Tổng quan | `dashboard.view` | Xem bảng điều khiển & doanh thu | | |
| Sản phẩm | `products.view` | Xem sản phẩm | ✓ | |
| Sản phẩm | `products.edit` | Thêm / sửa sản phẩm | ✓ | |
| Sản phẩm | `products.toggle` | Bật / tắt kinh doanh | ✓ | |
| Danh mục | `categories.manage` | Quản lý danh mục | ✓ | |
| Kho | `inventory.view` | Xem tồn kho | ✓ | |
| Kho | `inventory.import` | Nhập hàng | ✓ | |
| Kho | `inventory.count` | Kiểm kê | ✓ | |
| Đơn hàng | `orders.view` | Xem đơn hàng | | ✓ |
| Đơn hàng | `orders.complete` | Hoàn tất đơn hàng | | ✓ |
| Đơn hàng | `orders.cancel` | Hủy đơn hàng | | ✓ |
| Đơn hàng | `orders.print` | In phiếu giao hàng | | ✓ |
| Khách hàng | `customers.view` | Xem khách hàng | | ✓ |
| Khách hàng | `customers.lock` | Khóa / mở khóa khách hàng | | ✓ |
| Đánh giá | `reviews.view` | Xem đánh giá | | ✓ |
| Quầy nhận quà | `giftcounter.lookup` | Tra cứu mã nhận quà | | ✓ |
| Quầy nhận quà | `giftcounter.claim` | Xác nhận giao quà | | ✓ |
| Mã giảm giá | `coupons.view` | Xem mã giảm giá | | |
| Mã giảm giá | `coupons.manage` | Tạo / sửa / tạm dừng mã | | |
| Quà tặng | `gifts.view` | Xem quà tặng | | |
| Quà tặng | `gifts.manage` | Thêm / sửa quà tặng | | |
| Hạng thành viên | `tiers.manage` | Cấu hình hạng thành viên | | |

Quản trị viên cao cấp có tất cả. Menu, nút bấm và cả máy chủ đều kiểm tra quyền (ẩn nút không đủ để bảo mật —
gọi trực tiếp đường dẫn vẫn bị từ chối, hiện trang "Không có quyền truy cập").

---

## §8. (MỚI) Cửa hàng web cho khách hàng — TRẠNG THÁI: BỔ SUNG

**8.1 Tài khoản:** đăng ký (họ tên, email không trùng — không phân biệt hoa thường, SĐT 10 số bắt đầu bằng 0,
mật khẩu ≥ 8 ký tự, nhập lại mật khẩu); đăng nhập; đăng xuất. Tài khoản mới ở hạng 0 với 0 điểm. Chưa có sửa hồ
sơ, đổi mật khẩu, quên mật khẩu.

**8.2 Xem và tìm sản phẩm:** trang chủ (danh mục + sản phẩm mới), danh sách theo danh mục, tìm kiếm (quy tắc như
§2: không phân biệt dấu, khớp tên sản phẩm hoặc tên danh mục, mọi từ phải khớp), trang chi tiết (thư viện ảnh,
giá, mô tả, bảng thông số, tình trạng còn/hết hàng, đánh giá và điểm trung bình). Sản phẩm tắt kinh doanh không
xuất hiện.

**8.3 Giỏ hàng:** lưu theo phiên trình duyệt; thêm, đổi số lượng (1–99 và không quá tồn kho), xóa; hiển thị tạm
tính, phí vận chuyển, tổng. Giá luôn lấy theo giá hiện tại của sản phẩm.

**8.4 Đặt hàng (chỉ COD):** bắt buộc đăng nhập; chọn địa chỉ đã lưu hoặc nhập địa chỉ mới (lưu vào sổ địa chỉ,
có thể đặt mặc định); nhập mã giảm giá (§4). Khi đặt: kiểm tra lại từng sản phẩm còn bán và đủ hàng, trừ kho,
tạo đơn `CONFIRMED` / thanh toán `PENDING`, xóa giỏ. Nếu một bước thất bại thì **không có gì được tạo** (kể cả
địa chỉ mới) và khách được báo sản phẩm/mã nào gây lỗi. Sau đó: "Đơn hàng của tôi" (chỉ thấy đơn của mình) và
tự hủy đơn khi còn `CONFIRMED`.

**8.5 Đánh giá:** với đơn `COMPLETED`, mỗi sản phẩm trong đơn có ô chấm **1–5 sao** (bắt buộc) và nhận xét (không
bắt buộc, ≤ 1000 ký tự). **Mỗi dòng sản phẩm của một đơn chỉ đánh giá một lần**; không sửa/xóa. Đánh giá hiện
ngay trên trang sản phẩm và trong lưới đánh giá của quản trị.

**8.6 Điểm thưởng & đổi quà** (`/diem-thuong`): hiển thị hạng, thanh tiến độ và số điểm còn thiếu để lên hạng kế
tiếp, điểm khả dụng, tổng điểm tích lũy, lịch sử điểm, danh sách quà đang mở đổi, và "Quà đã đổi" (mã, quà,
điểm, trạng thái *Chờ nhận tại cửa hàng* / *Đã nhận* + ngày).
- **Đổi quà** chỉ được khi: điểm khả dụng ≥ điểm cần; hạng ≥ hạng tối thiểu; quà còn hàng; quà đang mở đổi. Nếu
  không, nút bị khóa kèm lý do (VD "Bạn không đủ điểm (cần 300 điểm nữa).", "Cần đạt hạng cao hơn để đổi quà
  này.", "Quà đã hết.").
- Khi đổi: trừ **điểm khả dụng** (không trừ điểm tích lũy → **hạng không bao giờ giảm vì đổi quà**), trừ 1 số
  lượng quà, tạo mã `GIFT-XXXX-XXXX` duy nhất ở trạng thái `ISSUED`, ghi lịch sử điểm. Hai yêu cầu đổi cùng lúc
  không thể dùng chung một lượng điểm hay cùng lấy món quà cuối cùng.

---

## §9. (MỚI) Lưới dữ liệu ở trang quản trị — TRẠNG THÁI: BỔ SUNG

Áp dụng cho **mọi danh sách quản trị**: sản phẩm, kho, danh mục, đơn hàng, khách hàng, đánh giá, mã giảm giá,
quà tặng, nhân sự, vai trò.

- **Sắp xếp:** bấm tiêu đề cột để sắp xếp tăng/giảm (có mũi tên).
- **Tìm kiếm:** ô từ khóa luôn hiển thị.
- **Bộ lọc:** nút **Bộ lọc** mở bảng chứa mọi điều kiện lọc của danh sách (chọn giá trị, khoảng số, khoảng ngày);
  **Áp dụng bộ lọc** để lọc, **Hủy** để đóng mà không áp dụng. Các điều kiện đang lọc hiện thành nhãn có nút ✕ và
  nút **Xóa tất cả**.
- **Không tải lại trang:** sắp xếp, lọc, chuyển trang, đổi số dòng (20/50/100) chỉ tải lại phần danh sách; địa chỉ
  trang luôn phản ánh bộ lọc hiện tại nên dùng được nút Back và lưu dấu trang.
- **Thao tác hàng loạt:** ô chọn ở mỗi dòng + ô chọn cả trang + menu **Thao tác hàng loạt** (chỉ hiện các thao tác
  người dùng có quyền); hỏi xác nhận; kết quả "Đã … N / M" kèm lý do các dòng bị bỏ qua.
- **Thao tác từng dòng:** cột cuối có menu **Chọn ▾** (Xem, Sửa, Khóa…), chỉ hiện thao tác hợp lệ với quyền và
  trạng thái của dòng (VD "Hoàn tất" chỉ có với đơn `CONFIRMED`).

| Danh sách | Thao tác hàng loạt |
|---|---|
| Sản phẩm | Bật kinh doanh, Tắt kinh doanh |
| Đơn hàng | Hoàn tất đơn hàng, Hủy đơn, In phiếu giao hàng |
| Khách hàng | Khóa tài khoản, Mở khóa |
| Mã giảm giá | Tạm dừng, Kích hoạt |
| Quà tặng | Mở đổi quà, Tạm khóa |
| Nhân sự | Khóa tài khoản, Mở khóa (theo quy tắc an toàn §7) |
| Vai trò | Xóa vai trò (bỏ qua vai trò hệ thống và vai trò đang được gán) |
| Kho, Danh mục, Đánh giá | Không có |

---

## §10. (MỚI) Chưa triển khai — TRẠNG THÁI: BỔ SUNG (để ghi chú trong BRD)

- Thanh toán điện tử (MOCK_CARD, VNPay) trên cửa hàng; trạng thái `AWAITING_PAYMENT` / `FAILED` phát sinh từ
  cửa hàng (hiện chỉ có trong dữ liệu mẫu).
- Voucher giảm % theo hạng thành viên (`discountPercent` được cấu hình nhưng chưa dùng).
- Trả lời đánh giá; sửa/xóa đánh giá (theo đúng BRD: không cho phép).
- Sửa hồ sơ khách, đổi/quên mật khẩu, thông báo email.
- App Android và backend Spring Boot (dự án `../techies` là hệ thống riêng, chỉ dùng tham khảo).
