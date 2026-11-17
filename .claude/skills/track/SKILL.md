---
name: track
description: Use when the user explicitly asks to add, update, or view a task in the local-first Work Hub, or invokes /track.
argument-hint: "add <task> | update <task> | status"
trigger_keywords: track, Work Hub, task, add task, update task, task status
layer: contract
---

# Track trong Work Hub

Tham số: `$ARGUMENTS`

Chỉ thao tác khi user gọi `/track` hoặc yêu cầu rõ việc quản lý task. Dùng thư
mục Work Hub đầu tiên tồn tại: `WORK_HUB_DIR`, `~/orca/work-hub`, rồi
`~/work-hub`. Nếu không tìm thấy `TASK-SYSTEM.md`, hỏi user đường dẫn.

Đọc `TASK-SYSTEM.md` và `CLAUDE.md` của vault trước khi làm. Mỗi task là một file
Markdown; `status` trong frontmatter là nguồn trạng thái duy nhất. Không dùng
Notion/MCP, không tự commit hoặc push.

## `/track add <task>`

1. Tìm `title` tương tự trong mọi file có `type: task`; hỏi trước nếu có khả năng
   trùng.
2. Nếu project rõ, dùng `<project>/_tasks/`; nếu chưa rõ, dùng
   `30-Inbox/tasks/`.
3. Nếu vault có script tạo task, dùng script đó. Chỉ ghi deadline, priority,
   status và parent khi user cung cấp; mặc định lần lượt rỗng, `medium`, `todo`, rỗng.
4. Nếu là sub-task, đặt `parent_id` bằng ID task cha. Đọc lại frontmatter và báo
   đường dẫn file mới.

## `/track update <task>`

Tìm theo ID hoặc title; chỉ sửa khi kết quả duy nhất hoặc user đã chọn. Giữ
nguyên `id`. Khi hoàn thành đặt `status: done` và `completed: YYYY-MM-DD`; khi mở
lại đặt trạng thái user yêu cầu và xóa `completed`. Khi đổi project, chuyển file
vào `_tasks/` của project mới rồi cập nhật `project` và `area`. Đọc lại
frontmatter và báo đường dẫn.

## `/track status`

Đọc các file `type: task`, bỏ qua `_templates/` và `90-Archive/`. Tóm tắt quá
hạn, hôm nay, bảy ngày tới và chưa có deadline; nhóm theo area/project. Không sửa
file.
