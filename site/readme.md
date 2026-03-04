---
title: Home
layout: simple
og_type: website
---

<section class="text-center py-5">
  <div class="container">
    <img src="{{site.basepath}}/img/tomlyn.svg" alt="Tomlyn" class="img-fluid" style="width: min(100%, 12rem); height: auto;">
    <h1 class="display-4 mt-4">Tomlyn</h1>
    <p class="lead mt-3 mb-4">
      High-performance TOML 1.1 for .NET: <strong>lexer/parser</strong>, <strong>roundtrippable syntax</strong>,
      and a <strong>System.Text.Json-style</strong> object serializer.
    </p>
    <div class="d-flex justify-content-center gap-3 mt-4 flex-wrap">
      <a href="{{site.basepath}}/docs/getting-started/" class="btn btn-primary btn-lg"><i class="bi bi-rocket-takeoff"></i> Get started</a>
      <a href="{{site.basepath}}/api/" class="btn btn-outline-secondary btn-lg"><i class="bi bi-filetype-cs"></i> API Reference</a>
      <a href="https://github.com/xoofx/Tomlyn" class="btn btn-info btn-lg"><i class="bi bi-github"></i> GitHub</a>
    </div>
    <div class="mt-4 text-start mx-auto" style="max-width: 48rem;">
      <pre class="language-shell-session"><code>dotnet add package Tomlyn</code></pre>
      <p class="text-center text-secondary mt-2" style="font-size: 0.85rem;">Available on <a href="https://www.nuget.org/packages/Tomlyn/" class="text-secondary">NuGet</a> — net8.0, net10.0, netstandard2.0</p>
    </div>
  </div>
</section>

<section class="container my-5">
  <div class="card">
    <div class="card-header display-6">
      <i class="bi bi-code-slash lunet-feature-icon lunet-icon--lists"></i> Quick example
    </div>
    <div class="card-body">

```csharp
using Tomlyn;

public sealed record Person(string Name, int Age);

var toml = TomlSerializer.Serialize(new Person("Ada", 37));

// name = "Ada"
// age = 37

var person = TomlSerializer.Deserialize<Person>(toml);
```

</div>
  </div>
</section>
