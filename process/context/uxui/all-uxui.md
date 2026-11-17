---
name: context:all-uxui
description: "Vue customer/admin screens, shared components, navigation, and styling"
keywords: UI, UX, frontend, Vue, component, page, layout, route, PrimeVue, responsive, accessibility
related: [context:all-tests]
date: 16-11-26
---

# UI/UX Context

## Scope

This group covers customer and admin web screens, navigation, shared components, frontend state, and styling. It does not own backend business rules or API persistence.

## Read When

- Changing a customer or admin page, component, layout, or route.
- Changing frontend state, API calls, responsive behavior, or visual interaction.
- Checking what screens and role-protected routes exist.

## Quick Routing

- Routes and auth guards: `frontend/src/router/index.ts`
- Customer screens: `frontend/src/views/`
- Admin screens: `frontend/src/views/admin/`
- Shared components/layouts: `frontend/src/components/` and `frontend/src/layouts/`
- API services and state: `frontend/src/services/` and `frontend/src/stores/`
- User-facing walkthrough: `docs/HUONG_DAN_SU_DUNG.md`

The frontend uses Vue 3, TypeScript, Vite, PrimeVue, Pinia, Vue Router, and Axios. Route-level access guards redirect unauthenticated visitors to login and non-admin users away from admin routes.

## Source Paths

- `frontend/src/router/index.ts`
- `frontend/src/views/`
- `frontend/src/views/admin/`
- `frontend/src/components/`
- `frontend/src/layouts/`
- `frontend/src/stores/`

## Update Triggers

Update this entrypoint when the route map, frontend framework, design system, role guards, or shared screen conventions change.

