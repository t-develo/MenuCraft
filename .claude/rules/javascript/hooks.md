---
paths:
  - "**/*.js"
  - "src/client/**"
---
# Vanilla JavaScript Hooks

> This file extends [common/hooks.md](../common/hooks.md) with Vanilla JS specific content.

## PostToolUse Hooks

Configure in `~/.claude/settings.json`:

- **Prettier**: Auto-format `.js` files after edit (if Prettier is installed)
- **ESLint**: Run `npx eslint --fix` after editing `.js` files

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
            "command": "npx prettier --write {file} 2>/dev/null || true"
          }
        ]
      }
    ]
  }
}
```

## Stop Hooks

- **console.log audit**: Check all modified JS files for `console.log` before session ends
- **innerHTML audit**: Warn about `innerHTML` assignments in modified files

## Warnings

- Warn about `console.log()` statements in edited `.js` files
- Warn about `innerHTML =` assignments without DOMPurify
- Warn about `var` declarations (should use `const`/`let`)
- Warn about `eval()` or `new Function()` usage
