# Architecture documentation standard

This is an architecture-only standard. It does not prescribe product vision, requirements management, project roadmaps, or a coding methodology.

## Ngôn ngữ tài liệu

Mặc định viết tài liệu kiến trúc hướng tới người dùng bằng tiếng Việt: tiêu đề, nội dung, chú thích sơ đồ, ADR, chỉ mục và hướng dẫn agent. Chỉ dùng ngôn ngữ khác khi người dùng yêu cầu rõ. Giữ nguyên tên riêng, identifier trong code, tên service/sản phẩm, giao thức, tên field API, lệnh, đường dẫn và giá trị trạng thái có tính máy đọc; giải thích thuật ngữ chuyên môn bằng tiếng Việt khi cần. Khi cập nhật tài liệu hiện có, chuyển phần diễn giải sang tiếng Việt nhưng không dịch hoặc làm sai hợp đồng kỹ thuật.

## Roles of the methods

| Method | Use it for | Do not use it as |
|---|---|---|
| `AGENTS.md` | Concise, durable instructions for coding agents; point to the architecture description and state update triggers. | A schema for architecture content, a substitute for `ARCHITECTURE.md`, or a technical enforcement mechanism. |
| arc42 | A customizable content map for an architecture description: goals, constraints, context, strategy, building blocks, runtime, deployment, cross-cutting concerns, decisions, quality, risks, glossary. | A requirement to fill all 12 sections in every project. |
| C4 | Diagrams that zoom through system context, containers, components, and (rarely) code. | A full architecture document or a requirement to draw all four levels. |
| ADR / MADR | A durable record of a consequential choice, why it was made, options, and consequences. | A log of every implementation detail or a substitute for an architecture overview. |
| ISO/IEC/IEEE 42010:2022 | A completeness lens for system stakeholders, concerns, viewpoints, and model kinds when formal architecture-description governance is useful. | A default template or a claim of conformance unless the project explicitly adopts and verifies conformance. |

## Default project outputs

```text
AGENTS.md                 # concise project rules and architecture update trigger
ARCHITECTURE.md            # canonical, tailored arc42 architecture description
docs/decisions/            # only when consequential decisions exist
  README.md                # index and status rules
  NNNN-short-title.md      # one MADR-style decision per file
```

Keep the architecture description in root `ARCHITECTURE.md` as the default. Put C4 diagrams there while they remain readable. Create `docs/architecture/` or separate diagram files only when the architecture document becomes unwieldy or a viewpoint has a distinct audience. Link back to its source section from `ARCHITECTURE.md`.

### Agent instruction file interoperability

`AGENTS.md` is a portable Markdown convention, not a schema and not an enforcement mechanism. Keep it short and use it to identify the architecture source of truth and its update trigger. Current Claude Code versions can read `AGENTS.md`; by default, when a `CLAUDE.md` exists in scope, Claude Code may load only `CLAUDE.md` unless configured to load both. If a project must support older Claude Code sessions or has Claude-specific instructions, use an `@AGENTS.md` import or the documented configuration for loading both files. Do not create a `CLAUDE.md` symlink by default, especially for repositories that support Windows.

## arc42 content selection

Use these 12 arc42 concerns as a menu, not a checklist to fill blindly:

1. **Introduction and goals:** system purpose, scope, stakeholders, quality goals.
2. **Constraints:** technical, organizational, regulatory, and compatibility constraints.
3. **Context and scope:** system boundary, actors, external dependencies, interfaces; include C4 System Context when useful.
4. **Solution strategy:** key architectural approach and why it serves the goals.
5. **Building block view:** modules/services/data stores and responsibilities; include C4 Container, and C4 Component only for a meaningful boundary.
6. **Runtime view:** only key interactions or failure/recovery flows that explain the design.
7. **Deployment view:** environments and runtime topology when deployment affects architecture.
8. **Cross-cutting concepts:** security, data ownership, errors, observability, configuration, compatibility, or other system-wide rules.
9. **Architecture decisions:** link to ADRs; keep rationale in the ADR rather than duplicating it.
10. **Quality requirements:** concrete quality scenarios and trade-offs, not vague adjectives.
11. **Risks and technical debt:** known gaps, consequences, owner/action where known.
12. **Glossary:** only terms that are project-specific or likely to be misunderstood.

