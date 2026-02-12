---
discard: true
title: "API & Internal Design Assessment (Updated)"
---

# API & Internal Design Assessment (Updated)

> **Scope**: `XenoAtom.CommandLine` and `XenoAtom.CommandLine.Terminal`  
> **Goal**: keep the library simple and expressive while making the API contract clearer, more consistent, and future-proof for targeted CLI scenarios.

---

## 1. Assessment Principles

This assessment uses five principles to evaluate API decisions:

1. **Composition-first**: collection initializer syntax remains the primary experience.
2. **Predictability**: behavior should be obvious from signatures and names.
3. **Low-ceremony extensibility**: extension points should be explicit and stable.
4. **AOT/trimming friendliness**: avoid reflection-heavy or dynamic designs.
5. **Scope discipline**: avoid turning the library into a generic application framework.

---

## 2. Current Strengths (Keep As-Is)

### 2.1 Core shape is strong

- The type hierarchy is compact and easy to reason about.
- `CommandNode` has an internal constructor, which protects core invariants while still allowing extension through public abstract leaves (`Option`, `CommandArgument`, `ArgumentSource`).
- The collection-initializer authoring model is a clear differentiator and remains highly usable.

### 2.2 Output model is well-factored

- `ICommandOutput` cleanly isolates rendering from parsing/execution.
- `CommandOutputHelper` provides the right primitives for custom renderers.
- `CommandConfig.OutputFactory` allows output selection from runtime context (`CommandRunConfig`), and output resolution is deferred in the invocation pipeline.
- `ICommandNodeDescriptor` and `IHelpPreformattedContent` provide lightweight, composable hooks for help rendering without introducing heavy renderer abstractions.

### 2.3 Terminal extension architecture is on the right path

- `TerminalVisualOutputOptions : TerminalMarkupOutputOptions` is the correct options layering.
- `Add<TCommand, TVisual>(..., TVisual visual)` prevents string/visual ambiguity from implicit `string -> Visual` conversions.
- Inline visual integration via `TerminalVisualNode` + shared help model avoids duplicate traversal logic.

---

## 3. Corrections / Clarifications to Preserve Accuracy

1. **`CompletionCommands` usage**  
   There is no `Register` method; `CompletionCommands` is directly added as a `CommandGroup` node.

2. **Deferred output resolution scope**  
   Deferred output resolution applies to `RunAsync`/`Parse` invocation flow (via deferred output wrapper). Direct `ShowHelp(runConfig)` resolves output immediately.

3. **`CommandGroup` inlining semantics**  
   Group children are inlined into the parent, but the `CommandGroup` node itself remains in `Nodes`. This matters for custom traversals.

4. **Text separator authoring pattern**  
   Help text sections are authored via single-string nodes (`"Options:"`, `const string _ = ""`), not via `{ "", "Section Header" }` (which targets the remainder overload and is invalid).

---

## 4. Consistency Gaps and Recommendations

### 4.1 Naming & semantic clarity

#### Findings

- `CommandNode.IsThisNodeActive` is a delegate property named like a boolean value.
- `OptionException` is the only exception type not prefixed with `Command`.
- `Command.OptionsName` is semantically a section header label, not an "option name".
- `OptionValueType.Optional` and `CommandArgument.ValueCardinality.Optional` are both valid but semantically different.

#### Recommendations

- **2.0 candidate**: rename `IsThisNodeActive` to `ActivePredicate` (or convert to a virtual boolean method).
- **2.0 candidate**: rename `OptionException` to `CommandOptionException` (keep compatibility alias during migration).
- **2.0 candidate**: rename `OptionsName` to `OptionsSectionName`.
- **Optional**: rename `OptionValueType.Optional` to `OptionalValue` only if ambiguity is seen in user feedback (not mandatory).

### 4.2 Mutability model is inconsistent

#### Findings

{.table}
| Member | Mutable today |
|---|---|
| `Command.Hidden` | Yes |
| `Option.Hidden` | No |
| `CommandArgument.Hidden` | No |
| `Option.EnvironmentVariable*` | Yes |
| `ValueCompleter` | Yes (`Option` / `CommandArgument`) |

This creates uneven expectations for post-construction edits.

#### Recommendation

Pick one policy and apply it consistently in 2.0:

- **Preferred**: metadata is construction-time (`init`/ctor) and immutable at runtime.
- Alternative: allow late mutation for all equivalent metadata fields.

Either is acceptable; inconsistency is the issue.

### 4.3 `Add` overload matrix has parity gaps

#### Findings

{.table}
| Family | `hidden` | `envVar` | `validate` |
|---|---:|---:|---:|
| `Action<string?>` | ✅ | ✅ | ✅ |
| `Action<T>` | ❌ | ✅ | ✅ |
| `ICollection<T>` | ❌ | ✅ | ✅ |
| `Action<string, string?>` | ✅ | ❌ | ❌ |
| `Action<TKey, TValue>` | ❌ | ❌ | ❌ |

