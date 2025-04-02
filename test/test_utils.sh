#!/bin/bash

# Common test utilities for Commandify test scripts

PORT=12345
HOST=localhost

# Helper function to send command and get response
send_command() {
    echo "$1" | nc $HOST $PORT
}

# Helper function to run test
run_test() {
    local test_name=$1
    local command=$2
    local expected_pattern=$3

    echo "Running test: $test_name"
    local result=$(send_command "$command")
    if echo "$result" | grep -q "$expected_pattern"; then
        echo "✅ Test passed: $test_name"
    else
        echo "❌ Test failed: $test_name"
        echo "Command: $command"
        echo "Expected pattern: $expected_pattern"
        echo "Got: $result"
        exit 1
    fi
}
