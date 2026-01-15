#!/bin/bash
# AIRoutine FullStack Project Initialization Script
# This script initializes git and adds required submodules

set -e

echo -e "\033[36mInitializing AIRoutine FullStack project...\033[0m"

# Check if git is installed
if ! command -v git &> /dev/null; then
    echo -e "\033[31mError: git is not installed or not in PATH\033[0m"
    exit 1
fi

# Check if already a git repository
if [ -d ".git" ]; then
    echo -e "\033[33mGit repository already exists\033[0m"
else
    echo -e "\033[32mInitializing git repository...\033[0m"
    git init
fi

# Add submodules
add_submodule() {
    local path=$1
    local url=$2

    if [ -d "$path" ]; then
        echo -e "\033[33mSubmodule '$path' already exists\033[0m"
    else
        echo -e "\033[32mAdding submodule: $path...\033[0m"
        git submodule add "$url" "$path"
    fi
}

add_submodule "subm/uno" "https://github.com/AIRoutine/UnoFramework.git"
add_submodule "subm/cli" "https://github.com/AIRoutine/Automation.Cli.git"

echo ""
echo -e "\033[32mProject initialized successfully!\033[0m"
echo ""
echo -e "\033[36mNext steps:\033[0m"
echo "  1. Start 'claude' to setup Claude Code plugins"
echo "  2. Build the solution with 'dotnet build'"
echo ""
