#!/bin/bash
# Run simulation engine tests

set -e

cd "$(dirname "$0")"

echo "=== Building tests ==="
dotnet build

echo ""
echo "=== Running tests ==="
dotnet test --verbosity normal

echo ""
echo "=== Tests complete ==="
echo "To run with coverage:"
echo "  dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./coverage/"
