# Git Auth Troubleshooting

## Current Working Setup

- Remote: `https://github.com/YuShimoji/MiniMapGame.git`
- Auth path: Git Credential Manager (GCM)
- Global helper: `/home/planner007/.local/bin/git-credential-manager.exe`

## Why Push Can Work Without Manual PAT Input

GitHub write access still requires authentication.
In this environment, the saved GitHub credential is supplied automatically by GCM, so manual PAT entry is not normally needed on each push.

## Quick Checks

1. Confirm remote:
   - `git remote -v`
2. Confirm credential helper:
   - `git config --global --get credential.helper`
3. Confirm stored credential can be resolved:
   - `printf 'protocol=https\nhost=github.com\npath=YuShimoji/MiniMapGame.git\n\n' | git credential fill`
4. Confirm push path before real push:
   - `git push --dry-run origin master`

## Known Failure Modes

### `could not read Username for 'https://github.com'`

Meaning:
- Git could not obtain a GitHub username/token at push time.

Likely causes:
- GCM is not reachable from the current shell
- Stored credential is missing or expired
- Terminal prompt is disabled and Git cannot fall back to interactive login

Recommended checks:
- `ls -la /home/planner007/.local/bin/git-credential-manager.exe`
- `'/home/planner007/.local/bin/git-credential-manager.exe' --version`
- `git config --global --get credential.helper`

### `Permission denied (publickey)`

Meaning:
- SSH auth is not configured for `git@github.com`.

This project currently uses HTTPS + GCM, not SSH.

## Current Project Status

- Push recovered successfully on 2026-03-10
- `master` is now synchronized with `origin/master`