For a small system, combine related sections or omit irrelevant ones. Do not leave blank headings, `TBD`, example diagrams, or template prompts in a finished document. Use explicit `Not applicable` only when the absence itself matters to readers.

### Architecture status and evidence

At the top, identify the scope, status, owner/deciders if known, and last reviewed date. Viết phần diễn giải bằng tiếng Việt và dùng nhãn trạng thái nhất quán; có thể ghi kèm nhãn tiếng Anh khi cần đối chiếu:

- **Current / As-is:** supported by source, configuration, tests, deployment, or runtime evidence. Link the evidence and state its limits.
- **Target / To-be:** proposed future design or an explicitly approved direction. Approval does not imply implementation.
- **Proposed:** under review; not an owner decision.
- **Open:** needs evidence or an owner decision.

If current and target designs differ, show them as separate views or sections. Record the gap and migration implication; do not merge the two into one misleading diagram.

### Diagram rules

- Select the diagram level to answer a reader's question. Context and container are common starting points; component and code views are conditional.
- State the view's status (current or target), scope, and purpose next to the diagram. Explain important boundaries and flows in words.
- Use the notation/renderer already supported by the project. If none exists, propose one and record the choice before adding tooling.
- Validate source syntax and rendered output when a renderer is available. Keep diagrams editable as source; do not keep screenshots as the sole source.
- Do not force C4 for a view it does not express well, such as a detailed data lifecycle or deployment sequence; choose an appropriate view and label it.

## MADR-style decision records

Create an ADR when a decision changes a system or trust boundary, a public contract, data ownership, a quality-attribute trade-off, deployment/failure behavior, a dependency policy, or a costly-to-reverse technology choice. Routine local implementation details do not need ADRs.

Use monotonically increasing IDs; never reuse an ID. Giữ nguyên các giá trị trạng thái máy đọc: `Proposed`, `Accepted`, `Rejected`, `Deprecated`, và `Superseded by ADR-NNNN`; phần giải thích và nội dung ADR viết bằng tiếng Việt.

- `Proposed` records options and a recommendation but has not been accepted by the accountable owner.
- `Accepted` includes the decision maker and date. Do not infer approval from an agent report, a merged implementation, or silence.
- Preserve accepted history. Change a decision by creating a new ADR that names the superseded record; do not rewrite history to make a past decision appear different.
- Keep an index in `docs/decisions/README.md` with ID, title, status, date, and superseding ADR when relevant.

## Architecture governance and update triggers

The project owner or explicitly delegated architect owns the decisions. The designated documentation lead is the sole editor during a coordinated update. If no owner is known, keep decisions Proposed/Open.

Update `ARCHITECTURE.md` when a change affects one or more of:

- system boundary, container/service/module responsibilities, or dependency direction;
- public API/event/data contract or compatibility strategy;
- identity, trust, security, privacy, or data ownership boundary;
- persistence, consistency, ordering, delivery/failure/recovery semantics;
- deployment topology, scaling/failure domain, or material operational responsibility;
- a quality goal or a trade-off that can change architecture choices.

Add a short architecture pointer/update trigger to root `AGENTS.md` when the user asks to establish project-wide agent instructions or when that file is already being changed for the architecture workflow. Do not create or rewrite unrelated agent instructions automatically.

## Brownfield workflow: existing repository

1. Read root and scoped instructions, existing docs/ADRs, README, manifests, source boundaries, tests, deployment, and CI. Preserve unrelated local work.
2. Build an evidence map. Record what was inspected and which claims are supported by code, configuration, tests, or runtime proof. Do not claim a runtime property based on source shape alone.
3. Describe the current/as-is system first. Use C4 context/container views if useful; verify names and connections against code and deployment configuration.
4. Separate existing behavior from target proposals and prior approved decisions. Identify mismatches, unknowns, technical debt, and migration risks.
5. For significant proposals, compare options and record decisions as Proposed until the owner accepts them. Do not turn a target diagram into an implementation claim.
6. Establish a baseline for any architecture violations before proposing automated enforcement. Prefer a ratchet that prevents new violations over a large unrelated cleanup.

