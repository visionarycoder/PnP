# Git Worktrees

**Description**: Git worktrees allow you to check out multiple branches of the same repository simultaneously in different directories.
**Language/Technology**: Git, Version Control  
**Prerequisites**: Git 2.5+, existing Git repository

## Basic Worktree Operations

**Code**:

```bash
# List all worktrees
git worktree list

# Add a new worktree for an existing branch
git worktree add ../feature-branch feature-branch

# Add worktree for a new branch
git worktree add ../new-feature -b new-feature

# Add worktree from remote branch
git worktree add ../hotfix -b hotfix origin/hotfix

# Remove a worktree
git worktree remove ../feature-branch

# Prune stale worktree references
git worktree prune
```

**Usage**:

```bash
# Current repository structure
$ git worktree list
/home/user/myproject         a1b2c3d [main]

# Add worktree for feature development
$ git worktree add ../myproject-feature -b feature/user-auth
Preparing worktree (new branch 'feature/user-auth')
HEAD is now at a1b2c3d Initial commit

# Check worktrees
$ git worktree list  
/home/user/myproject         a1b2c3d [main]
/home/user/myproject-feature e4f5g6h [feature/user-auth]

# Work in feature branch
$ cd ../myproject-feature
$ git status
On branch feature/user-auth
nothing to commit, working tree clean
```

## Advanced Worktree Scenarios

**Code**:

```bash
# Create worktree in specific location with custom branch
git worktree add /path/to/custom/location -b custom-branch origin/develop

# Add detached HEAD worktree for specific commit
git worktree add ../hotfix-test a1b2c3d

# Force add worktree (overwrite existing directory)
git worktree add --force ../existing-dir -b new-branch

# Add worktree and track remote branch
git worktree add ../release --track -b release/v2.0 origin/release/v2.0

# Add bare worktree (no files checked out)
git worktree add --no-checkout ../bare-worktree develop
```

**Usage**:

```bash
# Parallel development scenario
$ git branch -a
* main
  develop  
  feature/login
  remotes/origin/main
  remotes/origin/develop
  remotes/origin/hotfix/security-fix

# Create worktrees for parallel work
$ git worktree add ../project-develop develop
$ git worktree add ../project-hotfix -b hotfix origin/hotfix/security-fix
$ git worktree add ../project-release -b release/v1.2

# Check all worktrees
$ git worktree list
/home/user/project             a1b2c3d [main]
/home/user/project-develop     e4f5g6h [develop] 
/home/user/project-hotfix      i7j8k9l [hotfix]
/home/user/project-release     m1n2o3p [release/v1.2]
```

## Worktree Management

**Code**:

```bash
# Lock a worktree to prevent deletion
git worktree lock ../project-feature --reason "In active development"

# Unlock a locked worktree  
git worktree unlock ../project-feature

# Move a worktree to new location
git worktree move ../old-location ../new-location

# Repair worktree if moved manually
git worktree repair

# Remove worktree even if working directory has changes
git worktree remove --force ../project-feature

# Get detailed worktree information
git worktree list --porcelain
```

**Usage**:

```bash
# Lock critical worktree
$ git worktree lock ../project-release --reason "Production release preparation"
$ git worktree list
/home/user/project             a1b2c3d [main]
/home/user/project-release     m1n2o3p [release/v1.2] locked

# Attempt to remove locked worktree
$ git worktree remove ../project-release
fatal: '../project-release' is locked (reason: Production release preparation)

# Unlock and remove
$ git worktree unlock ../project-release
$ git worktree remove ../project-release
```

## Worktree Best Practices

**Code**:

```bash
# Setup script for consistent worktree structure
#!/bin/bash
REPO_NAME=$(basename "$(git rev-parse --show-toplevel)")
BASE_DIR="../${REPO_NAME}-worktrees"

# Create base directory for worktrees
mkdir -p "$BASE_DIR"

# Function to add worktree with consistent naming
add_worktree() {
    local branch_name="$1"
    local worktree_path="${BASE_DIR}/${branch_name//\//-}"
    
    if [ -z "$2" ]; then
        # Existing branch
        git worktree add "$worktree_path" "$branch_name"
    else
        # New branch
        git worktree add "$worktree_path" -b "$branch_name" "$2"
    fi
    
    echo "Worktree created at: $worktree_path"
}

# Usage examples
# add_worktree "feature/user-login"
# add_worktree "hotfix/security" "origin/main"
```

**Usage**:

```bash
# Organized worktree structure
project/
├── main-repo/                 # Original repository
├── project-worktrees/         # Worktrees directory
│   ├── develop/              # develop branch
│   ├── feature-user-auth/    # feature/user-auth branch  
│   ├── hotfix-security/      # hotfix/security branch
│   └── release-v1.2/         # release/v1.2 branch

# Create this structure
$ mkdir ../myproject-worktrees
$ git worktree add ../myproject-worktrees/develop develop  
$ git worktree add ../myproject-worktrees/feature-auth -b feature/user-auth
$ git worktree add ../myproject-worktrees/release -b release/v1.2 origin/develop
```

## Integration with IDEs and Tools

**Code**:

```bash
# VS Code: Open multiple worktrees in different windows
code ~/project/main-repo
code ~/project/project-worktrees/develop  
code ~/project/project-worktrees/feature-auth

# Create shell aliases for quick navigation
# Add to ~/.bashrc or ~/.zshrc
alias cdmain='cd ~/project/main-repo'
alias cddev='cd ~/project/project-worktrees/develop'
alias cdfeature='cd ~/project/project-worktrees/feature-auth'

# Git aliases for worktree management
git config --global alias.wa 'worktree add'
git config --global alias.wl 'worktree list'  
git config --global alias.wr 'worktree remove'
git config --global alias.wp 'worktree prune'
```

**Usage**:

```bash
# Quick worktree commands with aliases
$ git wa ../hotfix -b hotfix/bug-123 origin/main
$ git wl
/home/user/project             a1b2c3d [main]
/home/user/hotfix              e4f5g6h [hotfix/bug-123]

# Navigate using shell aliases
$ cdmain
$ pwd
/home/user/project

$ cddev  
$ pwd
/home/user/project-worktrees/develop
```

**Notes**:

**Worktree Benefits**:

- Work on multiple branches simultaneously without constant switching
- Maintain different build states for testing
- Parallel development and code review
- Isolated environments for experiments

**Limitations**:

- Each worktree uses disk space (no shared working directory)
- Submodules may need special handling
- Some tools might not understand worktree setup
- Shared configuration affects all worktrees

**Best Practices**:

- Use consistent naming conventions for worktree directories
- Lock worktrees during critical operations
- Clean up unused worktrees regularly with `git worktree prune`
- Consider worktree organization structure from the start

**Common Use Cases**:

- **Feature Development**: Separate worktrees for each feature branch
- **Hotfixes**: Quick fixes without disrupting main development  
- **Release Preparation**: Isolated environment for release testing
- **Code Review**: Check out PR branches for local review

**Related Snippets**:

- [Common Commands](common-commands.md)
- [Advanced Git Techniques](advanced-techniques.md)
