# Enterprise Git Command Reference

**Description**: Professional Git workflows and commands for enterprise software development teams.

**Language/Technology**: Git

**Code**:
```bash
# ============================================
# Basic Operations
# ============================================

# Initialize a new repository
git init

# Clone a repository
git clone <repository-url>
git clone <repository-url> <directory-name>

# Check repository status
git status
git status -s  # Short format

# Add files to staging
git add <file>
git add .                    # Add all changes
git add -A                   # Add all changes including deletions
git add -p                   # Interactive staging (patch mode)

# Commit changes
git commit -m "Commit message"
git commit -am "Message"     # Add and commit tracked files
git commit --amend           # Amend last commit
git commit --amend --no-edit # Amend without changing message

# ============================================
# Viewing History
# ============================================

# View commit history
git log
git log --oneline            # Compact view
git log --graph --oneline    # Graph view
git log -n 5                 # Last 5 commits
git log --author="John"      # Filter by author
git log --since="2 weeks ago"
git log --grep="fix"         # Search commit messages

# View changes
git diff                     # Unstaged changes
git diff --staged            # Staged changes
git diff HEAD                # All changes since last commit
git diff <commit1> <commit2> # Compare commits
git diff <branch1>..<branch2> # Compare branches

# Show commit details
git show <commit-hash>
git show HEAD                # Show last commit
git show <commit>:<file>     # Show file at specific commit

# ============================================
# Branch Management
# ============================================

# List branches
git branch                   # Local branches
git branch -r               # Remote branches
git branch -a               # All branches

# Create and switch branches
git branch <branch-name>     # Create branch
git checkout <branch-name>   # Switch branch
git checkout -b <branch>     # Create and switch
git switch <branch-name>     # New command to switch
git switch -c <branch>       # New command to create and switch

# Delete branches
git branch -d <branch>       # Delete merged branch
git branch -D <branch>       # Force delete
git push origin --delete <branch>  # Delete remote branch

# Rename branch
git branch -m <old-name> <new-name>
git branch -m <new-name>     # Rename current branch

# ============================================
# Remote Operations
# ============================================

# View remotes
git remote -v
git remote show origin

# Add/remove remotes
git remote add <name> <url>
git remote remove <name>
git remote rename <old> <new>

# Fetch and pull
git fetch                    # Fetch all remotes
git fetch origin             # Fetch specific remote
git pull                     # Fetch and merge
git pull --rebase           # Fetch and rebase

# Push changes
git push
git push origin <branch>
git push -u origin <branch>  # Set upstream and push
git push --force-with-lease  # Safer force push
git push --all               # Push all branches

# ============================================
# Undoing Changes
# ============================================

# Discard changes
git checkout -- <file>       # Discard working directory changes
git restore <file>           # New command to discard changes
git restore --staged <file>  # Unstage file

# Reset commits
git reset HEAD~1             # Undo last commit, keep changes
git reset --soft HEAD~1      # Undo commit, keep staged
git reset --hard HEAD~1      # Undo commit, discard changes
git reset --hard origin/main # Reset to remote state

# Revert commits (safe for shared branches)
git revert <commit>          # Create new commit that undoes changes
git revert HEAD              # Revert last commit

# Clean untracked files
git clean -n                 # Dry run
git clean -f                 # Remove untracked files
git clean -fd                # Remove untracked files and directories

# ============================================
# Stashing
# ============================================

# Save work in progress
git stash                    # Stash changes
git stash save "description" # Stash with message
git stash -u                 # Include untracked files

# View stashes
git stash list

# Apply stashes
git stash apply              # Apply last stash
git stash apply stash@{1}    # Apply specific stash
git stash pop                # Apply and remove stash

# Remove stashes
git stash drop stash@{0}     # Remove specific stash
git stash clear              # Remove all stashes

# ============================================
# Merging and Rebasing
# ============================================

# Merge branches
git merge <branch>           # Merge branch into current
git merge --no-ff <branch>   # Create merge commit
git merge --squash <branch>  # Squash merge

# Rebase
git rebase <branch>          # Rebase current onto branch
git rebase -i HEAD~3         # Interactive rebase last 3 commits
git rebase --continue        # Continue after resolving conflicts
git rebase --abort           # Abort rebase

# ============================================
# Tags
# ============================================

# Create tags
git tag <tag-name>           # Lightweight tag
git tag -a v1.0 -m "Version 1.0"  # Annotated tag

# List and show tags
git tag
git show v1.0

# Push tags
git push origin <tag-name>
git push origin --tags       # Push all tags

# Delete tags
git tag -d <tag-name>        # Local
git push origin --delete <tag-name>  # Remote
```

**Usage**:
```bash
# Common workflow example
cd my-project
git checkout -b feature/new-feature
# ... make changes ...
git add .
git commit -m "Add new feature"
git push -u origin feature/new-feature

# Update branch with latest main
git checkout main
git pull
git checkout feature/new-feature
git rebase main
git push --force-with-lease

# Clean up after merge
git checkout main
git branch -d feature/new-feature
git push origin --delete feature/new-feature
```

**Notes**:

- Always pull before pushing to avoid conflicts
- Use `--force-with-lease` instead of `--force` for safer force pushes
- `git restore` and `git switch` are newer alternatives to `git checkout`
- Interactive rebase (`-i`) is powerful but be careful with shared branches
- Use descriptive commit messages (imperative mood: "Add feature" not "Added feature")
- Related: [Git Aliases](git-aliases.md), [Git Workflows](git-workflows.md)
