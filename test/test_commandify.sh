#!/bin/bash

# Test script for Commandify
# This script tests various commands available in the Commandify server

set -e  # Exit on error

# Source common test utilities
source "$(dirname "$0")/test_utils.sh"

echo "Starting Commandify tests..."

# Scene Management Tests
run_test "List all scenes" "scene list --all" ".*"
run_test "List opened scenes" "scene list --opened" ".*"
run_test "List active scene" "scene list --active" ".*"

# GameObject Creation Tests
run_test "Create empty GameObject" "create TestObject" "Created.*TestObject"
run_test "Create GameObject with components" "create CameraObj --with Camera,AudioListener" "Created.*CameraObj"

# Asset Operation Tests
run_test "List assets in Assets folder" "asset list Assets" ".*"
run_test "List available asset types" "asset create-types" ".*"

# Component Tests
run_test "Search all components" "component search \"*\"" ".*"
run_test "List components on main camera" "component list MainCamera" ".*"

# Transform Tests
run_test "Check object position" "transform translate TestObject" ".*"
run_test "Check object rotation" "transform rotate TestObject" ".*"
run_test "Check object scale" "transform scale TestObject" ".*"

# View Operation Tests
run_test "List objects with full format" "list --format full --components TestObject" ".*"

echo "All tests completed successfully! 🎉"
