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

## Mandatory Build Verification (CRITICAL)

**ALWAYS run `dotnet build` after any change to `.cs` or `.csproj` files, before committing.**

If `dotnet` is not found in the environment, install it first:

```bash
# Check if dotnet is available
dotnet --version 2>/dev/null || apt-get install -y dotnet-sdk-10.0

# Then verify the build
dotnet build src/api/
```

**Rules:**
- Never skip this step — a passing build is the minimum bar before committing
- If `dotnet` is missing, install it via `apt-get install -y dotnet-sdk-10.0` before proceeding
- Fix all build errors before committing; do not commit broken code
- This applies to any change: `.cs` files, `.csproj` files, `host.json`, etc.

## Warnings

- Warn about `Console.WriteLine()` statements in edited `.cs` files (use `ILogger<T>` instead)
- Warn about `.Result` or `.Wait()` on async calls in edited files (deadlock risk)
- Warn about hardcoded connection strings or passwords
