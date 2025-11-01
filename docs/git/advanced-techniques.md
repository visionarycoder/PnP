# Advanced Git Techniques

**Description**: Advanced Git operations and techniques for complex version control scenarios.
**Language/Technology**: Git, Version Control
**Prerequisites**: Git basics, understanding of branching and merging

## Interactive Rebase

**Code**:

```bash
# Interactive rebase for last 3 commits
git rebase -i HEAD~3

# Interactive rebase from specific commit
git rebase -i abc1234

# Rebase onto different base branch
git rebase -i --onto main feature-old feature-new

# Continue rebase after resolving conflicts
git rebase --continue

# Skip current commit during rebase
git rebase --skip

# Abort rebase and return to original state
git rebase --abort
```

**Usage**:

```bash
# Example interactive rebase session
$ git rebase -i HEAD~3
# Editor opens with:
# pick abc1234 Add user authentication
# pick def5678 Fix login validation  
# pick ghi9012 Update documentation

# Change to:
# pick abc1234 Add user authentication
# squash def5678 Fix login validation
# reword ghi9012 Update documentation

# Git will combine commits and ask for new commit messages
```

## Advanced Branching Strategies

**Code**:

```bash
# Create orphan branch (no commit history)
git checkout --orphan new-root-branch
git rm -rf .
echo "New start" > README.md
git add README.md
git commit -m "Initial commit for new branch"

# Create branch from specific commit
git branch feature-branch abc1234
git checkout -b feature-branch abc1234

# Branch with tracking information
git checkout -b feature --track origin/feature

# Rename branches
git branch -m old-name new-name         # Rename current branch
git branch -m old-name new-name         # Rename any branch

# Delete branches safely
git branch -d feature-branch            # Delete merged branch
git branch -D feature-branch            # Force delete unmerged branch
git push origin --delete feature-branch # Delete remote branch
```

**Usage**:

```bash
# Complex branching workflow
$ git checkout main
$ git checkout -b release/v2.0
$ git checkout -b feature/user-profiles release/v2.0

# Work on feature...
$ git add -A && git commit -m "Add user profile functionality"

# Create hotfix from main
$ git checkout main  
$ git checkout -b hotfix/security-fix
# Fix and commit...

# Merge hotfix to both main and release
$ git checkout main
$ git merge --no-ff hotfix/security-fix
$ git checkout release/v2.0
$ git merge --no-ff hotfix/security-fix
```

## Advanced Merge Strategies

**Code**:

```bash
# Merge with custom strategy
git merge -X ours feature-branch        # Prefer current branch on conflicts
git merge -X theirs feature-branch      # Prefer incoming branch on conflicts

# Octopus merge (multiple branches)
git merge branch1 branch2 branch3

# Merge without fast-forward
git merge --no-ff feature-branch

# Squash merge (combine all commits into one)
git merge --squash feature-branch
git commit -m "Add feature: combined commits"

# Merge with custom commit message
git merge --no-edit feature-branch
git merge -m "Custom merge message" feature-branch
```

**Usage**:

```bash
# Strategic merge example
$ git checkout main
$ git merge --no-ff --no-edit release/v1.5
# Creates merge commit even if fast-forward is possible

# Squash merge for clean history
$ git checkout main
$ git merge --squash feature/new-ui
$ git commit -m "Add new UI components

- Modernized button styles
- Improved responsive layout  
- Added dark theme support"
```

## Git Stashing Advanced

**Code**:

```bash
# Stash with message
git stash push -m "Work in progress on feature X"

# Stash specific files
git stash push -m "Partial work" file1.js file2.css

# Stash including untracked files
git stash -u

# Stash including ignored files  
git stash -a

# List all stashes with details
git stash list --stat

# Show stash contents
git stash show stash@{0}
git stash show -p stash@{0}      # Show patch/diff

# Apply stash to different branch
git stash branch new-branch stash@{0}

# Create patch from stash
git stash show -p stash@{0} > my-changes.patch
```

**Usage**:

