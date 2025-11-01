# Basic CMD Commands

**Description**: Essential Windows Command Prompt commands for daily file and directory operations.
**Language/Technology**: Windows CMD, Batch
**Prerequisites**: Windows operating system with Command Prompt access

## Directory Navigation

**Code**:

```cmd
:: Change directory
cd C:\Users\YourName\Documents
cd ..                    # Go up one directory
cd ..\..                 # Go up two directories  
cd \                     # Go to root directory
cd /d D:\Projects        # Change drive and directory

:: Show current directory
cd
echo %cd%               # Using environment variable

:: List directory contents
dir                     # Basic listing
dir /a                  # Show all files (including hidden)
dir /s                  # Include subdirectories
dir /w                  # Wide format
dir /o:n               # Sort by name
dir /o:d               # Sort by date
dir /o:s               # Sort by size

:: Tree view
tree                   # Show directory tree
tree /f                # Include files in tree
tree /a                # Use ASCII characters
```

**Usage**:

```cmd
C:\>cd C:\Users\John\Documents
C:\Users\John\Documents>dir /o:d
 Volume in drive C has no label.
 Directory of C:\Users\John\Documents

10/15/2023  02:30 PM    <DIR>          .
10/15/2023  02:30 PM    <DIR>          ..
10/01/2023  10:15 AM    <DIR>          Projects
10/05/2023  03:22 PM           1,024   readme.txt
               1 File(s)          1,024 bytes
               3 Dir(s)  15,234,567,890 bytes free
```

## File Operations

**Code**:

```cmd
:: Create files
echo Hello World > hello.txt         # Create file with content
echo Additional line >> hello.txt    # Append to file
copy nul empty.txt                   # Create empty file
type nul > empty2.txt               # Alternative method

:: Copy files
copy source.txt destination.txt      # Copy single file
copy *.txt backup\                   # Copy all .txt files
copy /y source.txt dest.txt         # Overwrite without prompt
xcopy source\ dest\ /s /e           # Copy directories recursively

:: Move/Rename files
move oldname.txt newname.txt         # Rename file
move file.txt C:\NewLocation\        # Move file
ren oldname.txt newname.txt         # Rename (alternative)

:: Delete files
del file.txt                        # Delete single file
del *.tmp                           # Delete all .tmp files
del /q /s temp\                     # Delete quietly and recursively
```

**Usage**:

```cmd
C:\Temp>echo Sample content > test.txt
C:\Temp>copy test.txt backup.txt
        1 file(s) copied.
C:\Temp>dir *.txt
 Volume in drive C has no label.
 Directory of C:\Temp

10/15/2023  03:45 PM              15 test.txt
10/15/2023  03:45 PM              15 backup.txt
               2 File(s)             30 bytes
```

## Directory Management

**Code**:

```cmd
:: Create directories
mkdir NewFolder                     # Create single directory
md "Folder With Spaces"             # Create directory with spaces
mkdir folder1\subfolder1\subsubf    # Create nested directories

:: Remove directories
rmdir EmptyFolder                   # Remove empty directory
rd /s /q FolderWithContent         # Remove directory and all contents
deltree OldFolder                  # Alternative (older systems)

:: Directory attributes
attrib +h SecretFolder             # Hide directory
attrib -h SecretFolder             # Unhide directory
attrib +r ReadOnlyFolder           # Make read-only
```

**Usage**:

```cmd
C:\Projects>mkdir MyApp\src\components
C:\Projects>cd MyApp
C:\Projects\MyApp>tree /a
Folder PATH listing
|   
\---src
    \---components

C:\Projects\MyApp>rmdir /s src
src, Are you sure (Y/N)? y
```

## Environment Variables

**Code**:

```cmd
:: Display environment variables
set                                 # Show all variables
echo %PATH%                        # Show specific variable
echo %USERNAME%                    # Current user
echo %COMPUTERNAME%               # Computer name
echo %DATE%                       # Current date
echo %TIME%                       # Current time

:: Set environment variables
set MYVAR=Hello World             # Set for current session
setx MYVAR "Hello World"          # Set permanently for user
setx MYVAR "Hello World" /m       # Set permanently system-wide

:: Use variables in commands
echo Hello %USERNAME%!
cd %USERPROFILE%\Documents
```

**Usage**:

```cmd
C:\>echo Current user: %USERNAME%
Current user: John

C:\>set TEMP_DIR=C:\MyTemp
C:\>mkdir %TEMP_DIR%
C:\>echo Directory created: %TEMP_DIR%
Directory created: C:\MyTemp
```

**Notes**:

**Command Tips**:

- Use quotes around paths with spaces: `cd "C:\Program Files"`
- Use `/q` flag for quiet operation (no prompts)
- Use `/s` flag for recursive operations
- Use `/?` after any command for help: `dir /?`

**Shortcuts**:

- `Tab` key for auto-completion
- `↑` and `↓` arrows for command history  
- `F7` to show command history window
- `Ctrl+C` to interrupt running command

**Safety Tips**:

- Always verify paths before using destructive commands like `del` or `rmdir`
- Use `/p` flag for prompting before each operation: `del *.* /p`
- Test commands on sample files first
- Consider using `echo` before destructive commands to preview

**Related Snippets**:

- [Batch Scripts](batch-scripts.md)
