# Logging Utilities

**Description**: Comprehensive logging solutions and utilities for applications
**Language/Technology**: Multiple / Logging

## Simple Logger Implementation

**Code**:

```bash
# Bash logging utilities
LOG_LEVEL_ERROR=0
LOG_LEVEL_WARN=1
LOG_LEVEL_INFO=2
LOG_LEVEL_DEBUG=3

LOG_LEVEL=${LOG_LEVEL:-$LOG_LEVEL_INFO}
LOG_FILE=${LOG_FILE:-""}

log() {
    local level="$1"
    local message="$2"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local log_entry="[$timestamp] [$level] $message"
    
    echo "$log_entry" >&2
    
    if [[ -n "$LOG_FILE" ]]; then
        echo "$log_entry" >> "$LOG_FILE"
    fi
}

log_error() {
    if [[ $LOG_LEVEL -ge $LOG_LEVEL_ERROR ]]; then
        log "ERROR" "$1"
    fi
}

log_warn() {
    if [[ $LOG_LEVEL -ge $LOG_LEVEL_WARN ]]; then
        log "WARN" "$1"
    fi
}

log_info() {
    if [[ $LOG_LEVEL -ge $LOG_LEVEL_INFO ]]; then
        log "INFO" "$1"
    fi
}

log_debug() {
    if [[ $LOG_LEVEL -ge $LOG_LEVEL_DEBUG ]]; then
        log "DEBUG" "$1"
    fi
}

# Log rotation utility
rotate_log() {
    local log_file="$1"
    local max_size="${2:-10M}"
    local keep_files="${3:-5}"
    
    if [[ -f "$log_file" ]]; then
        local size=$(stat -c%s "$log_file")
        local max_bytes=$(numfmt --from=iec "$max_size")
        
        if [[ $size -gt $max_bytes ]]; then
            # Rotate existing logs
            for ((i=keep_files-1; i>0; i--)); do
                if [[ -f "${log_file}.$i" ]]; then
                    mv "${log_file}.$i" "${log_file}.$((i+1))"
                fi
            done
            
            # Move current log to .1
            mv "$log_file" "${log_file}.1"
            echo "Log rotated: $log_file"
        fi
    fi
}
```

