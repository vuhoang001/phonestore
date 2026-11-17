# Hướng dẫn sử dụng PhoneStore

Tài liệu này hướng dẫn các chức năng khách hàng và quản trị viên **theo từng bước, mỗi bước có ảnh minh họa riêng**. Ảnh được chụp tự động bằng Playwright trên bản đang chạy (`http://localhost:5174`) nên phản ánh đúng giao diện thực tế. Hướng dẫn giữ nguyên IMEI trong quy trình bán hàng và bảo hành.

## 📑 Mục lục nhanh

Bấm vào tên chức năng để nhảy thẳng tới phần hướng dẫn tương ứng.

**▶ Bắt đầu:** [1. Khởi động và đăng nhập](#1-khởi-động-và-đăng-nhập)

| 👤 Luồng khách hàng | 🛠️ Luồng quản trị viên |
|---|---|
| [2.1. Duyệt, tìm và lọc điện thoại](#21-duyệt-tìm-và-lọc-điện-thoại) | [3.1. Vào trang quản trị](#31-vào-trang-quản-trị) |
| [2.2. Xem chi tiết và chọn biến thể](#22-xem-chi-tiết-và-chọn-biến-thể) | [3.2. Dashboard và báo cáo](#32-dashboard-và-báo-cáo) |
| [2.3. So sánh điện thoại](#23-so-sánh-điện-thoại) | [3.3. Quản lý thương hiệu và danh mục](#33-quản-lý-thương-hiệu-và-danh-mục) |
| [2.4. Giỏ hàng](#24-giỏ-hàng) | [3.4. Quản lý sản phẩm, biến thể và tồn kho](#34-quản-lý-sản-phẩm-biến-thể-và-tồn-kho) |
| [2.5. Đặt hàng và thanh toán](#25-đặt-hàng-và-thanh-toán) | [3.5. Xử lý đơn hàng và IMEI](#35-xử-lý-đơn-hàng-và-imei) |
| [2.6. Theo dõi đơn hàng, hủy và mua lại](#26-theo-dõi-đơn-hàng-hủy-và-mua-lại) | [3.6. Cấu hình vận chuyển](#36-cấu-hình-vận-chuyển) |
| [2.7. Thanh toán lại và trả góp](#27-thanh-toán-lại-và-trả-góp) | [3.7. Coupon/khuyến mãi mã giảm giá](#37-couponkhuyến-mãi-mã-giảm-giá) |
| [2.8. Danh sách yêu thích và thông báo](#28-danh-sách-yêu-thích-và-thông-báo) | [3.8. Flash sale](#38-flash-sale) |
| [2.9. Tài khoản và mật khẩu](#29-tài-khoản-và-mật-khẩu) | [3.9. Yêu cầu thu cũ đổi mới](#39-yêu-cầu-thu-cũ-đổi-mới) |
| [2.10. Tra cứu bảo hành bằng IMEI hoặc mã đơn](#210-tra-cứu-bảo-hành-bằng-imei-hoặc-mã-đơn) | [3.10. Nhật ký hoạt động](#310-nhật-ký-hoạt-động) |
| [2.11. Thu cũ đổi mới](#211-thu-cũ-đổi-mới) | |
| [2.12. Trang hỗ trợ và chính sách](#212-trang-hỗ-trợ-và-chính-sách) | |

**▶ Khác:** [Trạng thái kiểm chứng](#trạng-thái-kiểm-chứng) · [4. Tình trạng tích hợp và giới hạn demo](#4-tình-trạng-tích-hợp-và-giới-hạn-demo) · [5. Ghi chú về ảnh minh họa](#5-ghi-chú-về-ảnh-minh-họa)

---

## Trạng thái kiểm chứng

- **Đã chạy qua trình duyệt:** toàn bộ ảnh trong tài liệu được chụp trực tiếp từ các thao tác thật trên web.
- Bản chạy hiện dùng thanh toán **mô phỏng**; thao tác này không chuyển tiền thật. Email mặc định được ghi log nếu chưa cấu hình SMTP.
- Một vài bước là thao tác hạ tầng (cài Docker, chạy compose) **không có giao diện** nên không kèm ảnh — được ghi chú rõ.
- Tài khoản demo: admin `admin@phone.com` / `Admin@123` · khách `customer@phone.com` / `Customer@123`.

---

## 1. Khởi động và đăng nhập

1. Cài Docker Desktop hoặc Docker Engine và Docker Compose. _(Bước hạ tầng — thao tác dòng lệnh, không có giao diện.)_
2. Tại thư mục dự án, sao chép `.env.example` thành `.env` nếu chưa có. Chỉ điền thông tin dịch vụ khi cần; không đưa secret thật vào tài liệu hoặc Git. _(Bước hạ tầng — không có giao diện.)_
3. Chạy `docker compose up -d --build`. _(Bước hạ tầng — không có giao diện.)_
4. Mở website tại `http://localhost:5174`; Swagger API mặc định ở `http://localhost:8081/swagger`.

![Trang chủ PhoneStore sau khi mở website](images/huong-dan/s1/step4.png)

5. Đăng nhập tài khoản admin demo: `admin@phone.com` / `Admin@123` (hoặc khách `customer@phone.com` / `Customer@123`). Nếu mật khẩu đã đổi qua `.env`, dùng thông tin cấu hình hiện tại.

![Màn hình đăng nhập với hai trường email và mật khẩu](images/huong-dan/s1/step5.png)

6. Để tạo tài khoản khách, vào **Đăng ký**, điền họ tên, email, số điện thoại và mật khẩu rồi gửi biểu mẫu. Nếu bật xác nhận email, mở thư xác nhận trước khi đăng nhập.

![Biểu mẫu đăng ký tài khoản khách](images/huong-dan/s1/step6.png)

---

## 2. Luồng khách hàng

### 2.1. Duyệt, tìm và lọc điện thoại

1. Mở **Sản phẩm** hoặc trang chủ — danh sách hiện lưới sản phẩm và bộ lọc thương hiệu/dung lượng/danh mục/khoảng giá bên trái.

![Danh sách sản phẩm với bộ lọc bên trái](images/huong-dan/s2-1/step1.png)

2. Nhập tên máy/nhãn hiệu vào ô tìm kiếm (ví dụ `iPhone`).

![Tìm kiếm từ khóa iPhone](images/huong-dan/s2-1/step2.png)

3. Chọn bộ lọc thương hiệu (ví dụ Samsung) — kết quả thu hẹp theo hãng, chip lọc hiển thị ngay trên danh sách.

![Lọc theo thương hiệu Samsung](images/huong-dan/s2-1/step3.png)

4. Sắp xếp danh sách theo lựa chọn hiển thị (ví dụ "Bán chạy").

![Sắp xếp theo Bán chạy](images/huong-dan/s2-1/step4.png)

5. Chọn một thẻ sản phẩm để mở trang chi tiết; kiểm tra giá, tình trạng hàng, khuyến mãi, cấu hình và mô tả.

![Mở trang chi tiết sản phẩm](images/huong-dan/s2-1/step5.png)

**Lưu ý đã quan sát:** bộ lọc **dung lượng** (64GB, 128GB...) hiện có thể trả danh sách trống vì dữ liệu seed không gắn dung lượng vào bộ lọc theo biến thể. Bộ lọc từ 4 sao cũng có thể trả 0 với dữ liệu hiện có.

### 2.2. Xem chi tiết và chọn biến thể

1. Mở sản phẩm từ danh sách — trang chi tiết hiện ảnh, giá, lựa chọn màu/dung lượng, tồn kho và các nút mua.

![Trang chi tiết sản phẩm](images/huong-dan/s2-2/step1.png)

2. Chọn **màu** và **dung lượng** nếu sản phẩm có biến thể.

![Chọn biến thể màu và dung lượng](images/huong-dan/s2-2/step2.png)

3. Kiểm tra giá và tồn kho thay đổi theo biến thể đã chọn (biến thể hết hàng bị khóa, không chọn được).

![Biến thể hết hàng bị khóa](images/huong-dan/s2-2/step3.png)

4. Xem **thông số kỹ thuật** gom nhóm (màn hình, chip, camera, pin...), mô tả, đánh giá và sản phẩm liên quan.

![Bảng thông số kỹ thuật](images/huong-dan/s2-2/step4.png)

5. Chọn **Thêm vào giỏ** để mua sau hoặc **Mua ngay** để sang bước mua hàng.

![Khu vực mua hàng với nút Thêm vào giỏ và Mua ngay](images/huong-dan/s2-2/step5.png)

### 2.3. So sánh điện thoại

1. Tại danh sách hoặc trang chi tiết, bấm nút **thêm vào so sánh** trên các máy muốn đối chiếu — thanh so sánh hiện ở dưới (ví dụ "So sánh 2/4").

![Thêm máy vào so sánh](images/huong-dan/s2-3/step1.png)

2. Mở **So sánh** — bảng thông số hai máy đặt cạnh nhau.

![Bảng so sánh hai máy](images/huong-dan/s2-3/step2.png)

3. Cuộn bảng để đọc thêm các thông số (màn hình, danh mục, trả góp...).

![Cuộn bảng so sánh xem thêm thông số](images/huong-dan/s2-3/step3.png)

4. Xóa máy khỏi danh sách so sánh khi không cần nữa — bảng trống hiện gợi ý khám phá sản phẩm.

![Danh sách so sánh sau khi xóa máy](images/huong-dan/s2-3/step4.png)

### 2.4. Giỏ hàng

1. Mở biểu tượng giỏ hàng hoặc đường dẫn `/cart` — xem các dòng hàng và tóm tắt đơn.

![Giỏ hàng với các dòng sản phẩm](images/huong-dan/s2-4/step1.png)

2. Đánh dấu từng dòng hàng hoặc chọn tất cả sản phẩm muốn đặt.

![Tick chọn dòng hàng](images/huong-dan/s2-4/step2.png)

3. Điều chỉnh số lượng bằng nút tăng/giảm; số lượng không được vượt quá tồn kho.

![Chỉnh số lượng bằng nút tăng/giảm](images/huong-dan/s2-4/step3.png)

4. Xóa mặt hàng không còn cần bằng nút thùng rác đỏ trên dòng.

![Nút xóa mặt hàng](images/huong-dan/s2-4/step4.png)

5. Kiểm tra tạm tính rồi chọn tiếp tục thanh toán.

![Tóm tắt tạm tính và nút mua hàng](images/huong-dan/s2-4/step5.png)

Giỏ hàng khách vãng lai được lưu ở trình duyệt; sau khi đăng nhập, giỏ có thể được đồng bộ với tài khoản.

### 2.5. Đặt hàng và thanh toán

1. Đăng nhập hoặc đăng ký tài khoản — trang thanh toán yêu cầu đăng nhập.

![Yêu cầu đăng nhập trước khi thanh toán](images/huong-dan/s2-5/step1.png)

2. Tại `/checkout`, chọn hoặc nhập **địa chỉ nhận hàng**.

![Chọn địa chỉ giao hàng](images/huong-dan/s2-5/step2.png)

3. Chọn **phương thức giao hàng** đang khả dụng.

![Chọn phương thức vận chuyển](images/huong-dan/s2-5/step3.png)

4. Nhập **mã giảm giá** nếu có rồi áp dụng (ví dụ `WELCOME10`); kiểm tra điều kiện tối thiểu và số tiền giảm.

![Áp mã giảm giá WELCOME10](images/huong-dan/s2-5/step4.png)

5. Chọn **COD**, **VNPay** hoặc **trả góp** nếu tùy chọn được cung cấp cho đơn.

![Chọn phương thức thanh toán COD](images/huong-dan/s2-5/step5.png)

6. Với **trả góp**, chọn kỳ hạn hiển thị (ví dụ 6/9/12 tháng) và xem số tiền dự kiến mỗi kỳ.

![Chọn trả góp và xem kỳ hạn](images/huong-dan/s2-5/step6.png)

7. Ghi chú cho cửa hàng nếu cần, rà soát sản phẩm, địa chỉ, phí ship và tổng tiền.

![Rà soát toàn bộ đơn trước khi đặt](images/huong-dan/s2-5/step7.png)

8. Gửi đặt hàng — màn xác nhận hiện **mã đơn**; ghi lại để tra cứu.

![Màn xác nhận đơn với mã đơn](images/huong-dan/s2-5/step8.png)

9. Với COD, chờ cửa hàng xác nhận và giao hàng. Với VNPay trong môi trường demo, làm theo màn hình mô phỏng; không coi đây là giao dịch tiền thật.

![Chi tiết đơn vừa đặt với lịch sử trạng thái](images/huong-dan/s2-5/step9.png)

### 2.6. Theo dõi đơn hàng, hủy và mua lại

1. Đăng nhập rồi mở **Đơn hàng của tôi** (`/orders`).

![Danh sách đơn hàng của tôi](images/huong-dan/s2-6/step1.png)

2. Lọc hoặc chọn đơn cần xem.

![Mở một đơn để xem chi tiết](images/huong-dan/s2-6/step2.png)

3. Mở chi tiết để xem sản phẩm, thanh toán, địa chỉ và lịch sử trạng thái.

![Chi tiết đơn hàng đầy đủ](images/huong-dan/s2-6/step3.png)

4. Đơn đang **chờ** hoặc **đã xác nhận** có thể hiện thao tác **hủy** nếu chính sách cho phép (đơn đã hoàn tất không có nút hủy).

![Đơn đã hoàn tất không còn nút hủy](images/huong-dan/s2-6/step4.png)

5. Với đơn đã hoàn tất hoặc đã hủy, dùng **Mua lại** nếu nút này khả dụng để đưa sản phẩm trở lại giỏ.

![Nút Mua lại trên đơn đã hoàn tất](images/huong-dan/s2-6/step5.png)

6. Khi đơn đã giao/xuất kho, nhân viên gán IMEI/serial thiết bị; khách có thể xem thông tin IMEI trong chi tiết đơn theo trạng thái và quyền hiển thị.

![Chi tiết đơn hoàn tất — nơi hiển thị IMEI khi đã gán](images/huong-dan/s2-6/step6.png)

### 2.7. Thanh toán lại và trả góp

1. Mở đơn chưa thanh toán từ trang chi tiết.

![Chi tiết đơn hàng](images/huong-dan/s2-7/step1.png)

2. Nếu thanh toán VNPay còn hiệu lực, chọn thanh toán và hoàn tất bước được yêu cầu. _(Đơn COD không có nút thanh toán lại — cần đơn VNPay chưa thanh toán mới hiện nút này.)_

![Đơn COD không có nút thanh toán lại VNPay](images/huong-dan/s2-7/step2.png)

3. Để xem các khoản **trả góp**, đăng nhập và mở `/installments` — danh sách các kỳ, kỳ đã trả và kỳ cần thanh toán.

![Trang Trả góp của tôi với lịch các kỳ](images/huong-dan/s2-7/step3.png)

4. Chọn **kỳ** cần xử lý và bấm **Thanh toán**.

![Chọn kỳ cần thanh toán](images/huong-dan/s2-7/step4.png)

5. Làm theo hướng dẫn trên màn hình để hoàn tất kỳ. Môi trường hiện tại là luồng **mô phỏng** — không dùng kết quả mô phỏng làm bằng chứng đã thu tiền thật.

![Xác nhận thanh toán kỳ trả góp](images/huong-dan/s2-7/step5.png)

### 2.8. Danh sách yêu thích và thông báo

1. Đăng nhập — trang chủ sau đăng nhập.

![Trang chủ sau khi đăng nhập](images/huong-dan/s2-8/step1.png)

2. Thêm sản phẩm vào **yêu thích** từ thẻ sản phẩm hoặc trang chi tiết (nút trái tim) — hiện thông báo "Đã cập nhật yêu thích".

![Thêm sản phẩm vào yêu thích](images/huong-dan/s2-8/step2.png)

3. Mở `/wishlist` để xem, bỏ yêu thích hoặc quay lại sản phẩm.

![Danh sách yêu thích](images/huong-dan/s2-8/step3.png)

4. Mở `/notifications` để đọc thông báo; trạng thái đã đọc được cập nhật khi chọn thông báo.

![Danh sách thông báo](images/huong-dan/s2-8/step4.png)

### 2.9. Tài khoản và mật khẩu

1. Mở **Tài khoản** (`/account`) khi đã đăng nhập.

![Trang tài khoản — hồ sơ và sổ địa chỉ](images/huong-dan/s2-9/step1.png)

2. Cập nhật thông tin cá nhân hoặc **thêm địa chỉ** theo các trường trên trang rồi lưu.

![Form thêm địa chỉ mới](images/huong-dan/s2-9/step2.png)

3. Đăng xuất bằng nút tài khoản ở góc trên.

![Menu tài khoản với tùy chọn đăng xuất](images/huong-dan/s2-9/step3.png)

4. Nếu quên mật khẩu, mở **Quên mật khẩu** (`/forgot-password`), gửi email đăng ký.

![Trang quên mật khẩu](images/huong-dan/s2-9/step4.png)

5. Dùng liên kết đặt lại được gửi tới email. Nếu chưa xác nhận email, mở liên kết xác nhận trong thư.

![Xác nhận đã gửi email đặt lại mật khẩu](images/huong-dan/s2-9/step5.png)

### 2.10. Tra cứu bảo hành bằng IMEI hoặc mã đơn

1. Mở **Tra cứu bảo hành** (`/warranty`) — có hai tab: *Theo IMEI* và *Theo mã đơn*.

![Biểu mẫu tra cứu bảo hành](images/huong-dan/s2-10/step1.png)

2. Nhập **IMEI** của máy (hoặc chuyển tab để nhập mã đơn).

![Nhập IMEI vào ô tra cứu](images/huong-dan/s2-10/step2.png)

3. Bấm **Tra cứu** — phiếu bảo hành hiện ngay trong trang (model, IMEI, mã đơn, trạng thái còn/hết hạn).

![Kết quả phiếu bảo hành theo IMEI](images/huong-dan/s2-10/step3.png)

4. Đối chiếu model, mã đơn, ngày bắt đầu/kết thúc và trạng thái bảo hành được trả về.

![Thẻ bảo hành với đầy đủ thông tin](images/huong-dan/s2-10/step4.png)

5. Nếu tra theo **mã đơn** có nhiều thiết bị, kiểm tra từng bản ghi/IMEI trong kết quả.

![Tra theo mã đơn trả về nhiều thiết bị](images/huong-dan/s2-10/step5.png)

6. Nếu không có kết quả, màn hình báo "Không tìm thấy" — kiểm tra lại ký tự IMEI/mã đơn và liên hệ cửa hàng.

![Trạng thái không tìm thấy phiếu bảo hành](images/huong-dan/s2-10/step6.png)

IMEI được giữ trong hồ sơ thiết bị và quy trình bảo hành; không bỏ qua bước gán IMEI khi hoàn tất đơn.

### 2.11. Thu cũ đổi mới

1. Mở `/trade-in` — biểu mẫu khai máy cũ.

![Biểu mẫu thu cũ đổi mới](images/huong-dan/s2-11/step1.png)

2. Đăng nhập nếu được yêu cầu (nếu đã đăng nhập, biểu mẫu giữ nguyên).

![Biểu mẫu thu cũ — đã đăng nhập](images/huong-dan/s2-11/step2.png)

3. Điền **model máy cũ**, **tình trạng** và **ghi chú**.

![Điền thông tin máy cũ](images/huong-dan/s2-11/step3.png)

4. Gửi yêu cầu để cửa hàng tiếp nhận định giá.

![Gửi yêu cầu thu cũ thành công](images/huong-dan/s2-11/step4.png)

5. Theo dõi phản hồi từ cửa hàng qua trạng thái yêu cầu/thông báo.

![Trạng thái yêu cầu thu cũ](images/huong-dan/s2-11/step5.png)

### 2.12. Trang hỗ trợ và chính sách

1. Ở **chân trang** (footer) mọi trang có các liên kết: Trung tâm trợ giúp, Hướng dẫn mua hàng, Trả hàng & hoàn tiền, Tra cứu bảo hành, Giới thiệu, Điều khoản, Chính sách bảo mật, Liên hệ.

![Footer với các liên kết hỗ trợ và chính sách](images/huong-dan/s2-12/step1.png)

2. Mở **Chính sách bảo hành** (`/bao-hanh`) để đọc nội dung chính sách.

![Trang chính sách bảo hành](images/huong-dan/s2-12/step2.png)

3. Mở **Trả hàng & hoàn tiền** (`/doi-tra`) để xem điều kiện đổi trả.

![Trang trả hàng và hoàn tiền](images/huong-dan/s2-12/step3.png)

4. Mở **Liên hệ** (`/lien-he`) để lấy email, hotline và địa chỉ hỗ trợ.

![Trang liên hệ hợp tác](images/huong-dan/s2-12/step4.png)

---

## 3. Luồng quản trị viên

### 3.1. Vào trang quản trị

1. Đăng nhập bằng tài khoản có quyền admin.

![Đăng nhập tài khoản admin](images/huong-dan/s3-1/step1.png)

2. Mở `/admin` hoặc chọn **Quản trị** — vào trang dashboard quản trị.

![Trang dashboard quản trị](images/huong-dan/s3-1/step2.png)

3. Nếu tài khoản không có quyền admin, ứng dụng chuyển về trang khách hàng (ảnh minh họa trạng thái có quyền, vào được admin).

![Vào được trang quản trị với tài khoản có quyền](images/huong-dan/s3-1/step3.png)

4. Dùng **thanh điều hướng quản trị** để mở dashboard, báo cáo, sản phẩm, thương hiệu, danh mục, đơn hàng, vận chuyển, coupon, flash sale, thu cũ đổi mới và nhật ký.

![Thanh điều hướng quản trị](images/huong-dan/s3-1/step4.png)

### 3.2. Dashboard và báo cáo

1. Mở **Dashboard** để xem các số liệu tóm tắt.

![Dashboard với các thẻ số liệu](images/huong-dan/s3-2/step1.png)

2. Mở **Báo cáo** (`/admin/reports`).

![Trang báo cáo](images/huong-dan/s3-2/step2.png)

3. Chọn khoảng thời gian/bộ lọc nếu có.

![Chọn bộ lọc trên trang báo cáo](images/huong-dan/s3-2/step3.png)

4. Đọc các biểu đồ và số liệu doanh thu, đơn hàng, sản phẩm hoặc khách hàng.

![Biểu đồ và số liệu báo cáo](images/huong-dan/s3-2/step4.png)

5. Đối chiếu báo cáo với trạng thái đơn đã cập nhật; dữ liệu demo có thể không đại diện cho giao dịch thật.

![Phần số liệu bổ sung ở cuối trang báo cáo](images/huong-dan/s3-2/step5.png)

### 3.3. Quản lý thương hiệu và danh mục

1. Mở **Thương hiệu** hoặc **Danh mục** trong admin.

![Danh sách thương hiệu](images/huong-dan/s3-3/step1.png)

2. Xem danh sách hiện có; dùng tìm kiếm/lọc nếu giao diện cung cấp.

![Danh sách thương hiệu đầy đủ](images/huong-dan/s3-3/step2.png)

3. Chọn **Thêm mới**, điền tên và thông tin bắt buộc rồi lưu.

![Form thêm thương hiệu mới](images/huong-dan/s3-3/step3.png)

4. Dùng thao tác **sửa** để cập nhật thông tin.

![Form sửa thương hiệu](images/huong-dan/s3-3/step4.png)

5. **Xóa** chỉ khi không còn sản phẩm/phụ thuộc liên quan; đọc xác nhận trước khi hoàn tất.

![Hộp xác nhận xóa](images/huong-dan/s3-3/step5.png)

### 3.4. Quản lý sản phẩm, biến thể và tồn kho

1. Mở **Sản phẩm** (`/admin/products`).

![Danh sách sản phẩm quản trị](images/huong-dan/s3-4/step1.png)

2. Chọn **thêm mới** hoặc mở sản phẩm cần sửa.

![Form thêm/sửa sản phẩm](images/huong-dan/s3-4/step2.png)

3. Nhập tên, slug, thương hiệu, danh mục, mô tả, giá, giá khuyến mãi và trạng thái hiển thị.

![Nhập thông tin cơ bản của sản phẩm](images/huong-dan/s3-4/step3.png)

4. Khai báo **biến thể** theo màu/dung lượng và tồn kho tương ứng.

![Khai báo biến thể màu/dung lượng/tồn kho](images/huong-dan/s3-4/step4.png)

5. Bổ sung **thông số kỹ thuật** và **ảnh** nếu biểu mẫu có hỗ trợ.

![Phần thông số kỹ thuật và ảnh](images/huong-dan/s3-4/step5.png)

6. Lưu rồi kiểm tra sản phẩm ở giao diện khách hàng.

![Danh sách sản phẩm sau khi lưu](images/huong-dan/s3-4/step6.png)

7. Khi xuất bán máy cụ thể, dùng quy trình đơn hàng để gán IMEI/serial cho đúng thiết bị; không dùng chung một IMEI cho nhiều máy.

![Chi tiết sản phẩm trong quản trị](images/huong-dan/s3-4/step7.png)

### 3.5. Xử lý đơn hàng và IMEI

1. Mở **Đơn hàng** (`/admin/orders`) và lọc đơn theo trạng thái/mã đơn.

![Lọc đơn hàng theo trạng thái](images/huong-dan/s3-5/step1.png)

2. Mở chi tiết đơn, đối chiếu mặt hàng, số lượng, địa chỉ và phương thức thanh toán.

![Chi tiết đơn hàng](images/huong-dan/s3-5/step2.png)

3. **Xác nhận** đơn khi đã kiểm tra tồn kho và thanh toán/điều kiện COD.

![Xác nhận đơn hàng](images/huong-dan/s3-5/step3.png)

4. Trong bước chuẩn bị giao hàng, **gán IMEI/serial** duy nhất cho từng thiết bị điện thoại theo đơn.

![Nhập IMEI cho thiết bị trong đơn](images/huong-dan/s3-5/step4.png)

5. Kiểm tra IMEI gắn đúng sản phẩm/biến thể và không bị trùng.

![Kiểm tra IMEI đã gán](images/huong-dan/s3-5/step5.png)

6. Cập nhật lần lượt trạng thái xử lý/giao hàng theo các thao tác được phép.

![Cập nhật trạng thái giao hàng](images/huong-dan/s3-5/step6.png)

7. Khi hoàn tất, xác nhận đơn; kiểm tra IMEI và phiếu bảo hành được lưu để khách tra cứu.

![Hoàn tất đơn hàng](images/huong-dan/s3-5/step7.png)

8. Chỉ hủy hoặc thay đổi trạng thái theo quy trình cửa hàng; các thay đổi được ghi vào nhật ký nếu bật audit.

![Danh sách đơn sau khi xử lý](images/huong-dan/s3-5/step8.png)

> Ghi chú: để thao tác xác nhận/gán IMEI hiển thị đầy đủ, đơn cần ở trạng thái phù hợp (ví dụ đã xác nhận, đang chuẩn bị giao). IMEI minh họa chỉ dùng để chụp, chưa ghi vào DB nếu không bấm lưu.

### 3.6. Cấu hình vận chuyển

1. Mở **Vận chuyển** (`/admin/shipping`).

![Danh sách phương thức vận chuyển](images/huong-dan/s3-6/step1.png)

2. Xem các phương thức, khu vực, mức phí và điều kiện đang thiết lập.

![Chi tiết phương thức, phí và điều kiện](images/huong-dan/s3-6/step2.png)

3. Thêm hoặc chỉnh sửa phương thức/phí theo các trường trên biểu mẫu.

![Form thêm/sửa phương thức vận chuyển](images/huong-dan/s3-6/step3.png)

4. Lưu và kiểm tra lựa chọn/phí hiển thị tại checkout.

![Danh sách sau khi lưu](images/huong-dan/s3-6/step4.png)

### 3.7. Coupon/khuyến mãi mã giảm giá

1. Mở **Coupons** (`/admin/coupons`).

![Danh sách mã giảm giá](images/huong-dan/s3-7/step1.png)

2. Tạo mã, mô tả, loại giảm (phần trăm hoặc số tiền), mức giảm, đơn tối thiểu, thời hạn và giới hạn sử dụng.

![Form tạo mã giảm giá](images/huong-dan/s3-7/step2.png)

3. Điền đủ thông tin rồi lưu coupon.

![Form coupon đã điền thông tin](images/huong-dan/s3-7/step3.png)

4. Kiểm tra trạng thái hoạt động trong danh sách.

![Kiểm tra coupon trong danh sách](images/huong-dan/s3-7/step4.png)

5. Khi hết hạn hoặc cần dừng, **sửa** trạng thái hoặc thời hạn theo lựa chọn của giao diện.

![Form sửa coupon](images/huong-dan/s3-7/step5.png)

### 3.8. Flash sale

1. Mở **Flash Sale** (`/admin/flash-sale`).

![Danh sách chương trình flash sale](images/huong-dan/s3-8/step1.png)

2. Chọn sản phẩm/biến thể cho chương trình.

![Chọn sản phẩm cho flash sale](images/huong-dan/s3-8/step2.png)

3. Nhập giá sale và số lượng.

![Nhập giá sale và số lượng](images/huong-dan/s3-8/step3.png)

4. Đặt thời gian bắt đầu/kết thúc.

![Đặt thời gian chương trình](images/huong-dan/s3-8/step4.png)

5. Lưu chương trình; kiểm tra giá và nhãn sale ở danh sách/chi tiết sản phẩm trong thời gian hiệu lực, theo dõi tồn kho để tránh bán vượt lượng.

![Danh sách flash sale sau khi lưu](images/huong-dan/s3-8/step5.png)

### 3.9. Yêu cầu thu cũ đổi mới

1. Mở **Thu cũ đổi mới** (`/admin/trade-in`).

![Danh sách yêu cầu thu cũ](images/huong-dan/s3-9/step1.png)

2. Chọn yêu cầu mới và đọc model, tình trạng, ghi chú của khách.

![Chi tiết yêu cầu thu cũ](images/huong-dan/s3-9/step2.png)

3. Nhập **giá định giá** hoặc thông tin đề nghị theo biểu mẫu.

![Nhập giá định giá](images/huong-dan/s3-9/step3.png)

4. Chuyển trạng thái sang bước xử lý phù hợp và lưu.

![Chọn trạng thái xử lý](images/huong-dan/s3-9/step4.png)

5. Theo dõi các yêu cầu đến khi hoàn tất/từ chối.

![Theo dõi danh sách yêu cầu thu cũ](images/huong-dan/s3-9/step5.png)

### 3.10. Nhật ký hoạt động

1. Mở **Audit logs** (`/admin/audit-logs`).

![Danh sách nhật ký hoạt động](images/huong-dan/s3-10/step1.png)

2. Tìm theo loại thao tác, người thực hiện hoặc khoảng thời gian nếu có bộ lọc.

![Bộ lọc nhật ký](images/huong-dan/s3-10/step2.png)

3. Mở bản ghi để xem đối tượng và thay đổi được ghi nhận.

![Chi tiết một bản ghi nhật ký](images/huong-dan/s3-10/step3.png)

4. Dùng nhật ký để đối soát thao tác quản trị; không coi log là thay thế cho chứng từ thanh toán.

![Tổng quan nhật ký để đối soát](images/huong-dan/s3-10/step4.png)

---

## 4. Tình trạng tích hợp và giới hạn demo

- VNPay trong cấu hình mẫu dùng sandbox/placeholder; thanh toán mock chỉ kiểm thử giao diện.
- Email mặc định dùng provider `Log`; xác nhận tài khoản/đặt lại mật khẩu cần cấu hình SMTP để nhận thư thật.
- MinIO phục vụ lưu ảnh; kiểm tra endpoint và credential cấu hình trước khi dùng upload thật.
- Không nhập thông tin thẻ thật hoặc dữ liệu khách hàng thật vào môi trường demo.

## 5. Ghi chú về ảnh minh họa

- Toàn bộ ảnh trong tài liệu được chụp bằng **Playwright** trên bản đang chạy (`http://localhost:5174`) với dữ liệu seed, lưu tại `docs/images/huong-dan/s<mục>/stepN.png`.
- Một số luồng cần dữ liệu/điều kiện cụ thể mới hiện đầy đủ (ví dụ: nút thanh toán lại VNPay cần đơn VNPay chưa thanh toán; thao tác xác nhận/gán IMEI cần đơn ở trạng thái phù hợp). Các chỗ này được ghi chú ngay trong bước tương ứng.
- Thao tác tạo/sửa ở phần quản trị chỉ chụp minh họa biểu mẫu, không ghi bản ghi rác vào dữ liệu seed.
