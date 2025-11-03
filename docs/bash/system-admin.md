# System Administration

**Description**: System administration tasks and monitoring in Bash
**Language/Technology**: Bash / System Administration

## Process Management

**Code**:

```bash
#!/bin/bash
set -euo pipefail

# Safe process management with validation
find_process() {
    local process_name="$1"
    
    [[ -z "$process_name" ]] && { echo "ERROR: Process name required" >&2; return 1; }
    
    local pids
    if pids=$(pgrep -f "$process_name" 2>/dev/null); then
        echo "Found processes for '$process_name':"
        echo "$pids" | while IFS= read -r pid; do
            ps -p "$pid" -o pid,ppid,user,cmd --no-headers 2>/dev/null || true
        done
        return 0
    else
        echo "INFO: No processes found for '$process_name'"
        return 1
    fi
}

# Safe process termination with confirmation
terminate_process() {
    local process_pattern="$1"
    local signal="${2:-TERM}"
    local force="${3:-false}"
    
    [[ -z "$process_pattern" ]] && { echo "ERROR: Process pattern required" >&2; return 1; }
    
    local pids
    if ! pids=$(pgrep -f "$process_pattern" 2>/dev/null); then
        echo "INFO: No processes found matching '$process_pattern'"
        return 0
    fi
    
    local pid_count
    pid_count=$(echo "$pids" | wc -l)
    
    echo "Found $pid_count processes matching '$process_pattern':"
    echo "$pids" | while IFS= read -r pid; do
        ps -p "$pid" -o pid,user,cmd --no-headers 2>/dev/null || true
    done
    
    if [[ "$force" != "true" ]]; then
        read -p "Terminate these processes with signal $signal? [y/N]: " -r response
        [[ ! "$response" =~ ^[Yy]$ ]] && { echo "INFO: Operation cancelled"; return 0; }
    fi
    
    echo "$pids" | while IFS= read -r pid; do
        if kill -"$signal" "$pid" 2>/dev/null; then
            echo "INFO: Sent $signal signal to PID $pid"
        else
            echo "WARNING: Failed to signal PID $pid (may already be terminated)" >&2
        fi
    done
    
    # Wait and verify termination
    sleep 2
    local remaining
    if remaining=$(pgrep -f "$process_pattern" 2>/dev/null); then
        echo "WARNING: Some processes still running: $remaining"
        return 1
    else
        echo "INFO: All processes successfully terminated"
        return 0
    fi
}

# Process monitoring with resource usage
monitor_process() {
    local process_name="$1"
    local interval="${2:-5}"
    local duration="${3:-60}"
    
    [[ -z "$process_name" ]] && { echo "ERROR: Process name required" >&2; return 1; }
    
    echo "Monitoring processes matching '$process_name' for ${duration}s (interval: ${interval}s)"
    
    local end_time
    end_time=$(($(date +%s) + duration))
    
    while [[ $(date +%s) -lt $end_time ]]; do
        clear
        echo "=== Process Monitor ($(date)) ==="
        echo
        
        if pgrep -f "$process_name" >/dev/null 2>&1; then
            ps -C "$process_name" -o pid,ppid,%cpu,%mem,vsz,rss,tty,stat,start,time,cmd 2>/dev/null || {
                pgrep -f "$process_name" | while IFS= read -r pid; do
                    ps -p "$pid" -o pid,ppid,%cpu,%mem,vsz,rss,tty,stat,start,time,cmd --no-headers 2>/dev/null || true
                done
            }
        else
            echo "No processes found matching '$process_name'"
        fi
        
        sleep "$interval"
    done
}

# Resource-intensive process detection
find_resource_hogs() {
    local cpu_threshold="${1:-80}"
    local mem_threshold="${2:-80}"
    
    echo "=== Resource Usage Analysis ==="
    echo
    
    echo "High CPU usage processes (>$cpu_threshold%):"
    ps aux --sort=-%cpu --no-headers | awk -v threshold="$cpu_threshold" '$3 > threshold {printf "PID: %s, CPU: %s%%, CMD: %s\n", $2, $3, $11}' | head -5
    
    echo
    echo "High memory usage processes (>$mem_threshold%):"
    ps aux --sort=-%mem --no-headers | awk -v threshold="$mem_threshold" '$4 > threshold {printf "PID: %s, MEM: %s%%, CMD: %s\n", $2, $4, $11}' | head -5
    
    echo
    echo "System load average:"
    uptime
    
    echo
    echo "Memory usage:"
    free -h
}
```

## System Information

**Code**:

