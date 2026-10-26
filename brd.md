## **1\. Bảng điều khiển trung tâm (Dashboard)**

Nơi Admin nắm bắt nhanh tình hình kinh doanh khi vừa mở trang web:

* **Thống kê Số đơn hàng chưa hoàn thành (Unfulfilled / Pending Orders):**  
  * Hiển thị nổi bật tổng số đơn hàng đang chờ xử lý (gồm các đơn ở trạng thái Chờ thanh toán \- AWAITING\_PAYMENT và Đã xác nhận / Chờ giao \- CONFIRMED).  
  * Tích hợp liên kết điều hướng nhanh (Quick-link): Click trực tiếp vào con số để chuyển thẳng sang màn hình Quản lý đơn hàng đã được lọc sẵn theo đúng trạng thái đó để duyệt ngay.  
* **Biểu đồ doanh thu (Revenue Chart):**  
  * Hiển thị tổng doanh thu thực thu (tính từ các đơn hàng đã hoàn tất \- COMPLETED) kèm biểu đồ trực quan (Line/Bar Chart) theo dõi xu hướng tăng giảm theo thời gian.  
  * Hỗ trợ bộ lọc thời gian linh hoạt: 7 ngày qua, Tháng này, Quý này, Năm nay (kèm chỉ số % tăng/giảm so với kỳ trước).  
*  **Danh sách Đơn hàng gần đây (Recent Orders):**  
  * Hiển thị bảng tóm tắt 5–10 đơn hàng vừa mới phát sinh trên hệ thống theo thời gian thực (gồm: Mã đơn hàng, Tên người nhận, Thời gian đặt, Tổng tiền, Phương thức thanh toán, Trạng thái hiện tại).  
  * Cho phép Admin click thẳng vào mã đơn để mở nhanh trang chi tiết và tiến hành in phiếu giao hàng hoặc xử lý đơn.  
* **Top Sản phẩm bán chạy (Best-selling Products):**  
  * Xếp hạng các mặt hàng (Điện thoại, Laptop, Tivi...) có số lượng bán ra và doanh thu cao nhất trong tuần, tháng, quý, năm.  
  * Hiển thị kèm Số lượng tồn kho hiện tại ngay bên cạnh mỗi sản phẩm bán chạy (kèm nhãn cảnh báo đỏ nếu sắp hết hàng) để chủ động nhập thêm các mã hàng đang có sức mua lớn.

## **2\. Quản lý Sản phẩm & Kho hàng (Catalog & Inventory)**

Nơi Admin biên tập toàn bộ nội dung hàng hóa hiển thị trên ứng dụng mua sắm của khách và kiểm soát tồn kho chặt chẽ:

* **Tương tác với Danh mục hàng hóa (Categories):**  
  * Tạo danh mục mới, tải ảnh icon đại diện và đặt tên.  
*  **Tương tác với Sản phẩm (Products \- Đã bỏ từ khóa Biến thể/SKU để khớp Backend):**  
  *  **Tìm kiếm & Lọc nhanh:** Gõ tên sản phẩm vào ô tìm kiếm, lọc theo từng danh mục hoặc lọc riêng các sản phẩm đang hết hàng (inStock \= false).  
  * **Thêm / Sửa sản phẩm:** Nhập tên máy, giá bán, viết bài mô tả, tải lên ảnh đại diện chính (thumbnailUrl) và kéo thả nhiều ảnh vào thư viện ảnh chi tiết (images).  
  * **Thiết lập bảng thông số kỹ thuật:** Bấm nút "Thêm dòng thông số" để tự điền các cặp thông tin tùy theo loại máy (ví dụ với Tivi thì điền Kích cỡ màn hình \- 75 inch, với Điện thoại thì điền Dung lượng RAM \- 8GB) hoặc bấm xóa dòng thông số không cần thiết.  
  * **Bật / Tắt kinh doanh:** Gạt công tắc để tạm ẩn sản phẩm khỏi ứng dụng khi ngừng bán (khách sẽ không tìm thấy hoặc không đặt mua được nữa) mà không làm mất dữ liệu ở các đơn hàng cũ.  
