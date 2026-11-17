# PhoneStore — Website bán điện thoại (đồ án TMĐT full-stack)

Hệ thống thương mại điện tử chuyên **điện thoại & phụ kiện**, xây theo **Clean Architecture (.NET 8)** + **Vue 3**. Mô phỏng kiến trúc dự án ShopViet nhưng nghiệp vụ đặc thù ngành điện thoại.

## Tech stack
- **Backend**: ASP.NET Core 8 (Clean Architecture: Domain / Application / Infrastructure / API), EF Core 8 + PostgreSQL 16, JWT auth, FluentValidation, Serilog, MinIO (lưu ảnh), SignalR (thông báo realtime), Swagger.
- **Frontend**: Vue 3 + TypeScript + Vite, PrimeVue 4, Pinia, Vue Router, Axios, Chart.js.
- **Hạ tầng**: Docker Compose (db + minio + api + web).

## Nghiệp vụ đặc thù điện thoại
- Thương hiệu (Apple/Samsung/Xiaomi...), biến thể **Màu × Dung lượng**, **thông số kỹ thuật** gom nhóm, **so sánh máy**.
- **IMEI/Serial + phiếu bảo hành** (tra cứu theo IMEI/mã đơn), **trả góp** (mock), **thu cũ đổi mới**.
- Giỏ hàng (khách vãng lai + đồng bộ khi đăng nhập), đặt hàng, thanh toán VNPay/COD/Trả góp, coupon, Flash Sale, đánh giá, wishlist, thông báo realtime, báo cáo/thống kê, phân quyền Admin/Customer.

## Chạy nhanh
```bash
cp .env.example .env   # điền secret nếu cần
docker compose up -d --build
# web: http://localhost:5174 · api: http://localhost:8081/swagger
# (DB 5433 · MinIO 9002/9003 — đổi cổng để chạy song song dự án 'hai')
```
Tài khoản demo: `admin@phone.com` / `Admin@123`.

Hướng dẫn từng bước cho luồng khách hàng và quản trị viên: [`docs/HUONG_DAN_SU_DUNG.md`](docs/HUONG_DAN_SU_DUNG.md).

## Cấu trúc
```
backend/               # .NET 8 solution (PhoneStore.sln)
  src/PhoneStore.Domain          # Entities + Enums (lõi)
  src/PhoneStore.Application      # DTOs, Interfaces, Services (business logic)
  src/PhoneStore.Infrastructure   # EF Core DbContext, JWT, MinIO, Seeder
  src/PhoneStore.API              # Controllers, Middleware, Program.cs
frontend/              # Vue 3 + PrimeVue + Pinia
docker-compose.yml
```

> Trạng thái: đang phát triển theo module — xem [`TODO.md`](TODO.md).

## Agent Tooling

Project agent definitions and reusable skills are maintained locally and shared by Claude Code and Codex.

### 15 Agents

| Agent | Definition |
|---|---|
| `vc-code-reviewer` | [`.claude/agents/vc-code-reviewer.md`](.claude/agents/vc-code-reviewer.md) |
| `vc-code-simplifier` | [`.claude/agents/vc-code-simplifier.md`](.claude/agents/vc-code-simplifier.md) |
| `vc-debugger` | [`.claude/agents/vc-debugger.md`](.claude/agents/vc-debugger.md) |
| `vc-execute-agent` | [`.claude/agents/vc-execute-agent.md`](.claude/agents/vc-execute-agent.md) |
| `vc-fast-mode-agent` | [`.claude/agents/vc-fast-mode-agent.md`](.claude/agents/vc-fast-mode-agent.md) |
| `vc-git-manager` | [`.claude/agents/vc-git-manager.md`](.claude/agents/vc-git-manager.md) |
| `vc-innovate-agent` | [`.claude/agents/vc-innovate-agent.md`](.claude/agents/vc-innovate-agent.md) |
| `vc-plan-agent` | [`.claude/agents/vc-plan-agent.md`](.claude/agents/vc-plan-agent.md) |
| `vc-quick-fix-agent` | [`.claude/agents/vc-quick-fix-agent.md`](.claude/agents/vc-quick-fix-agent.md) |
| `vc-research-agent` | [`.claude/agents/vc-research-agent.md`](.claude/agents/vc-research-agent.md) |
| `vc-spec-agent` | [`.claude/agents/vc-spec-agent.md`](.claude/agents/vc-spec-agent.md) |
| `vc-tester` | [`.claude/agents/vc-tester.md`](.claude/agents/vc-tester.md) |
| `vc-ui-ux-designer` | [`.claude/agents/vc-ui-ux-designer.md`](.claude/agents/vc-ui-ux-designer.md) |
| `vc-update-process-agent` | [`.claude/agents/vc-update-process-agent.md`](.claude/agents/vc-update-process-agent.md) |
| `vc-validate-agent` | [`.claude/agents/vc-validate-agent.md`](.claude/agents/vc-validate-agent.md) |

### 62 Skills

- `architecture-docs`
- `ask-matt`
- `code-review`
- `codebase-design`
- `diagnosing-bugs`
- `domain-modeling`
- `grill-me`
- `grill-with-docs`
- `grilling`
- `handoff`
- `implement`
- `implement-spec`
- `improve-codebase-architecture`
- `pr`
- `prototype`
- `research`
- `retro`
- `setup-matt-pocock-skills`
- `tdd`
- `teach`
- `to-questionnaire`
- `to-spec`
- `to-tickets`
- `track`
- `triage`
- `vc-agent-browser`
- `vc-agent-strategy-compare`
- `vc-audit-context`
- `vc-audit-plans`
- `vc-audit-vc`
- `vc-autopilot`
- `vc-autoresearch`
- `vc-context-discovery`
- `vc-debug`
- `vc-docs-seeker`
- `vc-feasibility-test`
- `vc-frontend-design`
- `vc-generate-closeout`
- `vc-generate-context`
- `vc-generate-phase-program`
- `vc-generate-plan`
- `vc-generate-spec`
- `vc-intent-clarify`
- `vc-plan-discovery`
- `vc-predict`
- `vc-problem-solving`
- `vc-publish`
- `vc-review-situation`
- `vc-risk-evidence-pack`
- `vc-scenario`
- `vc-scout`
- `vc-security`
- `vc-sequential-thinking`
- `vc-setup`
- `vc-test-coverage-plan`
- `vc-update`
- `vc-validate-findings`
- `vc-web-testing`
- `wait-what`
- `wayfinder`
- `wizard`
- `writing-for-agents`
### Using skills

Claude Code and Codex discover these skills from `.claude/skills/` through the `.agents/skills/` link. The repository's RIPER-5 workflow in `AGENTS.md` and `CLAUDE.md` controls task routing and phase approvals.
