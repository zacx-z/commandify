#!/bin/bash

# Define colors
CYAN=$'\e[0;36m'
NC=$'\e[0m' # No Color

# Enable command history
HISTFILE="${XDG_DATA_HOME:-$HOME/.local/share}/commandify_history"
HISTSIZE=1000
HISTFILESIZE=2000

shopt -s histappend

# Create history directory if it doesn't exist
mkdir -p "$(dirname "$HISTFILE")"
touch "$HISTFILE"
history -r

PORT=12345
HOST=localhost
SINGLE_COMMAND=""

# Function to print usage
print_usage() {
    echo "Usage: $0 [options]"
    echo "Options:"
    echo "  -p, --port PORT      Specify port (default: 12345)"
    echo "  -h, --host HOST      Specify host (default: localhost)"
    echo "  -c, --command CMD    Run a single command and exit"
    echo "  --help              Show this help message"
}

# Function to handle server response
handle_response() {
    local response="$1"
    local has_error=0

    # Process each line and route to appropriate output
    while IFS= read -r line; do
        if [[ "$line" == "[OUT]"* ]]; then
            # Output to stdout, removing the [OUT] prefix
            echo "${line#"[OUT]"}"
        elif [[ "$line" == "[ERR]"* ]]; then
            # Output to stderr, removing the [ERR] prefix
            echo "${line#"[ERR]"}" >&2
            has_error=1
        else
            # Invalid protocol line, treat as error
            echo "Protocol Error: Invalid line format: $line" >&2
            has_error=1
        fi
    done <<< "$response"

    return $has_error
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
        -c|--command)
            SINGLE_COMMAND="$2"
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

# If single command mode, execute it and exit
if [[ -n "$SINGLE_COMMAND" ]]; then
    response=$(echo "$SINGLE_COMMAND" | nc $HOST $PORT)
    if [[ -n "$response" ]]; then
        handle_response "$response"
        exit $?  # Exit with the error code from handle_response
    fi
    exit 0
fi

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
    echo -n $CYAN

    # Use read with readline support for history and arrow keys
    if ! read -e -p "commandify> " cmd; then
        # Handle Ctrl+D (EOF)
        echo -e "${NC}\nGoodbye!"
        history -w
        exit 0
    fi

    echo -n $NC

    # Add command to history if non-empty
    if [[ -n "$cmd" ]]; then
        history -s "$cmd"
    fi

    # Check for exit command
    if [[ "$cmd" == "exit" ]]; then
        echo "Goodbye!"
        history -w
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
        echo "Script Operations:"
        echo "  run <script-path> [<options>]  Execute a bash script with optional arguments"
        echo
        echo "Type 'exit' to quit"
        continue
    fi

    # Skip empty commands
    if [[ -z "$cmd" ]]; then
        continue
    fi

    # Handle run command
    if [[ "$cmd" =~ ^run[[:space:]]+([^[:space:]]+)(.*)$ ]]; then
        script_path="${BASH_REMATCH[1]}"
        script_args="${BASH_REMATCH[2]}"

        if [[ ! -f "$script_path" ]]; then
            echo "[ERR]Error: Script file '$script_path' not found" >&2
            continue
        fi
        if [[ ! -x "$script_path" ]]; then
            echo "[ERR]Error: Script file '$script_path' is not executable" >&2
            continue
        fi

        # Execute the script with any provided arguments and process each line
        while IFS= read -r line; do
            if [[ -n "$line" ]]; then
                # Send the command to server and handle response
                response=$(echo "$line" | nc $HOST $PORT)
                if [[ -n "$response" ]]; then
                    handle_response "$response"
                fi
            fi
        done < <($script_path $script_args || echo "[ERR]Script execution failed" >&2)
        continue
    fi

    # Send command and handle response
    if [[ -n "$cmd" ]]; then
        response=$(echo "$cmd" | nc $HOST $PORT)
        if [[ -n "$response" ]]; then
            handle_response "$response"
            # Don't exit on error in interactive mode
        fi
    fi
done
