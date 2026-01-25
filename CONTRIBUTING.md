# Contributing

Thanks for contributing to Rvne.ReflectiveOptions!
If you need anything changed or updated, feel free to open an issue right here on GitHub.

## Build targets

- `net6.0`
- `net8.0`
- `net10.0`

## Tests

```bash
dotnet test
```

## Implementation notes

- The builder is reflection-based and does not cache results.
- Attribute discovery uses `inherit: true`, so base-class attributes are applied.
