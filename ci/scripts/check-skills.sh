#!/usr/bin/env bash
# check-skills.sh — schema-lint for installable skills in templates/skills/.
# Contract: templates/skills/SKILL-CONTRACT.md
set -u

SKILLS_DIR="${1:-templates/skills}"
FAIL=0

# Canonical section headings (case-insensitive match on heading lines).
# Full canonical names are required — see templates/skills/SKILL-CONTRACT.md.
REQUIRED_SECTIONS=(
  "purpose and non-goals"
  "applicability and exclusions"
  "required inputs"
  "procedure"
  "evidence requirements"
  "finding schema"
  "severity and confidence"
  "outputs and downstream consumer"
  "trigger or schedule"
  "limitations and expected false positives"
)

shopt -s nullglob
for skill_dir in "$SKILLS_DIR"/*/; do
  name="$(basename "$skill_dir")"

  # acceptance-bootstrap is a support-template bundle, not an installable skill.
  if [ "$name" = "acceptance-bootstrap" ]; then
    continue
  fi

  skill="$skill_dir/SKILL.md"
  checklist="$skill_dir/CHECKLIST.md"

  if [ ! -f "$skill" ]; then
    echo "FAIL $name: SKILL.md missing"
    FAIL=1
    continue
  fi

  if [ ! -f "$checklist" ]; then
    echo "FAIL $name: CHECKLIST.md missing"
    FAIL=1
  fi

  # 1. YAML frontmatter with name and description (must be properly closed).
  if ! head -1 "$skill" | grep -q '^---$'; then
    echo "FAIL $name: YAML frontmatter missing"
    FAIL=1
  elif ! awk 'NR>1 && /^---$/{found=1; exit} END{exit !found}' "$skill"; then
    echo "FAIL $name: YAML frontmatter not closed"
    FAIL=1
  else
    fm="$(awk 'NR==1 && /^---$/{f=1;next} f && /^---$/{exit} f' "$skill")"
    echo "$fm" | grep -q '^name:' || { echo "FAIL $name: frontmatter 'name' missing"; FAIL=1; }
    echo "$fm" | grep -q '^description:' || { echo "FAIL $name: frontmatter 'description' missing"; FAIL=1; }
  fi

  # 2. Forbidden legacy heading.
  if grep -qi 'anti-hallucination' "$skill"; then
    echo "FAIL $name: contains 'Anti-Hallucination' — rename to 'Evidence Requirements'"
    FAIL=1
  fi

  # 3. Required sections (headings only). The phrase must appear as whole words in
  #    a heading — a substring inside a longer word (e.g. "Procedures") does not count.
  headings="$(grep -i '^#\{1,3\} ' "$skill" || true)"
  for section in "${REQUIRED_SECTIONS[@]}"; do
    if ! echo "$headings" | grep -qiE "(^|[^a-z])${section}([^a-z]|$)"; then
      echo "FAIL $name: required section missing ('$section')"
      FAIL=1
    fi
  done

  # 4. Canonical confidence labels (forbid legacy CERTAIN/REVIEW markers).
  if grep -qE '\bCERTAIN\b|\[REVIEW\]' "$skill"; then
    echo "FAIL $name: legacy confidence labels (CERTAIN/REVIEW) — use CONFIRMED/NEEDS_REVIEW"
    FAIL=1
  fi

  # 5. Frontmatter name must match the skill directory name.
  fm_name="$(echo "$fm" | sed -n 's/^name:[[:space:]]*//p' | tr -d '"' | tr -d "'" | tr -d '[:space:]')"
  if [ -n "$fm_name" ] && [ "$fm_name" != "$name" ]; then
    echo "FAIL $name: frontmatter name '$fm_name' does not match directory name"
    FAIL=1
  fi

  # 6. Finding schema must carry the canonical severity/confidence labels.
  if ! grep -q 'CONFIRMED' "$skill" || ! grep -q 'NEEDS_REVIEW' "$skill"; then
    echo "FAIL $name: canonical confidence labels (CONFIRMED/NEEDS_REVIEW) missing from SKILL.md"
    FAIL=1
  fi

  # 7. Checklist markers: only [ ], [-], [x] (contract: Checklist Item Marking).
  if [ -f "$checklist" ]; then
    bad_markers="$(grep -nE '^\s*[-*]?\s*\[[^ x-]\]' "$checklist" || true)"
    if [ -n "$bad_markers" ]; then
      echo "FAIL $name: CHECKLIST.md uses non-canonical checkbox markers (allowed: [ ], [-], [x]):"
      echo "$bad_markers"
      FAIL=1
    fi
  fi
done

if [ "$FAIL" -eq 0 ]; then
  echo "OK: all skills pass the schema-lint."
else
  echo "Schema-lint FAILED."
fi
exit "$FAIL"
