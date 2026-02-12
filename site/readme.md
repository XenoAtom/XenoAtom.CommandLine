---
title: Home
layout: simple
og_type: website
---

<section class="text-center py-5 text-white hero-text">
  <div class="container">
    <h1 class="fw-bold display-6">
      <span class="c64-text">XenoAtom.CommandLine</span>
    </h1>
    <p class="lead mt-3 mb-4">
      A lightweight, powerful and NativeAOT-friendly command-line parser for .NET.<br>
      Build composable CLIs with commands, options, arguments, validation, and pluggable outputs.
    </p>
    <div class="d-flex justify-content-center gap-3 mt-4 flex-wrap">
      <a href="{{site.basepath}}/docs/getting-started/" class="btn btn-primary btn-lg"><i class="bi bi-rocket-takeoff"></i> Get started</a>
      <a href="{{site.basepath}}/docs/" class="btn btn-outline-light btn-lg"><i class="bi bi-book"></i> Browse docs</a>
      <a href="https://github.com/XenoAtom/XenoAtom.CommandLine/" class="btn btn-info btn-lg"><i class="bi bi-github"></i> GitHub</a>
    </div>
    <div class="mt-4 text-start mx-auto" style="max-width: 56rem;">
      <pre class="language-shell-session"><code>dotnet add package XenoAtom.CommandLine</code></pre>
      <pre class="language-shell-session"><code>dotnet add package XenoAtom.CommandLine.Terminal # For terminal UI and visual output</code></pre>
    </div>
    <img class="terminal img-fluid" src="{{site.basepath}}/img/xenoatom-commandline-show.gif"></img>
  </div>
</section>

<section class="container my-5">
  <h2 class="display-6 mb-4"><i class="bi bi-stars xenoatom-feature-icon xenoatom-icon--themes"></i>Features</h2>
  <div class="row row-cols-1 row-cols-lg-2 row-cols-xxl-3 gx-4 gy-4">
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-diagram-3 xenoatom-feature-icon xenoatom-icon--layout"></i> Commands &amp; Composition</div>
        <div class="card-body">
          <ul>
            <li><strong>Structure:</strong> nested commands and command trees</li>
            <li><strong>Authoring:</strong> collection initializer and fluent helpers</li>
            <li><strong>Sections:</strong> usage/text nodes and explicit group headers</li>
            <li><strong>Conditionality:</strong> runtime-activated groups</li>
          </ul>
          <p><a href="{{site.basepath}}/docs/commands/">Commands</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-terminal xenoatom-feature-icon xenoatom-icon--input"></i> Parsing &amp; Syntax</div>
        <div class="card-body">
          <ul>
            <li><strong>Prefixes:</strong> <code>-</code>, <code>--</code>, <code>/</code> with aliases</li>
            <li><strong>Values:</strong> required, optional, and key/value option forms</li>
            <li><strong>Short bundles:</strong> POSIX-style <code>-abc</code> expansion</li>
            <li><strong>Positionals:</strong> <code>&lt;arg&gt;</code>, <code>?/*/+</code>, and <code>&lt;&gt;</code></li>
          </ul>
          <p><a href="{{site.basepath}}/docs/options/">Options</a> · <a href="{{site.basepath}}/docs/arguments/">Arguments</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-shield-check xenoatom-feature-icon xenoatom-icon--actions"></i> Validation &amp; Constraints</div>
        <div class="card-body">
          <ul>
            <li><strong>Built-ins:</strong> range, non-empty, one-of, and path helpers</li>
            <li><strong>Custom rules:</strong> delegate validation on options/arguments</li>
            <li><strong>Exclusivity:</strong> mutually exclusive option sets</li>
            <li><strong>Dependencies:</strong> requires constraints between options</li>
          </ul>
          <p><a href="{{site.basepath}}/docs/validation/">Validation &amp; Constraints</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-chat-square-text xenoatom-feature-icon xenoatom-icon--debug"></i> Help &amp; Diagnostics</div>
        <div class="card-body">
          <ul>
            <li><strong>Help generation:</strong> usage and sections from declarations</li>
            <li><strong>Suggestions:</strong> unknown command/option hints</li>
            <li><strong>Context:</strong> diagnostics carry command/option/argument info</li>
            <li><strong>Readability:</strong> clear errors with usage guidance</li>
          </ul>
          <p><a href="{{site.basepath}}/docs/help-output/">Help &amp; Output</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-plug xenoatom-feature-icon xenoatom-icon--binding"></i> Extensible Output</div>
        <div class="card-body">
          <ul>
            <li><strong>Pluggable:</strong> replace <code>ICommandOutput</code> end-to-end</li>
            <li><strong>Modes:</strong> default, markup, and visual help/error rendering</li>
            <li><strong>Factory-based:</strong> context-aware output construction</li>
            <li><strong>Custom formats:</strong> add JSON or app-specific renderers</li>
          </ul>
          <p><a href="{{site.basepath}}/docs/help-output/">Custom Output Rendering</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-display xenoatom-feature-icon xenoatom-icon--controls"></i> Terminal Integration</div>
        <div class="card-body">
          <ul>
            <li><strong>Optional package:</strong> <code>XenoAtom.CommandLine.Terminal</code></li>
            <li><strong>Markup output:</strong> colored help and diagnostics</li>
            <li><strong>Visual output:</strong> rich help trees and grouped blocks</li>
            <li><strong>Composable visuals:</strong> inline visual nodes in command trees</li>
          </ul>
          <p><a href="{{site.basepath}}/docs/advanced/">Advanced Topics</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-tools xenoatom-feature-icon xenoatom-icon--data"></i> Tooling &amp; Workflow</div>
        <div class="card-body">
          <ul>
            <li><strong>Parse API:</strong> inspect parse results without execution</li>
            <li><strong>Response files:</strong> <code>@file</code> expansion with comments/quotes</li>
            <li><strong>Completions:</strong> bash/zsh/fish/PowerShell generation</li>
            <li><strong>Environment:</strong> option fallback from environment variables</li>
          </ul>
          <p><a href="{{site.basepath}}/docs/advanced/">Advanced Topics</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-lightning-charge xenoatom-feature-icon xenoatom-icon--themes"></i> Runtime &amp; Platform</div>
        <div class="card-body">
          <ul>
            <li><strong>NativeAOT:</strong> trimming-friendly architecture</li>
            <li><strong>Performance:</strong> low-allocation, span-based parser paths</li>
            <li><strong>Portability:</strong> cross-platform CLI semantics</li>
            <li><strong>Minimalism:</strong> lightweight core package and focused API</li>
          </ul>
          <p><a href="{{site.basepath}}/docs/migration-2.0/">Migration 2.0</a></p>
        </div>
      </div>
    </div>
  </div>
</section>
