# Configuration Helpers

**Description**: Utilities for managing application configuration files and settings
**Language/Technology**: Multiple / Configuration Management

## Configuration File Parsers

**Code**:

```bash
# INI file parser
parse_ini() {
    local file="$1"
    local section="$2"
    local key="$3"
    
    awk -v section="[$section]" -v key="$key" '
    $0 == section { in_section = 1; next }
    /^\[/ { in_section = 0 }
    in_section && $0 ~ "^" key "=" {
        sub("^" key "=", "")
        print $0
        exit
    }
    ' "$file"
}

# Environment variable loader
load_env_file() {
    local env_file="${1:-.env}"
    if [[ -f "$env_file" ]]; then
        set -a
        source "$env_file"
        set +a
        echo "Loaded environment from $env_file"
    else
        echo "Environment file $env_file not found"
        return 1
    fi
}

# Configuration validator
validate_config() {
    local config_file="$1"
    local required_keys=("$@")
    
    for key in "${required_keys[@]:1}"; do
        if ! grep -q "^$key=" "$config_file"; then
            echo "Missing required configuration: $key"
            return 1
        fi
    done
    echo "Configuration validation passed"
}
```

```python
# Python configuration utilities
import json
import yaml
import configparser
import os
from pathlib import Path
from typing import Dict, Any, Optional

class ConfigManager:
    """Unified configuration manager supporting multiple formats"""
    
    def __init__(self, config_path: str):
        self.config_path = Path(config_path)
        self.config_data = {}
        self.load_config()
    
    def load_config(self):
        """Load configuration based on file extension"""
        if not self.config_path.exists():
            raise FileNotFoundError(f"Config file not found: {self.config_path}")
        
        suffix = self.config_path.suffix.lower()
        
        if suffix == '.json':
            self._load_json()
        elif suffix in ['.yaml', '.yml']:
            self._load_yaml()
        elif suffix == '.ini':
            self._load_ini()
        elif suffix == '.env':
            self._load_env()
        else:
            raise ValueError(f"Unsupported config format: {suffix}")
    
    def _load_json(self):
        with open(self.config_path) as f:
            self.config_data = json.load(f)
    
    def _load_yaml(self):
        with open(self.config_path) as f:
            self.config_data = yaml.safe_load(f)
    
    def _load_ini(self):
        parser = configparser.ConfigParser()
        parser.read(self.config_path)
        self.config_data = {section: dict(parser[section]) 
                          for section in parser.sections()}
    
    def _load_env(self):
        with open(self.config_path) as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith('#'):
                    key, value = line.split('=', 1)
                    self.config_data[key] = value
    
    def get(self, key: str, default=None):
        """Get configuration value with dot notation support"""
        keys = key.split('.')
        value = self.config_data
        
        for k in keys:
            if isinstance(value, dict) and k in value:
                value = value[k]
            else:
                return default
        
        return value
    
    def set(self, key: str, value: Any):
        """Set configuration value with dot notation support"""
        keys = key.split('.')
        config = self.config_data
        
        for k in keys[:-1]:
            if k not in config:
                config[k] = {}
            config = config[k]
        
        config[keys[-1]] = value
    
    def validate_required(self, required_keys: list) -> bool:
        """Validate that all required keys exist"""
        missing = []
        for key in required_keys:
            if self.get(key) is None:
                missing.append(key)
        
        if missing:
            raise ValueError(f"Missing required configuration keys: {missing}")
        
        return True
    
    def save(self, path: Optional[str] = None):
        """Save configuration back to file"""
        save_path = Path(path) if path else self.config_path
        suffix = save_path.suffix.lower()
        
        if suffix == '.json':
            with open(save_path, 'w') as f:
                json.dump(self.config_data, f, indent=2)
        elif suffix in ['.yaml', '.yml']:
            with open(save_path, 'w') as f:
                yaml.dump(self.config_data, f, default_flow_style=False)
        elif suffix == '.ini':
            parser = configparser.ConfigParser()
            for section, values in self.config_data.items():
                parser[section] = values
            with open(save_path, 'w') as f:
                parser.write(f)

class EnvironmentConfig:
    """Environment-based configuration management"""
    
    @staticmethod
    def get_env(key: str, default=None, required=False):
        """Get environment variable with validation"""
        value = os.getenv(key, default)
        if required and value is None:
            raise ValueError(f"Required environment variable not set: {key}")
        return value
    
    @staticmethod
    def get_env_bool(key: str, default=False):
        """Get boolean environment variable"""
        value = os.getenv(key, str(default)).lower()
        return value in ('true', '1', 'yes', 'on')
    
    @staticmethod
    def get_env_int(key: str, default=0):
        """Get integer environment variable"""
        try:
            return int(os.getenv(key, default))
        except ValueError:
            return default
    
    @staticmethod
    def load_env_file(env_file='.env'):
        """Load environment variables from file"""
        env_path = Path(env_file)
        if env_path.exists():
            with open(env_path) as f:
                for line in f:
                    line = line.strip()
                    if line and not line.startswith('#') and '=' in line:
                        key, value = line.split('=', 1)
                        os.environ[key] = value
```

