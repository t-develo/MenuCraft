---
paths:
  - "**/*.cs"
  - "**/*.csproj"
---
# C#/.NET Hooks

> This file extends [common/hooks.md](../common/hooks.md) with C#/.NET specific content.

## PostToolUse Hooks

Configure in `~/.claude/settings.json`:

- **dotnet format**: Auto-format `.cs` files after edit
- **dotnet build**: Run compilation check after editing `.cs` files to catch errors immediately

## Recommended Hook Configuration

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          {
            "type": "command",
            "command": "dotnet format src/api/ --include {file} 2>/dev/null || true"
          }
        ]
      }
    ]
  }
}
```

## Warnings

- Warn about `Console.WriteLine()` statements in edited `.cs` files (use `ILogger<T>` instead)
- Warn about `.Result` or `.Wait()` on async calls in edited files (deadlock risk)
- Warn about hardcoded connection strings or passwords