```bash
# Complex stash scenario
$ git status
On branch feature/auth
Changes not staged for commit:
  modified:   src/auth.js
  modified:   src/utils.js

Untracked files:
  src/new-feature.js

# Stash everything including untracked
$ git stash -u -m "Auth work + new experimental file"
Saved working directory and index state WIP on feature/auth: a1b2c3d Add login form

# Switch to hotfix
$ git checkout main
$ git checkout -b hotfix/critical-bug
# ... fix bug and commit

# Return and apply stash
$ git checkout feature/auth
$ git stash pop
# Continue work...
```

## Cherry-picking and Patch Management

**Code**:

```bash
# Cherry-pick single commit
git cherry-pick abc1234

# Cherry-pick multiple commits
git cherry-pick abc1234 def5678 ghi9012

# Cherry-pick range of commits  
git cherry-pick abc1234..ghi9012

# Cherry-pick with custom commit message
git cherry-pick -e abc1234

# Cherry-pick without committing (staging only)
git cherry-pick -n abc1234

# Handle cherry-pick conflicts
git cherry-pick --continue
git cherry-pick --skip
git cherry-pick --abort

# Create patch files
git format-patch HEAD~3              # Last 3 commits
git format-patch main..feature       # All commits in feature not in main
git format-patch --stdout HEAD~2 > my-patches.patch

# Apply patches
git apply my-patches.patch           # Apply without committing
git am *.patch                      # Apply and commit patch files
```

**Usage**:

```bash
# Cherry-pick scenario: Apply hotfix to multiple branches
$ git log --oneline main
a1b2c3d Fix security vulnerability
b4e5f6g Add user management
...

$ git checkout release/v1.0
$ git cherry-pick a1b2c3d
[release/v1.0 x7y8z9a] Fix security vulnerability
 Date: Mon Oct 15 10:30:00 2023 -0700
 1 file changed, 5 insertions(+), 2 deletions(-)

$ git checkout release/v2.0  
$ git cherry-pick a1b2c3d
# Apply same fix to v2.0 branch
```

## Advanced History Manipulation

**Code**:

```bash
# Reset to specific commit (keep changes staged)
git reset --soft HEAD~2

# Reset to specific commit (keep changes in working directory)  
git reset --mixed HEAD~2

# Reset to specific commit (discard all changes)
git reset --hard HEAD~2

# Interactive commit editing
git commit --amend                   # Modify last commit
git commit --amend --no-edit        # Add files to last commit without changing message

# Split a commit
git rebase -i HEAD~2
# Mark commit as 'edit', then:
git reset HEAD~1
# Stage and commit files separately
git add file1.js && git commit -m "Part 1"
git add file2.js && git commit -m "Part 2"
git rebase --continue

# Remove file from history completely
git filter-branch --force --index-filter \
  'git rm --cached --ignore-unmatch secret-file.txt' \
  --prune-empty --tag-name-filter cat -- --all
```

**Usage**:

```bash
# History cleanup example
$ git log --oneline
a1b2c3d Add feature complete
b4e5f6g WIP: debugging
c7d8e9f More debugging  
f0g1h2i Initial feature work

# Squash debugging commits
$ git rebase -i HEAD~4
# Change 'pick' to 'squash' for debugging commits
# Result: clean single commit for the feature
```

**Notes**:

**Interactive Rebase Actions**:

- `pick` - Use commit as-is
- `reword` - Change commit message
- `edit` - Stop and amend commit
- `squash` - Combine with previous commit
- `drop` - Remove commit entirely

**Safety Guidelines**:

- Never rebase commits that have been pushed to shared repositories
- Use `git reflog` to recover from mistakes
- Create backup branches before destructive operations
- Test thoroughly after history modifications

**Advanced Merge Conflict Resolution**:

- Use `git mergetool` for visual conflict resolution
- Configure preferred merge tool: `git config --global merge.tool vimdiff`
- Use `git checkout --ours file.txt` or `--theirs` for entire file selection

**Performance Tips**:

- Use `git gc` periodically to optimize repository
- Use shallow clones for large repositories: `git clone --depth=1`
- Use sparse checkout for large monorepos
- Configure appropriate `.gitignore` patterns

**Related Snippets**:

- [Common Commands](common-commands.md)
- [Git Worktrees](worktrees.md)