* **Tương tác Kiểm kho & Nhập hàng:**  
  * Xem danh sách tồn kho khả dụng (available) hiện tại của từng mặt hàng.  
  * Nhập số lượng hàng mới về để cộng thêm vào kho, hoặc sửa lại số lượng thực tế khi kiểm kê.

## **3\. Quản lý & Điều phối Đơn hàng (Order Management)**

Nơi Admin xử lý vòng đời đơn hàng khép kín từ lúc khách đặt mua thành công đến khi nhận hàng:

*  **Tra cứu & Phân loại đơn hàng (Khớp 100% với 5 trạng thái của Backend & App Android):**  
  * Chuyển đổi qua lại giữa 5 Tab trạng thái đơn hàng (status):  
    * AWAITING\_PAYMENT (Chờ thanh toán): Đơn chọn thanh toán điện tử (MOCK\_CARD / VNPay) nhưng khách chưa hoàn tất thanh toán.  
    * CONFIRMED (Đã xác nhận / Chờ giao): Đơn COD vừa đặt thành công, hoặc đơn MOCK\_CARD đã thanh toán xong (paymentStatus \= PAID). Kho đã giữ hàng, chờ Admin đóng gói và giao đi.  
    * COMPLETED (Đã hoàn tất): Đơn hàng đã giao thành công cho khách và thu đủ tiền.  
    * CANCELLED (Đã hủy): Đơn bị hủy bởi khách hàng hoặc do Admin hủy khi không liên lạc được / giao hàng thất bại.  
    * FAILED (Giao dịch lỗi): Đơn thất bại ngay lúc đặt hoặc thanh toán (kèm mã lỗi failureCode: PAYMENT\_FAILED, OUT\_OF\_STOCK, SERVICE\_UNAVAILABLE).  
  * Gõ mã đơn hàng (VD: ORD-100117) hoặc số điện thoại người nhận để tìm đích danh đơn hàng cần hỗ trợ.  
*  **Xem chi tiết, Xác định tiền thu hộ & In phiếu giao hàng:**  
  * Click vào một đơn hàng để mở trang chi tiết: Xem rõ thông tin người nhận (shippingAddress), danh sách từng món hàng khách mua (items), tạm tính (subtotal), phí vận chuyển (shippingFee), mã giảm giá (couponCode, discount), phương thức thanh toán (COD hoặc MOCK\_CARD) và trạng thái thanh toán (PENDING, PAID, DECLINED, REFUNDED).  
  * Tiêu chí xác định "Số tiền cần thu hộ (COD)" khi giao hàng:  
    * Nếu paymentMethod \= COD (lúc này paymentStatus \= PENDING): Số tiền cần thu \= Tổng tiền đơn hàng (total).  
    * Nếu paymentMethod \= MOCK\_CARD và paymentStatus \= PAID: Số tiền cần thu \= 0đ.  
*  **Thao tác Xác nhận Hoàn tất & Hủy đơn (Quy trình đối soát giao hàng trên Admin):**  
  * Xác nhận giao hàng thành công (Chuyển từ CONFIRMED sang COMPLETED): Sau khi đóng gói, bàn giao cho shipper và nhận báo cáo đã giao hàng thành công kèm tiền thu hộ, Admin bấm nút "Hoàn tất đơn hàng" (COMPLETED). Ngay khi bấm, hệ thống tự động cộng điểm thưởng tích lũy vào tài khoản của khách (1 điểm cho mỗi 1.000đ của subtotal \- discount).  
  * Hủy đơn hàng / Giao thất bại (Chuyển từ CONFIRMED sang CANCELLED): Khi khách gọi điện báo hủy hoặc shipper báo không liên lạc được / khách từ chối nhận hàng, Admin bấm nút "Hủy đơn" (chỉ áp dụng khi đơn đang ở trạng thái CONFIRMED). Số lượng hàng trong đơn sẽ tự động được trả về kho (restore stock), hoàn lại voucher (nếu có) và chuyển trạng thái thanh toán sang REFUNDED (nếu đã trả tiền trước).

## **4\. Quản lý Mã giảm giá (Promotions & Coupons)**