```python
# Python logging utilities
import logging
import logging.handlers
import json
import sys
from datetime import datetime
from pathlib import Path
from typing import Optional, Dict, Any

class ColoredFormatter(logging.Formatter):
    """Colored console formatter"""
    
    COLORS = {
        'DEBUG': '\033[36m',    # Cyan
        'INFO': '\033[32m',     # Green
        'WARNING': '\033[33m',  # Yellow
        'ERROR': '\033[31m',    # Red
        'CRITICAL': '\033[35m', # Magenta
        'RESET': '\033[0m'      # Reset
    }
    
    def format(self, record):
        log_color = self.COLORS.get(record.levelname, self.COLORS['RESET'])
        reset_color = self.COLORS['RESET']
        
        # Add color to levelname
        record.levelname = f"{log_color}{record.levelname}{reset_color}"
        
        return super().format(record)

class JSONFormatter(logging.Formatter):
    """JSON formatter for structured logging"""
    
    def format(self, record):
        log_entry = {
            'timestamp': datetime.fromtimestamp(record.created).isoformat(),
            'level': record.levelname,
            'logger': record.name,
            'message': record.getMessage(),
            'module': record.module,
            'function': record.funcName,
            'line': record.lineno
        }
        
        # Add exception info if present
        if record.exc_info:
            log_entry['exception'] = self.formatException(record.exc_info)
        
        # Add extra fields
        for key, value in record.__dict__.items():
            if key not in ['name', 'msg', 'args', 'levelname', 'levelno', 
                          'pathname', 'filename', 'module', 'lineno', 
                          'funcName', 'created', 'msecs', 'relativeCreated', 
                          'thread', 'threadName', 'processName', 'process',
                          'exc_info', 'exc_text', 'stack_info']:
                log_entry[key] = value
        
        return json.dumps(log_entry)

class Logger:
    """Enhanced logger with multiple output formats"""
    
    def __init__(self, name: str, level: str = 'INFO'):
        self.logger = logging.getLogger(name)
        self.logger.setLevel(getattr(logging, level.upper()))
        self.logger.handlers.clear()  # Remove existing handlers
        
        # Prevent duplicate logs from parent loggers
        self.logger.propagate = False
    
    def add_console_handler(self, colored: bool = True, json_format: bool = False):
        """Add console handler with optional coloring"""
        handler = logging.StreamHandler(sys.stdout)
        
        if json_format:
            formatter = JSONFormatter()
        elif colored:
            formatter = ColoredFormatter(
                '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
            )
        else:
            formatter = logging.Formatter(
                '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
            )
        
        handler.setFormatter(formatter)
        self.logger.addHandler(handler)
        return self
    
    def add_file_handler(self, 
                        filename: str, 
                        json_format: bool = False,
                        max_bytes: int = 10485760,  # 10MB
                        backup_count: int = 5):
        """Add rotating file handler"""
        Path(filename).parent.mkdir(parents=True, exist_ok=True)
        
        handler = logging.handlers.RotatingFileHandler(
            filename, maxBytes=max_bytes, backupCount=backup_count
        )
        
        if json_format:
            formatter = JSONFormatter()
        else:
            formatter = logging.Formatter(
                '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
            )
        
        handler.setFormatter(formatter)
        self.logger.addHandler(handler)
        return self
    
    def add_syslog_handler(self, address: tuple = ('localhost', 514)):
        """Add syslog handler for centralized logging"""
        handler = logging.handlers.SysLogHandler(address=address)
        formatter = logging.Formatter(
            '%(name)s[%(process)d]: %(levelname)s - %(message)s'
        )
        handler.setFormatter(formatter)
        self.logger.addHandler(handler)
        return self
    
    def debug(self, message: str, **kwargs):
        self.logger.debug(message, extra=kwargs)
    
    def info(self, message: str, **kwargs):
        self.logger.info(message, extra=kwargs)
    
    def warning(self, message: str, **kwargs):
        self.logger.warning(message, extra=kwargs)
    
    def error(self, message: str, **kwargs):
        self.logger.error(message, extra=kwargs)
    
    def critical(self, message: str, **kwargs):
        self.logger.critical(message, extra=kwargs)
    
    def exception(self, message: str, **kwargs):
        self.logger.exception(message, extra=kwargs)

# Context manager for request logging
class RequestLogger:
    """Context manager for request/operation logging"""
    
    def __init__(self, logger: Logger, operation: str, **context):
        self.logger = logger
        self.operation = operation
        self.context = context
        self.start_time = None
    
    def __enter__(self):
        self.start_time = datetime.now()
        self.logger.info(f"Starting {self.operation}", **self.context)
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        duration = datetime.now() - self.start_time
        duration_ms = duration.total_seconds() * 1000
        
        context = {**self.context, 'duration_ms': round(duration_ms, 2)}
        
        if exc_type:
            self.logger.error(f"Failed {self.operation}", 
                            exception_type=exc_type.__name__, 
                            **context)
        else:
            self.logger.info(f"Completed {self.operation}", **context)

# Performance logger decorator
def log_performance(logger: Logger, operation: str = None):
    """Decorator to log function performance"""
    def decorator(func):
        def wrapper(*args, **kwargs):
            op_name = operation or f"{func.__module__}.{func.__name__}"
            
            with RequestLogger(logger, op_name):
                return func(*args, **kwargs)
        
        return wrapper
    return decorator
```

