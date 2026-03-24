# Verification Command

Run comprehensive verification on current codebase state.

## Instructions

Execute verification in this exact order:

1. **Build Check**
   - Run the build command for this project
   - If it fails, report errors and STOP

2. **Type Check / Compile Check**
   - Run `dotnet build src/api/` for C# code
   - Report all errors with file:line

3. **Lint Check**
   - Run linter for JS: `npx eslint src/client/js/` if available
   - Report warnings and errors

4. **Test Suite**
   - Run all tests: `dotnet test`
   - Report pass/fail count
   - Report coverage percentage if available

5. **Console.log / Console.WriteLine Audit**
   - Search for debug output in source files
   - Report locations

6. **Git Status**
   - Show uncommitted changes
   - Show files modified since last commit

## Output

Produce a concise verification report:

```
VERIFICATION: [PASS/FAIL]

Build:    [OK/FAIL]
Compile:  [OK/X errors]
Lint:     [OK/X issues]
Tests:    [X/Y passed, Z% coverage]
Secrets:  [OK/X found]
Logs:     [OK/X console outputs]

Ready for PR: [YES/NO]
```

If any critical issues, list them with fix suggestions.

## Arguments

$ARGUMENTS can be:
- `quick` - Only build + compile
- `full` - All checks (default)
- `pre-commit` - Checks relevant for commits
- `pre-pr` - Full checks plus security scan
