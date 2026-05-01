# Project rules for Codex

- Always preserve Thai text and emoji as UTF-8.
- Never rewrite text through ANSI, latin1, cp1252, cp874, TIS-620, or Windows default encoding.
- Do not replace readable text with question marks.
- Do not rewrite entire files unless explicitly requested.
- Prefer small, targeted patches.
- Before editing, run git status.
- After editing, run git diff.
- Do not use PowerShell Set-Content or Out-File without -Encoding utf8.
