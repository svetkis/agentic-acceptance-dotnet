#!/usr/bin/env bash
# check-knowledge-map.sh — verifies that every public artifact is present
# in the knowledge map (docs/README.md).
set -u

MAP="docs/README.md"
FAIL=0

if [ ! -f "$MAP" ]; then
    echo "ERROR: knowledge map not found: $MAP"
    exit 2
fi

check_present() {
    local needle="$1"
    local kind="$2"
    if ! grep -qF "$needle" "$MAP"; then
        echo "MISSING from knowledge map ($kind): $needle"
        FAIL=1
    fi
}

# Skills (installable)
for d in templates/skills/*/; do
    name="$(basename "$d")"
    check_present "$name" "skill"
done

# Test patterns
for f in tests/patterns/*.cs tests/conventions/*.cs tests/conventions/*.md; do
    [ -e "$f" ] || continue
    check_present "$(basename "$f")" "test pattern"
done

# Docs: traps, solutions, obstacles, case studies, agents
for f in docs/traps/*.md docs/solutions/*.md docs/obstacles/*.md docs/case-studies/*.md docs/agents/*.md; do
    [ -e "$f" ] || continue
    check_present "$(basename "$f")" "doc"
done

# Rules
for f in rules/*.md; do
    [ -e "$f" ] || continue
    check_present "$(basename "$f")" "rule"
done

# Skill-contract and bootstrap support templates
check_present "SKILL-CONTRACT.md" "skill contract"
for f in templates/skills/acceptance-bootstrap/*.md templates/skills/*.md; do
    [ -e "$f" ] || continue
    check_present "$(basename "$f")" "bootstrap/support template"
done

# Repo-maintenance CI scripts
for f in ci/scripts/*.sh; do
    [ -e "$f" ] || continue
    check_present "$(basename "$f")" "ci script"
done

if [ "$FAIL" -eq 0 ]; then
    echo "OK: knowledge map covers all artifacts."
else
    echo "Knowledge-map check FAILED."
fi
exit "$FAIL"
