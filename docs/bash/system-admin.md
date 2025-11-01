# System Administration

**Description**: System administration tasks and monitoring in Bash
**Language/Technology**: Bash / System Administration

## Process Management

**Code**:

```bash
# Process information
ps aux                              # All processes with detailed info
ps -ef                             # Alternative format
pgrep process_name                 # Find process by name
pidof process_name                 # Get PID of process

# Process control
kill PID                           # Terminate process
kill -9 PID                        # Force kill process
killall process_name               # Kill all processes by name
pkill -f pattern                   # Kill processes matching pattern

# Job control
jobs                              # List background jobs
fg %1                            # Bring job 1 to foreground
bg %1                            # Send job 1 to background
nohup command &                  # Run command immune to hangups

# Process monitoring
top                              # Real-time process viewer
htop                             # Enhanced process viewer
watch -n 2 'ps aux | grep apache' # Watch processes every 2 seconds
```

## System Information

**Code**:

```bash
# System details
uname -a                         # System information
hostname                         # System hostname
uptime                          # System uptime and load
whoami                          # Current user
id                              # User and group IDs
groups                          # User group memberships

# Hardware information
lscpu                           # CPU information
free -h                         # Memory usage (human readable)
df -h                          # Disk space usage
lsblk                          # Block devices
lsusb                          # USB devices
lspci                          # PCI devices

# Network information
ifconfig                        # Network interfaces (deprecated)
ip addr show                    # Network interfaces (modern)
netstat -tuln                  # Network connections
ss -tuln                       # Socket statistics (modern)
```

## Service Management

**Code**:

```bash
# systemd services (modern Linux)
systemctl status service_name    # Check service status
systemctl start service_name     # Start service
systemctl stop service_name      # Stop service
systemctl restart service_name   # Restart service
systemctl enable service_name    # Enable service at boot
systemctl disable service_name   # Disable service at boot
systemctl list-units --type=service # List all services

# Traditional init (older systems)
service service_name status      # Check service status
/etc/init.d/service_name start  # Start service
chkconfig service_name on       # Enable service (Red Hat)
update-rc.d service_name enable # Enable service (Debian)
```

## Log Management

**Code**:

```bash
# System logs
journalctl                       # systemd journal (all logs)
journalctl -u service_name       # Logs for specific service
journalctl -f                    # Follow logs in real-time
journalctl --since "1 hour ago"  # Logs from last hour
journalctl -p err                # Only error messages

# Traditional log files
tail -f /var/log/syslog         # Follow system log
tail -f /var/log/messages       # Follow messages log
grep ERROR /var/log/apache2/error.log # Search for errors

# Log rotation
logrotate -d /etc/logrotate.conf # Test log rotation (dry run)
logrotate -f /etc/logrotate.conf # Force log rotation
```

## User and Permission Management

**Code**:

```bash
# User management
sudo useradd -m username         # Add user with home directory
sudo usermod -aG group username  # Add user to group
sudo userdel -r username         # Delete user and home directory
passwd username                  # Change password
chage -l username               # Check password aging info

# Group management
sudo groupadd groupname         # Create group
sudo groupdel groupname         # Delete group
getent group groupname          # Get group information

# Permission management
chmod 755 file_or_directory     # Set permissions
chown user:group file           # Change ownership
chgrp group file               # Change group ownership
umask 022                      # Set default permissions mask

# ACL (Access Control Lists)
setfacl -m u:username:rwx file  # Set ACL for user
getfacl file                   # Display ACL
```

## System Monitoring Scripts

**Code**:

```bash
#!/bin/bash

# System health check script
system_health_check() {
    echo "=== System Health Check ==="
    echo "Date: $(date)"
    echo
    
    # CPU usage
    echo "CPU Usage:"
    top -bn1 | grep "Cpu(s)" | awk '{print $2}' | sed 's/%us,//'
    echo
    
    # Memory usage
    echo "Memory Usage:"
    free -h | grep "Mem:" | awk '{printf "Used: %s/%s (%.1f%%)\n", $3, $2, ($3/$2)*100}'
    echo
    
    # Disk usage
    echo "Disk Usage:"
    df -h | grep -vE '^Filesystem|tmpfs|cdrom' | awk '{print $5 " " $6}' | while read output;
    do
        usage=$(echo $output | awk '{print $1}' | sed 's/%//g')
        partition=$(echo $output | awk '{print $2}')
        if [ $usage -ge 90 ]; then
            echo "WARNING: $partition is ${usage}% full"
        else
            echo "OK: $partition is ${usage}% full"
        fi
    done
    echo
    
    # Load average
    echo "Load Average:"
    uptime | awk -F'load average:' '{print $2}'
    echo
}

# Process monitoring
monitor_process() {
    local process_name="$1"
    local max_cpu=80
    local max_mem=80
    
    while true; do
        pid=$(pgrep "$process_name")
        if [ -n "$pid" ]; then
            cpu_usage=$(ps -p "$pid" -o %cpu --no-headers)
            mem_usage=$(ps -p "$pid" -o %mem --no-headers)
            
            cpu_int=$(echo "$cpu_usage" | cut -d. -f1)
            mem_int=$(echo "$mem_usage" | cut -d. -f1)
            
            if [ "$cpu_int" -gt "$max_cpu" ] || [ "$mem_int" -gt "$max_mem" ]; then
                echo "$(date): WARNING - $process_name (PID: $pid) - CPU: ${cpu_usage}%, MEM: ${mem_usage}%"
            fi
        else
            echo "$(date): Process $process_name not running"
        fi
        sleep 10
    done
}

# Cleanup script
cleanup_system() {
    echo "Starting system cleanup..."
    
    # Clean package cache (Debian/Ubuntu)
    if command -v apt-get &> /dev/null; then
        sudo apt-get autoremove -y
        sudo apt-get autoclean
    fi
    
    # Clean package cache (Red Hat/CentOS)
    if command -v yum &> /dev/null; then
        sudo yum autoremove -y
        sudo yum clean all
    fi
    
    # Clean temporary files
    sudo rm -rf /tmp/*
    sudo rm -rf /var/tmp/*
    
    # Clean log files older than 30 days
    find /var/log -name "*.log" -type f -mtime +30 -exec rm -f {} \;
    
    echo "System cleanup completed"
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