```bash
#!/bin/bash
set -euo pipefail

# Comprehensive system information gathering
gather_system_info() {
    local output_file="${1:-system_info_$(date +%Y%m%d_%H%M%S).txt}"
    
    echo "Gathering system information..."
    
    {
        echo "=== System Information Report ==="
        echo "Generated: $(date)"
        echo "Hostname: $(hostname)"
        echo "User: $(whoami)"
        echo
        
        echo "=== System Details ==="
        uname -a
        echo
        
        echo "=== Uptime and Load ==="
        uptime
        echo
        
        echo "=== CPU Information ==="
        if command -v lscpu >/dev/null 2>&1; then
            lscpu | head -20
        else
            grep -E '^(processor|model name|cpu MHz|cache size|cpu cores)' /proc/cpuinfo | head -10
        fi
        echo
        
        echo "=== Memory Information ==="
        free -h
        echo
        echo "Memory breakdown:"
        awk '/MemTotal:|MemFree:|MemAvailable:|Buffers:|Cached:/ {printf "%-15s %s\n", $1, $2}' /proc/meminfo
        echo
        
        echo "=== Disk Usage ==="
        df -h | grep -vE '^(tmpfs|udev)'
        echo
        
        echo "=== Block Devices ==="
        if command -v lsblk >/dev/null 2>&1; then
            lsblk
        else
            fdisk -l 2>/dev/null | grep -E '^Disk /dev' || echo "Block device info requires root privileges"
        fi
        echo
        
        echo "=== Network Interfaces ==="
        if command -v ip >/dev/null 2>&1; then
            ip addr show | grep -E '^[0-9]+:|inet '
        else
            ifconfig | grep -E '^[a-zA-Z]|inet addr:' 2>/dev/null || echo "Network info unavailable"
        fi
        echo
        
        echo "=== Active Network Connections ==="
        if command -v ss >/dev/null 2>&1; then
            ss -tuln | head -20
        else
            netstat -tuln 2>/dev/null | head -20 || echo "Network connections info unavailable"
        fi
        
    } | tee "$output_file"
    
    echo "INFO: System information saved to '$output_file'"
}

# Check system health with alerts
check_system_health() {
    local cpu_threshold="${1:-85}"
    local mem_threshold="${2:-90}"
    local disk_threshold="${3:-85}"
    local alert_log="${4:-/tmp/system_health_alerts.log}"
    
    local timestamp
    timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    local alerts=()
    
    # CPU load check
    local load_avg
    load_avg=$(uptime | awk '{print $(NF-2)}' | sed 's/,//')
    local cpu_cores
    cpu_cores=$(nproc)
    local load_percentage
    load_percentage=$(awk -v load="$load_avg" -v cores="$cpu_cores" 'BEGIN {printf "%.0f", (load/cores)*100}')
    
    if [[ $load_percentage -gt $cpu_threshold ]]; then
        alerts+=("HIGH CPU LOAD: ${load_percentage}% (threshold: ${cpu_threshold}%)")
    fi
    
    # Memory usage check
    local mem_info
    mem_info=$(free | awk 'NR==2{printf "%.0f", $3/$2*100}')
    if [[ $mem_info -gt $mem_threshold ]]; then
        alerts+=("HIGH MEMORY USAGE: ${mem_info}% (threshold: ${mem_threshold}%)")
    fi
    
    # Disk usage check
    while IFS= read -r line; do
        local usage
        usage=$(echo "$line" | awk '{print $5}' | sed 's/%//')
        local mount
        mount=$(echo "$line" | awk '{print $6}')
        
        if [[ $usage -gt $disk_threshold ]]; then
            alerts+=("HIGH DISK USAGE: ${usage}% on $mount (threshold: ${disk_threshold}%)")
        fi
    done < <(df -h | grep -vE '^(tmpfs|udev|Filesystem)' | grep -E '^/dev')
    
    # Report results
    if [[ ${#alerts[@]} -gt 0 ]]; then
        echo "⚠️  SYSTEM HEALTH ALERTS ⚠️"
        printf '%s\n' "${alerts[@]}"
        
        # Log alerts
        {
            echo "[$timestamp] HEALTH CHECK ALERTS:"
            printf '%s\n' "${alerts[@]}"
            echo "---"
        } >> "$alert_log"
        
        return 1
    else
        echo "✅ System health check passed - all metrics within thresholds"
        echo "[$timestamp] Health check: OK" >> "$alert_log"
        return 0
    fi
}

# Hardware inventory with error handling
collect_hardware_info() {
    local output_file="${1:-hardware_inventory_$(date +%Y%m%d_%H%M%S).txt}"
    
    echo "Collecting hardware inventory..."
    
    {
        echo "=== Hardware Inventory Report ==="
        echo "Generated: $(date)"
        echo "System: $(hostname)"
        echo
        
        echo "=== PCI Devices ==="
        if command -v lspci >/dev/null 2>&1; then
            lspci | head -20
        else
            echo "lspci not available - install pciutils package"
        fi
        echo
        
        echo "=== USB Devices ==="
        if command -v lsusb >/dev/null 2>&1; then
            lsusb
        else
            echo "lsusb not available - install usbutils package"
        fi
        echo
        
        echo "=== Storage Devices ==="
        if [[ -r /proc/scsi/scsi ]]; then
            cat /proc/scsi/scsi
        else
            echo "SCSI info not accessible"
        fi
        echo
        
        echo "=== DMI Information ==="
        if command -v dmidecode >/dev/null 2>&1 && [[ $EUID -eq 0 ]]; then
            dmidecode -t system | grep -E 'Manufacturer|Product|Serial'
        else
            echo "DMI info requires root privileges and dmidecode package"
        fi
        
    } | tee "$output_file"
    
    echo "INFO: Hardware inventory saved to '$output_file'"
}
```

