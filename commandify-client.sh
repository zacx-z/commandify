#!/bin/bash

# Enable command history
HISTFILE="${XDG_DATA_HOME:-$HOME/.local/share}/commandify_history"
HISTSIZE=1000
HISTFILESIZE=2000

# Create history directory if it doesn't exist
mkdir -p "$(dirname "$HISTFILE")"
touch "$HISTFILE"

PORT=12345
HOST=localhost

# Function to print usage
print_usage() {
    echo "Usage: $0 [options]"
    echo "Options:"
    echo "  -p, --port PORT    Specify port (default: 12345)"
    echo "  -h, --host HOST    Specify host (default: localhost)"
    echo "  --help            Show this help message"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--port)
            PORT="$2"
            shift 2
            ;;
        -h|--host)
            HOST="$2"
            shift 2
            ;;
        --help)
            print_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            print_usage
            exit 1
            ;;
    esac
done

echo "Connecting to Commandify server at $HOST:$PORT..."

# Test server connection
if ! nc -z $HOST $PORT 2>/dev/null; then
    echo "Error: Could not connect to Commandify server at $HOST:$PORT"
    echo "Make sure the Unity Editor is running and the Commandify server is started."
    exit 1
fi

echo "Type 'help' for available commands, or 'exit' to quit."
echo "----------------------------------------"

# Main interaction loop
while true; do
    # Use read with readline support for history and arrow keys
    if ! read -e -p "commandify> " cmd; then
        # Handle Ctrl+D (EOF)
        echo -e "\nGoodbye!"
        exit 0
    fi

    # Add command to history if non-empty
    if [[ -n "$cmd" ]]; then
        history -s "$cmd"
    fi

    # Check for exit command
    if [[ "$cmd" == "exit" ]]; then
        echo "Goodbye!"
        exit 0
    fi

    # Check for help command
    if [[ "$cmd" == "help" ]]; then
        echo "Available Commands:"
        echo "Scene Management:"
        echo "  scene list [--opened | --all | --active]"
        echo "  scene open [--additive] <path>"
        echo "  scene new [<scene-template-name>]"
        echo "  scene save"
        echo "  scene unload <scene-specifier>..."
        echo "  scene activate <scene-specifier>"
        echo
        echo "Asset Operations:"
        echo "  asset list [--filter <filterspec> | --recursive] <path>"
        echo "  asset create <type> <path>"
        echo "  asset move <path> <new-path>"
        echo "  asset create-types"
        echo
        echo "Prefab Operations:"
        echo "  prefab instantiate <hierarchy-path>"
        echo "  prefab create [--variant] <selector> <path>"
        echo
        echo "View Operations:"
        echo "  list [--filter <filterspec>] [--path] <selector>"
        echo
        echo "Edit Operations:"
        echo "  select [--add] [--children] <selector>"
        echo "  property <command> <selector> [<args>]"
        echo "  component <command> <selector> [<args>]"
        echo "  transform <command> <selector> [<args>]"
        echo
        echo "Type 'exit' to quit"
        continue
    fi

    # Skip empty commands
    if [[ -z "$cmd" ]]; then
        continue
    fi

    # Send command to server and get response
    response=$(echo "$cmd" | nc $HOST $PORT)

    # Print response
    if [[ -n "$response" ]]; then
        echo "$response"
    fi
done
