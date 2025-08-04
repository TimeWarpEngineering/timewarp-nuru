#!/bin/bash

# Comprehensive test that compares Delegate vs Mediator implementations
# Tests both JIT and AOT versions

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Change to the repository root (parent of Tests directory)
pushd "$SCRIPT_DIR/.." > /dev/null || exit 1

# Ensure we return to original directory on exit
trap 'popd > /dev/null' EXIT

# Create log file with timestamp in logs directory
LOG_FILE="Tests/logs/comparison-results-$(date '+%Y%m%d-%H%M%S').log"
mkdir -p "$(dirname "$LOG_FILE")"
exec > >(tee "$LOG_FILE")
exec 2>&1

# Color codes for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

PASSED=0
FAILED=0

# Function to run test and validate output
run_test() {
    local name="$1"
    local command="$2"
    local expected="$3"
    
    # Run command and capture both stdout and stderr
    output=$(eval "\"$EXECUTABLE\" $command 2>&1")
    
    # Check if output contains expected string
    if echo "$output" | grep -q "$expected"; then
        ((PASSED++))
    else
        echo -e "${RED}✗ FAILED${NC}: $name"
        echo "    Command: $command"
        echo "    Expected to contain: '$expected'"
        echo "    Actual output: '$output'"
        ((FAILED++))
    fi
}

run_all_tests() {
    # Reset counters
    PASSED=0
    FAILED=0
    
    # Test 1: Basic Commands
    run_test "System status check" "status" "✓ System is running"
    run_test "Version display" "version" "TimeWarp.Nuru v1.0.0"

    # Test 2: Sub-Commands
    run_test "Git status" "git status" "On branch main"
    run_test "Git commit (no changes)" "git commit" "Nothing to commit, working tree clean"
    run_test "Git push" "git push" "Everything up-to-date"

    # Test 3: Option-Based Routing
    run_test "Git amend" "git commit --amend" "Amending previous commit"
    run_test "Git amend no-edit" "git commit --amend --no-edit" "Amending without editing message"

    # Test 4: Options with Values
    run_test "Git commit with message (long form)" 'git commit --message "Initial commit"' "Initial commit"
    run_test "Git commit with message (short form)" 'git commit -m "Quick fix"' "Quick fix"
    run_test "Git log with count" "git log --max-count 5" "Showing last 5 commits"
    run_test "Git log with count (int parsing)" "git log --max-count 10" "Showing last 10 commits"

    # Test 5: Docker Pass-Through with Enhanced Features
    run_test "Docker run with enhanced logs" "docker run --enhance-logs nginx" "🚀 Running nginx with enhanced logging"
    run_test "Docker run standard (catch-all)" "docker run -d -p 8080:80 nginx" "docker run -d -p 8080:80 nginx"
    run_test "Docker run interactive (catch-all)" "docker run --rm -it ubuntu bash" "docker run --rm -it ubuntu bash"

    # Test 6: Docker Build Pass-Through
    run_test "Docker build with tag" 'docker build -t myapp .' "docker build -t myapp ."
    run_test "Docker ps all" "docker ps -a" "docker ps -a"

    # Test 7: kubectl Enhancement
    run_test "kubectl enhanced watch" "kubectl get pods --watch --enhanced" "⚡ Enhanced watch for pods"
    run_test "kubectl standard watch" "kubectl get pods --watch" "Watching pods..."
    run_test "kubectl get services" "kubectl get services" "NAME.*READY.*STATUS.*RESTARTS.*AGE"
    run_test "kubectl apply" "kubectl apply -f deployment.yaml" "deployment.apps/deployment.yaml configured"

    # Test 8: npm with Options
    run_test "npm install dev dependency" "npm install express --save-dev" "📦 Installing express as dev dependency"
    run_test "npm install dependency" "npm install express --save" "📦 Installing express as dependency"
    run_test "npm install (no flag)" "npm install express" "📦 Installing express"
    run_test "npm run script" "npm run build" "🏃 Running script: build"

    # Test 9: Option Order Independence
    run_test "Options: -m then --amend" 'git commit -m "Test message" --amend' "Amending with message: Test message"
    run_test "Options: --amend then -m" 'git commit --amend -m "Test message"' "Amending with message: Test message"
    run_test "Options: --amend then --message" 'git commit --amend --message "Test message"' "Amending with message: Test message"
    run_test "Options: --message then --amend" 'git commit --message "Test message" --amend' "Amending with message: Test message"

    # Test 10: Option Aliases
    run_test "Short form -m" 'git commit -m "Short form"' "Short form.*using -m shorthand"
    run_test "Long form --message" 'git commit --message "Long form"' "Long form.*using --message flag"

    # Test 11: Async void methods
    run_test "Async void method" "async-test" "Async operation completed"

    # Test 12: Ultimate Catch-All
    run_test "Unknown command" "some random command that does not match anything" "Unknown command: some random command that does not match anything"
    run_test "Multiple unknown args" "foo bar baz qux" "Unknown command: foo bar baz qux"

    # Test 13: Help System
    run_test "Help flag" "--help" "TimeWarp.Nuru Integration Tests"

    # Test 14: Parameter Type Conversion
    run_test "Integer parameter" "git log --max-count 42" "Showing last 42 commits"
    run_test "Invalid integer" "git log --max-count abc" "Cannot convert 'abc' to type System.Int32"

    # Test 15: Catch-All Parameters
    run_test "Docker complex command" "docker run -v /host:/container -e ENV=prod --name test nginx" "docker run -v /host:/container -e ENV=prod --name test nginx"
    run_test "npm complex command" "npm install react react-dom @types/react --save-dev --legacy-peer-deps" "npm install react react-dom @types/react --save-dev --legacy-peer-deps"
    
    # Test 16: Optional Parameters
    run_test "Deploy with required param only" "deploy prod" "Deploying to prod with latest tag"
    run_test "Deploy with both params" "deploy prod v2.0" "Deploying to prod with tag v2.0"
    run_test "Backup with required param only" "backup mydata" "Backing up mydata to default location"
    run_test "Backup with both params" "backup mydata /backup/location" "Backing up mydata to /backup/location"
    
    # Test 17: Nullable Type Parameters (Currently failing - tracked in task 003)
    run_test "Sleep with int? parameter" "sleep 5" "Sleeping for 5 seconds"
    run_test "Sleep with default (no param)" "sleep" "Sleeping for 1 seconds"
}

