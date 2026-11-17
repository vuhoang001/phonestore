# Tài liệu kiến trúc — [Tên hệ thống]

> Khung arc42 được điều chỉnh theo dự án. Xóa hướng dẫn này và bỏ các mục không hữu ích cho người đọc. Không để lại văn bản mẫu hoặc chỗ trống trong tài liệu hoàn chỉnh.

**Trạng thái:** Current / Target / Proposed
**Phạm vi:** [hệ thống/repository và nội dung được mô tả]
**Người chịu trách nhiệm / người quyết định:** [vai trò hoặc Chưa xác định]
**Rà soát lần cuối:** [YYYY-MM-DD]

## 1. Giới thiệu và mục tiêu

Mô tả mục đích và ranh giới hệ thống. Nêu các bên liên quan và những mục tiêu chất lượng ảnh hưởng đến các lựa chọn kiến trúc chính.

## 2. Ràng buộc

Ghi các ràng buộc ảnh hưởng đáng kể đến kiến trúc: nền tảng, tương thích, bảo mật, riêng tư, tổ chức, quy định, vận hành hoặc chi phí. Phân biệt điều đã xác minh với giả định.

## 3. Bối cảnh và phạm vi

Mô tả người dùng, hệ thống liên quan, ranh giới tin cậy và giao diện bên ngoài. Thêm sơ đồ C4 System Context nếu giúp làm rõ các mối quan hệ; ghi trạng thái Current hoặc Target và giải thích bên dưới.

## 4. Chiến lược giải pháp

Tóm tắt cách tiếp cận kiến trúc chính và cách nó đáp ứng mục tiêu. Liên kết các lựa chọn quan trọng tới ADR thay vì lặp lại phần lý do.

## 5. Các khối cấu thành

Mô tả các service, module, kho dữ liệu có ý nghĩa và trách nhiệm của chúng. Thêm sơ đồ C4 Container khi hữu ích; chỉ thêm Component view khi có ranh giới thực sự cần giải thích.

## 6. Luồng hoạt động

Mô tả các luồng giúp giải thích tương tác quan trọng, lỗi, khôi phục, bảo mật hoặc tính nhất quán. Dùng sequence diagram khi giúp dễ hiểu hơn.

## 7. Triển khai

Mô tả môi trường triển khai, topology và vùng lỗi khi chúng ảnh hưởng đến kiến trúc hoặc vận hành. Tách riêng cách triển khai đã xác minh với thiết kế mục tiêu.

## 8. Các mối quan tâm xuyên suốt

Mô tả các quy tắc dùng chung có liên quan như định danh, phân quyền, quyền sở hữu dữ liệu, xử lý lỗi, khả năng quan sát, cấu hình, bảo mật, riêng tư hoặc tương thích.

## 9. Quyết định kiến trúc

Liên kết tới `docs/decisions/README.md` nếu có ADR. Ghi lý do trong ADR và chỉ tóm tắt các hệ quả hiện còn hiệu lực ở đây.

## 10. Yêu cầu chất lượng

Mô tả các tình huống có thể đo lường cho những thuộc tính chất lượng định hình hệ thống. Liên kết tới yêu cầu hoặc quyết định liên quan nếu có.

## 11. Rủi ro và nợ kỹ thuật

Liệt kê các khoảng trống kiến trúc đã biết, tác động và người phụ trách hoặc bước tiếp theo nếu đã rõ. Đánh dấu vấn đề chưa giải quyết là Open; không trình bày thiết kế tương lai chưa được duyệt như quyết định chính thức.

## 12. Thuật ngữ

Chỉ giải thích thuật ngữ riêng của dự án mà người đọc có thể hiểu nhầm.