## Service Management

**Code**:

```bash
#!/bin/bash
set -euo pipefail

# Service management with validation and error handling
manage_service() {
    local service_name="$1"
    local action="$2"
    local enable_on_start="${3:-false}"
    
    [[ -z "$service_name" ]] && { echo "ERROR: Service name required" >&2; return 1; }
    [[ -z "$action" ]] && { echo "ERROR: Action required (status|start|stop|restart|enable|disable)" >&2; return 1; }
    
    # Check if service exists
    if ! systemctl list-unit-files --type=service | grep -q "^${service_name}.service"; then
        echo "ERROR: Service '$service_name' not found" >&2
        return 1
    fi
    
    echo "Managing service '$service_name' - Action: $action"
    
    case "$action" in
        "status")
            systemctl status "$service_name" --no-pager || {
                echo "INFO: Service is not active"
                return 1
            }
            ;;
        "start")
            if systemctl is-active --quiet "$service_name"; then
                echo "INFO: Service '$service_name' is already running"
            else
                if systemctl start "$service_name"; then
                    echo "INFO: Service '$service_name' started successfully"
                    
                    if [[ "$enable_on_start" == "true" ]]; then
                        systemctl enable "$service_name"
                        echo "INFO: Service '$service_name' enabled for boot"
                    fi
                else
                    echo "ERROR: Failed to start service '$service_name'" >&2
                    return 1
                fi
            fi
            ;;
        "stop")
            if systemctl is-active --quiet "$service_name"; then
                if systemctl stop "$service_name"; then
                    echo "INFO: Service '$service_name' stopped successfully"
                else
                    echo "ERROR: Failed to stop service '$service_name'" >&2
                    return 1
                fi
            else
                echo "INFO: Service '$service_name' is already stopped"
            fi
            ;;
        "restart")
            if systemctl restart "$service_name"; then
                echo "INFO: Service '$service_name' restarted successfully"
            else
                echo "ERROR: Failed to restart service '$service_name'" >&2
                return 1
            fi
            ;;
        "enable")
            if systemctl enable "$service_name"; then
                echo "INFO: Service '$service_name' enabled for boot"
            else
                echo "ERROR: Failed to enable service '$service_name'" >&2
                return 1
            fi
            ;;
        "disable")
            if systemctl disable "$service_name"; then
                echo "INFO: Service '$service_name' disabled from boot"
            else
                echo "ERROR: Failed to disable service '$service_name'" >&2
                return 1
            fi
            ;;
        *)
            echo "ERROR: Invalid action '$action'. Use: status|start|stop|restart|enable|disable" >&2
            return 1
            ;;
    esac
}

# Service health monitoring
monitor_services() {
    local services=("$@")
    local check_interval=30
    local log_file="/var/log/service_monitor.log"
    
    [[ ${#services[@]} -eq 0 ]] && { echo "ERROR: No services specified to monitor" >&2; return 1; }
    
    echo "Starting service monitoring for: ${services[*]}"
    echo "Check interval: ${check_interval}s"
    echo "Log file: $log_file"
    
    while true; do
        local timestamp
        timestamp=$(date '+%Y-%m-%d %H:%M:%S')
        
        for service in "${services[@]}"; do
            if systemctl is-active --quiet "$service"; then
                echo "[$timestamp] ✅ $service: ACTIVE"
            else
                echo "[$timestamp] ❌ $service: INACTIVE/FAILED" | tee -a "$log_file"
                
                # Attempt automatic restart for critical services
                if [[ "$service" =~ ^(nginx|apache2|mysql|postgresql|docker)$ ]]; then
                    echo "[$timestamp] 🔄 Attempting to restart critical service: $service" | tee -a "$log_file"
                    if systemctl restart "$service"; then
                        echo "[$timestamp] ✅ Successfully restarted $service" | tee -a "$log_file"
                    else
                        echo "[$timestamp] ❌ Failed to restart $service - manual intervention required" | tee -a "$log_file"
                    fi
                fi
            fi
        done
        
        sleep "$check_interval"
    done
}

# Service discovery and analysis
analyze_services() {
    local output_file="${1:-service_analysis_$(date +%Y%m%d_%H%M%S).txt}"
    
    echo "Analyzing system services..."
    
    {
        echo "=== Service Analysis Report ==="
        echo "Generated: $(date)"
        echo
        
        echo "=== Active Services ==="
        systemctl list-units --type=service --state=active --no-pager
        echo
        
        echo "=== Failed Services ==="
        systemctl list-units --type=service --state=failed --no-pager
        echo
        
        echo "=== Enabled Services ==="
        systemctl list-unit-files --type=service --state=enabled --no-pager | head -20
        echo
        
        echo "=== Service Dependencies (Critical Services) ==="
        for service in sshd nginx apache2 mysql postgresql docker; do
            if systemctl list-unit-files --type=service | grep -q "^${service}.service"; then
                echo "--- $service dependencies ---"
                systemctl list-dependencies "$service" --no-pager 2>/dev/null | head -10
                echo
            fi
        done
        
    } | tee "$output_file"
    
    echo "INFO: Service analysis saved to '$output_file'"
}
```

