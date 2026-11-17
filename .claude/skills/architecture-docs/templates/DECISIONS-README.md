# Quyết định kiến trúc

Danh sách này ghi các quyết định kiến trúc quan trọng. Nội dung ADR lưu bối cảnh và lý do; không lặp lại phần lý do tại đây.

| ID | Quyết định | Trạng thái | Ngày | Thay thế / bị thay thế bởi |
|---|---|---|---|---|

## Quy tắc

- Dùng ID số tiếp theo chưa được sử dụng. Không dùng lại ID, kể cả quyết định bị từ chối hoặc bị thay thế.
- `Proposed` nghĩa là đang chờ người chịu trách nhiệm xem xét. `Accepted` phải có tên người quyết định và ngày chấp thuận.
- Giữ nguyên các bản ghi đã chấp thuận. Khi thay đổi quyết định, tạo ADR mới và liên kết việc thay thế trong cả hai ADR và chỉ mục này.
- Không tạo ADR cho chi tiết triển khai thông thường, trừ khi nó ảnh hưởng đến ranh giới kiến trúc, đánh đổi chất lượng hoặc lựa chọn khó đảo ngược.
