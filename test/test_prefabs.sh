#!/bin/bash

# Test script for Commandify Prefab Operations
# This script focuses on testing prefab-related commands

set -e  # Exit on error

# Source common test utilities
source "$(dirname "$0")/test_utils.sh"

echo "Starting Prefab Operation Tests..."

# Create test GameObject first
run_test "Create test GameObject" "create TestPrefabSource --with MeshRenderer,BoxCollider" "Created.*TestPrefabSource"

# Prefab Creation Tests
run_test "Create basic prefab" "prefab create TestPrefabSource Assets/Prefabs/TestPrefab.prefab" ".*"

# Create a variant
run_test "Create prefab variant" "prefab create --variant Assets/Prefabs/TestPrefab.prefab Assets/Prefabs/TestPrefabVariant.prefab" ".*"

# Test prefab instantiation
run_test "Instantiate prefab" "create Assets/Prefabs/TestPrefab.prefab PrefabInstance" ".*"

# Test prefab modifications
run_test "Modify prefab" "create Button --prefab Assets/Prefabs/TestPrefab.prefab --with Button" ".*"

echo "All prefab tests completed successfully! 🎉"