## Log Management

**Code**:

```bash
#!/bin/bash
set -euo pipefail

# Advanced log analysis with filtering and alerting
analyze_logs() {
    local service_name="${1:-}"
    local time_range="${2:-1 hour ago}"
    local priority="${3:-info}"
    local output_file="${4:-log_analysis_$(date +%Y%m%d_%H%M%S).txt}"
    
    echo "Analyzing logs..."
    echo "Service: ${service_name:-all services}"
    echo "Time range: since $time_range"
    echo "Priority: $priority and higher"
    
    {
        echo "=== Log Analysis Report ==="
        echo "Generated: $(date)"
        echo "Time range: since $time_range"
        echo "Priority filter: $priority"
        echo
        
        if [[ -n "$service_name" ]]; then
            echo "=== Service-specific logs: $service_name ==="
            journalctl -u "$service_name" --since "$time_range" -p "$priority" --no-pager
        else
            echo "=== System logs (all services) ==="
            journalctl --since "$time_range" -p "$priority" --no-pager | head -100
        fi
        
        echo
        echo "=== Error Summary ==="
        journalctl --since "$time_range" -p err --no-pager | grep -E "(error|failed|fatal)" | sort | uniq -c | sort -rn
        
        echo
        echo "=== Failed Services ==="
        systemctl --failed --no-pager
        
    } | tee "$output_file"
    
    echo "INFO: Log analysis saved to '$output_file'"
}

# Real-time log monitoring with alerts
monitor_logs() {
    local service_name="${1:-}"
    local alert_patterns="${2:-error|failed|critical|fatal}"
    local alert_log="/tmp/log_alerts_$(date +%Y%m%d).log"
    
    echo "Starting log monitoring..."
    echo "Service: ${service_name:-all services}"
    echo "Alert patterns: $alert_patterns"
    echo "Alert log: $alert_log"
    
    local journal_cmd="journalctl -f --no-pager"
    [[ -n "$service_name" ]] && journal_cmd="$journal_cmd -u $service_name"
    
    $journal_cmd | while IFS= read -r line; do
        echo "$line"
        
        # Check for alert patterns
        if echo "$line" | grep -qiE "$alert_patterns"; then
            local timestamp
            timestamp=$(date '+%Y-%m-%d %H:%M:%S')
            local alert_entry="[$timestamp] ALERT: $line"
            
            echo "$alert_entry" >> "$alert_log"
            echo "🚨 ALERT DETECTED: $line" >&2
            
            # Optional: Send notification (requires configuration)
            # notify-send "Log Alert" "$line" 2>/dev/null || true
        fi
    done
}

# Log rotation and cleanup management
manage_log_rotation() {
    local config_file="${1:-/etc/logrotate.conf}"
    local dry_run="${2:-true}"
    local force_rotation="${3:-false}"
    
    [[ ! -r "$config_file" ]] && { echo "ERROR: Cannot read config file '$config_file'" >&2; return 1; }
    
    echo "Log rotation management"
    echo "Config file: $config_file"
    echo "Dry run: $dry_run"
    
    if [[ "$dry_run" == "true" ]]; then
        echo "=== Testing log rotation configuration ==="
        logrotate -d "$config_file" 2>&1 | head -20
    else
        if [[ "$force_rotation" == "true" ]]; then
            echo "=== Forcing log rotation ==="
            logrotate -f -v "$config_file"
        else
            echo "=== Running scheduled log rotation ==="
            logrotate -v "$config_file"
        fi
    fi
}

# Log space analysis and cleanup
analyze_log_space() {
    local threshold_gb="${1:-5}"
    local cleanup_days="${2:-30}"
    
    echo "=== Log Space Analysis ==="
    echo "Threshold: ${threshold_gb}GB"
    echo "Cleanup files older than: ${cleanup_days} days"
    echo
    
    # Check journal size
    echo "=== Journal Size ==="
    journalctl --disk-usage
    echo
    
    # Check log directories
    echo "=== Large Log Files (>100MB) ==="
    find /var/log -type f -size +100M -exec ls -lh {} \; 2>/dev/null | sort -k5 -hr | head -10
    
    echo
    echo "=== Old Log Files (>${cleanup_days} days) ==="
    find /var/log -type f -mtime "+$cleanup_days" -exec ls -lh {} \; 2>/dev/null | head -10
    
    # Calculate total size
    local total_size_mb
    total_size_mb=$(du -sm /var/log 2>/dev/null | cut -f1)
    local total_size_gb
    total_size_gb=$(awk -v size="$total_size_mb" 'BEGIN {printf "%.2f", size/1024}')
    
    echo
    echo "Total log directory size: ${total_size_gb}GB"
    
    if (( $(awk -v size="$total_size_gb" -v threshold="$threshold_gb" 'BEGIN {print (size > threshold)}') )); then
        echo "⚠️  WARNING: Log directory size exceeds ${threshold_gb}GB threshold"
        echo "Consider running cleanup or adjusting log retention policies"
        
        return 1
    else
        echo "✅ Log directory size within acceptable limits"
        return 0
    fi
}

# Search logs with context and highlighting
search_logs() {
    local search_pattern="$1"
    local service_name="${2:-}"
    local context_lines="${3:-5}"
    local time_range="${4:-1 day ago}"
    
    [[ -z "$search_pattern" ]] && { echo "ERROR: Search pattern required" >&2; return 1; }
    
    echo "Searching logs for pattern: '$search_pattern'"
    echo "Service: ${service_name:-all services}"
    echo "Context: $context_lines lines"
    echo "Time range: since $time_range"
    echo
    
    local journal_cmd="journalctl --since '$time_range' --no-pager"
    [[ -n "$service_name" ]] && journal_cmd="$journal_cmd -u $service_name"
    
    # Search with context and highlighting
    $journal_cmd | grep -i -C "$context_lines" --color=always "$search_pattern" || {
        echo "No matches found for pattern '$search_pattern'"
        return 1
    }
}
```

