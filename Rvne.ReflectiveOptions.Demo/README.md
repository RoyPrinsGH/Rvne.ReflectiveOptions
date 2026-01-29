# Demo: ReflectiveOptions CLI

This demo is a tiny, runnable CLI that shows how `Rvne.ReflectiveOptions` can:

- Derive option values from attributes.
- Use a parent context (`Cli`) to resolve derived values on child members.
- Apply multiple option domains (runner options + descriptions).

The key pieces live here:

- `Rvne.ReflectiveOptions.Demo/Program.cs`
- `Rvne.ReflectiveOptions.Demo/DemoInfrastructure.cs`

The demo also enables the source generator. Attributes like `[Timeout]`,
`[DerivedTimeout]`, `[Runnability]`, and `[Description]` are generated from the
options types in `Rvne.ReflectiveOptions.Demo/DemoInfrastructure.cs`. The
`[ReflectionOptions(...)]` attribute on those options types controls the
generated `AttributeUsage` targets, `Inherited`, and `AllowMultiple`.

## Run It

From the repo root:

```bash
dotnet run --project Rvne.ReflectiveOptions.Demo
```

This prints the resolved command tree for:

- `Environment.Local`
- `Environment.CI`

## Run A Command

You can also run a specific command by name:

```bash
dotnet run --project Rvne.ReflectiveOptions.Demo -- push-remote
dotnet run --project Rvne.ReflectiveOptions.Demo -- pull-remote
```

The runner:

- Resolves `CommandRunnerOptions` from the member attributes.
- Honors `Runnable`, `TimeoutMs`, and `Retries`.
- Calls `Execute()` on the matching command.

## Play Around (Recommended)

This demo is meant to be poked at. A few fun edits:

1. Change derived behavior: in `Cli.GetPullCommandTimeout`, swap the timeouts or add more branching.
2. Hide or allow a command outside local: the `Push` command uses `[DerivedRunnability(nameof(IsLocalEnv))]`.
3. Add a new command: create a new `ICommand` type with `[Description(...)]`, then add a property on `Cli` with attributes like `[Timeout(...)]` and `[Name(...)]`.
4. Change generated attribute names: edit the `[ReflectiveOptionAlias(...)]` values on the options types.
5. Move the derivation method: put a derivation method on a base class (like `EnvironmentAwareBase`) and reference it by name.
