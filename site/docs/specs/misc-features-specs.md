---
discard: true
title: "XenoAtom.CommandLine — New Features Specification"
---

# XenoAtom.CommandLine — New Features Specification

**Version scope:** v1.5 (Short-Term) and v1.6 (Medium-Term)  
**Date:** February 2026  
**Status:** Draft

---

## Guiding Principles

- Keep the core API small and composition-first.
- Prefer additive changes only (no signature changes to existing public APIs).
- Avoid overload ambiguities: any new `Add` overload that introduces new concepts (`envVar`, `validate`, …) must differ in required parameters so existing call sites remain unambiguous.
- Preserve NativeAOT / trimming friendliness: no reflection-based validation and no new external dependencies.
- Keep diagnostics consistent with existing formatting, and avoid leaking environment variable values in error messages by default (env vars frequently carry secrets).

---

## Table of Contents

0. [Guiding Principles](#guiding-principles)
1. [Environment Variable Fallback](#1-environment-variable-fallback) — v1.5
2. [Option & Argument Validation](#2-option--argument-validation) — v1.5
3. [Mutually Exclusive Options / Option Groups](#3-mutually-exclusive-options--option-groups) — v1.6
4. [Test Helper API](#4-test-helper-api) — v1.6

---

## 1. Environment Variable Fallback

**Target:** v1.5  
**Priority:** High

### 1.1 Motivation

CLI tools deployed in CI/CD pipelines, containers, and cloud environments commonly accept configuration through environment variables as a fallback when an option is not explicitly provided on the command line. Today, users must implement this manually:

```csharp
string? token = Environment.GetEnvironmentVariable("MY_TOKEN");
var app = new CommandApp("myexe")
{
    { "t|token=", "API {TOKEN}", v => token = v },
    // ...
};
```

This is verbose and the environment variable name is invisible to help output and shell completions. clap (Rust) and Click (Python) both support this natively.

### 1.2 Design Goals

- Integrate with the existing `Add` extension methods and collection-initializer pattern.
- Display the environment variable name in `--help` output.
- Keep shell completions non-executing and environment-agnostic (completions are based on the declared command tree, not runtime env var values).
- Env var is only consulted when the option is **not** provided on the command line.
- Support `ISpanParsable<T>` type conversion for env var values.
- Keep the feature opt-in per option — no global env-var convention.

### 1.3 API Design

#### 1.3.1 `Option.EnvironmentVariable` Property

```csharp
/// <summary>
/// Gets or sets the name of the environment variable used as a fallback
/// when this option is not provided on the command line.
/// </summary>
public string? EnvironmentVariable { get; set; }
```

Setting this property on any `Option` (including `HelpOption`, `VersionOption`, or custom subclasses) enables the fallback behavior.

#### 1.3.2 `Option.EnvironmentVariableDelimiter` Property (Optional)

Environment variables are typically single strings, but some options are naturally multi-valued (e.g., repeated `--include` paths).
To keep the default behavior predictable (especially on Unix, where `:` is common inside values such as URLs), **no splitting is performed by default**.
Instead, splitting is opt-in per option via a delimiter.

```csharp
/// <summary>
/// Gets or sets the delimiter used to split the environment variable value into
/// multiple occurrences of this option.
/// When null, the environment variable value is treated as a single value.
/// </summary>
public char? EnvironmentVariableDelimiter { get; set; }
```

Recommended delimiter for path-like lists: `Path.PathSeparator`.

#### 1.3.3 Extended `Add` Overloads

New overloads are added to `CommandExtensions` that accept an `envVar` parameter and (optionally) an `envVarDelimiter`.

To preserve **source compatibility** and avoid overload ambiguities, these new overloads make `envVar` a **required** parameter (typically passed by name). Existing call sites that don't use env vars remain unchanged and unambiguous.

**Single-value options:**

```csharp
// Shorthand (string action)
{ "t|token=", "API {TOKEN}", v => token = v, envVar: "MY_TOKEN" }

// Typed value
{ "p|port=", "Server {PORT}", (int v) => port = v, envVar: "SERVER_PORT" }

// Collection binding (opt-in splitting)
{ "i|include=", "Include {PATH}", includes, envVar: "MY_INCLUDES", envVarDelimiter: Path.PathSeparator }
```

If `prototype` is an **argument prototype** (e.g. `<file>`), specifying `envVar` is invalid and must throw an `ArgumentException` (env var fallback applies to options only).

**Implementation sketch:**

```csharp
public static TCommand Add<TCommand>(
    this TCommand command,
    string prototype,
    string? description,
    Action<string?> action,
    string envVar,
    char? envVarDelimiter = null,
    bool hidden = false)
    where TCommand : CommandContainer
{
    // ... existing logic to create ActionOption ...
    option.EnvironmentVariable = envVar;
    option.EnvironmentVariableDelimiter = envVarDelimiter;
    command.Add(option);
    return command;
}
```

### 1.4 Parsing Behavior

1. **Option parsing proceeds as normal.** If the user provides `--token VALUE` on the command line, the option action fires with `VALUE` and the env var is ignored.
2. **After option parsing completes for the current command,** apply env var fallbacks **before any step that depends on parsed option state**, including:
   - `CommandGroup` activation checks (`IsActive()`).
   - Sub-command resolution (`git commit` vs `git` default action).
   - Option-constraint validation (§3) and positional argument parsing.

   If `--help` was requested for this command, env var fallback must be skipped (help output should not fail because of invalid env vars).
3. **Fallback resolution:** for each option with a non-null `EnvironmentVariable` that was **not** explicitly set on the command line:
   - If the option is inactive (`!option.IsActive()`), skip it.
   - Resolve the env var via `CommandConfig.EnvironmentVariableResolver`.
   - If the resolved value is null or whitespace, treat it as "not set" and skip.
   - If `option.EnvironmentVariableDelimiter` is null: treat the env var value as a **single** occurrence of the option.
   - If `option.EnvironmentVariableDelimiter` is non-null: split the env var string on that delimiter, skip empty segments, and treat each segment as **one** occurrence of the option.
   - Each occurrence is fed through the same parsing/type-conversion/validation pipeline as if the user had provided the option on the command line.
4. **Conversion/validation errors:** if parsing fails, throw an `OptionException` that includes the option display name and the env var name, without echoing the env var value. Example:

   ```
   Invalid value for option `--port` (from environment variable `SERVER_PORT`): The value must be between 1 and 65535.
   ```
5. **Boolean/flag options (`OptionValueType.None`):** the env var value is interpreted as a boolean:
   - Truthy (`"true"`, `"1"`, `"yes"`, case-insensitive): invoke the option once (as if the flag was present).
   - Falsy (`"false"`, `"0"`, `"no"`, case-insensitive): treat as "not set" (do not invoke).
   - Any other non-empty value throws.

### 1.5 Help Output

In the default plain-text help output, when an option has an `EnvironmentVariable`, it is appended to the help description:

```
  -t, --token=TOKEN          API TOKEN [env: MY_TOKEN]
  -p, --port=PORT            Server PORT [env: SERVER_PORT]
```

The `[env: ...]` suffix is added automatically and is subject to `CommandConfig.Localizer`.

If a custom output renderer is used (see `doc/specs/pluggable-output-specs.md`), it may display environment variable fallback information differently while still using `Option.EnvironmentVariable` as the source of truth.

### 1.6 Shell Completions

- Completions are unaffected — they operate on the declared option tree, not on runtime values.
- Environment variable names are not suggested as completions (this is out of scope for shell completion protocols).

### 1.7 Testing Considerations

For testability, environment variable resolution can be overridden via a delegate on `CommandConfig`:

```csharp
/// <summary>
/// A delegate to resolve environment variables. Defaults to
/// <see cref="Environment.GetEnvironmentVariable(string)"/>.
/// </summary>
public Func<string, string?> EnvironmentVariableResolver { get; init; }
    = Environment.GetEnvironmentVariable;
```

This allows unit tests to inject controlled values without mutating the process environment.

### 1.8 Tracking State: "Was This Option Set?"

The parser needs to track two related facts:

- **Was the option explicitly set on the command line?** (used to decide whether to consult the env var)
- **Was the option set at all?** (command line *or* env var; used by constraints and test helpers)

A lightweight approach:

- Add internal state on `Option`:
  - `internal bool WasSetOnCommandLine;`
  - `internal bool WasSet;`
- Reset both flags at the start of parsing options for the current command.
- When the parser matches an option token from the command line, set `WasSetOnCommandLine = true` **before** invoking the option.
- When an option is invoked (from either command line parsing or env var fallback), set `WasSet = true`.
- Apply env var fallback only when `WasSetOnCommandLine == false`.

---

## 2. Option & Argument Validation

**Target:** v1.5  
**Priority:** High

### 2.1 Motivation

Currently, validation is performed either implicitly (via `ISpanParsable<T>` failing to parse) or explicitly in the command action. This leads to:

- Late error detection (validation happens after all parsing is complete).
- Manual error-message construction for common validations (ranges, non-empty strings, file existence).
- Inconsistent error formatting across options.

The goal is a **composable, chainable validation system** with built-in validators for common cases, producing consistent, user-friendly error messages, while keeping the inline validation story simple.

### 2.2 Design Goals

- **Chainable:** Multiple validators can be composed on a single option/argument.
- **Built-in validators** for common patterns: range, positive, non-empty, regex, set membership, file/directory existence.
- **Inline lambdas** for one-off validations with custom error messages.
- **Consistent error format:** All validation errors produce messages like:

  ```
  Invalid value for option `--port`: The value must be between 1 and 65535.
  ```
- **Apply at parse time:** Validation runs immediately after the value is parsed (inside `OnParseComplete`), before the next option/argument is processed.
- **AOT-friendly:** No reflection; validators are delegates or sealed classes.

### 2.3 API Design

#### 2.3.1 The `OptionValidator<T>` Delegate

```csharp
/// <summary>
/// Validates a parsed option value. Returns null if the value is valid,
/// or an error message describing why the value is invalid.
/// </summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
/// <param name="value">The parsed value to validate.</param>
/// <returns>An error message, or null if the value is valid.</returns>
public delegate string? OptionValidator<in T>(T value);
```

For arguments, the same delegate signature is reused (aliased or shared).

#### 2.3.2 Attaching Validators

Validators are attached at declaration time through new `Add` overloads (see §2.3.5). This keeps validation close to the option/argument definition and avoids adding mutable validation state to the `Option` / `CommandArgument` base types.

Custom `Option` / `CommandArgument` subclasses can continue to validate inside `OnParseComplete`.

#### 2.3.3 `Validate` Static Class — Built-in Validators

A static class provides factory methods that return `OptionValidator<T>` instances. Each validator produces a descriptive error message.

```csharp
/// <summary>
/// Provides built-in validators for options and arguments.
/// </summary>
public static class Validate
{
    /// <summary>
    /// Validates that a comparable value is within the specified inclusive range.
    /// Error: "The value must be between {min} and {max}."
    /// </summary>
    public static OptionValidator<T> Range<T>(T min, T max) where T : IComparable<T>;

    /// <summary>
    /// Validates that a numeric value is greater than zero.
    /// Error: "The value must be positive."
    /// </summary>
    public static OptionValidator<T> Positive<T>() where T : INumber<T>;

    /// <summary>
    /// Validates that a numeric value is zero or greater.
    /// Error: "The value must be zero or positive."
    /// </summary>
    public static OptionValidator<T> NonNegative<T>() where T : INumber<T>;

    /// <summary>
    /// Validates that a string is non-null and non-empty (after parsing).
    /// Error: "The value must not be empty."
    /// </summary>
    public static OptionValidator<string> NonEmpty();

    /// <summary>
    /// Validates that a string matches the specified regular expression pattern.
    /// Error: "The value must match the pattern '{pattern}'."
    /// </summary>
    public static OptionValidator<string> Matches(
        [StringSyntax(StringSyntaxAttribute.Regex)] string pattern,
        string? errorMessage = null);

    /// <summary>
    /// Validates that a string matches the specified regular expression.
    /// Prefer this overload for AOT friendliness by using <c>[GeneratedRegex]</c>.
    /// </summary>
    public static OptionValidator<string> Matches(Regex regex, string? errorMessage = null);

    /// <summary>
    /// Validates that the value is one of the specified allowed values.
    /// Error: "The value must be one of: {values}."
    /// </summary>
    public static OptionValidator<T> OneOf<T>(params T[] allowedValues)
        where T : IEquatable<T>;

    /// <summary>
    /// Validates that a path refers to an existing file.
    /// Error: "The file '{value}' does not exist."
    /// </summary>
    public static OptionValidator<string> FileExists();

    /// <summary>
    /// Validates that a path refers to an existing directory.
    /// Error: "The directory '{value}' does not exist."
    /// </summary>
    public static OptionValidator<string> DirectoryExists();

    /// <summary>
    /// Validates that a path refers to an existing file or directory.
    /// Error: "The path '{value}' does not exist."
    /// </summary>
    public static OptionValidator<string> PathExists();

    /// <summary>
    /// Combines multiple validators into a single validator that runs them
    /// in order and returns the first error message (short-circuit).
    /// </summary>
    public static OptionValidator<T> Chain<T>(params OptionValidator<T>[] validators);

    /// <summary>
    /// Creates a validator from an inline predicate with a custom error message.
    /// </summary>
    public static OptionValidator<T> That<T>(Func<T, bool> predicate, string errorMessage);

    /// <summary>
    /// Creates a validator from an inline function that returns an error message or null.
    /// </summary>
    public static OptionValidator<T> Custom<T>(OptionValidator<T> validator);
}
```

#### 2.3.4 `Validate.Chain` — Composing Validators

```csharp
public static OptionValidator<T> Chain<T>(params OptionValidator<T>[] validators)
{
    return value =>
    {
        foreach (var validator in validators)
        {
            var error = validator(value);
            if (error is not null) return error;
        }
        return null;
    };
}
```

Validators are composed by calling `Validate.Chain(...)` at declaration time.

#### 2.3.5 Extended `Add` Overloads

New overloads are added to `CommandExtensions` that accept a `validate` parameter.

To preserve **source compatibility** and avoid overload ambiguities, these new overloads make `validate` a **required** parameter (nullable). Existing call sites that don't use validation remain unchanged and unambiguous.

Typed option/argument bindings:

```csharp
public static TCommand Add<TCommand, T>(
    this TCommand command,
    string prototype,
    string? description,
    Action<T> action,
    OptionValidator<T>? validate,
    string? envVar = null,
    char? envVarDelimiter = null)
    where TCommand : CommandContainer
    where T : ISpanParsable<T>;
```

String option/argument bindings (with `hidden`, matching existing string-action overloads):

```csharp
public static TCommand Add<TCommand>(
    this TCommand command,
    string prototype,
    string? description,
    Action<string?> action,
    OptionValidator<string>? validate,
    string? envVar = null,
    char? envVarDelimiter = null,
    bool hidden = false)
    where TCommand : CommandContainer;
```

Notes:

- `envVar` / `envVarDelimiter` apply to **options only**. If `prototype` is an argument prototype (e.g. `<file>`) and `envVar` is specified, the method must throw an `ArgumentException`.
- When `envVarDelimiter` is non-null, it sets `Option.EnvironmentVariableDelimiter` (§1.3.2).

### 2.4 Usage Examples

#### Basic inline validation

```csharp
var app = new CommandApp("myexe")
{
    { "p|port=", "Server {PORT}", (int v) => port = v,
        validate: Validate.Range(1, 65535) },
};

// Error output:
// Invalid value for option `--port`: The value must be between 1 and 65535.
// Use `myexe --help` for usage.
```

#### Chained validators

```csharp
{ "n|count=", "Iteration {COUNT}", (int v) => count = v,
    validate: Validate.Chain(
        Validate.Positive<int>(),
        Validate.Range(1, 1000)
    )
}

// Error output:
// Invalid value for option `--count`: The value must be positive.
```

#### Inline predicate with custom message

```csharp
{ "e|email=", "Contact {EMAIL}", v => email = v,
    validate: Validate.That<string>(
        v => v.Contains('@'),
        "The value must be a valid email address."
    )
}

// Error output:
// Invalid value for option `--email`: The value must be a valid email address.
```

#### Non-empty string

```csharp
{ "n|name=", "Your {NAME}", v => name = v,
    validate: Validate.NonEmpty() }

// Error output:
// Invalid value for option `--name`: The value must not be empty.
```

#### File existence check

```csharp
{ "<input>", "Input {FILE}", v => input = v,
    validate: Validate.FileExists() }

// Error output:
// Invalid value for argument `<input>`: The file `missing.txt` does not exist.
```

#### Enum-like set membership

```csharp
{ "f|format=", "Output {FORMAT}", v => format = v,
    validate: Validate.OneOf("json", "xml", "csv") }

// Error output:
// Invalid value for option `--format`: The value must be one of: json, xml, csv.
```

#### Combining validation with env var fallback

```csharp
{ "p|port=", "Server {PORT}", (int v) => port = v,
    validate: Validate.Range(1, 65535),
    envVar: "SERVER_PORT" }
```

When the env var `SERVER_PORT=99999` is set and `--port` is not provided, the validator runs on the env var value and produces:

```
Invalid value for option `--port` (from environment variable `SERVER_PORT`): The value must be between 1 and 65535.
```

### 2.5 Error Format

All validation errors follow a consistent template, controlled by `CommandConfig.Localizer`:

```
Invalid value for option `{optionDisplayName}`: {validatorErrorMessage}
```

For arguments:

```
Invalid value for argument `{argumentDisplayName}`: {validatorErrorMessage}
```

When triggered by an environment variable:

```
Invalid value for option `{optionDisplayName}` (from environment variable `{envVarName}`): {validatorErrorMessage}
```

### 2.6 Implementation Notes

- Validation executes inside `OnParseComplete` of the internal `ActionOption<T>` / `ActionArgument<T>` classes, after type conversion but before invoking the user's action.
- For optional values (option value omitted, or optional positional argument missing), validation is skipped when the raw value is null.
- If validation fails, an `OptionException` (for options) or `CommandArgumentException` (for arguments) is thrown, consistent with existing error handling.
- Validators are pure functions (no side effects), making them testable and composable.
- All built-in validators in the `Validate` class are implemented as `static` methods returning delegate instances — no allocations per parse, AOT-friendly.
- The `Validate.Matches` method should use `[GeneratedRegex]` internally when possible, or accept a pre-compiled `Regex` overload.

---

## 3. Mutually Exclusive Options / Option Groups

**Target:** v1.6  
**Priority:** Medium

### 3.1 Motivation

Many CLI tools have options that conflict with each other (e.g., `--json` vs. `--xml`, `--quiet` vs. `--verbose`), or options that must appear together (e.g., `--user` requires `--password`). Currently, users validate these constraints manually in the command action. This is verbose and produces inconsistent error messages.

### 3.2 Design Goals

- Declare conflicts and requirements alongside option definitions.
- Produce clear error messages (see §3.5).
- Validate after all options are parsed (not during — since order shouldn't matter).
- Keep the API lightweight — avoid a full "option group" abstraction.
- Work with the `CommandGroup` conditional visibility system.

### 3.3 API Design

#### 3.3.1 `OptionConstraint` Class

A small sealed class hierarchy for expressing constraints:

```csharp
/// <summary>
/// Defines a constraint between options in a command.
/// </summary>
public abstract class OptionConstraint : CommandNode
{
}

/// <summary>
/// Declares that the specified options cannot be used together.
/// </summary>
public sealed class MutuallyExclusiveConstraint : OptionConstraint
{
    /// <summary>
    /// Creates a new mutually exclusive constraint.
    /// </summary>
    /// <param name="optionNames">
    /// Two or more option names (without prefix).
    /// These are matched against the option names declared in the same command.
    /// </param>
    public MutuallyExclusiveConstraint(params string[] optionNames);

    /// <summary>
    /// Gets the option names in this exclusive group.
    /// </summary>
    public IReadOnlyList<string> OptionNames { get; }
}

/// <summary>
/// Declares that when the specified option is present, all required options must also be present.
/// </summary>
public sealed class RequiresConstraint : OptionConstraint
{
    /// <summary>
    /// Creates a new "requires" constraint.
    /// </summary>
    /// <param name="optionName">The option that triggers the requirement (without prefix).</param>
    /// <param name="requiredOptionNames">One or more options required when <paramref name="optionName"/> is present.</param>
    public RequiresConstraint(string optionName, params string[] requiredOptionNames);

    /// <summary>
    /// Gets the option that triggers the requirement.
    /// </summary>
    public string OptionName { get; }

    /// <summary>
    /// Gets the options required when <see cref="OptionName"/> is present.
    /// </summary>
    public IReadOnlyList<string> RequiredOptionNames { get; }
}
```

#### 3.3.2 Collection-Initializer Integration

Constraints are added to a `Command`/`CommandApp` like any other node:

```csharp
var app = new CommandApp("myexe")
{
    { "j|json", "Output as JSON", v => json = true },
    { "x|xml",  "Output as XML",  v => xml = true },
    { "q|quiet",   "Suppress output", v => quiet = true },
    { "V|verbose", "Verbose output",  v => verbose = true },
    { "u|user=",     "Username", v => user = v },
    { "P|password=", "Password", v => password = v },

    // Constraints
    new MutuallyExclusiveConstraint("json", "xml"),
    new MutuallyExclusiveConstraint("quiet", "verbose"),
    new RequiresConstraint("password", "user"),
};
```

#### 3.3.3 Convenience Extension Methods

```csharp
/// <summary>
/// Declares that the specified options cannot be used together.
/// </summary>
public static TCommand AddMutuallyExclusive<TCommand>(
    this TCommand command,
    params string[] optionNames)
    where TCommand : CommandContainer;

/// <summary>
/// Declares that when <paramref name="optionName"/> is present,
/// all <paramref name="requiredOptionNames"/> must also be present.
/// </summary>
public static TCommand AddRequires<TCommand>(
    this TCommand command,
    string optionName,
    params string[] requiredOptionNames)
    where TCommand : CommandContainer;
```

### 3.4 Validation Timing

Constraint validation occurs after option parsing + env var fallback for the current command and before any step that depends on the effective option set (sub-command resolution, argument parsing, command action).

The order for a run is:

1. Parse command-line options for the current command (existing flow).
2. Apply environment variable fallbacks for this command (§1).
3. **Validate option constraints** for this command (§3).
4. If a sub-command token is present, resolve it and repeat steps 1–3 for the sub-command.
5. Parse positional arguments for the resolved command.
6. Invoke the resolved command action.

If `--help` is requested for a command, constraints should not be evaluated for that command (help output should not fail because of invalid env vars or constraint violations).

### 3.5 Error Messages

```
Options `--json` and `--xml` cannot be used together.
Option `--password` requires `--user` to also be specified.
```

All messages pass through `CommandConfig.Localizer`.

### 3.6 Interaction with Conditional Groups

Constraints reference options by name. If an option is inside a `CommandGroup` that is currently inactive, the option is treated as "not set" for constraint purposes. A constraint referencing an inactive option does **not** produce an error (the constraint is silently skipped for that option).

### 3.7 Help Output

Constraints are **not** displayed in help output by default. They are enforcement-only. A future enhancement could annotate mutually exclusive options in help (e.g., `[conflicts: --xml]`), but this is out of scope for the initial implementation.

---

## 4. Test Helper API

**Target:** v1.6  
**Priority:** Medium

### 4.1 Motivation

Testing CLI applications built with XenoAtom.CommandLine currently requires:

1. Redirecting `TextWriter` for stdout/stderr.
2. Invoking `RunAsync` with test arguments.
3. Asserting on the exit code and captured string output.

This works but is verbose. There is no way to inspect the parsed state (which options were set, which command was selected) without running the full action. System.CommandLine offers a `ParseResult` object; a lighter version would improve testability.

### 4.2 Design Goals

- Provide a structured result of parsing without executing the command action.
- Allow inspection of which options were set, their values, which command was resolved, and any errors.
- Keep the API small — a single method and a result type.
- Do not change the existing `RunAsync` behavior; this is an additive API.

### 4.3 API Design

#### 4.3.1 `CommandApp.Parse` Method

```csharp
/// <summary>
/// Parses the specified arguments without executing the command action.
/// Options actions are invoked (to capture values), but the command action is not.
/// </summary>
/// <param name="arguments">The arguments to parse.</param>
/// <param name="runConfig">
/// Optional run configuration. When null, <see cref="TextWriter.Null"/> is used for
/// stdout/stderr to minimize side effects from option actions during parsing.
/// </param>
/// <returns>A <see cref="ParseResult"/> describing the outcome.</returns>
public ParseResult Parse(IEnumerable<string> arguments, CommandRunConfig? runConfig = null);
```

#### 4.3.2 `ParseResult` Class

```csharp
/// <summary>
/// The result of parsing command-line arguments.
/// </summary>
public sealed class ParseResult
{
    /// <summary>
    /// Gets the command that was resolved by parsing (the deepest matched sub-command).
    /// </summary>
    public Command ResolvedCommand { get; }

    /// <summary>
    /// Gets the full command path that was resolved (e.g., "myexe commit").
    /// </summary>
    public string ResolvedCommandPath { get; }

    /// <summary>
    /// Gets the effective option values after parsing (including environment variable fallbacks).
    /// Values are the raw strings as seen by option actions (after option-value splitting).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string?>> OptionValues { get; }

    /// <summary>
    /// Gets the positional argument values that were parsed.
    /// </summary>
    public IReadOnlyList<string> ArgumentValues { get; }

    /// <summary>
    /// Gets any remaining/unprocessed arguments.
    /// </summary>
    public IReadOnlyList<string> RemainingArguments { get; }

    /// <summary>
    /// Gets the errors that occurred during parsing, if any.
    /// When non-empty, <see cref="HasErrors"/> is true.
    /// </summary>
    public IReadOnlyList<CommandException> Errors { get; }

    /// <summary>
    /// Gets a value indicating whether parsing produced errors.
    /// </summary>
    public bool HasErrors { get; }

    /// <summary>
    /// Gets a value indicating whether --help was requested.
    /// </summary>
    public bool HelpRequested { get; }

    /// <summary>
    /// Gets a value indicating whether --version was requested.
    /// </summary>
    public bool VersionRequested { get; }
}
```

#### 4.3.3 Behavior

- `Parse` performs the same parsing pipeline as `RunAsync` for the resolved command, including env var fallbacks (§1) and option constraints (§3), but it does **not** invoke the resolved command action (`Command.Action`).
- `Parse` invokes option and argument actions so that the caller's captured variables are populated (consistent with the library's composition/side-effect model).
- Errors are collected into `ParseResult.Errors` instead of being thrown. This allows tests to assert on error content without `try/catch`.
- `HelpRequested` is `true` when `--help` (or equivalent) was parsed. `Parse` does not call `ShowHelp`.
- `VersionRequested` is `true` when `--version` was parsed (when using `VersionOption`).
- Option actions may still write to `ctx.Out`/`ctx.Error` (e.g., custom options). When `runConfig` is null, `TextWriter.Null` is used to reduce noise in tests.

### 4.4 Usage Examples

#### Testing option parsing

```csharp
string? name = null;
int port = 0;

var app = new CommandApp("myexe")
{
    { "n|name=", "Your {NAME}", v => name = v },
    { "p|port=", "Server {PORT}", (int v) => port = v },
    new HelpOption(),
    (ctx, _) => ValueTask.FromResult(0),
};

var result = app.Parse(["--name", "Alice", "--port", "8080"]);

Assert.IsFalse(result.HasErrors);
Assert.AreEqual("Alice", name);
Assert.AreEqual(8080, port);
Assert.AreEqual("myexe", result.ResolvedCommandPath);
```

#### Testing sub-command resolution

```csharp
var app = new CommandApp("git")
{
    new Command("commit") { ... },
    new Command("push") { ... },
};

var result = app.Parse(["commit", "--message", "fix"]);

Assert.AreEqual("git commit", result.ResolvedCommandPath);
```

#### Testing error detection

```csharp
var result = app.Parse(["--unknown-option"]);

Assert.IsTrue(result.HasErrors);
Assert.IsTrue(result.Errors[0].Message.Contains("Unknown option"));
```

#### Testing help detection

```csharp
var result = app.Parse(["--help"]);

Assert.IsTrue(result.HelpRequested);
Assert.IsFalse(result.HasErrors);
```

### 4.5 Implementation Notes

- Internally, `Parse` can reuse the existing `ParseOptions` + env var fallback + constraint validation + `ParseArgumentsAndDefaultOption` flow, but must be implemented as a dedicated path (not by calling `RunAsync`) so it can collect errors without writing to stderr.
- Errors that are currently thrown as exceptions during parsing should be caught and added to the `Errors` list instead of propagating.
- The `OptionValues` dictionary should be keyed by a canonical option name (prefer a long alias when present) and contain the raw values as seen by option actions (including values originating from env var fallback).

---

## Cross-Cutting Concerns

### Parameter Ordering Convention

Throughout all new `Add` overloads, the parameter order follows this convention:

```
- Env-var overloads:  prototype, description, action/list, envVar, envVarDelimiter?, hidden?
- Validation overloads: prototype, description, action/list, validate, envVar?, envVarDelimiter?, hidden?
```

To avoid overload ambiguity and preserve source compatibility, `validate` (for validation overloads) and `envVar` (for env-var overloads) are **required** parameters (no defaults). Additional parameters (e.g., `envVarDelimiter`, `hidden`) are optional and intended to be passed by name.

### Localization

All new error messages (validation errors, constraint violations, env var errors) pass through `CommandConfig.Localizer` before being written to `Error` or included in exceptions.

### Rich Diagnostics for Error Rendering (v2.0 integration)

To enable rich error displays (e.g., re-printing the invocation and underlining the exact option/argument/value that failed, similar to Rust compiler diagnostics), parsing and validation errors should carry structured diagnostic metadata.

This is specified in `doc/specs/pluggable-output-specs.md` as an additive `CommandException.Diagnostic` payload (`CommandDiagnostic`, `CommandTokenSpan`, `CommandDiagnosticSource`). When the pluggable output system is implemented, the features in this document should populate that payload as follows:

- **Option value parse/validation errors:** set `Diagnostic.Node` to the `Option`, `Diagnostic.Source` to `CommandLine` (or `EnvironmentVariable` when coming from env var fallback), and `Diagnostic.TokenSpan` to the token/span corresponding to the failing value.
- **Argument parse/validation errors:** set `Diagnostic.Node` to the `CommandArgument`, `Diagnostic.Source` to `CommandLine`, and `Diagnostic.TokenSpan` to the failing positional token (or to a caret-at-end span for "missing required argument").
- **Env var fallback errors:** set `Diagnostic.Source` to `EnvironmentVariable` and `Diagnostic.SourceName` to the env var name; do not include the env var value.

The default plain-text output remains unchanged; custom output renderers can use the diagnostic payload to implement underlined, context-rich messages.

### AOT / Trimming Compatibility

All new features use:

- Delegates (no reflection).
- Sealed classes (no virtual dispatch overhead).
- Generic constraints (`IComparable<T>`, `IEquatable<T>`, `ISpanParsable<T>`, `INumber<T>`) already used elsewhere in the library / BCL.
- No `System.ComponentModel.DataAnnotations` dependency (that would require reflection and adds a dependency).

### Documentation Updates

Each feature requires:

1. XML doc comments on all public APIs.
2. Updates to `doc/readme.md` (user guide) with examples.
3. Updates to `readme.md` (project README) feature list if applicable.

### Test Coverage

Each feature requires:

1. Unit tests covering happy paths.
2. Unit tests covering error paths (invalid values, constraint violations).
3. Integration tests using `ParseResult` (§4) to verify combined behavior (e.g., validation + env var fallback).
4. Completion tests verifying that new features don't break existing completion behavior.