echo "=============================================="
echo "TimeWarp.Nuru Delegate vs Mediator Comparison"
echo "=============================================="
echo "Run started at: $(date '+%Y-%m-%d %H:%M:%S')"
echo ""

# Build all versions first
echo "Building projects..."
echo -n "  Delegate version (JIT)... "
if (cd Tests/TimeWarp.Nuru.TestApp.Delegates && dotnet build -c Release --nologo --verbosity quiet > /dev/null 2>&1); then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${RED}✗ Build failed${NC}"
    echo "Build error output:"
    (cd Tests/TimeWarp.Nuru.TestApp.Delegates && dotnet build -c Release --nologo)
    exit 1
fi

echo -n "  Mediator version (JIT)... "
if (cd Tests/TimeWarp.Nuru.TestApp.Mediator && dotnet build -c Release --nologo --verbosity quiet > /dev/null 2>&1); then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${RED}✗ Build failed${NC}"
    echo "Build error output:"
    (cd Tests/TimeWarp.Nuru.TestApp.Mediator && dotnet build -c Release --nologo)
    exit 1
fi

echo -n "  Delegate version (AOT)... "
if (cd Tests/TimeWarp.Nuru.TestApp.Delegates && dotnet publish -c Release -r linux-x64 -p:PublishAot=true -o ./aot-output --nologo --verbosity quiet > /dev/null 2>&1); then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${RED}✗ AOT build failed${NC}"
    echo "Build error output:"
    (cd Tests/TimeWarp.Nuru.TestApp.Delegates && dotnet publish -c Release -r linux-x64 -p:PublishAot=true -o ./aot-output --nologo)
    exit 1
fi