Also, `Add(string prototype, string? description)` is only valid for `"<>"`, but compiles for any two strings and fails at runtime.

#### Recommendations

1. Add `hidden` parity overloads for typed and list families.
2. Decide explicitly whether key/value families support env-var/validation; either add support or document deliberate non-support.
3. Add explicit helpers for intent clarity:
   - `AddRemainder(string? description)` (or equivalent)
   - `AddText(string text)` / `AddSection(string header)` convenience APIs  
   Keep existing overloads for compatibility.

### 4.4 `ICommandOutput` diagnostic completeness

#### Findings

`WriteUnknownTokens` receives token spans but not the invocation token list. `TerminalMarkupOutputOptions.InvocationTokensProvider` is a workaround in the extension package.

#### Recommendation

For 2.0, make unknown-token rendering self-contained in core API:

- Add invocation tokens to `WriteUnknownTokens`, **or**
- Replace parameters with a richer report object that includes tokens and spans.

This removes side-channel dependencies from output implementations.

### 4.5 Extension boundary policy should be explicit

#### Findings

- Core now exposes `IHelpPreformattedContent`, enabling preformatted help blocks.
- Terminal node extensibility currently relies on `InternalsVisibleTo` for official extension package integration.

#### Recommendation

Document official policy:

- If third-party node types are **not** a goal, keep current controlled boundary.
- If they become a goal, promote a supported public node extension mechanism (instead of expanding IVT usage ad hoc).

### 4.6 Invocation lifecycle and concurrency semantics need explicit contract

#### Findings

- Parsing mutates internal option state (`WasSet*`) per run.
- The model is not designed for concurrent `RunAsync` calls on the same command graph.
- `Parse` intentionally invokes option/argument actions and defaults streams to `TextWriter.Null`.

#### Recommendations

- Document command graph instances as **single-invocation at a time** (not thread-safe for concurrent runs).
- Document `Parse` side effects prominently (actions execute; only command action is skipped).
- Optionally add a debug guard against concurrent invocation on the same root command.

### 4.7 Interface contracts (`ICommandNodeDescriptor`, `IHelpPreformattedContent`)

#### Findings

- `ICommandNodeDescriptor` is a pragmatic interface: it gives a common descriptive surface across `Command`, `Option`, `CommandArgument`, `ArgumentSource`, `CommandUsage`, and internal text-like nodes.
- The interface is intentionally minimal (`Description` only), which keeps extension friction low but does not encode semantic role (section header vs option description vs free text). Rich renderers still pattern-match node types.
- `IHelpPreformattedContent` solves a concrete problem well: preserving verbatim formatting (e.g. FIGlet/block visuals) in default text help output.
- The current interaction model is sound: if a node implements both interfaces, preformatted content is used by the core writer while `Description` remains a fallback for outputs that only consume descriptors.

#### Recommendation

Keep both interfaces. They are wise and appropriately scoped for the library’s goals.

Refinements to document in API contract:

1. `ICommandNodeDescriptor.Description` should represent plain help text intent (nullable, may be empty line), not renderer-specific markup.
2. `IHelpPreformattedContent` is a writer-level contract for verbatim content and should be treated as higher priority than descriptor text in text outputs.
3. Custom outputs may ignore `IHelpPreformattedContent`, but if they do, they should fall back to descriptor text when available.

Avoid adding broader “render everything” interfaces unless there is a demonstrated need; this would increase abstraction cost and API complexity without clear benefit today.

---

## 5. Terminal Package-Specific Review

### What is good

- Options inheritance and style unification are solid.
- Inline visuals are integrated into both markup and visual help pipelines.
- Hidden node filtering and grouped visual sections are now aligned with standard help visibility.

### Remaining polish opportunities

1. `TerminalVisualCommandOutput` stores typed options redundantly due base/derived type split.  
   This is acceptable but can be cleaned later with a protected typed accessor in base.
2. Visual objects are single-parent by nature (`Terminal.UI`). Reusing the same `Visual` instance across different render paths has limits.  
   This should be documented as expected behavior.

---

## 6. Approved 2.0 Decisions (Accepted February 12, 2026)

The following decisions were explicitly accepted and now define the target API direction:

1. **Active predicate naming**  
   Adopt the recommended rename to `ActivePredicate` (from `IsThisNodeActive`) as a 2.0 breaking cleanup.

2. **Unknown-token output payload**  
   Use a richer report object approach for `ICommandOutput` unknown-token rendering (instead of side-channel token providers).

3. **`Add` overload parity**  
   Apply the recommended partial parity path:
   - add `hidden` parity for typed/list families,
   - keep key-value env-var/validation as an explicit non-goal for now (documented, not implicit).

4. **Mutability policy**  
   Move to the recommended construction-time metadata model (`init`/ctor), reducing post-construction mutability drift.

5. **Extension boundary**  
   Keep the controlled extensibility boundary (official package integration path, no broad public node-extensibility opening in 2.0).