```javascript
// Node.js configuration utilities
const fs = require('fs');
const path = require('path');
const yaml = require('js-yaml');

class ConfigManager {
    constructor(configPath) {
        this.configPath = configPath;
        this.config = {};
        this.loadConfig();
    }
    
    loadConfig() {
        if (!fs.existsSync(this.configPath)) {
            throw new Error(`Config file not found: ${this.configPath}`);
        }
        
        const ext = path.extname(this.configPath).toLowerCase();
        const content = fs.readFileSync(this.configPath, 'utf8');
        
        switch (ext) {
            case '.json':
                this.config = JSON.parse(content);
                break;
            case '.yaml':
            case '.yml':
                this.config = yaml.load(content);
                break;
            case '.env':
                this.config = this.parseEnvFile(content);
                break;
            default:
                throw new Error(`Unsupported config format: ${ext}`);
        }
    }
    
    parseEnvFile(content) {
        const config = {};
        content.split('\n').forEach(line => {
            line = line.trim();
            if (line && !line.startsWith('#') && line.includes('=')) {
                const [key, ...valueParts] = line.split('=');
                config[key] = valueParts.join('=');
            }
        });
        return config;
    }
    
    get(key, defaultValue = null) {
        const keys = key.split('.');
        let value = this.config;
        
        for (const k of keys) {
            if (value && typeof value === 'object' && k in value) {
                value = value[k];
            } else {
                return defaultValue;
            }
        }
        
        return value;
    }
    
    set(key, value) {
        const keys = key.split('.');
        let config = this.config;
        
        for (let i = 0; i < keys.length - 1; i++) {
            const k = keys[i];
            if (!config[k] || typeof config[k] !== 'object') {
                config[k] = {};
            }
            config = config[k];
        }
        
        config[keys[keys.length - 1]] = value;
    }
    
    validateRequired(requiredKeys) {
        const missing = requiredKeys.filter(key => this.get(key) === null);
        if (missing.length > 0) {
            throw new Error(`Missing required configuration keys: ${missing.join(', ')}`);
        }
        return true;
    }
}

// Environment utilities
function getEnvVar(key, defaultValue = null, required = false) {
    const value = process.env[key] || defaultValue;
    if (required && value === null) {
        throw new Error(`Required environment variable not set: ${key}`);
    }
    return value;
}

function getEnvBool(key, defaultValue = false) {
    const value = (process.env[key] || defaultValue.toString()).toLowerCase();
    return ['true', '1', 'yes', 'on'].includes(value);
}

function loadEnvFile(envFile = '.env') {
    if (fs.existsSync(envFile)) {
        const content = fs.readFileSync(envFile, 'utf8');
        content.split('\n').forEach(line => {
            line = line.trim();
            if (line && !line.startsWith('#') && line.includes('=')) {
                const [key, ...valueParts] = line.split('=');
                process.env[key] = valueParts.join('=');
            }
        });
    }
}
```

