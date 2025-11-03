# Git Worktrees

**Description**: Git worktrees allow you to check out multiple branches of the same repository simultaneously in different directories.
**Language/Technology**: Git, Version Control  
**Prerequisites**: Git 2.5+, existing Git repository

## Getting Started - Step-by-Step Setup Guide

**Understanding Worktrees**: A worktree is like having multiple copies of your repository, each checked out to different branches, but they all share the same Git history and configuration.

### Step 1: Understand Your Current Setup

**Code**:

```bash
# Check where you are and what you have
pwd                          # See current directory
git status                   # See current branch and status
git branch -a               # See all available branches
git worktree list           # See existing worktrees (likely just your main one)
```

**Usage**:

```bash
# Example: Starting in your main repository
$ pwd
D:\MyProjects\MyApp

$ git status
On branch main
Your branch is up to date with 'origin/main'.

$ git branch -a
* main
  feature/login
  remotes/origin/main
  remotes/origin/develop
  remotes/origin/feature/login

$ git worktree list
D:\MyProjects\MyApp  abc123d [main]
# This shows you have one worktree (your main repository)
```

### Step 2: Plan Your Worktree Structure

**Recommended Structure**:

```text
MyProjects/
├── MyApp/                  # Your original repository (main branch)
├── MyApp-develop/         # Worktree for develop branch
├── MyApp-feature/         # Worktree for feature work
└── MyApp-hotfix/          # Worktree for urgent fixes
```

### Step 3: Create Your First Worktree

**Code**:

```bash
# Method 1: Create worktree for existing branch
git worktree add ../MyApp-develop develop

# Method 2: Create worktree with new branch  
git worktree add ../MyApp-feature -b feature/new-feature

# Method 3: Create worktree from remote branch
git worktree add ../MyApp-hotfix -b hotfix origin/main
```

**Usage**:

```bash
# Example: Create a worktree for the develop branch
$ git worktree add ../MyApp-develop develop
Preparing worktree (checking out 'develop')
HEAD is now at def456e Added user authentication

# Verify it was created
$ git worktree list
D:\MyProjects\MyApp          abc123d [main]
D:\MyProjects\MyApp-develop  def456e [develop]

# Check the file system
$ ls ..
MyApp/
MyApp-develop/
```

### Step 4: Navigate and Work in Your New Worktree

**Code**:

```bash
# Navigate to your new worktree
cd ../MyApp-develop

# Verify you're in the right place
pwd
git status
git branch

# Work normally - make changes, commit, push, etc.
echo "New feature" > feature.txt
git add feature.txt
git commit -m "Add new feature"
git push origin develop
```

**Usage**:

```bash
# Example: Working in your develop worktree
$ cd ../MyApp-develop

$ pwd
D:\MyProjects\MyApp-develop

$ git status  
On branch develop
Your branch is up to date with 'origin/develop'.

$ git branch
* develop
  main

# You can see both branches, but you're on develop
# Changes here won't affect your main worktree
```

### Step 5: Switch Between Worktrees

**Code**:

```bash
# Go back to main repository
cd ../MyApp

# Or go to any other worktree
cd ../MyApp-feature

# Quick check of all worktrees
git worktree list
```

**Usage**:

```bash
# Example: Switching between worktrees
$ cd ../MyApp          # Back to main
$ git status
On branch main
Your branch is up to date with 'origin/main'.

$ cd ../MyApp-develop  # To develop worktree  
$ git status
On branch develop
Your branch is up to date with 'origin/develop'.

# Each worktree maintains its own working directory state
```

### Common Beginner Mistakes and Solutions

**Problem**: "I created a worktree but I can't find the folder"

**Solution**:

```bash
# Always use relative paths from your current location
pwd                           # Know where you are
git worktree add ../MyApp-feature -b feature/test
ls ..                        # Verify the folder was created

# Or use absolute paths to be sure
git worktree add D:\MyProjects\MyApp-feature -b feature/test
```

**Problem**: "I tried to create a worktree but got an error about the branch already existing"

**Solution**:

```bash
# If branch exists, don't use -b flag
git worktree add ../MyApp-existing existing-branch

# If you want a new branch, use a different name
git worktree add ../MyApp-new -b new-branch-name

# To see what branches exist
git branch -a
```

**Problem**: "My worktree has the wrong files/branch"

**Solution**:

```bash
# Remove the problematic worktree
git worktree remove ../MyApp-wrong

# Create it again with correct branch
git worktree add ../MyApp-correct correct-branch
```

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
    local branchName="$1"
    local worktree_path="${BASE_DIR}/${branchName//\//-}"
    
    if [ -z "$2" ]; then
        # Existing branch
        git worktree add "$worktree_path" "$branchName"
    else
        # New branch
        git worktree add "$worktree_path" -b "$branchName" "$2"
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

## Troubleshooting Worktrees

