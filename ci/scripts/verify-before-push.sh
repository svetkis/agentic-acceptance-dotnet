#!/usr/bin/env bash
# verify-before-push.sh — run the same verification the CI will run, locally.
# Born from a real incident: a Debug-only local check missed a CS0162 that
# Release + TreatWarningsAsErrors escalated to a build error on CI.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

echo "== 1/4 Repo quality scripts =="
bash ci/scripts/check-skills.sh
bash ci/scripts/check-knowledge-map.sh
bash ci/scripts/check-links.sh
bash ci/scripts/check-guardrail-lifecycle.sh

echo "== 2/4 Release build with warnings as errors (CI parity) =="
dotnet build examples/DemoProject/DemoProject.sln \
    --configuration Release -p:TreatWarningsAsErrors=true --nologo -v q

echo "== 3/4 Demo suites (green + expected-red) =="
bash ci/scripts/run-and-verify-tests.sh \
    examples/DemoProject/tests/DemoProject.Tests/DemoProject.Tests.csproj
bash ci/scripts/run-and-verify-tests.sh \
    examples/DemoProject.MinimalApi/tests/DemoProject.MinimalApi.Tests/DemoProject.MinimalApi.Tests.csproj
bash ci/scripts/run-and-verify-tests.sh \
    examples/DemoProject/tests/DemoProject.Traps.Tests/DemoProject.Traps.Tests.csproj --expect-failure

echo "== 4/4 Done — safe to push =="