## Configuration Templates

**Code**:

```yaml
# application.yaml template
app:
  name: "My Application"
  version: "1.0.0"
  environment: "development"

server:
  host: "localhost"
  port: 3000
  ssl:
    enabled: false
    cert_path: ""
    key_path: ""

database:
  type: "postgresql"
  host: "localhost"
  port: 5432
  name: "myapp_db"
  username: "${DB_USER}"
  password: "${DB_PASSWORD}"
  pool_size: 10
  timeout: 30

logging:
  level: "info"
  format: "json"
  outputs:
    - "console"
    - "file"
  file:
    path: "logs/app.log"
    max_size: "100MB"
    max_backups: 5

cache:
  type: "redis"
  host: "localhost"
  port: 6379
  ttl: 3600

monitoring:
  metrics:
    enabled: true
    port: 9090
  health_check:
    enabled: true
    endpoint: "/health"
```

```ini
# application.ini template
[app]
name = My Application
version = 1.0.0
environment = development

[server]
host = localhost
port = 3000
workers = 4

[database]
host = localhost
port = 5432
name = myapp_db
username = ${DB_USER}
password = ${DB_PASSWORD}

[logging]
level = info
file = logs/app.log
max_size = 100MB
```

```bash
# .env template
# Application settings
APP_NAME="My Application"
APP_VERSION="1.0.0"
APP_ENV="development"

# Server configuration
SERVER_HOST="localhost"
SERVER_PORT="3000"

# Database configuration
DB_TYPE="postgresql"
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="myapp_db"
DB_USER="dbuser"
DB_PASSWORD="dbpassword"

# Cache configuration
CACHE_TYPE="redis"
CACHE_HOST="localhost"
CACHE_PORT="6379"
CACHE_TTL="3600"

# Logging configuration
LOG_LEVEL="info"
LOG_FILE="logs/app.log"

# Security
JWT_SECRET="your-jwt-secret-here"
API_KEY="your-api-key-here"
```

**Usage**:

```bash
# Bash usage examples
parse_ini "config.ini" "database" "host"
load_env_file ".env.production"
validate_config "app.conf" "host" "port" "database_url"
```

```python
# Python usage examples
# Basic configuration management
config = ConfigManager('config.yaml')
db_host = config.get('database.host', 'localhost')
config.set('server.port', 8080)
config.validate_required(['app.name', 'database.host'])
config.save()

# Environment configuration
env = EnvironmentConfig()
debug = env.get_env_bool('DEBUG', False)
port = env.get_env_int('PORT', 3000)
api_key = env.get_env('API_KEY', required=True)

# Load from .env file
env.load_env_file('.env.local')
```

```javascript
// Node.js usage examples
const config = new ConfigManager('config.json');
const dbConfig = config.get('database');
config.set('server.port', 8080);
config.validateRequired(['app.name', 'database.url']);

// Environment variables
const port = getEnvVar('PORT', 3000);
const debug = getEnvBool('DEBUG');
loadEnvFile('.env.local');
```

**Notes**:

- **Security**: Never commit sensitive configuration files to version control
- **Environment Variables**: Use environment variables for sensitive data
- **Validation**: Always validate configuration on application startup
- **Defaults**: Provide sensible defaults for optional configuration
- **Documentation**: Document all configuration options and their purpose
- **Format Choice**: Choose configuration format based on complexity and team preferences
- **Environment-Specific**: Use different config files for different environments
- **Hot Reload**: Consider supporting configuration reload without restart

## Related Snippets

- [Environment Variables](../bash/system-admin.md) - Environment management
- [File Operations](../bash/file-operations.md) - File handling
- [JSON Processing](../javascript/README.md) - JSON utilities