```javascript
// Node.js logging utilities
const fs = require('fs');
const path = require('path');
const util = require('util');

class Logger {
    constructor(name, options = {}) {
        this.name = name;
        this.level = options.level || 'INFO';
        this.outputs = options.outputs || ['console'];
        this.logFile = options.logFile;
        this.jsonFormat = options.jsonFormat || false;
        this.colors = options.colors !== false;
        
        this.levels = {
            DEBUG: 0,
            INFO: 1,
            WARN: 2,
            ERROR: 3,
            FATAL: 4
        };
        
        this.colors_map = {
            DEBUG: '\x1b[36m',   // Cyan
            INFO: '\x1b[32m',    // Green
            WARN: '\x1b[33m',    // Yellow
            ERROR: '\x1b[31m',   // Red
            FATAL: '\x1b[35m',   // Magenta
            RESET: '\x1b[0m'     // Reset
        };
        
        // Create log directory if using file output
        if (this.logFile) {
            const logDir = path.dirname(this.logFile);
            if (!fs.existsSync(logDir)) {
                fs.mkdirSync(logDir, { recursive: true });
            }
        }
    }
    
    shouldLog(level) {
        return this.levels[level] >= this.levels[this.level];
    }
    
    formatMessage(level, message, extra = {}) {
        const timestamp = new Date().toISOString();
        
        if (this.jsonFormat) {
            return JSON.stringify({
                timestamp,
                level,
                logger: this.name,
                message,
                ...extra
            });
        } else {
            const color = this.colors ? this.colors_map[level] : '';
            const reset = this.colors ? this.colors_map.RESET : '';
            return `${timestamp} [${color}${level}${reset}] ${this.name}: ${message}`;
        }
    }
    
    log(level, message, extra = {}) {
        if (!this.shouldLog(level)) return;
        
        const formattedMessage = this.formatMessage(level, message, extra);
        
        // Console output
        if (this.outputs.includes('console')) {
            console.log(formattedMessage);
        }
        
        // File output
        if (this.outputs.includes('file') && this.logFile) {
            fs.appendFileSync(this.logFile, formattedMessage + '\n');
        }
    }
    
    debug(message, extra = {}) {
        this.log('DEBUG', message, extra);
    }
    
    info(message, extra = {}) {
        this.log('INFO', message, extra);
    }
    
    warn(message, extra = {}) {
        this.log('WARN', message, extra);
    }
    
    error(message, extra = {}) {
        this.log('ERROR', message, extra);
    }
    
    fatal(message, extra = {}) {
        this.log('FATAL', message, extra);
    }
    
    // Log rotation
    rotateLog(maxSize = 10 * 1024 * 1024, keepFiles = 5) {
        if (!this.logFile || !fs.existsSync(this.logFile)) return;
        
        const stats = fs.statSync(this.logFile);
        if (stats.size > maxSize) {
            // Rotate existing logs
            for (let i = keepFiles - 1; i > 0; i--) {
                const oldFile = `${this.logFile}.${i}`;
                const newFile = `${this.logFile}.${i + 1}`;
                
                if (fs.existsSync(oldFile)) {
                    fs.renameSync(oldFile, newFile);
                }
            }
            
            // Move current log to .1
            fs.renameSync(this.logFile, `${this.logFile}.1`);
            console.log(`Log rotated: ${this.logFile}`);
        }
    }
}

// Request logging middleware (Express.js)
function requestLoggingMiddleware(logger) {
    return (req, res, next) => {
        const startTime = Date.now();
        const originalEnd = res.end;
        
        res.end = function(...args) {
            const duration = Date.now() - startTime;
            
            logger.info('HTTP Request', {
                method: req.method,
                url: req.url,
                status: res.statusCode,
                duration: `${duration}ms`,
                userAgent: req.get('User-Agent'),
                ip: req.ip || req.connection.remoteAddress
            });
            
            originalEnd.apply(this, args);
        };
        
        next();
    };
}

// Performance logging decorator
function logPerformance(logger, operation) {
    return function(target, propertyKey, descriptor) {
        const originalMethod = descriptor.value;
        
        descriptor.value = async function(...args) {
            const startTime = Date.now();
            const opName = operation || `${target.constructor.name}.${propertyKey}`;
            
            try {
                logger.info(`Starting ${opName}`);
                const result = await originalMethod.apply(this, args);
                const duration = Date.now() - startTime;
                logger.info(`Completed ${opName}`, { duration: `${duration}ms` });
                return result;
            } catch (error) {
                const duration = Date.now() - startTime;
                logger.error(`Failed ${opName}`, { 
                    duration: `${duration}ms`,
                    error: error.message
                });
                throw error;
            }
        };
        
        return descriptor;
    };
}
```

