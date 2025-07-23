#!/bin/bash

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

echo "====================================="
echo "TimeWarp.Nuru Integration Tests"
echo "====================================="
echo ""

# Build the project
echo "Building project..."
dotnet build
echo ""

echo "Test 1: Basic Commands"
echo "---------------------"
dotnet run -- status
dotnet run -- version
echo ""

echo "Test 2: Sub-Commands"
echo "--------------------"
dotnet run -- git status
dotnet run -- git commit
dotnet run -- git push
echo ""

echo "Test 3: Option-Based Routing"
echo "-----------------------------"
dotnet run -- git commit --amend
dotnet run -- git commit --amend --no-edit
echo ""

echo "Test 4: Options with Values"
echo "----------------------------"
dotnet run -- git commit --message "Initial commit"
dotnet run -- git log --max-count 5
echo ""

echo "Test 5: Docker Pass-Through"
echo "----------------------------"
dotnet run -- docker run --enhance-logs nginx
dotnet run -- docker run -d -p 8080:80 nginx
dotnet run -- docker run --rm -it ubuntu bash
echo ""

echo "Test 6: Docker Build Pass-Through"
echo "----------------------------------"
dotnet run -- docker build -t myapp .
dotnet run -- docker ps -a
echo ""

echo "Test 7: kubectl Enhancement"
echo "----------------------------"
dotnet run -- kubectl get pods --watch --enhanced
dotnet run -- kubectl get pods --watch
dotnet run -- kubectl get services
dotnet run -- kubectl apply -f deployment.yaml
echo ""

echo "Test 8: npm with Options"
echo "-------------------------"
dotnet run -- npm install express --save-dev
dotnet run -- npm install express --save
dotnet run -- npm install express
dotnet run -- npm run build
echo ""

echo "Test 9: Option Order Independence"
echo "---------------------------------"
echo "These should all match the same handler:"
dotnet run -- git commit -m "Test message" --amend
dotnet run -- git commit --amend -m "Test message"
dotnet run -- git commit --amend --message "Test message"
dotnet run -- git commit --message "Test message" --amend
echo ""

echo "Test 10: Option Aliases"
echo "-----------------------"
echo "These should match different handlers (short vs long form):"
dotnet run -- git commit -m "Short form"
dotnet run -- git commit --message "Long form"
echo ""

echo "Test 11: Ultimate Catch-All"
echo "----------------------------"
dotnet run -- some random command that does not match anything
echo ""

echo "Test 12: Help"
echo "-------------"
dotnet run -- --help
echo ""

echo "====================================="
echo "All integration tests completed!"
echo "====================================="