echo -n "  Mediator version (AOT)... "
if (cd Tests/TimeWarp.Nuru.TestApp.Mediator && dotnet publish -c Release -r linux-x64 -p:PublishAot=true -o ./aot-output --nologo --verbosity quiet > /dev/null 2>&1); then
    echo -e "${GREEN}✓${NC}"
else
    echo -e "${RED}✗ AOT build failed (likely due to reflection warnings)${NC}"
fi

echo ""
echo "Running tests..."
echo ""

# Test Delegate JIT
echo -e "${BLUE}1. Delegate-based routing (JIT)${NC}"
echo "================================="
EXECUTABLE="./Tests/TimeWarp.Nuru.TestApp.Delegates/bin/Release/net9.0/TimeWarp.Nuru.TestApp.Delegates"
START_TIME=$(date +%s.%N)
run_all_tests
END_TIME=$(date +%s.%N)
DELEGATE_JIT_TIME=$(echo "$END_TIME - $START_TIME" | bc)
DELEGATE_JIT_PASSED=$PASSED
DELEGATE_JIT_FAILED=$FAILED
echo "Passed: $PASSED/44, Failed: $FAILED"
echo "Execution time: ${DELEGATE_JIT_TIME}s"

echo ""
echo -e "${BLUE}2. Mediator-based routing (JIT)${NC}"
echo "================================="
EXECUTABLE="./Tests/TimeWarp.Nuru.TestApp.Mediator/bin/Release/net9.0/TimeWarp.Nuru.TestApp.Mediator"
START_TIME=$(date +%s.%N)
run_all_tests
END_TIME=$(date +%s.%N)
MEDIATOR_JIT_TIME=$(echo "$END_TIME - $START_TIME" | bc)
MEDIATOR_JIT_PASSED=$PASSED
MEDIATOR_JIT_FAILED=$FAILED
echo "Passed: $PASSED/44, Failed: $FAILED"
echo "Execution time: ${MEDIATOR_JIT_TIME}s"

echo ""
echo -e "${BLUE}3. Delegate-based routing (AOT)${NC}"
echo "================================="
EXECUTABLE="./Tests/TimeWarp.Nuru.TestApp.Delegates/aot-output/TimeWarp.Nuru.TestApp.Delegates"
if [ -f "$EXECUTABLE" ]; then
    START_TIME=$(date +%s.%N)
    run_all_tests
    END_TIME=$(date +%s.%N)
    DELEGATE_AOT_TIME=$(echo "$END_TIME - $START_TIME" | bc)
    DELEGATE_AOT_PASSED=$PASSED
    DELEGATE_AOT_FAILED=$FAILED
    echo "Passed: $PASSED/44, Failed: $FAILED"
    echo "Execution time: ${DELEGATE_AOT_TIME}s"
else
    echo -e "${RED}AOT binary not found${NC}"
    DELEGATE_AOT_TIME="N/A"
fi

echo ""
echo -e "${BLUE}4. Mediator-based routing (AOT)${NC}"
echo "================================="
EXECUTABLE="./Tests/TimeWarp.Nuru.TestApp.Mediator/aot-output/TimeWarp.Nuru.TestApp.Mediator"
if [ -f "$EXECUTABLE" ]; then
    START_TIME=$(date +%s.%N)
    run_all_tests
    END_TIME=$(date +%s.%N)
    MEDIATOR_AOT_TIME=$(echo "$END_TIME - $START_TIME" | bc)
    MEDIATOR_AOT_PASSED=$PASSED
    MEDIATOR_AOT_FAILED=$FAILED
    echo "Passed: $PASSED/44, Failed: $FAILED"
    echo "Execution time: ${MEDIATOR_AOT_TIME}s"
else
    echo -e "${RED}AOT binary not found (build may have failed due to reflection)${NC}"
    MEDIATOR_AOT_TIME="N/A"
fi