## User and Permission Management

**Code**:

```bash
#!/bin/bash
set -euo pipefail

# Safe user management with validation
create_user() {
    local username="$1"
    local groups="${2:-users}"
    local shell="${3:-/bin/bash}"
    local create_home="${4:-true}"
    
    [[ -z "$username" ]] && { echo "ERROR: Username required" >&2; return 1; }
    [[ ! "$username" =~ ^[a-z_][a-z0-9_-]*$ ]] && { echo "ERROR: Invalid username format" >&2; return 1; }
    
    # Check if user already exists
    if id "$username" >/dev/null 2>&1; then
        echo "WARNING: User '$username' already exists"
        return 1
    fi
    
    # Validate shell
    if [[ ! -f "$shell" ]]; then
        echo "ERROR: Shell '$shell' not found" >&2
        return 1
    fi
    
    local useradd_cmd="useradd"
    [[ "$create_home" == "true" ]] && useradd_cmd="$useradd_cmd -m"
    useradd_cmd="$useradd_cmd -s $shell"
    
    echo "Creating user '$username'..."
    echo "Groups: $groups"
    echo "Shell: $shell"
    echo "Create home: $create_home"
    
    if sudo $useradd_cmd "$username"; then
        echo "INFO: User '$username' created successfully"
        
        # Add to groups
        IFS=',' read -ra group_array <<< "$groups"
        for group in "${group_array[@]}"; do
            group=$(echo "$group" | xargs) # trim whitespace
            if getent group "$group" >/dev/null; then
                sudo usermod -aG "$group" "$username"
                echo "INFO: Added user '$username' to group '$group'"
            else
                echo "WARNING: Group '$group' does not exist"
            fi
        done
        
        # Set initial password
        echo "Setting password for '$username'..."
        sudo passwd "$username"
        
        return 0
    else
        echo "ERROR: Failed to create user '$username'" >&2
        return 1
    fi
}

# Safe permission management
set_permissions() {
    local target="$1"
    local permissions="$2"
    local owner="${3:-}"
    local group="${4:-}"
    local recursive="${5:-false}"
    
    [[ -z "$target" ]] && { echo "ERROR: Target path required" >&2; return 1; }
    [[ -z "$permissions" ]] && { echo "ERROR: Permissions required" >&2; return 1; }
    [[ ! -e "$target" ]] && { echo "ERROR: Target '$target' does not exist" >&2; return 1; }
    
    # Validate permissions format
    if [[ ! "$permissions" =~ ^[0-7]{3,4}$ ]]; then
        echo "ERROR: Invalid permissions format. Use octal notation (e.g., 755)" >&2
        return 1
    fi
    
    echo "Setting permissions on '$target'"
    echo "Permissions: $permissions"
    echo "Owner: ${owner:-unchanged}"
    echo "Group: ${group:-unchanged}"
    echo "Recursive: $recursive"
    
    # Set permissions
    local chmod_cmd="chmod"
    [[ "$recursive" == "true" ]] && chmod_cmd="$chmod_cmd -R"
    
    if $chmod_cmd "$permissions" "$target"; then
        echo "INFO: Permissions set successfully"
    else
        echo "ERROR: Failed to set permissions" >&2
        return 1
    fi
    
    # Set ownership if specified
    if [[ -n "$owner" || -n "$group" ]]; then
        local chown_target="${owner:-}:${group:-}"
        [[ "$chown_target" == ":" ]] && return 0 # Nothing to change
        
        local chown_cmd="chown"
        [[ "$recursive" == "true" ]] && chown_cmd="$chown_cmd -R"
        
        if sudo $chown_cmd "$chown_target" "$target"; then
            echo "INFO: Ownership changed successfully"
        else
            echo "ERROR: Failed to change ownership" >&2
            return 1
        fi
    fi
}

# User audit and security check
audit_users() {
    local output_file="${1:-user_audit_$(date +%Y%m%d_%H%M%S).txt}"
    local inactive_days="${2:-90}"
    
    echo "Performing user audit..."
    
    {
        echo "=== User Audit Report ==="
        echo "Generated: $(date)"
        echo "Inactive threshold: $inactive_days days"
        echo
        
        echo "=== All Users ==="
        getent passwd | sort -t: -k3 -n
        echo
        
        echo "=== Users with Login Shells ==="
        getent passwd | awk -F: '$7 !~ /(nologin|false)$/ {print $1 ":" $3 ":" $7}' | sort -t: -k2 -n
        echo
        
        echo "=== Users with UID 0 (Root Privileges) ==="
        getent passwd | awk -F: '$3 == 0 {print $1}'
        echo
        
        echo "=== Users with Empty Passwords ==="
        sudo getent shadow | awk -F: '$2 == "" {print $1 " - NO PASSWORD SET"}' || echo "Shadow file access denied"
        echo
        
        echo "=== Recently Created Users (Last 30 days) ==="
        find /home -maxdepth 1 -type d -newerct "30 days ago" -exec ls -ld {} \; 2>/dev/null
        echo
        
        echo "=== Sudo Users ==="
        getent group sudo 2>/dev/null | cut -d: -f4 | tr ',' '\n' || echo "No sudo group found"
        getent group wheel 2>/dev/null | cut -d: -f4 | tr ',' '\n' || echo "No wheel group found"
        echo
        
        echo "=== Failed Login Attempts (Last 24 hours) ==="
        if command -v lastb >/dev/null 2>&1; then
            sudo lastb -t $(date --date='1 day ago' '+%Y%m%d%H%M%S') 2>/dev/null | head -20 || echo "No recent failed logins"
        else
            journalctl --since "24 hours ago" | grep -i "failed\|invalid" | grep -i "login\|user" | tail -10 || echo "Login failure info not available"
        fi
        
    } | tee "$output_file"
    
    echo "INFO: User audit saved to '$output_file'"
}
```

