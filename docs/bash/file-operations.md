# File Operations

**Description**: Common file and directory operations in Bash
**Language/Technology**: Bash / Shell Scripting

## Basic File Operations

**Code**:

```bash
#!/bin/bash

# Create directories recursively
mkdir -p /path/to/nested/directories

# Copy files with options
cp -r source_directory/ destination_directory/  # Recursive copy
cp -p file.txt backup.txt                       # Preserve permissions
cp -u newer_file.txt existing_file.txt          # Only if newer

# Move/rename files
mv old_name.txt new_name.txt
mv file.txt /new/location/

# Remove files safely
rm -i file.txt                    # Interactive removal
rm -rf directory/                 # Force recursive removal (dangerous!)
find /path -name "*.tmp" -delete  # Safe alternative for patterns

# File permissions
chmod 755 script.sh               # rwxr-xr-x
chmod +x script.sh                # Make executable
chmod -R 644 directory/           # Recursive permission change
chown user:group file.txt         # Change ownership
```

## Find and Locate Operations

**Code**:

```bash
# Find files by name
find /path -name "*.log"
find . -iname "*.PDF"              # Case insensitive

# Find by size
find /path -size +100M             # Files larger than 100MB
find /path -size -1k               # Files smaller than 1KB

# Find by modification time
find /path -mtime -7               # Modified in last 7 days
find /path -mtime +30              # Modified more than 30 days ago

# Find and execute commands
find /path -name "*.tmp" -exec rm {} \;
find /path -type f -exec grep -l "pattern" {} \;

# Locate (requires updatedb)
locate filename.txt
updatedb                           # Update locate database
```

## File Content Operations

**Code**:

```bash
# View file content
cat file.txt                       # Display entire file
less file.txt                      # Paginated view
head -n 10 file.txt               # First 10 lines
tail -n 20 file.txt               # Last 20 lines
tail -f /var/log/syslog           # Follow file changes

# File information
ls -la                            # Detailed listing
stat file.txt                    # Detailed file info
file unknown_file                 # Determine file type
wc -l file.txt                   # Count lines
du -sh directory/                # Directory size
df -h                           # Disk space usage
```

## Archive and Compression

**Code**:

```bash
# tar operations
tar -czf archive.tar.gz directory/          # Create compressed archive
tar -xzf archive.tar.gz                     # Extract compressed archive
tar -tzf archive.tar.gz                     # List contents without extracting

# zip operations
zip -r archive.zip directory/               # Create zip archive
unzip archive.zip                           # Extract zip archive
unzip -l archive.zip                        # List zip contents

# Other compression
gzip file.txt                              # Compress file (creates file.txt.gz)
gunzip file.txt.gz                         # Decompress
```

**Usage**:

```bash
# Batch rename files
for file in *.txt; do
    mv "$file" "${file%.txt}.bak"
done

# Find large files
find /home -size +1G -type f -exec ls -lh {} \; | sort -k5 -hr

# Clean up old log files
find /var/log -name "*.log" -mtime +30 -exec gzip {} \;

# Sync directories
rsync -av --delete source/ destination/

# Create backup with timestamp
backup_name="backup_$(date +%Y%m%d_%H%M%S).tar.gz"
tar -czf "$backup_name" important_directory/
```

**Notes**:

- **Safety**: Always use `-i` flag for interactive operations when deleting
- **Permissions**: Be careful with recursive chmod/chown operations
- **Paths**: Use quotes around paths with spaces: `"/path/with spaces/"`
- **Wildcards**: `*` matches any characters, `?` matches single character
- **Performance**: Use `find` instead of `ls` with pipes for better performance
- **Security**: Validate inputs in scripts to prevent injection attacks
- **Alternatives**: Consider `fd` as a faster alternative to `find`

## Related Snippets

- [System Administration](system-admin.md) - System-level file operations
- [Text Processing](text-processing.md) - Processing file contents
