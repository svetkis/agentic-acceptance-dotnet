#!/bin/bash
# GUARDRAIL: Runs all test projects via `dotnet run --project`.
# Automatically discovers test projects to avoid hardcoding paths.
#
# Project adaptation:
# - If tests live in a `tests/` folder — the script will find them itself.
# - If tests are scattered across `src/` — change `TEST_DIRS` below.

set -e

TEST_DIRS=("tests" "src")
FOUND=0

for dir in "${TEST_DIRS[@]}"; do
    if [ ! -d "$dir" ]; then
        continue
    fi

    # Look for projects using TUnit, xUnit, NUnit, or MSTest
    while IFS= read -r -d '' proj; do
        FOUND=1
        echo "========================================"
        echo "Running tests: $proj"
        echo "========================================"
        dotnet run --project "$proj" --configuration Release
    done < <(find "$dir" -name "*.csproj" -print0 | while IFS= read -r -d '' proj; do
        if grep -qiE "TUnit|xUnit|NUnit|MSTest|Microsoft\.NET\.Test\.Sdk" "$proj"; then
            printf '%s\0' "$proj"
        fi
    done)
done

if [ "$FOUND" -eq 0 ]; then
    echo "ERROR: No test projects found in ${TEST_DIRS[*]}."
    echo "Adapt TEST_DIRS in ci/scripts/run-tests.sh to match your structure."
    exit 1
fi