6. **Interface strategy**  
   Keep both `ICommandNodeDescriptor` and `IHelpPreformattedContent`, and formalize their contract (intent, precedence, fallback behavior).

7. **Rename scope**  
   Apply the recommended **minimal high-value rename set** for 2.0:
   - `OptionException` → `CommandOptionException`
   - `OptionsName` → `OptionsSectionName`
   - `IsThisNodeActive` → `ActivePredicate`  
   Keep lower-priority rename candidates (for example `OptionValueType.Optional`) unchanged unless user feedback proves ambiguity.

8. **Concurrency contract**  
   Adopt the recommended single-invocation contract: command graphs are documented as non-thread-safe for concurrent runs.

### 6.1 Next Implementation Pass (Derived from approved decisions)

1. Update API/spec docs first (including interface contract text and migration notes).
2. Apply breaking rename set in a focused 2.0 branch with compatibility notes.
3. Implement unknown-token report object and remove terminal-side workaround dependency.
4. Add overload parity improvements (`hidden` for typed/list) and document key-value scope boundaries.
5. Add explicit lifecycle/concurrency notes to user documentation.

### 6.2 Concrete 2.0 Execution Checklist

Use this checklist as the implementation order and release gate.

#### Phase A — Baseline and planning

- [ ] Create `2.0` working branch and freeze new 1.x feature additions.
- [ ] Open tracking issue(s) for each approved decision (one issue per checklist phase below).
- [ ] Add a migration notes draft (`doc/migration-2.0.md`) with rename map and API diffs.

#### Phase B — Contract-first documentation

- [ ] Update docs/specs to reflect final interface contracts:
  - `ICommandNodeDescriptor.Description` intent,
  - `IHelpPreformattedContent` precedence and fallback behavior,
  - command graph single-invocation concurrency contract.
- [ ] Update user-facing docs (`doc/help-output.md`, top-level `readme.md`) with 2.0 contract notes.
- [ ] Validate docs consistency with existing specs (`terminal-visual-specs`, `visual-as-command-specs`).

#### Phase C — Breaking rename set

- [ ] Rename `OptionException` → `CommandOptionException` in core and terminal packages.
- [ ] Rename `Command.OptionsName` → `OptionsSectionName`.
- [ ] Rename `CommandNode.IsThisNodeActive` → `ActivePredicate`.
- [ ] Add temporary compatibility strategy (type alias/obsolete shim) if desired for transition period.
- [ ] Update XML docs and examples for all renamed APIs.

#### Phase D — Unknown-token output redesign

- [ ] Introduce a richer unknown-token output report object (includes tokens + spans + kind + items).
- [ ] Update `ICommandOutput` to consume the new report shape.
- [ ] Remove terminal-side dependency on `InvocationTokensProvider`.
- [ ] Update default, markup, and visual outputs to use the new report object.
- [ ] Add regression tests for unknown token rendering (with/without suggestions, inactive matches, underlines).

#### Phase E — `Add` overload parity improvements

- [ ] Add `hidden` parity overloads for typed (`Action<T>`) and list (`ICollection<T>`) families.
- [ ] Keep key/value env-var+validation as an explicit non-goal and document it in API docs.
- [ ] Add explicit clarity helpers (`AddRemainder`, `AddText`/`AddSection`) without breaking existing initializer usage.
- [ ] Add overload resolution tests to ensure no new ambiguity is introduced.

#### Phase F — Metadata mutability alignment

- [ ] Apply chosen construction-time metadata policy (`init`/ctor) to targeted metadata fields.
- [ ] Ensure parser/runtime behavior remains deterministic after mutability changes.
- [ ] Update tests for hidden/environment-variable/completer setup patterns.
- [ ] Document final recommended construction pattern for command graphs.

#### Phase G — Terminal integration alignment

- [ ] Update terminal package for renamed APIs and output report changes.
- [ ] Confirm visual and markup outputs remain behaviorally aligned (ordering, hidden filtering, groups).
- [ ] Verify `IHelpPreformattedContent` fallback path still works for default output scenarios.
- [ ] Update terminal sample apps accordingly.

#### Phase H — Final quality gate and release prep

- [ ] Run full build and tests in Debug and Release configurations.
- [ ] Run API compatibility review and produce a concise breaking-changes table.
- [ ] Finalize `doc/migration-2.0.md` with before/after examples.
- [ ] Update changelog and package release notes.
- [ ] Tag release candidate only after all checklist items are complete.

#### Per-PR acceptance checklist

- [ ] Public API changes include XML docs and migration notes.
- [ ] Behavior changes include regression tests.
- [ ] Samples and user docs are updated in the same PR.
- [ ] No duplicate parse/help traversal logic introduced.

---

## 7. Final Verdict

The library is already strong in architecture and usability. The main risk is not structural weakness; it is **consistency drift** across naming, overload parity, and mutability conventions.

With the approved 2.0 decisions above applied consistently, the API can remain:

- **Simple** for day-to-day CLI usage,
- **Expressive** for richer help/output scenarios,
- **Stable** for long-term evolution without confusing developers.