## Log Analysis Utilities

**Code**:

```bash
# Log analysis functions
analyze_logs() {
    local log_file="$1"
    local time_range="${2:-1h}"
    
    echo "Log Analysis Report for: $log_file"
    echo "Time range: Last $time_range"
    echo "=================================="
    
    # Total log lines
    echo "Total entries: $(wc -l < "$log_file")"
    
    # Log level distribution
    echo -e "\nLog level distribution:"
    grep -oE '\[(DEBUG|INFO|WARN|ERROR|FATAL)\]' "$log_file" | sort | uniq -c | sort -nr
    
    # Error analysis
    echo -e "\nRecent errors:"
    grep -E '\[(ERROR|FATAL)\]' "$log_file" | tail -10
    
    # Top error messages
    echo -e "\nTop error patterns:"
    grep -E '\[(ERROR|FATAL)\]' "$log_file" | \
        sed 's/.*\] //' | sort | uniq -c | sort -nr | head -5
}

extract_slow_operations() {
    local log_file="$1"
    local threshold_ms="${2:-1000}"
    
    echo "Operations slower than ${threshold_ms}ms:"
    grep -E "duration.*[0-9]+ms" "$log_file" | \
        awk -v threshold="$threshold_ms" '
        match($0, /duration[^0-9]*([0-9]+)ms/, arr) {
            if (arr[1] > threshold) print $0
        }' | sort -k1,1
}

monitor_log_growth() {
    local log_file="$1"
    local interval="${2:-60}"
    
    while true; do
        if [[ -f "$log_file" ]]; then
            local size=$(stat -c%s "$log_file")
            local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
            echo "[$timestamp] Log size: $(numfmt --to=iec "$size")"
        fi
        sleep "$interval"
    done
}
```

**Usage**:

```bash
# Bash logging usage
LOG_LEVEL=3 LOG_FILE="app.log" 

log_info "Application started"
log_error "Database connection failed"
log_debug "Processing user request" 

# Log rotation
rotate_log "app.log" "10M" 5

# Log analysis
analyze_logs "app.log" "24h"
extract_slow_operations "app.log" 500
```

```python
# Python logging usage
# Basic logger setup
logger = Logger('myapp', level='DEBUG')
logger.add_console_handler(colored=True)
logger.add_file_handler('logs/app.log', json_format=True)

logger.info("Application started", version="1.0.0")
logger.error("Database error", error_code=500, table="users")

# Context logging
with RequestLogger(logger, "user_registration", user_id="12345"):
    # Registration logic here
    pass

# Performance logging decorator
@log_performance(logger, "data_processing")
def process_data(data):
    # Processing logic
    return processed_data
```

```javascript
// Node.js logging usage
const logger = new Logger('myapp', {
    level: 'DEBUG',
    outputs: ['console', 'file'],
    logFile: 'logs/app.log',
    jsonFormat: true
});

logger.info('Application started', { version: '1.0.0' });
logger.error('Database error', { errorCode: 500, table: 'users' });

// Express middleware
app.use(requestLoggingMiddleware(logger));

// Log rotation (call periodically)
setInterval(() => logger.rotateLog(), 24 * 60 * 60 * 1000); // Daily
```

**Notes**:

- **Performance**: Asynchronous logging for high-throughput applications
- **Security**: Sanitize log messages to prevent log injection
- **Storage**: Consider log aggregation services for production
- **Format**: Use structured logging (JSON) for better parsing
- **Rotation**: Implement log rotation to manage disk space
- **Levels**: Use appropriate log levels to control verbosity
- **Context**: Include relevant context information in logs
- **Monitoring**: Set up alerts for error patterns and log volume

## Related Snippets

- [Error Handling](../csharp/exception-handling.md) - Exception logging patterns
- [Configuration Helpers](configuration-helpers.md) - Logger configuration
- [System Administration](../bash/system-admin.md) - Log monitoring
