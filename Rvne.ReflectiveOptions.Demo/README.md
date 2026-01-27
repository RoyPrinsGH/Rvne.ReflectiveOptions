# Demo: ReflectiveOptions CLI

This demo is a tiny, runnable CLI that shows how `Rvne.ReflectiveOptions` can:

- Derive option values from attributes.
- Use a parent context (`Cli`) to resolve derived values on child members.
- Apply multiple option domains (runner options + descriptions).

The key pieces live here:

- `Rvne.ReflectiveOptions.Demo/Program.cs`
- `Rvne.ReflectiveOptions.Demo/DemoInfrastructure.cs`

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

1. Change derived behavior.
   - In `Cli.GetPullCommandTimeout`, swap the timeouts or add more branching.
2. Hide a command outside local.
   - The `Push` command uses `[DerivedRunnability(nameof(IsLocalEnv))]`.
   - Try changing it to always return `true` or `false`.
3. Add a new command.
   - Create a new `ICommand` type with `[Description(...)]`.
   - Add a new property on classes specifying a `UsageTemplate`
4. Move the derivation method.
   - Try putting a derivation method on a base class (like `EnvironmentAwareBase`) and reference it by name.