### Common Issues and Solutions

**Issue**: "fatal: '../MyApp-feature' already exists"

**Solutions**:

```bash
# Option 1: Remove existing directory first
rm -rf ../MyApp-feature          # Linux/Mac/WSL
rmdir /s ../MyApp-feature        # Windows CMD
Remove-Item ../MyApp-feature -Recurse -Force  # PowerShell

# Then create worktree
git worktree add ../MyApp-feature -b feature/new

# Option 2: Force overwrite (be careful!)
git worktree add --force ../MyApp-feature -b feature/new

# Option 3: Use a different name
git worktree add ../MyApp-feature2 -b feature/new
```

**Issue**: "fatal: 'feature-branch' is already checked out"

**Solutions**:

```bash
# You can't have the same branch in multiple worktrees
# Option 1: Use different branch name
git worktree add ../MyApp-feature2 -b feature-branch-copy feature-branch

# Option 2: Check which worktree has the branch
git worktree list | grep feature-branch

# Option 3: Remove the existing worktree first
git worktree remove ../path-to-existing-worktree
```

**Issue**: Worktree shows as "prunable" or "broken"

**Solutions**:

```bash
# Check worktree status
git worktree list --porcelain

# Clean up broken references
git worktree prune

# If directory was moved manually, repair it
cd /new/location/of/worktree
git worktree repair

# If completely broken, remove and recreate
git worktree remove ../broken-worktree --force
git worktree add ../MyApp-fixed -b branch-name
```

### Cleaning Up Worktrees

**Code**:

```bash
# List all worktrees to see what you have
git worktree list

# Remove specific worktree (directory must be clean)
git worktree remove ../MyApp-feature

# Force remove worktree (ignores uncommitted changes)
git worktree remove ../MyApp-feature --force

# Remove worktree and delete the branch
git worktree remove ../MyApp-feature
git branch -d feature-branch      # Safe delete
git branch -D feature-branch      # Force delete

# Clean up any stale references
git worktree prune

# Remove all worktrees except main (advanced)
git worktree list --porcelain | grep "^worktree" | grep -v "$(git rev-parse --show-toplevel)" | while read -r line; do
    path=$(echo "$line" | sed 's/^worktree //')
    echo "Removing worktree: $path"
    git worktree remove "$path" --force
done
```

**Usage**:

```bash
# Example: Clean up after feature completion
$ git worktree list
D:\MyProjects\MyApp          abc123d [main]
D:\MyProjects\MyApp-feature  def456e [feature/user-auth]
D:\MyProjects\MyApp-hotfix   ghi789j [hotfix/bug-123]

# Feature is done, clean it up
$ cd ../MyApp-feature
$ git status                 # Make sure everything is committed
$ git push origin feature/user-auth    # Push final changes
$ cd ../MyApp               # Go back to main

# Remove the worktree
$ git worktree remove ../MyApp-feature
$ git branch -d feature/user-auth      # Delete local branch

# Verify cleanup
$ git worktree list
D:\MyProjects\MyApp          abc123d [main] 
D:\MyProjects\MyApp-hotfix   ghi789j [hotfix/bug-123]
```

## Real-World Workflow Examples

### Scenario 1: Feature Development Workflow

**Code**:

```bash
# 1. Start new feature from main
git worktree add ../MyApp-feature -b feature/user-dashboard origin/main

# 2. Work in feature worktree
cd ../MyApp-feature
# ... make changes, commit, push ...

# 3. Switch to main for urgent hotfix
cd ../MyApp
git worktree add ../MyApp-hotfix -b hotfix/critical-bug

# 4. Work on hotfix
cd ../MyApp-hotfix  
# ... fix bug, test, commit, push ...

# 5. Clean up when done
cd ../MyApp
git worktree remove ../MyApp-hotfix --force
git worktree remove ../MyApp-feature --force
```

### Scenario 2: Code Review Workflow  

**Code**:

```bash
# Reviewer: Check out PR branch for local testing
git fetch origin pull/123/head:pr-123
git worktree add ../MyApp-review pr-123

# Test the changes
cd ../MyApp-review
npm install    # Install dependencies
npm test      # Run tests
npm start     # Test locally

# After review, clean up
cd ../MyApp
git worktree remove ../MyApp-review
git branch -d pr-123
```

### Scenario 3: Release Management Workflow

**Code**:

```bash
# Create release worktree
git worktree add ../MyApp-release -b release/v2.1 origin/develop

# Lock it during release preparation
git worktree lock ../MyApp-release --reason "Release v2.1 in progress"

# Work on release
cd ../MyApp-release
# ... version bumps, changelog, final testing ...

# After release
cd ../MyApp
git worktree unlock ../MyApp-release  
git worktree remove ../MyApp-release
```

**Related Snippets**:

- [Common Commands](common-commands.md)
- [Advanced Git Techniques](advanced-techniques.md)
