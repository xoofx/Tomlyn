---
title: Home
layout: simple
og_type: website
---

<section class="text-center py-5">
  <div class="container">
    <img src="{{site.basepath}}/img/Tomlyn.svg" alt="Tomlyn" class="img-fluid" style="width: min(100%, 12rem); height: auto;">
    <h1 class="display-4 mt-4">Tomlyn</h1>
    <p class="lead mt-3 mb-4">
      High-performance TOML 1.1 for .NET: <strong>lexer/parser</strong>, <strong>roundtrippable syntax tree</strong>,
      and a <strong>System.Text.Json-style</strong> object serializer.
    </p>
    <div class="d-flex justify-content-center gap-3 mt-4 flex-wrap">
      <a href="{{site.basepath}}/docs/getting-started/" class="btn btn-primary btn-lg"><i class="bi bi-rocket-takeoff"></i> Get started</a>
      <a href="{{site.basepath}}/docs/serialization/" class="btn btn-outline-secondary btn-lg"><i class="bi bi-braces"></i> Serialization</a>
      <a href="{{site.basepath}}/docs/low-level/" class="btn btn-outline-secondary btn-lg"><i class="bi bi-diagram-3"></i> Low-level APIs</a>
      <a href="https://github.com/xoofx/Tomlyn" class="btn btn-info btn-lg"><i class="bi bi-github"></i> GitHub</a>
    </div>
    <div class="mt-4 text-start mx-auto" style="max-width: 48rem;">
      <pre class="language-shell-session"><code>dotnet add package Tomlyn</code></pre>
      <p class="text-center text-secondary mt-2" style="font-size: 0.85rem;">Available on <a href="https://www.nuget.org/packages/Tomlyn/" class="text-secondary">NuGet</a> - net8.0, net10.0, netstandard2.0</p>
      <p class="text-center mt-3" style="font-size: 1rem;">
        <img src="https://xoofx.github.io/SharpYaml/img/SharpYaml.png" alt="SharpYaml" style="height: 2rem; width: auto; vertical-align: text-bottom; margin-right: .45rem;">
        Looking for <span style="font-weight: 600;">YAML</span> support? Check out <a href="https://xoofx.github.io/SharpYaml/" class="text-secondary">SharpYaml</a>.
      </p>
    </div>
  </div>
</section>

<section class="container my-5">
  <div class="row row-cols-1 row-cols-lg-2 gx-5 gy-4">
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-lightning-charge lunet-feature-icon lunet-icon--controls"></i> Familiar API</div>
        <div class="card-body">
          Tomlyn is intentionally shaped like <code>System.Text.Json</code>: <code>TomlSerializer</code>, <code>TomlSerializerOptions</code>,
          source-generated contexts, and a converter pipeline.
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-boxes lunet-feature-icon lunet-icon--lists"></i> Two layers</div>
        <div class="card-body">
          Use the <strong>syntax/model layer</strong> for lossless parsing, source spans, and tooling.
          Use <strong>TomlSerializer</strong> to map TOML into .NET objects.
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-scissors lunet-feature-icon lunet-icon--lists"></i> NativeAOT ready</div>
        <div class="card-body">
          Reflection fallback can be disabled. For NativeAOT and trimming, use generated metadata via <code>TomlSerializerContext</code> and <code>TomlTypeInfo&lt;T&gt;</code>.
        </div>
      </div>
    </div>
    <div class="col">
      <div class="card h-100">
        <div class="card-header display-6"><i class="bi bi-tag lunet-feature-icon lunet-icon--lists"></i> JSON attribute interop</div>
        <div class="card-body">
          Most common <code>System.Text.Json.Serialization</code> attributes work out of the box
          (for example <code>[JsonPropertyName]</code>, <code>[JsonIgnore]</code>, <code>[JsonConstructor]</code>, and <code>[JsonObjectCreationHandling]</code>).
        </div>
      </div>
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
using System.Text.Json;
using Tomlyn;

public sealed record Person(string Name, int Age);

var options = new TomlSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

var toml = TomlSerializer.Serialize(new Person("Ada", 37), options);

// name = "Ada"
// age = 37

var person = TomlSerializer.Deserialize<Person>(toml, options);
```

Next: read the [Getting started](docs/getting-started.md) guide.

</div>
  </div>
</section>
