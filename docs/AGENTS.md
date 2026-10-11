# Repository Guide for Coding Agents

Read `DEVELOPER_ROADMAP.md` before making non-trivial changes. It documents the current architecture, page-to-CSS/JS ownership map, asset loading order, design system, conflict-debugging workflow, and validation workflow.

## Required workflow

1. Run `git status --short` first. Preserve unrelated user changes; do not reset, clean, or overwrite them.
2. Trace the complete feature path before editing: controller/route → model or data source → Razor view → page CSS/JS → shared layout and components. Use the roadmap's page-to-asset tables to identify actual owners.
3. Inspect the actual loaded asset order in `Shop/Views/Shared/_Shop.cshtml`. Files present in the repository are not necessarily loaded by the layout.
4. Make the smallest coherent change in the owning layer. Avoid duplicate markup, duplicate event handlers, broad global selectors, and new `!important` overrides unless the existing cascade makes them necessary.
5. Keep pages Persian-first and RTL. Preserve responsive behavior, keyboard access, accessible names, and reduced-motion support when relevant.
6. For visual changes, check shared tokens before using new colors, borders, spacing, radius, or shadows. Inspect the winning cascade rule and actual load order; prefer fixing the component's source stylesheet over appending another override to `industrial-system.css`.
7. Validate with the narrowest relevant check, then build with `dotnet build Shop.sln` when appropriate. There is currently no test project in the solution; do not claim tests ran unless they did.
8. Never assume demo browser data is server-persisted. Current catalog and commerce behavior is mostly static/demo data; see the roadmap before changing data flows.

## Repository scope

- `Shop/` is the ASP.NET Core MVC application and the only project currently included in `Shop.sln`.
- `ClockTemplate/` currently contains generated/build artifacts rather than a source project in the solution; do not treat it as an application entry point without verifying new source files and solution membership.
- Do not delete generated, vendor, or apparently unused files as cleanup unless references and build/runtime usage have been checked.