echo ""
echo "=============================================="
echo "                  SUMMARY"
echo "=============================================="
echo ""
echo "Test Results:"
printf "%-25s | %-10s | %-15s\n" "Implementation" "Tests" "Execution Time"
printf "%-25s | %-10s | %-15s\n" "-------------------------" "----------" "---------------"
printf "%-25s | %-10s | %-15s\n" "Delegate (JIT)" "$DELEGATE_JIT_PASSED/$((DELEGATE_JIT_PASSED + DELEGATE_JIT_FAILED))" "${DELEGATE_JIT_TIME}s"
printf "%-25s | %-10s | %-15s\n" "Mediator (JIT)" "$MEDIATOR_JIT_PASSED/$((MEDIATOR_JIT_PASSED + MEDIATOR_JIT_FAILED))" "${MEDIATOR_JIT_TIME}s"
printf "%-25s | %-10s | %-15s\n" "Delegate (AOT)" "$DELEGATE_AOT_PASSED/$((DELEGATE_AOT_PASSED + DELEGATE_AOT_FAILED))" "${DELEGATE_AOT_TIME}s"
printf "%-25s | %-10s | %-15s\n" "Mediator (AOT)" "$MEDIATOR_AOT_PASSED/$((MEDIATOR_AOT_PASSED + MEDIATOR_AOT_FAILED))" "${MEDIATOR_AOT_TIME}s"

echo ""
echo "Performance Comparison:"
if [ "$DELEGATE_JIT_TIME" != "N/A" ] && [ "$MEDIATOR_JIT_TIME" != "N/A" ]; then
    JIT_DIFF=$(echo "scale=2; (($MEDIATOR_JIT_TIME - $DELEGATE_JIT_TIME) / $DELEGATE_JIT_TIME) * 100" | bc)
    echo "- JIT: Mediator is ${JIT_DIFF}% slower than Delegate"
fi

if [ "$DELEGATE_AOT_TIME" != "N/A" ] && [ "$MEDIATOR_AOT_TIME" != "N/A" ]; then
    AOT_DIFF=$(echo "scale=2; (($MEDIATOR_AOT_TIME - $DELEGATE_AOT_TIME) / $DELEGATE_AOT_TIME) * 100" | bc)
    echo "- AOT: Mediator is ${AOT_DIFF}% slower than Delegate"
fi

if [ "$DELEGATE_JIT_TIME" != "N/A" ] && [ "$DELEGATE_AOT_TIME" != "N/A" ]; then
    DELEGATE_AOT_IMPROVEMENT=$(echo "scale=2; (($DELEGATE_JIT_TIME - $DELEGATE_AOT_TIME) / $DELEGATE_JIT_TIME) * 100" | bc)
    echo "- Delegate: AOT is ${DELEGATE_AOT_IMPROVEMENT}% faster than JIT"
fi

if [ "$MEDIATOR_JIT_TIME" != "N/A" ] && [ "$MEDIATOR_AOT_TIME" != "N/A" ]; then
    MEDIATOR_AOT_IMPROVEMENT=$(echo "scale=2; (($MEDIATOR_JIT_TIME - $MEDIATOR_AOT_TIME) / $MEDIATOR_JIT_TIME) * 100" | bc)
    echo "- Mediator: AOT is ${MEDIATOR_AOT_IMPROVEMENT}% faster than JIT"
fi

# Check binary sizes
echo ""
echo "Binary Sizes:"
if [ -f "./Tests/TimeWarp.Nuru.TestApp.Delegates/aot-output/TimeWarp.Nuru.TestApp.Delegates" ]; then
    DELEGATE_SIZE=$(ls -lh "./Tests/TimeWarp.Nuru.TestApp.Delegates/aot-output/TimeWarp.Nuru.TestApp.Delegates" | awk '{print $5}')
    echo "- Delegate AOT: $DELEGATE_SIZE"
fi
if [ -f "./Tests/TimeWarp.Nuru.TestApp.Mediator/aot-output/TimeWarp.Nuru.TestApp.Mediator" ]; then
    MEDIATOR_SIZE=$(ls -lh "./Tests/TimeWarp.Nuru.TestApp.Mediator/aot-output/TimeWarp.Nuru.TestApp.Mediator" | awk '{print $5}')
    echo "- Mediator AOT: $MEDIATOR_SIZE"
fi

echo ""
echo "Run completed at: $(date '+%Y-%m-%d %H:%M:%S')"