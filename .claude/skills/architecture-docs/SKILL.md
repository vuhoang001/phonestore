---
name: architecture-docs
description: Create, review, or update software architecture documentation for existing or new projects. Use arc42 as a tailored content structure, C4 diagrams where useful, MADR-style records for consequential decisions, and choose automated architecture checks by the actual project stack. Do not use for routine feature implementation or general product roadmaps.
trigger_keywords: architecture, docs, create, review, update, software, documentation, existing
layer: contract
---

# Architecture documentation workflow

Use this skill when the user asks for an architecture description, architecture review, architecture documentation bootstrap, or a consequential architecture change.

## Principles

- Write all project-facing architecture documentation in Vietnamese by default, including `ARCHITECTURE.md`, `AGENTS.md`, ADRs, indexes, diagram labels, and explanatory text in API/event contracts. Use another language only when the user explicitly requests it. Preserve code identifiers, product/service names, protocol terms, API field names, commands, file paths, and machine-readable status values as written; explain technical terms in Vietnamese where useful.
- Use the shared standard in `references/standard.md`, the root `AGENTS.md` snippet in `templates/AGENTS.md`, and the root `ARCHITECTURE.md` template in `templates/ARCHITECTURE.md`.
- Keep `AGENTS.md` short: state durable agent rules and point to `ARCHITECTURE.md`; do not copy the architecture into agent instructions.
- Treat arc42 as a customizable content map, C4 as a diagramming model, and ADR/MADR as a way to record significant decisions. They solve different problems.
- Document current/as-is behavior separately from proposed/to-be design. Cite repository evidence for current claims. Never label a proposal as implemented or approved without evidence.
- Do not retain empty template prompts. Omit inapplicable sections, or write `Not applicable` with a reason when a reader needs that context.
- Make the smallest useful set: normally one root `ARCHITECTURE.md`; add `docs/decisions/` only when there are consequential decisions to record. Split diagrams or viewpoints into more files only when the main document becomes difficult to navigate.
- Do not add implementation, dependencies, CI configuration, or architecture-test code during a documentation-only request.

## Workflow

1. **Choose the path.** For an existing repository, use the brownfield workflow in `references/standard.md`. For a new project, use the greenfield workflow. If the project has code and a future target, document both with distinct status labels.
2. **Inspect evidence.** Read repository instructions, existing architecture and decision records, manifests, source boundaries, tests, deployment configuration, and CI. Preserve local changes. Do not treat an old proposal as current behavior.
3. **Identify concerns.** Record stakeholders, system scope, quality goals, constraints, trust/data boundaries, and decisions that materially shape the architecture. Mark unknowns instead of guessing.
4. **Decide whether review is consequential.** Use the threshold in `references/standard.md`. For significant changes, delegate read-only perspectives to domain/product, architecture/implementation, security/operations, and an independent critic when those agents are available. Prefer different providers when configured. Workers return findings only; the lead is the sole writer. Do not use a vote to make owner decisions. For small changes, a single focused review is enough.
5. **Draft the root architecture description.** Tailor arc42 headings to the project. Include a C4 context and container view when they clarify system boundaries; use component views only when they clarify a real boundary. Keep runtime, deployment, and detailed cross-cutting views proportional to actual concerns.
6. **Record decisions.** Create a MADR-style ADR for a decision that crosses boundaries, constrains future changes, changes a quality trade-off, or is costly to reverse. Use `templates/ADR.md`; create `docs/decisions/README.md` from `templates/DECISIONS-README.md` when the first ADR is added. Mark proposals as `Proposed` until an accountable owner accepts them.
7. **Choose enforcement by stack.** Follow the decision procedure in `references/standard.md`. Recommend checks against explicit dependency or boundary rules; do not install tools or change CI unless requested. Keep human review for architecture properties that cannot be checked deterministically.
8. **Validate and report.** Check links, status labels, diagram notation/render support, ADR references, and that no template placeholders remain. Summarize what changed, evidence and assumptions, open owner decisions, worker availability, and any checks not run.

## Approval and ownership

Name the architecture owner/deciders when known; do not invent them. The lead may prepare a `Proposed` architecture and ADR, but only the owner or an explicitly delegated decision maker can mark it `Accepted`. If owner approval is not available, leave consequential choices open and state the next decision needed.
