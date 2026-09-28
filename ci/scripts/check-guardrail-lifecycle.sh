#!/usr/bin/env bash
# check-guardrail-lifecycle.sh — stale suppressions and expired decision guards.
#
# 1. NuGetAuditSuppress entries must have an adjacent XML comment with
#    Owner: and Review: YYYY-MM-DD (risk acceptance with expiry).
# 2. <NoWarn> entries in csproj/props follow the same rule — a disabled
#    warning is accepted risk, whatever the mechanism.
# 3. `#pragma warning disable` in sources follows the same rule (comment
#    with Owner:/Review: on the pragma line or up to 5 lines above).
# 4. Decision Guard records (**Review date:** YYYY-MM-DD) past due are reported
#    as warnings (they do not invalidate the decision — see DECISION-GUARDS.md).
set -u

FAIL=0
TODAY="$(date +%Y-%m-%d)"

# Validates one suppression hit ("file:lineno:rest") for Owner/Review context.
check_suppression() {
    local kind="$1" line="$2"
    local file lineno context review
    file="${line%%:*}"
    lineno="${line#*:}"; lineno="${lineno%%:*}"
    # Look at up to 5 lines above the suppression (incl. the line itself,
    # so an inline comment on the pragma counts) for Owner/Review.
    context="$(sed -n "$((lineno > 5 ? lineno - 5 : 1)),${lineno}p" "$file")"
    if ! echo "$context" | grep -qi "owner:"; then
        echo "STALE SUPPRESSION ($kind, no owner): $file:$lineno"
        FAIL=1
    fi
    if ! echo "$context" | grep -qiE "review:[[:space:]]*[0-9]{4}-[0-9]{2}-[0-9]{2}"; then
        echo "STALE SUPPRESSION ($kind, no review date): $file:$lineno"
        FAIL=1
    else
        review="$(echo "$context" | grep -oiE "review:[[:space:]]*[0-9]{4}-[0-9]{2}-[0-9]{2}" | tail -1 | grep -oE "[0-9]{4}-[0-9]{2}-[0-9]{2}")"
        if [[ "$review" < "$TODAY" ]]; then
            echo "EXPIRED SUPPRESSION ($kind, review $review < $TODAY): $file:$lineno"
            FAIL=1
        fi
    fi
}

# --- 1. NuGetAuditSuppress ---
while IFS= read -r line; do
    check_suppression "NuGetAuditSuppress" "$line"
done < <(grep -rn "NuGetAuditSuppress" --include="*.csproj" --include="*.props" --exclude-dir=bin --exclude-dir=obj . || true)

# --- 2. <NoWarn> ---
while IFS= read -r line; do
    check_suppression "NoWarn" "$line"
done < <(grep -rn "<NoWarn>" --include="*.csproj" --include="*.props" --exclude-dir=bin --exclude-dir=obj . || true)

# --- 3. #pragma warning disable ---
while IFS= read -r line; do
    check_suppression "pragma" "$line"
done < <(grep -rn "#pragma warning disable" --include="*.cs" --exclude-dir=bin --exclude-dir=obj . || true)

# --- 4. Decision Guard review dates (warnings only) ---
while IFS= read -r line; do
    file="${line%%:*}"
    date_str="$(echo "$line" | grep -oE "[0-9]{4}-[0-9]{2}-[0-9]{2}" | head -1)"
    if [ -n "$date_str" ] && [[ "$date_str" < "$TODAY" ]]; then
        echo "WARNING: expired Decision Guard review date ($date_str): $file"
    fi
done < <(grep -rn "Review date:" --include="DECISION-GUARDS.md" . || true)

if [ "$FAIL" -eq 0 ]; then
    echo "OK: no stale suppressions."
else
    echo "Guardrail lifecycle check FAILED."
fi
exit "$FAIL"
