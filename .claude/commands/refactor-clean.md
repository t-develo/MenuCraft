# Refactor Clean

Safely identify and remove dead code with test verification at every step.

## Step 1: Detect Dead Code

Run analysis tools based on project type:

| Tool | What It Finds | Command |
|------|--------------|---------|
| dotnet build warnings | Unused variables, unreachable code | `dotnet build src/api/ 2>&1 \| grep -i warning` |
| eslint | Unused JS variables/functions | `npx eslint src/client/js/ --rule 'no-unused-vars: warn'` |
| grep | TODO/FIXME markers, commented-out code | `grep -rn "TODO\|FIXME\|//.*=" src/` |
| knip | Unused JS files, exports, dependencies | `npx knip` |
| depcheck | Unused npm dependencies | `npx depcheck` |

If no tool is available, use Grep to find functions with zero call sites:
```
# Find function declarations, then check if they're called anywhere
```

## Step 2: Categorize Findings

Sort findings into safety tiers:

| Tier | Examples | Action |
|------|----------|--------|
| **SAFE** | Unused local variables, dead code warnings, commented-out blocks | Delete with confidence |
| **CAUTION** | Utility functions, helper modules, private methods | Verify no dynamic references |
| **DANGER** | Public API interfaces, shared models, entry points | Investigate before touching |

## Step 3: Safe Deletion Loop

For each SAFE item:

1. **Run full test suite** — Establish baseline (all green)
2. **Delete the dead code** — Use Edit tool for surgical removal
3. **Re-run test suite** — Verify nothing broke
4. **If tests fail** — Immediately revert with `git checkout -- <file>` and skip this item
5. **If tests pass** — Move to next item

## Step 4: Handle CAUTION Items

Before deleting CAUTION items:
- Search for dynamic references in string form
- Check if referenced from JavaScript client code
- Verify no external consumers
- Check if part of a public API contract

## Step 5: Consolidate Duplicates

After removing dead code, look for:
- Near-duplicate functions (>80% similar) — merge into one
- Redundant type definitions — consolidate
- Wrapper functions that add no value — inline them
- Re-exports that serve no purpose — remove indirection

## Step 6: Summary

Report results:

```
Dead Code Cleanup
──────────────────────────────
Deleted:   8 unused private methods
           2 unused utility files
           3 unused NuGet packages
Skipped:   1 item (tests failed)
Saved:     ~320 lines removed
──────────────────────────────
All tests passing
```

## Rules

- **Never delete without running tests first**
- **One deletion at a time** — Atomic changes make rollback easy
- **Skip if uncertain** — Better to keep dead code than break production
- **Don't refactor while cleaning** — Separate concerns (clean first, refactor later)