Nơi Admin tạo các chương trình khuyến mãi kích cầu mua sắm và kiểm soát ngân sách marketing:

*  **Tạo và chỉnh sửa mã giảm giá (Khớp với logic Coupon cố định của Backend):**  
  * Tự đặt mã code dễ nhớ (VD: TECHIES50K, TECHIES500K, FREESHIP30K), nhập số tiền giảm cố định (VND) trừ thẳng vào tạm tính (subtotal) và điều kiện giá trị đơn hàng tối thiểu được phép áp dụng (Lưu ý: Giảm theo % là cơ chế riêng của Tier Voucher khi lên hạng thành viên, còn Coupon do Admin tạo chỉ giảm số tiền cố định).   
* **Kiểm soát chiến dịch khuyến mãi:**  
  * Bấm nút Tạm dừng hoặc Kích hoạt lại mã giảm giá bất kỳ lúc nào (khi tạm dừng, khách nhập mã trên app sẽ nhận được thông báo mã không khả dụng \- COUPON\_NOT\_APPLICABLE).  
  * Thiết lập ngày bắt đầu và ngày hết hạn tự động cho từng mã.

## **5\. Quản lý Quà tặng & Đổi điểm tại quầy (Loyalty & Gift Exchange)**

Nơi Admin vận hành chương trình khách hàng thân thiết và hỗ trợ nhân viên cửa hàng trả quà cho khách:

* **Quản lý danh mục & Tồn kho quà tặng đổi điểm:**  
  * Đăng tải các món quà phụ kiện (Ốp lưng, Cáp sạc, Pin dự phòng, Tai nghe...) kèm hình ảnh (imageUrl) và mô tả.  
  * Cài đặt số điểm cần thiết để đổi món quà đó (pointsCost) và quy định hạng thành viên tối thiểu được phép đổi (minTier từ 0 đến 3).  
  * Quản lý trạng thái tồn kho quà tặng (inStock): Cập nhật số lượng quà có sẵn hoặc gạt nút báo Hết quà (inStock \= false) để khóa nút đổi quà trên ứng dụng của khách khi quà trong kho đã phát hết.  
*  **Tra cứu mã nhận quà tại quầy (Khớp với thiết kế Terminal Claim của Backend):**  
  * Khi khách hàng mang mã nhận quà trên điện thoại (VD: GIFT-G53Y-ZDS3) ra cửa hàng, Admin/Nhân viên gõ mã này vào ô tra cứu trên Web CMS.  
  * Màn hình hiển thị ngay thông tin chi tiết của lượt đổi quà (mã đổi quà code, tên món quà giftName, số điểm đã dùng pointsSpent, tên khách hàng và thời điểm đổi claimedAt) để nhân viên kiểm tra và giao quà cho khách tại quầy.  
* **Điều chỉnh chính sách Hạng thành viên:**  
  * Thay đổi mốc điểm yêu cầu (thresholdPoints) để lên hạng (Tier 1, Tier 2, Tier 3\) và mức phần trăm ưu đãi (discountPercent) của từng hạng thành viên khi thay đổi chính sách.

## **6\. Quản lý Khách hàng & Đánh giá (Customers & Reviews)**

Nơi Admin hỗ trợ người dùng và kiểm duyệt cộng đồng mua sắm:

*  **Tra cứu hồ sơ khách hàng (Chế độ Read-Only & Quản lý trạng thái):**  
  * Tìm kiếm khách hàng theo Email, Tên hoặc Số điện thoại.  
  * Click vào một khách hàng để xem toàn bộ lịch sử mua sắm, số điểm tích lũy hiện có (lifetimePoints, balance), hạng thành viên hiện tại (tier) và danh sách địa chỉ của họ nhằm hỗ trợ khi khách khiếu nại.  
  * Quy tắc bảo mật dữ liệu khách hàng: Admin không được phép tạo mới tài khoản khách hàng hay tự ý chỉnh sửa thông tin cá nhân (Họ tên, SĐT, Email, Địa chỉ) của khách có sẵn. Admin chỉ được quyền gạt nút Khóa / Mở khóa tài khoản đối với các user có hành vi spam đơn hàng ảo.  
