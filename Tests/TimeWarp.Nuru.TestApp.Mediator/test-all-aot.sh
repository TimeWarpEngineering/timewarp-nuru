#!/bin/bash

# Comprehensive integration test suite with output validation
# Tests real-world CLI scenarios and validates actual output

# Color codes for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

PASSED=0
FAILED=0

# Function to run test and validate output
run_test() {
    local name="$1"
    local command="$2"
    local expected="$3"
    
    echo -n "  $name ... "
    
    # Run command and capture both stdout and stderr
    # Use eval to properly handle quoted arguments
    output=$(eval "\"$EXECUTABLE\" $command 2>&1")
    
    # Check if output contains expected string
    if echo "$output" | grep -q "$expected"; then
        echo -e "${GREEN}✓ PASSED${NC}"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAILED${NC}"
        echo "    Command: $command"
        echo "    Expected to contain: '$expected'"
        echo "    Actual output: '$output'"
        ((FAILED++))
    fi
}

echo "======================================"
echo "TimeWarp.Nuru Integration Tests (AOT)"
echo "======================================"
echo ""

# Build the AOT project first
echo "Building AOT project..."
echo "This may take a minute..."
dotnet publish --nologo -c Release -r linux-x64 -p:PublishAot=true -o ./aot-output 2>&1 | grep -E "(error|warning|Generating native code)" || true
if [ ${PIPESTATUS[0]} -ne 0 ]; then
    echo -e "${RED}AOT build failed!${NC}"
    exit 1
fi

# Find the AOT executable
EXECUTABLE="./aot-output/TimeWarp.Nuru.TestApp"
if [ ! -f "$EXECUTABLE" ]; then
    echo -e "${RED}Could not find AOT executable at $EXECUTABLE${NC}"
    exit 1
fi

# Check if it's actually AOT
echo "Verifying AOT binary..."
file "$EXECUTABLE" | grep -q "ELF" || {
    echo -e "${RED}Not an AOT binary!${NC}"
    exit 1
}

# Show binary size
BINARY_SIZE=$(ls -lh "$EXECUTABLE" | awk '{print $5}')
echo "AOT binary size: $BINARY_SIZE"
echo ""

echo "Test 1: Basic Commands"
echo "---------------------"
run_test "System status check" "status" "✓ System is running"
run_test "Version display" "version" "TimeWarp.Nuru v1.0.0"

echo ""
echo "Test 2: Sub-Commands"
echo "--------------------"
run_test "Git status" "git status" "On branch main"
run_test "Git commit (no changes)" "git commit" "Nothing to commit, working tree clean"
run_test "Git push" "git push" "Everything up-to-date"

echo ""
echo "Test 3: Option-Based Routing"
echo "-----------------------------"
run_test "Git amend" "git commit --amend" "Amending previous commit"
run_test "Git amend no-edit" "git commit --amend --no-edit" "Amending without editing message"

echo ""
echo "Test 4: Options with Values"
echo "----------------------------"
run_test "Git commit with message (long form)" 'git commit --message "Initial commit"' "Initial commit"
run_test "Git commit with message (short form)" 'git commit -m "Quick fix"' "Quick fix"
run_test "Git log with count" "git log --max-count 5" "Showing last 5 commits"
run_test "Git log with count (int parsing)" "git log --max-count 10" "Showing last 10 commits"

echo ""
echo "Test 5: Docker Pass-Through with Enhanced Features"
echo "--------------------------------------------------"
run_test "Docker run with enhanced logs" "docker run --enhance-logs nginx" "🚀 Running nginx with enhanced logging"
run_test "Docker run standard (catch-all)" "docker run -d -p 8080:80 nginx" "docker run -d -p 8080:80 nginx"
run_test "Docker run interactive (catch-all)" "docker run --rm -it ubuntu bash" "docker run --rm -it ubuntu bash"

echo ""
echo "Test 6: Docker Build Pass-Through"
echo "----------------------------------"
run_test "Docker build with tag" 'docker build -t myapp .' "docker build -t myapp ."
run_test "Docker ps all" "docker ps -a" "docker ps -a"

echo ""
echo "Test 7: kubectl Enhancement"
echo "----------------------------"
run_test "kubectl enhanced watch" "kubectl get pods --watch --enhanced" "⚡ Enhanced watch for pods"
run_test "kubectl standard watch" "kubectl get pods --watch" "Watching pods..."
run_test "kubectl get services" "kubectl get services" "NAME.*READY.*STATUS.*RESTARTS.*AGE"
run_test "kubectl apply" "kubectl apply -f deployment.yaml" "deployment.apps/deployment.yaml configured"

echo ""
echo "Test 8: npm with Options"
echo "-------------------------"
run_test "npm install dev dependency" "npm install express --save-dev" "📦 Installing express as dev dependency"
run_test "npm install dependency" "npm install express --save" "📦 Installing express as dependency"
run_test "npm install (no flag)" "npm install express" "📦 Installing express"
run_test "npm run script" "npm run build" "🏃 Running script: build"

echo ""
echo "Test 9: Option Order Independence"
echo "---------------------------------"
echo "  (All should match the same handler)"
run_test "Options: -m then --amend" 'git commit -m "Test message" --amend' "Amending with message: Test message"
run_test "Options: --amend then -m" 'git commit --amend -m "Test message"' "Amending with message: Test message"
run_test "Options: --amend then --message" 'git commit --amend --message "Test message"' "Amending with message: Test message"
run_test "Options: --message then --amend" 'git commit --message "Test message" --amend' "Amending with message: Test message"

echo ""
echo "Test 10: Option Aliases"
echo "-----------------------"
run_test "Short form -m" 'git commit -m "Short form"' "Short form.*using -m shorthand"
run_test "Long form --message" 'git commit --message "Long form"' "Long form.*using --message flag"

echo ""
echo "Test 11: Ultimate Catch-All"
echo "----------------------------"
run_test "Unknown command" "some random command that does not match anything" "Unknown command: some random command that does not match anything"
run_test "Multiple unknown args" "foo bar baz qux" "Unknown command: foo bar baz qux"

echo ""
echo "Test 12: Help System"
echo "--------------------"
run_test "Help flag" "--help" "TimeWarp.Nuru Integration Tests"

echo ""
echo "Test 13: Parameter Type Conversion"
echo "----------------------------------"
run_test "Integer parameter" "git log --max-count 42" "Showing last 42 commits"
run_test "Invalid integer" "git log --max-count abc" "Cannot convert 'abc' to type System.Int32"

echo ""
echo "Test 14: Catch-All Parameters"
echo "-----------------------------"
run_test "Docker complex command" "docker run -v /host:/container -e ENV=prod --name test nginx" "docker run -v /host:/container -e ENV=prod --name test nginx"
run_test "npm complex command" "npm install react react-dom @types/react --save-dev --legacy-peer-deps" "npm install react react-dom @types/react --save-dev --legacy-peer-deps"

echo ""
echo "======================================"
echo -e "Results: ${GREEN}$PASSED passed${NC}, ${RED}$FAILED failed${NC}"
echo "======================================"

# Exit with error if any tests failed
if [ $FAILED -gt 0 ]; then
    exit 1
fi