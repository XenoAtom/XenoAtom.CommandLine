// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace XenoAtom.CommandLine;

/// <summary>
/// Adds built-in shell completion support to a <see cref="CommandApp"/>.
/// </summary>
/// <remarks>
/// It adds:
/// - <c>completion &lt;shell&gt;</c>: prints shell glue scripts (bash/zsh/fish/powershell).
/// - <c>__complete</c>: a hidden command used by the scripts to query completion candidates.
/// </remarks>
public sealed class CompletionCommands : CommandGroup
{
    /// <summary>
    /// Creates an instance of <see cref="CompletionCommands"/>.
    /// </summary>
    /// <param name="completionCommandName">The public command name used to generate completion scripts. Default is <c>completion</c>.</param>
    /// <param name="completeRequestCommandName">The hidden command name used by glue scripts. Default is <c>__complete</c>.</param>
    public CompletionCommands(string completionCommandName = "completion", string completeRequestCommandName = "__complete")
    {
        Add(new CompletionRequestCommand(completeRequestCommandName));
        Add(new CompletionScriptCommand(completionCommandName, completeRequestCommandName));
    }

    private sealed class CompletionRequestCommand : NoLicenseCommand
    {
        public CompletionRequestCommand(string name) : base(name, "Internal command used to request completion candidates.")
        {
            Hidden = true;

            string? line = null;
            int cursor = 0;
            string? commandName = null;
            var tokens = new List<string>();
            var tokenIndex = -1;

            this.Add("line=", "The full command {0:LINE}", v => line = v);
            this.Add("cursor=", "The cursor {0:POSITION} (0-based)", (int v) => cursor = v);
            this.Add("command-name=", "The invoked {0:COMMAND} name", v => commandName = v);
            this.Add("token=", "A tokenized argument {0:TOKEN} (repeatable)", v =>
            {
                if (v is not null) tokens.Add(v);
            });
            this.Add("index=", "The token {0:INDEX} being completed (0-based)", (int v) => tokenIndex = v);

            Action = (ctx, _) =>
            {
                var app = GetCommandApp();
                if (app is null)
                    throw new InvalidOperationException("Cannot resolve CommandApp from the current command.");

                IEnumerable<string> candidates;
                if (tokens.Count > 0)
                {
                    if (tokenIndex < 0)
                        throw new OptionException("Missing required value for option 'index' when using token mode.", "index");
                    candidates = app.GetCompletionsForTokens(tokens, tokenIndex, commandName);
                }
                else
                {
                    if (line is null)
                        throw new OptionException("Missing required value for option 'line'.", "line");
                    candidates = app.GetCompletionsForLine(line, cursor, commandName);
                }

                foreach (var candidate in candidates)
                {
                    ctx.Out.WriteLine(candidate);
                }

                return ValueTask.FromResult(0);
            };
        }
    }

    private sealed class CompletionScriptCommand : NoLicenseCommand
    {
        private readonly string _completeRequestCommandName;

        public CompletionScriptCommand(string name, string completeRequestCommandName) : base(name, "Generate shell completion scripts.")
        {
            Hidden = true;

            _completeRequestCommandName = completeRequestCommandName;

            Add(new HelpOption());

            Add(new NoLicenseCommand("bash", "Generate bash completion script")
            {
                Action = (ctx, _) => PrintScript(ctx, Shell.Bash)
            });

            Add(new NoLicenseCommand("zsh", "Generate zsh completion script")
            {
                Action = (ctx, _) => PrintScript(ctx, Shell.Zsh)
            });

            Add(new NoLicenseCommand("fish", "Generate fish completion script")
            {
                Action = (ctx, _) => PrintScript(ctx, Shell.Fish)
            });

            Add(new NoLicenseCommand("powershell", "Generate PowerShell completion script")
            {
                Action = (ctx, _) => PrintScript(ctx, Shell.PowerShell)
            });

            Action = (ctx, _) =>
            {
                ctx.ShouldShowHelp = true;
                return ValueTask.FromResult(0);
            };
        }

        private ValueTask<int> PrintScript(CommandRunContext ctx, Shell shell)
        {
            var app = GetCommandApp();
            if (app is null)
                throw new InvalidOperationException("Cannot resolve CommandApp from the current command.");

            var commandName = app.Name;
            var invocationArguments = GetCompletionInvocationArguments(commandName);
            ctx.Out.Write(GenerateScript(shell, commandName, invocationArguments, _completeRequestCommandName));
            return ValueTask.FromResult(0);
        }