*  **Theo dõi đánh giá sản phẩm (Reviews):**  
  * Xem danh sách các lượt chấm sao (rating 1–5) và bình luận (comment) của khách hàng sau khi mua.  
  * Lọc nhanh các đánh giá 1–2 sao để bộ phận chăm sóc khách hàng nắm bắt vấn đề chất lượng sản phẩm và liên hệ hỗ trợ kịp thời (không chỉnh sửa hoặc xóa tạm thời chưa làm chức năng reply comment).

## **7\. Quản lý Nhân sự & Phân quyền (Staff & RBAC)**

Nơi Super Admin quản trị đội ngũ vận hành nội bộ, phân tách quyền hạn theo đúng vị trí công việc và giám sát an toàn hệ thống:

*  **Quản lý Tài khoản Nhân viên & Quy trình cấp quyền:**  
  * Khởi tạo tài khoản Super Admin gốc: Hệ thống Web Admin không có trang tự đăng ký công khai. Tài khoản Super Admin đầu tiên được tạo sẵn trong cơ sở dữ liệu (Database Seeding) khi cài đặt hệ thống.  
  * Quy trình 4 bước tạo mới và phân quyền cho Staff:  
    * Bước 1: Super Admin đăng nhập, truy cập menu Quản lý Nhân sự và bấm "Thêm nhân viên mới".  
    * Bước 2: Điền thông tin nhân viên (Họ tên, Email công việc, Số điện thoại, Mật khẩu khởi tạo) và chọn gán ngay Vai trò (Role) từ danh sách chọn (Dropdown) ngay trên form.  
    * Bước 3: Bấm Lưu để kích hoạt tài khoản ở trạng thái Đang hoạt động (ACTIVE) và cấp thông tin đăng nhập cho nhân viên.  
    * Bước 4: Nhân viên đăng nhập vào Web CMS, hệ thống tự động kiểm tra Role đã gán để ẩn/hiện các menu đúng với quyền hạn.  
  * Cơ chế Khóa / Mở khóa tài khoản khi nghỉ việc hoặc chuyển bộ phận:  
    * Khi nhân viên nghỉ việc: Super Admin gạt nút Khóa tài khoản (LOCKED) ngay lập tức thay vì xóa cứng khỏi Database. Nhân viên bị đăng xuất và chặn truy cập ngay lập tức, nhưng toàn bộ lịch sử đơn hàng/kho bãi người đó từng xử lý vẫn được bảo toàn.  
    * Khi nhân viên chuyển bộ phận: Super Admin mở thông tin tài khoản nhân viên đó và đổi sang Vai trò (Role) mới phù hợp với vị trí công việc mới (hoặc cấp lại mật khẩu Reset Password khi nhân viên quên).  
*  **Thiết lập Vai trò & Phân quyền (Role-Based Access Control \- RBAC):**  
  * Quyền tùy chỉnh phân quyền: Chỉ duy nhất role Super Admin mới có quyền truy cập phân hệ này để tạo tài khoản nhân viên và thay đổi phân quyền.  
  * Định nghĩa 3 nhóm quyền hạn rõ ràng để nhân viên chỉ nhìn thấy và thao tác được trên các menu được cấp phép:  
    * **Super Admin (Quản trị viên cao cấp):** Toàn quyền truy cập tất cả phân hệ, xem báo cáo doanh thu, cấu hình chính sách Hạng thành viên và quản lý nhân sự.  
    * **Nhân viên Kho & Sản phẩm (Inventory/Catalog Staff):** Chỉ được quyền thêm/sửa sản phẩm, cập nhật thông số và nhập/kiểm kho.  
    * **Nhân viên Điều phối & Quầy (Order/Counter Staff):** Chỉ được quyền xử lý đơn hàng, in phiếu giao hàng, tra cứu mã nhận quà tại quầy và xem hồ sơ/đánh giá khách hàng để hỗ trợ (không được xem tổng doanh thu hay tự ý sửa giá sản phẩm, sửa mốc điểm thành viên).