## Greenfield workflow: new repository

1. Identify stakeholders, system scope, drivers, constraints, quality goals, external systems, and operational context from the user brief.
2. Mark unconfirmed assumptions and ask only for choices that materially change boundaries, risk, or cost. Continue with clearly labeled proposals where possible.
3. Describe the target architecture with alternatives and trade-offs. Use C4 context/container views when they aid understanding.
4. Record consequential choices as Proposed ADRs. The owner marks them Accepted.
5. Define only the dependency and quality rules needed to preserve the chosen architecture; select checks based on the actual stack and CI.

## When architecture change is consequential

Use a multi-perspective review when a proposal touches a boundary, public contract, security/data boundary, quality trade-off, deployment/failure model, or hard-to-reverse technology decision. Recommended perspectives:

- domain/product: actors, responsibilities, contract and use cases;
- architecture/implementation: boundaries, ownership, dependencies, migration;
- security/operations: trust boundaries, failure/recovery, deployability, observability;
- independent critic: missing options, assumptions, risks, and measurable acceptance.

Workers provide read-only findings; one lead reconciles evidence and writes the canonical documents. Prefer distinct providers when available and report when they are not. Resolve factual conflicts from repository evidence. Do not settle owner decisions by majority vote. For a small, reversible clarification that changes no boundary or quality trade-off, use a focused single-agent review.

## Stack-specific architecture checks

Automated checks enforce only deterministic rules the project actually chose. They do not replace architecture judgment or owner approval.

1. Detect the languages/frameworks and build/test/CI entry points from repository manifests and workflows. Confirm ambiguous or mixed-stack scope with the user.
2. Write each invariant as a specific allowed/forbidden dependency, layer, package, module, or cycle rule. Avoid generic slogans such as “follow clean architecture.”
3. Prefer existing project tooling. If none is suitable, recommend a candidate with trade-offs and maturity; do not add a dependency or CI workflow unless requested.
4. For a brownfield system, measure current violations and use an explicit baseline/ratchet. For greenfield, start with a small set of rules and positive/negative examples.
5. Put checks in the project's existing test/lint/CI path. Verify that a permitted dependency passes and a forbidden dependency fails.
6. Leave semantic concerns, quality trade-offs, and correctness of current/target diagrams to review; automation cannot reliably infer those from imports alone.

Candidate examples (verify current maintenance, compatibility, and maturity for the detected stack before recommending):

| Stack | Possible candidate | Typical check |
|---|---|---|
| JavaScript / TypeScript | [dependency-cruiser](https://github.com/sverweij/dependency-cruiser) | import direction, cycles, module boundaries |
| Python | [Import Linter](https://import-linter.readthedocs.io/en/stable/) | package import contracts and layer direction |
| Java | [ArchUnit](https://www.archunit.org/) | package/layer dependencies and cycles |
| .NET | Evaluate [ArchUnit.NET](https://www.archunit.net/dotnet/) or another maintained architecture-test library against the repo's target framework and test runner. | project/layer references, dependency direction, cycles |

This list is illustrative, not a preferred-tool mandate. Check project manifests, CI compatibility, and upstream status; a tool in alpha or a mismatched runtime is not an automatic fit. For example, ArchUnit.NET's current documentation labels its release alpha, so review its maturity before adopting it.

## References

- [AGENTS.md project](https://github.com/agentsmd/agents.md)
- [arc42 overview](https://arc42.org/overview/)
- [C4 model diagrams](https://c4model.com/diagrams)
- [MADR](https://adr.github.io/madr/)
- [ISO/IEC/IEEE 42010:2022](https://webstore.iec.ch/en/publication/80194)
- [Claude Code instructions and AGENTS.md behavior](https://code.claude.com/docs/en/memory)
- [Codex skills](https://developers.openai.com/blog/skills-agents-sdk)
