---
title: Home
layout: simple
og_type: website
---

<section class="text-center py-5">
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
      <a href="{{site.basepath}}/docs/" class="btn btn-outline-secondary btn-lg"><i class="bi bi-book"></i> Browse docs</a>
      <a href="https://github.com/XenoAtom/XenoAtom.CommandLine/" class="btn btn-info btn-lg"><i class="bi bi-github"></i> GitHub</a>
    </div>
    <div class="row row-cols-1 row-cols-lg-2 gx-3 gy-3 mt-4 text-start mx-auto" style="max-width: 66rem;">
      <div class="col">
        <div class="card h-100">
          <div class="card-header h5"><i class="bi bi-box-seam lunet-feature-icon lunet-icon--input"></i>Core package</div>
          <div class="card-body">
            <p class="mb-2">Install <code>XenoAtom.CommandLine</code> for the parser, help system, validation, and completions.</p>
            <pre class="language-shell-session mb-0"><code>dotnet add package XenoAtom.CommandLine</code></pre>
          </div>
        </div>
      </div>
      <div class="col">
        <div class="card h-100">
          <div class="card-header h5"><i class="bi bi-display lunet-feature-icon lunet-icon--controls"></i>Terminal visuals (optional)</div>
          <div class="card-body">
            <p class="mb-2">Add <code>XenoAtom.CommandLine.Terminal</code> for markup and rich visual output powered by <a href="https://xenoatom.github.io/terminal">XenoAtom.Terminal.UI</a>.</p>
            <pre class="language-shell-session mb-0"><code>dotnet add package XenoAtom.CommandLine.Terminal</code></pre>
          </div>
        </div>
      </div>
    </div>
    <div class="mt-4">
      <p class="mb-2 text-body-secondary">Animation preview (default help, markup, visual output, and diagnostics):</p>
      <img class="terminal img-fluid" src="{{site.basepath}}/img/xenoatom-commandline-show.gif" alt="XenoAtom.CommandLine output modes animation">
    </div>
  </div>
</section>

<section class="container my-5">
  <h2 class="display-6 mb-4"><i class="bi bi-stars lunet-feature-icon lunet-icon--themes"></i>Features</h2>
  <div class="row row-cols-1 row-cols-lg-2 gx-4 gy-4">
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-diagram-3 lunet-feature-icon lunet-icon--layout"></i> Commands &amp; Arguments</div>
        <div class="card-body">
          <p class="card-text mb-2">Compose nested command trees and strict positional arguments with cardinality support.</p>
          <p class="mb-0"><a href="{{site.basepath}}/docs/commands/">Commands docs</a> · <a href="{{site.basepath}}/docs/arguments/">Arguments docs</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-terminal lunet-feature-icon lunet-icon--input"></i> Option Parsing</div>
        <div class="card-body">
          <p class="card-text mb-2">Support <code>-</code>/<code>--</code>/<code>/</code>, aliases, short bundles, key/value forms, and typed parsing.</p>
          <p class="mb-0"><a href="{{site.basepath}}/docs/options/">Options docs</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-shield-check lunet-feature-icon lunet-icon--actions"></i> Validation &amp; Constraints</div>
        <div class="card-body">
          <p class="card-text mb-2">Use built-in validators, custom delegates, mutually-exclusive options, and requires relationships.</p>
          <p class="mb-0"><a href="{{site.basepath}}/docs/validation/">Validation docs</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-chat-square-text lunet-feature-icon lunet-icon--debug"></i> Help &amp; Diagnostics</div>
        <div class="card-body">
          <p class="card-text mb-2">Generate help automatically and provide context-aware errors with useful suggestions.</p>
          <p class="mb-0"><a href="{{site.basepath}}/docs/help-output/">Help &amp; output docs</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-plug lunet-feature-icon lunet-icon--binding"></i> Extensible Output</div>
        <div class="card-body">
          <p class="card-text mb-2">Swap output pipelines through <code>ICommandOutput</code> and choose default, markup, or custom formats.</p>
          <p class="mb-0"><a href="{{site.basepath}}/docs/help-output/">Output customization docs</a></p>
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header h4"><i class="bi bi-display lunet-feature-icon lunet-icon--controls"></i> Terminal &amp; Advanced</div>
        <div class="card-body">
          <p class="card-text mb-2">Add terminal markup/visual output, response files, completions, Parse API, and NativeAOT-friendly runtime behavior.</p>
          <p class="mb-0"><a href="{{site.basepath}}/docs/help-output/">Help &amp; output docs</a> · <a href="{{site.basepath}}/docs/advanced/">Advanced docs</a> · <a href="{{site.basepath}}/docs/migration-2.0/">Migration 2.0</a></p>
        </div>
      </div>
    </div>
  </div>
</section>
