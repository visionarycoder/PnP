# File Operations

**Description**: Common file handling patterns for reading, writing, and processing files safely following PEP 8 standards.

**Language/Technology**: Python 3.12+

**Code**:

```python
import os
from pathlib import Path
from typing import List, Iterator, Optional

class FileOperations:
    """Collection of safe file operation utilities following PEP 8."""
    
    @staticmethod
    def read_file_safely(filepath: str, encoding: str = 'utf-8') -> str:
        """Safely read entire file contents.
        
        Args:
            filepath: Path to the file.
            encoding: File encoding (default: utf-8).
            
        Returns:
            File contents as string.
        """
        try:
            with open(filepath, 'r', encoding=encoding) as f:
                return f.read()
        except FileNotFoundError:
            print(f"File not found: {filepath}")
            return ""
        except PermissionError:
            print(f"Permission denied: {filepath}")
            return ""
        except UnicodeDecodeError as e:
            print(f"Encoding error reading file: {e}")
            return ""
        except Exception as e:
            print(f"Unexpected error reading file: {e}")
            return ""
    
    @staticmethod
    def read_lines(filepath: str, encoding: str = 'utf-8') -> List[str]:
        """
        Read file lines into a list
        
        Args:
            filepath: Path to the file
            encoding: File encoding (default: utf-8)
            
        Returns:
            List of lines (without newline characters)
        """
        try:
            with open(filepath, 'r', encoding=encoding) as f:
                return [line.rstrip('\n') for line in f]
        except Exception as e:
            print(f"Error reading file: {e}")
            return []
    
    @staticmethod
    def read_lines_generator(filepath: str, encoding: str = 'utf-8') -> Iterator[str]:
        """
        Read file lines lazily using a generator (memory efficient for large files)
        
        Args:
            filepath: Path to the file
            encoding: File encoding (default: utf-8)
            
        Yields:
            Individual lines from the file
        """
        try:
            with open(filepath, 'r', encoding=encoding) as f:
                for line in f:
                    yield line.rstrip('\n')
        except Exception as e:
            print(f"Error reading file: {e}")
    
    @staticmethod
    def write_file_safely(filepath: str, content: str, encoding: str = 'utf-8') -> bool:
        """
        Safely write content to a file
        
        Args:
            filepath: Path to the file
            content: Content to write
            encoding: File encoding (default: utf-8)
            
        Returns:
            True if successful, False otherwise
        """
        try:
            # Create directory if it doesn't exist
            Path(filepath).parent.mkdir(parents=True, exist_ok=True)
            
            with open(filepath, 'w', encoding=encoding) as f:
                f.write(content)
            return True
        except Exception as e:
            print(f"Error writing file: {e}")
            return False
    
    @staticmethod
    def append_to_file(filepath: str, content: str, encoding: str = 'utf-8') -> bool:
        """
        Append content to a file
        
        Args:
            filepath: Path to the file
            content: Content to append
            encoding: File encoding (default: utf-8)
            
        Returns:
            True if successful, False otherwise
        """
        try:
            with open(filepath, 'a', encoding=encoding) as f:
                f.write(content)
            return True
        except Exception as e:
            print(f"Error appending to file: {e}")
            return False
    
    @staticmethod
    def file_exists(filepath: str) -> bool:
        """Check if a file exists"""
        return os.path.isfile(filepath)
    
    @staticmethod
    def get_file_size(filepath: str) -> int:
        """Get file size in bytes"""
        try:
            return os.path.getsize(filepath)
        except Exception:
            return 0
```

**Usage**:

```python
# Example 1: Read entire file
content = FileOperations.read_file_safely('data.txt')
print(content)

# Example 2: Read lines into list
lines = FileOperations.read_lines('data.txt')
for line in lines:
    print(line)

# Example 3: Read large file efficiently with generator
for line in FileOperations.read_lines_generator('large_file.txt'):
    process_line(line)  # Process one line at a time

# Example 4: Write to file
success = FileOperations.write_file_safely('output.txt', 'Hello, World!')
if success:
    print("File written successfully")

# Example 5: Append to file
FileOperations.append_to_file('log.txt', 'New log entry\n')

# Example 6: Check file existence and size
if FileOperations.file_exists('data.txt'):
    size = FileOperations.get_file_size('data.txt')
    print(f"File size: {size} bytes")
```

**Notes**:

- Uses context managers (`with` statement) for safe file handling
- Automatically creates parent directories when writing files
- Generator pattern for memory-efficient processing of large files
- Includes error handling with try/except blocks
- Works with Python 3.6+
- Uses pathlib for cross-platform path handling
- All methods use UTF-8 encoding by default
- Related: [CSV Processing](csv-processing.md), [JSON Operations](json-operations.md)
