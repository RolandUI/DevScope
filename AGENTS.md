# DevScope AGENTS.md

DevScope is a lightweight, local, in-process continuation of the classic Avalonia F12 DevTools for Avalonia 12 applications.

Preserve this file as the project context for future agents.

Target Avalonia line: `Avalonia 12`. The current baseline is `12.1.0`, with `net8.0` and `net10.0` targets. Do not default implementation work to Avalonia 11.x guidance. When package versions, APIs, internal API shapes, or breaking changes matter, verify against the current stable Avalonia 12 release, official documentation, or tagged Avalonia source before implementing.

## Agent Ownership

Development in this repository is expected to be performed 100% by autonomous coding agents.

- Agents own implementation, test writing, build validation, test validation, documentation updates, commits, pushes, issue updates, and handoff notes end-to-end.
- Do not leave routine coding, formatting, validation, commit, push, or documentation work for the user.
- Prefer the smallest complete vertical slice that satisfies the selected issue.
- If a decision cannot be derived from the issue, this file, or the codebase, make the smallest product-aligned choice and record non-obvious decisions in the issue or repository documentation.
- If credentials, publishing permissions, or a product decision block completion, stop at the nearest validated state and report the precise blocker.
- Each meaningful change must include relevant automated tests or an explicit validation note explaining why tests are not applicable.

## GitHub Workflow

GitHub Issues in `RolandUI/DevScope` are the primary backlog and progress-tracking source for this repository.

- Inspect the selected issue, its acceptance criteria, comments, labels, milestone, and related code before starting meaningful implementation work.
- Keep at most one issue actively in progress unless the user explicitly requests parallel work.
- Add a short start comment describing the intended scope and validation before implementation.
- Keep implementation within the selected issue unless correctness requires a narrowly related change.
- After a coherent change, update the issue with the commit hash, validation result, and any non-obvious decision.
- Do not close an issue until its acceptance criteria and validation commands pass.
- Commit and push completed vertical slices; do not leave validated work uncommitted without an explicit reason.

## Product Scope and Commitments

Primary goal:

- Preserve the familiar local F12 diagnostics workflow for Avalonia 12 applications.
- Keep the package lightweight, offline-capable, and useful for community and small-project scenarios.
- Support visual and logical tree inspection, events, property and style diagnostics, runtime-safe editing, overlays, hotkeys, and screenshots.

Hard scope boundaries:

- Do not implement, maintain, or reverse-engineer remote development protocols.
- Do not turn this repository into a remote debugger or external diagnostics platform.
- Do not position the project as a replacement for Avalonia Accelerate.
- Preserve the Avalonia Accelerate promotional banner and the README commitments around the official ecosystem.

## Current Architecture

- `src/DevTools.cs`: public application-level attach API.
- `src/Hosting/DevToolsApplicationSession.cs`: application-lifetime input subscription and session ownership.
- `src/Hosting/DevToolsWindowHost.cs`: global desktop DevTools host.
- `src/IDevToolsRootSource.cs` and `src/Rooting/`: application and presentation-root discovery.
- `src/Shell/` and `src/Views/Shell/`: DevTools shell, tabs, overlays, and host window.
- `src/Elements/`: tree inspection, selection, property inspection/editing, styles, and layout diagnostics.
- `src/ViewModels/` and `src/Views/`: events, trace, hotkeys, settings, and supporting views.
- `tests/DevScope.Tests/`: headless unit and integration-oriented tests.

Keep host selection, root discovery, diagnostics state, and views separated. Do not concentrate lifetime, platform, and UI logic into a single control or static service.

## Avalonia API Rules

- Prefer public Avalonia APIs first.
- Use compiled XAML and compiled bindings with `x:DataType` by default.
- Keep all visual-tree and control access on the owning Avalonia dispatcher.
- Treat `TopLevel` as a runtime service host, not as a guaranteed visual root.
- Resolve clipboard, storage, screen, and other platform services from the relevant `TopLevel` at interaction time.
- Dispose input, property, collection, trace, and lifetime subscriptions deterministically.
- Avoid reflection when a compiled or typed Avalonia API exists.

This project intentionally uses `IgnoresAccessChecksToGenerator` because classic diagnostics requires limited access to Avalonia internals. Any new internal access must:

- be necessary for a diagnostics capability that has no suitable public API,
- be isolated in `AvaloniaMutatedApiAccessor` or another single-purpose adapter boundary,
- fail gracefully when the expected internal API shape is unavailable,
- include focused compatibility tests,
- be documented as version-sensitive.

Do not scatter direct internal Avalonia calls through views or viewmodels.

## Development Rules

- Reuse existing viewmodel, property-accessor, editor-factory, reactive, and disposal patterns.
- Preserve public API compatibility unless the selected issue explicitly authorizes a breaking change.
- Keep UI in AXAML where practical; keep behavior and state in C#.
- Keep unbounded diagnostic streams, event histories, and tree materialization out of the UI. Use explicit limits and filters.
- Do not block the inspected application UI thread with diagnostics processing.
- Keep DevTools controls out of the inspected application tree and selection results.
- Preserve existing behavior on classic desktop while adding new lifetime or platform support.
- Update README limitations whenever a capability is completed or its support boundary changes.

## Release Workflow

- Treat `docs/RELEASING.md` as the release source of truth.
- Use one release-tracking issue with locked scope, explicit deferrals, validation evidence, and the final release commit.
- Release only from a clean, pushed `main` commit after the complete Debug/Release and `net8.0`/`net10.0` gate passes.
- Prepare and review a draft GitHub Release before publication. Publishing the draft is externally visible and requires explicit release authorization.
- Let `.github/workflows/nuget-publish.yml` create and publish the NuGet artifacts; do not publish them manually from a development machine.
- Verify the GitHub Release, workflow run, NuGet registration, package hash, and consumer restore before closing the release issue.

## Validation

Run validation proportional to the change. The normal full matrix is:

```powershell
dotnet restore DevScope.slnx
dotnet test DevScope.slnx --configuration Debug --no-restore
dotnet test DevScope.slnx --configuration Release --no-restore
dotnet pack src/DevScope.csproj --configuration Release --no-build --no-restore
```

Requirements:

- Validate both `net8.0` and `net10.0`.
- Treat new compiler, XAML compiler, and package warnings as actionable.
- Add headless tests for lifetime, tree, property-editor, and reactive behavior where possible.
- For platform-specific UI that cannot be fully automated, record the exact manual smoke-test surface in the issue.

## Context From Project Adoption

The project is already migrated to Avalonia `12.1.0`. A single global desktop DevTools window, application-root inspection, style and pseudo-class editing, flags-enum editing, and collection navigation are present. The current open roadmap covers mutable collection item editing, a real Trace view, an embedded single-view host, and experimental diagnostic clock controls.
