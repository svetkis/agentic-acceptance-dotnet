#!/usr/bin/env bash
# run-and-verify-tests.sh — single source of test-running logic.
#
# Runs test projects via `dotnet run --project`, then verifies:
#   1. tests actually ran (no "0 tests ran" / "no tests found" / "discovered: 0")
#   2. the runner produced a result line (passed/failed/skipped/total)
#   3. exit code and failure count match the expected mode
#
# Usage:
#   ./run-and-verify-tests.sh <test.csproj>                  # expect all tests to pass
#   ./run-and-verify-tests.sh <test.csproj> --expect-failure # traps: tests MUST fail
#   ./run-and-verify-tests.sh                                # discover and verify all
#                                                             # test projects in tests/ and src/
#
# Project adaptation: if test projects live elsewhere, pass explicit
# .csproj paths or adjust TEST_DIRS in the discovery branch below.
set -u

fail() {
    echo "ERROR: $1"
    exit 1
}

# Runs one test project once and verifies the result.
run_one() {
    local proj="$1"
    local mode="${2:-}"

    if [ ! -f "$proj" ]; then
        fail "project file not found: $proj"
    fi

    echo "========================================"
    echo "Running tests: $proj ${mode}"
    echo "========================================"

    local test_output exit_code
    test_output=$(dotnet run --project "$proj" --configuration Release 2>&1)
    exit_code=$?
    echo "$test_output"

    # GUARDRAIL: "0 tests ran" with exit code 0 must not look green.
    if echo "$test_output" | grep -qi "0 tests ran\|no tests found\|discovered: 0"; then
        fail "Tests did not run in $proj (0 tests)."
    fi

    if ! echo "$test_output" | grep -qi "passed\|failed\|skipped\|total:"; then
        fail "Cannot determine test results for $proj. Test runner may be misconfigured."
    fi

    if [ "$mode" = "--expect-failure" ]; then
        # GUARDRAIL: traps project — tests MUST fail; a green run means guardrails broke.
        if [ "$exit_code" -eq 0 ]; then
            fail "Traps tests PASSED. Guardrails are broken — traps are no longer caught."
        fi
        if ! echo "$test_output" | grep -q "failed:"; then
            fail "Expected test failures, but got an unexpected error (crashed or did not run)."
        fi
        echo "OK: Traps correctly caught by guardrails (exit code $exit_code)."
        return 0
    fi

    if [ "$exit_code" -ne 0 ]; then
        fail "Test run failed in $proj (exit code $exit_code)."
    fi
    if echo "$test_output" | grep -q "failed: [1-9]"; then
        fail "Tests failed in $proj."
    fi

    echo "OK: Tests were executed and passed in $proj."
}

if [ $# -ge 1 ]; then
    run_one "$1" "${2:-}"
    exit $?
fi

# Discovery mode: find all test projects under TEST_DIRS and verify each.
TEST_DIRS=("tests" "src")
FOUND=0

for dir in "${TEST_DIRS[@]}"; do
    [ -d "$dir" ] || continue
    # Look for projects using TUnit, xUnit, NUnit, or MSTest
    while IFS= read -r -d '' proj; do
        FOUND=1
        run_one "$proj"
    done < <(find "$dir" -name "*.csproj" -print0 | while IFS= read -r -d '' proj; do
        if grep -qiE "TUnit|xUnit|NUnit|MSTest|Microsoft\.NET\.Test\.Sdk" "$proj"; then
            printf '%s\0' "$proj"
        fi
    done)
done

if [ "$FOUND" -eq 0 ]; then
    fail "No test projects found in ${TEST_DIRS[*]}. Adapt TEST_DIRS in ci/scripts/run-and-verify-tests.sh to match your structure."
fi