## System Monitoring Scripts

**Code**:

```bash
#!/bin/bash
set -euo pipefail

# Comprehensive system health monitoring with detailed reporting
comprehensive_health_check() {
    local output_file="${1:-system_health_$(date +%Y%m%d_%H%M%S).txt}"
    local cpu_threshold="${2:-80}"
    local mem_threshold="${3:-85}"
    local disk_threshold="${4:-90}"
    
    echo "Running comprehensive health check..."
    
    {
        echo "=== COMPREHENSIVE SYSTEM HEALTH REPORT ==="
        echo "Generated: $(date)"
        echo "Thresholds: CPU=${cpu_threshold}%, Memory=${mem_threshold}%, Disk=${disk_threshold}%"
        echo
        
        echo "=== SYSTEM OVERVIEW ==="
        echo "Hostname: $(hostname)"
        echo "Uptime: $(uptime -p)"
        echo "Kernel: $(uname -r)"
        echo "Architecture: $(uname -m)"
        echo
        
        echo "=== CPU ANALYSIS ==="
        local cpu_usage
        cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | sed 's/%us,//' | sed 's/%//')
        echo "Current CPU Usage: ${cpu_usage}%"
        
        if (( $(awk -v cpu="$cpu_usage" -v threshold="$cpu_threshold" 'BEGIN {print (cpu > threshold)}') )); then
            echo "⚠️  WARNING: CPU usage (${cpu_usage}%) exceeds threshold (${cpu_threshold}%)"
        else
            echo "✅ CPU usage within acceptable limits"
        fi
        
        echo "Load Average: $(uptime | awk -F'load average:' '{print $2}')"
        echo "CPU Cores: $(nproc)"
        echo
        
        echo "=== MEMORY ANALYSIS ==="
        local mem_total mem_used mem_available mem_percentage
        mem_total=$(free -b | awk 'NR==2{print $2}')
        mem_used=$(free -b | awk 'NR==2{print $3}')
        mem_available=$(free -b | awk 'NR==2{print $7}')
        mem_percentage=$(awk -v used="$mem_used" -v total="$mem_total" 'BEGIN {printf "%.1f", (used/total)*100}')
        
        echo "Memory Usage: $(free -h | awk 'NR==2{printf "%s/%s (%.1f%%)", $3, $2, ($3/$2)*100}')"
        
        if (( $(awk -v mem="$mem_percentage" -v threshold="$mem_threshold" 'BEGIN {print (mem > threshold)}') )); then
            echo "⚠️  WARNING: Memory usage (${mem_percentage}%) exceeds threshold (${mem_threshold}%)"
        else
            echo "✅ Memory usage within acceptable limits"
        fi
        echo
        
        echo "=== DISK ANALYSIS ==="
        while IFS= read -r line; do
            local usage partition
            usage=$(echo "$line" | awk '{print $5}' | sed 's/%//')
            partition=$(echo "$line" | awk '{print $6}')
            
            if [[ "$usage" =~ ^[0-9]+$ ]] && (( usage > disk_threshold )); then
                echo "⚠️  WARNING: $partition is ${usage}% full (threshold: ${disk_threshold}%)"
            else
                echo "✅ $partition: ${usage}% used"
            fi
        done < <(df -h | grep -vE '^(Filesystem|tmpfs|udev|devtmpfs)')
        echo
        
        echo "=== NETWORK STATUS ==="
        echo "Active connections:"
        ss -tuln | wc -l | awk '{print "Total: " $1 " connections"}'
        
        echo "Network interfaces:"
        ip -o link show | awk '{print $2 ": " $9}' | sed 's/://'
        echo
        
        echo "=== SERVICE STATUS ==="
        echo "Failed services:"
        if systemctl --failed --quiet; then
            systemctl --failed --no-pager
        else
            echo "✅ No failed services"
        fi
        echo
        
        echo "=== SECURITY CHECKS ==="
        echo "Recent authentication failures:"
        journalctl --since "24 hours ago" -u ssh 2>/dev/null | grep -i "failed\|invalid" | wc -l | awk '{print "SSH failures: " $1}'
        
        echo "Root login sessions:"
        last root | head -5 | grep -v "wtmp begins" || echo "No recent root logins"
        
    } | tee "$output_file"
    
    echo
    echo "INFO: Comprehensive health report saved to '$output_file'"
}

# Advanced process monitoring with resource tracking
monitor_system_resources() {
    local duration="${1:-300}" # Default 5 minutes
    local interval="${2:-10}"  # Default 10 seconds
    local log_file="${3:-resource_monitor_$(date +%Y%m%d_%H%M%S).log}"
    
    echo "Starting system resource monitoring..."
    echo "Duration: ${duration}s, Interval: ${interval}s"
    echo "Log file: $log_file"
    
    local start_time end_time
    start_time=$(date +%s)
    end_time=$((start_time + duration))
    
    {
        echo "=== SYSTEM RESOURCE MONITORING LOG ==="
        echo "Started: $(date)"
        echo "Duration: ${duration}s, Interval: ${interval}s"
        echo
        echo "Format: TIMESTAMP,CPU%,MEM%,LOAD1,LOAD5,LOAD15,DISK_IO_READ,DISK_IO_WRITE"
    } > "$log_file"
    
    while [[ $(date +%s) -lt $end_time ]]; do
        local timestamp cpu_usage mem_usage load_avg
        timestamp=$(date '+%Y-%m-%d %H:%M:%S')
        
        # Get CPU usage
        cpu_usage=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | sed 's/%us,//' | sed 's/%//')
        
        # Get memory usage
        mem_usage=$(free | awk 'NR==2{printf "%.1f", ($3/$2)*100}')
        
        # Get load averages
        load_avg=$(uptime | awk -F'load average:' '{print $2}' | sed 's/^ *//')
        
        # Get disk I/O (simplified)
        local disk_reads disk_writes
        disk_reads=$(awk '/sda/ {print $6}' /proc/diskstats 2>/dev/null | head -1 || echo "0")
        disk_writes=$(awk '/sda/ {print $10}' /proc/diskstats 2>/dev/null | head -1 || echo "0")
        
        # Log data
        echo "$timestamp,$cpu_usage,$mem_usage,$load_avg,$disk_reads,$disk_writes" >> "$log_file"
        
        # Display current status
        printf "\r⏱️  %s | CPU: %s%% | MEM: %s%% | Load: %s" \
               "$(date '+%H:%M:%S')" "$cpu_usage" "$mem_usage" "${load_avg// /}"
        
        sleep "$interval"
    done
    
    echo
    echo "INFO: Resource monitoring completed. Data saved to '$log_file'"
}

# Intelligent system cleanup with safety checks
smart_system_cleanup() {
    local dry_run="${1:-true}"
    local log_retention_days="${2:-30}"
    local temp_age_days="${3:-7}"
    local min_free_space_gb="${4:-1}"
    
    echo "=== SMART SYSTEM CLEANUP ==="
    echo "Dry run: $dry_run"
    echo "Log retention: $log_retention_days days"
    echo "Temp file age threshold: $temp_age_days days"
    echo "Minimum free space required: ${min_free_space_gb}GB"
    echo
    
    local cleanup_summary=()
    
    # Check available space
    local free_space_gb
    free_space_gb=$(df / | awk 'NR==2 {printf "%.1f", $4/1024/1024}')
    echo "Current free space: ${free_space_gb}GB"
    
    if (( $(awk -v free="$free_space_gb" -v min="$min_free_space_gb" 'BEGIN {print (free < min)}') )); then
        echo "⚠️  WARNING: Free space below minimum threshold"
    fi
    
    # Package cache cleanup
    echo
    echo "=== PACKAGE CACHE CLEANUP ==="
    if command -v apt-get >/dev/null 2>&1; then
        local apt_cache_size
        apt_cache_size=$(du -sh /var/cache/apt 2>/dev/null | cut -f1 || echo "0")
        echo "APT cache size: $apt_cache_size"
        
        if [[ "$dry_run" == "false" ]]; then
            sudo apt-get autoremove -y
            sudo apt-get autoclean
            cleanup_summary+=("APT cache cleaned")
        else
            echo "DRY RUN: Would clean APT cache"
        fi
    fi
    
    if command -v dnf >/dev/null 2>&1; then
        local dnf_cache_size
        dnf_cache_size=$(du -sh /var/cache/dnf 2>/dev/null | cut -f1 || echo "0")
        echo "DNF cache size: $dnf_cache_size"
        
        if [[ "$dry_run" == "false" ]]; then
            sudo dnf autoremove -y
            sudo dnf clean all
            cleanup_summary+=("DNF cache cleaned")
        else
            echo "DRY RUN: Would clean DNF cache"
        fi
    fi
    
    # Temporary files cleanup
    echo
    echo "=== TEMPORARY FILES CLEANUP ==="
    local temp_dirs=("/tmp" "/var/tmp")
    
    for temp_dir in "${temp_dirs[@]}"; do
        if [[ -d "$temp_dir" ]]; then
            local temp_size old_files_count
            temp_size=$(du -sh "$temp_dir" 2>/dev/null | cut -f1 || echo "0")
            old_files_count=$(find "$temp_dir" -type f -mtime "+$temp_age_days" 2>/dev/null | wc -l)
            
            echo "$temp_dir size: $temp_size (files older than $temp_age_days days: $old_files_count)"
            
            if [[ "$dry_run" == "false" ]] && [[ $old_files_count -gt 0 ]]; then
                find "$temp_dir" -type f -mtime "+$temp_age_days" -delete 2>/dev/null || true
                cleanup_summary+=("Cleaned $old_files_count old files from $temp_dir")
            else
                echo "DRY RUN: Would remove $old_files_count old files from $temp_dir"
            fi
        fi
    done
    
    # Log files cleanup
    echo
    echo "=== LOG FILES CLEANUP ==="
    if [[ -d "/var/log" ]]; then
        local log_size old_logs_count
        log_size=$(du -sh /var/log 2>/dev/null | cut -f1 || echo "0")
        old_logs_count=$(find /var/log -name "*.log" -type f -mtime "+$log_retention_days" 2>/dev/null | wc -l)
        
        echo "Log directory size: $log_size (old log files: $old_logs_count)"
        
        if [[ "$dry_run" == "false" ]] && [[ $old_logs_count -gt 0 ]]; then
            find /var/log -name "*.log" -type f -mtime "+$log_retention_days" -exec rm -f {} \; 2>/dev/null || true
            cleanup_summary+=("Removed $old_logs_count old log files")
        else
            echo "DRY RUN: Would remove $old_logs_count old log files"
        fi
    fi
    
    # Journal cleanup
    echo
    echo "=== SYSTEMD JOURNAL CLEANUP ==="
    local journal_size
    journal_size=$(journalctl --disk-usage 2>/dev/null | grep -o '[0-9.]*[KMGT]B' || echo "Unknown")
    echo "Journal size: $journal_size"
    
    if [[ "$dry_run" == "false" ]]; then
        sudo journalctl --vacuum-time="${log_retention_days}d"
        cleanup_summary+=("Journal cleaned (retained ${log_retention_days} days)")
    else
        echo "DRY RUN: Would clean journal older than $log_retention_days days"
    fi
    
    # Summary
    echo
    echo "=== CLEANUP SUMMARY ==="
    if [[ "$dry_run" == "false" ]]; then
        printf '%s\n' "${cleanup_summary[@]}"
        echo "Cleanup completed successfully"
    else
        echo "Dry run completed - no changes made"
        echo "Run with 'false' as first parameter to execute cleanup"
    fi
}
```

**Usage**:

```bash
# Monitor system resources
watch -n 5 'free -m && echo && df -h'

# Find processes using most CPU
ps aux --sort=-%cpu | head -10

# Find processes using most memory
ps aux --sort=-%mem | head -10

# Check for failed systemd services
systemctl --failed

# Monitor network connections
netstat -tuln | grep :80  # Check web server connections

# System maintenance
# Run the health check
system_health_check

# Monitor a specific process
monitor_process "apache2" &

# Clean up system
cleanup_system
```

**Notes**:

- **Permissions**: Many system administration commands require sudo privileges
- **Safety**: Always test scripts in non-production environments first
- **Monitoring**: Set up proper monitoring tools for production systems
- **Logging**: Ensure important operations are logged for audit purposes
- **Security**: Regularly update systems and monitor for vulnerabilities
- **Backup**: Always have backups before making system changes
- **Automation**: Use cron jobs for regular maintenance tasks

## Related Snippets

- [File Operations](file-operations.md) - File system management
- [Text Processing](text-processing.md) - Log analysis and processing
