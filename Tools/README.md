# Tools

Project-specific development, content validation, build-support and data-generation tools live here.

Prototype Phase rule: add tooling only when it directly supports Prototype 0.1, repeatable builds, testing, telemetry, or validation. Avoid building a large internal platform before the core/meta loop is proven.

`DomainSmoke` compiles and executes the Unity-independent puzzle/analytics tests under .NET.

`UnityScriptSmoke` compiles the Unity-facing runtime scripts against a deliberately tiny API stub. It catches ordinary C# compile/symbol regressions while Unity Build Automation is unavailable, but it is not a substitute for a real Unity Editor/cloud compile because the stub only covers the API surface Project 77 currently uses.