        private static string[] GetCompletionInvocationArguments(string commandName)
        {
            var processPath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(processPath))
                return [commandName];

            // If invoked via `dotnet <app.dll>`, `Environment.ProcessPath` points to dotnet, so we need the entry assembly path as well.
            var fileName = Path.GetFileNameWithoutExtension(processPath);
            if (string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase))
            {
                var commandLine = Environment.GetCommandLineArgs();
                for (var i = 1; i < commandLine.Length; i++)
                {
                    var arg = commandLine[i];
                    if (!arg.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!File.Exists(arg))
                        continue;
                    return [processPath, arg];
                }
            }

            return [processPath];
        }

        private static string GenerateScript(Shell shell, string commandName, string[] invocationArguments, string completeRequestCommandName)
        {
            return shell switch
            {
                Shell.Bash => GenerateBash(commandName, invocationArguments, completeRequestCommandName),
                Shell.Zsh => GenerateZsh(commandName, invocationArguments, completeRequestCommandName),
                Shell.Fish => GenerateFish(commandName, invocationArguments, completeRequestCommandName),
                Shell.PowerShell => GeneratePowerShell(commandName, invocationArguments, completeRequestCommandName),
                _ => throw new ArgumentOutOfRangeException(nameof(shell), shell, null),
            };
        }

        private static string GenerateBash(string commandName, string[] invocationArguments, string completeRequestCommandName)
        {
            var fn = $"_{commandName}_complete";
            var sb = new StringBuilder();
            sb.AppendLine($"# Bash completion for {commandName}");
            sb.AppendLine($"# Usage (on PATH):          eval \"$({commandName} completion bash)\"");
            sb.AppendLine($"# Usage (current folder):   eval \"$(./{commandName} completion bash)\"");
            sb.AppendLine($"# Usage (Windows/Git Bash): eval \"$(./{commandName}.exe completion bash)\"");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"{fn}() {{");
            sb.AppendLine("  local IFS=$'\\n'");
            sb.AppendLine("  COMPREPLY=()");
            sb.Append("  local -a cmd=(");
            AppendPosixArray(sb, invocationArguments);
            sb.AppendLine(")");
            sb.Append("  local out=$(\"${cmd[@]}\" ");
            sb.Append(QuotePosixSingle(completeRequestCommandName));
            sb.AppendLine(" --command-name \"${COMP_WORDS[0]}\" --line \"$COMP_LINE\" --cursor \"$COMP_POINT\" 2>/dev/null)");
            sb.AppendLine("  local line");
            sb.AppendLine("  while IFS= read -r line; do");
            sb.AppendLine("    [[ -z \"$line\" ]] && continue");
            sb.AppendLine("    COMPREPLY+=(\"$line\")");
            sb.AppendLine("  done <<< \"$out\"");
            sb.AppendLine("}");
            sb.AppendLine($"complete -o default -F {fn} -- {QuotePosixSingle(commandName)} {QuotePosixSingle("./" + commandName)} {QuotePosixSingle(commandName + ".exe")} {QuotePosixSingle("./" + commandName + ".exe")}");
            return sb.ToString();
        }

        private static string GenerateZsh(string commandName, string[] invocationArguments, string completeRequestCommandName)
        {
            var fn = $"_{commandName}_complete";
            var sb = new StringBuilder();
            sb.AppendLine($"#compdef {commandName}");
            sb.AppendLine();
            sb.AppendLine($"# Zsh completion for {commandName}");
            sb.AppendLine($"# Usage (current session): source <({commandName} completion zsh)");
            sb.AppendLine($"# Usage (~/.zshrc):        source <({commandName} completion zsh)");
            sb.AppendLine($"# Optional (compinit-based install):");
            sb.AppendLine($"#   {commandName} completion zsh > \"${{fpath[1]}}/_{commandName}\"");
            sb.AppendLine($"#   autoload -U compinit && compinit");
            sb.AppendLine();
            sb.AppendLine("(( ${+functions[compdef]} )) || { autoload -Uz compinit && compinit }");
            sb.AppendLine();
            sb.AppendLine($"{fn}() {{");
            sb.AppendLine("  local -a candidates");
            sb.Append("  local -a cmd=(");
            AppendPosixArray(sb, invocationArguments);
            sb.AppendLine(")");
            sb.Append("  candidates=(${(f)\"$(");
            sb.Append("\"${cmd[@]}\" ");
            sb.Append(QuotePosixSingle(completeRequestCommandName));
            sb.AppendLine(" --command-name \"$words[1]\" --line \"$BUFFER\" --cursor \"$CURSOR\" 2>/dev/null)\"})");
            sb.AppendLine("  compadd -Q -- \"${candidates[@]}\"");
            sb.AppendLine("}");
            sb.AppendLine($"compdef {fn} {commandName}");
            return sb.ToString();
        }

        private static string GenerateFish(string commandName, string[] invocationArguments, string completeRequestCommandName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Fish completion for {commandName}");
            sb.AppendLine($"# Usage (current session): {commandName} completion fish | source");
            sb.AppendLine($"# Usage (~/.config/fish/config.fish): {commandName} completion fish | source");
            sb.AppendLine($"# Optional (file install): {commandName} completion fish > ~/.config/fish/completions/{commandName}.fish");
            sb.AppendLine();
            sb.AppendLine($"function __fish_{commandName}_complete");
            sb.AppendLine("  set -l line (commandline)");
            sb.AppendLine("  set -l cursor (commandline -C)");
            sb.AppendLine("  set -l cmdName (commandline -opc)[1]");
            sb.Append("  set -l invocation ");
            AppendFishArray(sb, invocationArguments);
            sb.AppendLine();
            sb.Append("  $invocation ");
            sb.Append(QuoteFishDouble(completeRequestCommandName));
            sb.AppendLine(" --command-name \"$cmdName\" --line \"$line\" --cursor \"$cursor\" 2>/dev/null");
            sb.AppendLine("end");
            sb.AppendLine();
            sb.AppendLine($"complete -c {commandName} -f -a \"(__fish_{commandName}_complete)\"");
            return sb.ToString();
        }

        private static string GeneratePowerShell(string commandName, string[] invocationArguments, string completeRequestCommandName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# PowerShell completion for {commandName}");
            sb.AppendLine($"# Usage: {commandName} completion powershell | Out-String | Invoke-Expression");
            sb.AppendLine();
            sb.AppendLine($"Register-ArgumentCompleter -Native -CommandName '{commandName}' -ScriptBlock {{");
            sb.AppendLine("  param($wordToComplete, $commandAst, $cursorPosition)");
            sb.AppendLine("  $line = $commandAst.ToString()");
            sb.AppendLine("  $cursor = [int]$cursorPosition");
            sb.AppendLine("  $cmdName = $commandAst.CommandElements[0].ToString()");
            sb.Append("  $candidates = ");
            sb.Append(BuildPowerShellInvocation(invocationArguments));
            sb.Append(' ');
            sb.Append(QuotePowerShellSingle(completeRequestCommandName));
            sb.AppendLine(" --command-name $cmdName --line $line --cursor $cursor 2>$null");
            sb.AppendLine("  foreach ($c in $candidates) {");
            sb.AppendLine("    if ([string]::IsNullOrWhiteSpace($c)) { continue }");
            sb.AppendLine("    if ($wordToComplete -and -not $c.StartsWith($wordToComplete, [System.StringComparison]::OrdinalIgnoreCase)) { continue }");
            sb.AppendLine("    [System.Management.Automation.CompletionResult]::new($c, $c, 'ParameterValue', $c)");
            sb.AppendLine("  }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void AppendPosixArray(StringBuilder sb, string[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(QuotePosixSingle(values[i]));
            }
        }

        private static void AppendFishArray(StringBuilder sb, string[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(QuoteFishDouble(values[i]));
            }
        }

        private static string QuotePosixSingle(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('\'');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '\'')
                {
                    sb.Append("'\\''");
                }
                else
                {
                    sb.Append(c);
                }
            }
            sb.Append('\'');
            return sb.ToString();
        }

        private static string QuoteFishDouble(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '\\' || c == '"' || c == '$')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string QuotePowerShellSingle(string value)
        {
            return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
        }

        private static string BuildPowerShellInvocation(string[] invocationArguments)
        {
            var sb = new StringBuilder();
            sb.Append("& ");
            for (var i = 0; i < invocationArguments.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(QuotePowerShellSingle(invocationArguments[i]));
            }
            return sb.ToString();
        }

        private enum Shell
        {
            Bash,
            Zsh,
            Fish,
            PowerShell,
        }
    }

    private class NoLicenseCommand(string name, string? help = null) : Command(name, help)
    {
        protected override CommandRunContext CreateCommandContext(CommandRunConfig config)
        {
            var ctx = base.CreateCommandContext(config);
            ctx.ShouldShowLicenseOnRun = false;
            return ctx;
        }
    }
}
