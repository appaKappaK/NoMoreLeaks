#!/usr/bin/env bash
# exp-memleaks.sh
# Extract and summarise memory leak data from a KSP log file.
#
# Usage:
#   ./exp-memleaks.sh [log_file]
#
# All output filenames are timestamped. If a summaries directory exists beside
# this script, outputs are written there; otherwise they use the current
# directory.
# Override the timestamp via: EXPORT_LEAKS_TIMESTAMP=my-label ./exp-memleaks.sh

set -euo pipefail

# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

readonly TIMESTAMP="${EXPORT_LEAKS_TIMESTAMP:-$(date +%Y-%m-%d_%H-%M-%S)}"
readonly LOG_FILE="${1:-KSP.log}"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
readonly SCRIPT_DIR

if [[ -d "$SCRIPT_DIR/summaries" ]]; then
    readonly OUTPUT_DIR="$SCRIPT_DIR/summaries"
else
    readonly OUTPUT_DIR="$PWD"
fi

readonly OUT_RAW="$OUTPUT_DIR/KSPCF-memory-leaks-raw-${TIMESTAMP}.txt"
readonly OUT_SUMMARY="$OUTPUT_DIR/KSPCF-memory-leaks-summary-${TIMESTAMP}.txt"
readonly OUT_UNHANDLED_RAW="$OUTPUT_DIR/KSPCF-memory-leaks-unhandled-raw-${TIMESTAMP}.txt"
readonly OUT_UNHANDLED_SUMMARY="$OUTPUT_DIR/KSPCF-memory-leaks-unhandled-summary-${TIMESTAMP}.txt"
readonly OUT_SCENES="$OUTPUT_DIR/KSPCF-memory-leaks-scenes-${TIMESTAMP}.txt"
readonly OUT_SCENES_SUMMARY="$OUTPUT_DIR/KSPCF-memory-leaks-scenes-summary-${TIMESTAMP}.txt"
readonly OUT_WARNINGS="$OUTPUT_DIR/KSPCF-memory-leaks-warnings-${TIMESTAMP}.txt"
readonly OUT_WARNINGS_SUMMARY="$OUTPUT_DIR/KSPCF-memory-leaks-warnings-summary-${TIMESTAMP}.txt"
readonly OUT_NML_RAW="$OUTPUT_DIR/NoMoreLeaks-debug-raw-${TIMESTAMP}.txt"
readonly OUT_NML_SUMMARY="$OUTPUT_DIR/NoMoreLeaks-debug-summary-${TIMESTAMP}.txt"
readonly OUT_NML_MARKERS="$OUTPUT_DIR/NoMoreLeaks-debug-markers-${TIMESTAMP}.txt"

readonly PAT_LEAKS='\[KSPCF:MemoryLeaks\] Removed a .* callback owned by a destroyed'
readonly PAT_UNHANDLED='\[KSPCF:MemoryLeaks\] A destroyed .* instance is owning a .* callback\. No action has been taken'
readonly PAT_SCENES='\[KSPCF:MemoryLeaks\] Leaving scene ".*", cleaned [0-9]+ memory leaks'
readonly PAT_NML_DEBUG='\[NoMoreLeaks:Debug\]'
readonly PAT_WARNINGS='(\[(WRN|ERR|EXC) [^]]+\].*(NoMoreLeaks|Harmony|KSPCF:MemoryLeaks|TransmitterModule|ModuleScienceLab|ModuleScienceExperiment|DeployedScienceExperiment|GroundScience|CommNet|RealAntennas|Cannot Transmit|No usable|Failed to find Vessel for Experiment|Collection was modified)|\[NoMoreLeaks\].*(Missing|Could not|Exception|[Ff]ail|[Ww]arning))'

readonly SUMMARY_MAX_LINES=40
readonly WARNINGS_PREVIEW_LINES=80

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

die()  { echo "error: $*" >&2; exit 1; }
info() { echo "  $*"; }

count_lines() { wc -l < "$1"; }

count_matches() {
    local pattern="$1" file="$2" count
    count=$(grep -Ec "$pattern" "$file" || true)
    echo "${count:-0}"
}

# Search wrapper — prefers rg, falls back to grep -E
search() {
    local pattern="$1" file="$2"
    if command -v rg &>/dev/null; then
        rg -e "$pattern" -- "$file"
    else
        grep -E "$pattern" -- "$file"
    fi
}

search_n() {   # with line numbers
    local pattern="$1" file="$2"
    if command -v rg &>/dev/null; then
        rg -n -e "$pattern" -- "$file"
    else
        grep -nE "$pattern" -- "$file"
    fi
}

# ---------------------------------------------------------------------------
# Pre-flight checks
# ---------------------------------------------------------------------------

[[ -f "$LOG_FILE" ]] || die "log file not found: $LOG_FILE"

# KSP (and Proton) writes CRLF; strip it so regex anchors work correctly
CLEAN_LOG="$(mktemp --suffix=.ksplog)"
trap 'rm -f "$CLEAN_LOG"' EXIT
tr -d '\r' < "$LOG_FILE" > "$CLEAN_LOG"

# ---------------------------------------------------------------------------
# Extract
# ---------------------------------------------------------------------------

echo "Scanning $(realpath "$LOG_FILE") ..."
echo "Writing outputs to $OUTPUT_DIR"
echo

search   "$PAT_LEAKS"     "$CLEAN_LOG" > "$OUT_RAW"           || true
search   "$PAT_UNHANDLED" "$CLEAN_LOG" > "$OUT_UNHANDLED_RAW" || true
search   "$PAT_SCENES"    "$CLEAN_LOG" > "$OUT_SCENES"        || true
search   "$PAT_NML_DEBUG" "$CLEAN_LOG" > "$OUT_NML_RAW"       || true
search_n "$PAT_WARNINGS"  "$CLEAN_LOG" > "$OUT_WARNINGS"      || true

# ---------------------------------------------------------------------------
# Summarise: KSPCF leak callbacks (top N by instance type + callback type)
# ---------------------------------------------------------------------------

sed -E 's/^.*Removed a ([^ ]+) callback owned by a destroyed ([^ ]+) instance.*$/\2 | \1/' \
        "$OUT_RAW" \
    | sort \
    | uniq -c \
    | sort -rn \
    | head -"$SUMMARY_MAX_LINES" \
    > "$OUT_SUMMARY"

# KSPCF reports third-party callback owners but intentionally leaves them in
# place. Keep these separate from callbacks KSPCF actually removed.
sed -E 's/^.*A destroyed ([^ ]+) instance is owning a ([^ ]+) callback\..*$/\1 | \2/' \
        "$OUT_UNHANDLED_RAW" \
    | sort \
    | uniq -c \
    | sort -rn \
    | head -"$SUMMARY_MAX_LINES" \
    > "$OUT_UNHANDLED_SUMMARY"

# Scene cleanup totals are useful context because KSPCF's "cleaned N memory
# leaks" includes unhandled entries that are not in the removed-callback file.
awk '
    match($0, /Leaving scene "([^"]+)", cleaned ([0-9]+) memory leaks.*GameEvents callbacks : ([0-9]+).*Allocated memory : ([0-9.]+) GiB \(managed heap\), ([0-9.]+) GiB \(unmanaged\)/, p) {
        scene = p[1]
        cleaned = p[2] + 0
        total += cleaned
        changes++
        scene_count[scene]++
        scene_sum[scene] += cleaned
        if (cleaned > scene_max[scene])
            scene_max[scene] = cleaned
        last_callbacks = p[3]
        last_managed = p[4]
        last_unmanaged = p[5]
    }
    END {
        printf "%7d total cleaned across %d scene change(s)\n", total, changes
        if (changes > 0)
            printf "%7s final GameEvents callbacks, %s GiB managed, %s GiB unmanaged\n", last_callbacks, last_managed, last_unmanaged
        for (scene in scene_count)
            printf "%7d %3d change(s), max=%d | %s\n", scene_sum[scene], scene_count[scene], scene_max[scene], scene
    }
' "$OUT_SCENES" | sort -rn > "$OUT_SCENES_SUMMARY"

# Normalize line numbers, timestamps, and large IDs so repeated warnings group
# together instead of producing one unique line per science experiment instance.
sed -E 's/^[0-9]+:\[(WRN|ERR|EXC) [^]]+\] /\1 | /; s/[0-9]{4,}/<id>/g' \
        "$OUT_WARNINGS" \
    | sort \
    | uniq -c \
    | sort -rn \
    | head -"$SUMMARY_MAX_LINES" \
    > "$OUT_WARNINGS_SUMMARY"

# ---------------------------------------------------------------------------
# Summarise: NoMoreLeaks debug lines (aggregate callback counts per owner)
# ---------------------------------------------------------------------------

awk '
    match($0, /Removed ([0-9]+) callback\(s\) from (.*) owned by ([^ ]+) via (.*)$/, p) {
        sums[p[3] " | " p[2] " | " p[4]] += p[1]
    }
    END {
        for (k in sums)
            printf "%7d %s\n", sums[k], k
    }
' "$OUT_NML_RAW" | sort -rn > "$OUT_NML_SUMMARY"

# Record patch-path evidence so runs can be classified without relying only on
# export timestamps.
{
    printf "%7d %s\n" "$(count_matches '\[NoMoreLeaks\] Harmony patches applied' "$CLEAN_LOG")" "Harmony patches applied"
    printf "%7d %s\n" "$(count_matches '\[NoMoreLeaks:Debug\] Proactive sweep via EditorLogic\.exitEditor\.Prefix' "$CLEAN_LOG")" "Editor exit proactive sweep (v1.6+ marker)"
    printf "%7d %s\n" "$(count_matches '\[NoMoreLeaks:Debug\] Editor destroy-selected requested' "$CLEAN_LOG")" "Editor destroy-selected cleanup"
    printf "%7d %s\n" "$(count_matches '\[NoMoreLeaks:Debug\] Editor delete requested' "$CLEAN_LOG")" "Editor delete cleanup"
    printf "%7d %s\n" "$(count_matches '\[NoMoreLeaks:Debug\] Inventory delete-part-object cleanup' "$CLEAN_LOG")" "Inventory DeletePartObject cleanup"
    printf "%7d %s\n" "$(count_matches '\[NoMoreLeaks:Debug\] Lifecycle cleanup on .*children=[1-9]' "$CLEAN_LOG")" "Lifecycle cleanup with child parts"
} > "$OUT_NML_MARKERS"

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------

n_leaks=$(count_lines "$OUT_RAW")
n_unhandled=$(count_lines "$OUT_UNHANDLED_RAW")
n_scenes=$(count_lines "$OUT_SCENES")
n_debug=$(count_lines "$OUT_NML_RAW")
n_warn=$(count_lines  "$OUT_WARNINGS")

echo "Results"
echo "-------"
info "$n_leaks  KSPCF leak callback lines  →  $OUT_RAW"
info "$n_unhandled  KSPCF unhandled leak lines →  $OUT_UNHANDLED_RAW"
info "$n_scenes  KSPCF scene cleanup lines   →  $OUT_SCENES"
info "$n_debug  NoMoreLeaks debug lines    →  $OUT_NML_RAW"
info "$n_warn   warnings / errors          →  $OUT_WARNINGS"
echo
echo "Summaries"
echo "---------"
info "$OUT_SUMMARY"
info "$OUT_UNHANDLED_SUMMARY"
info "$OUT_SCENES_SUMMARY"
info "$OUT_WARNINGS_SUMMARY"
info "$OUT_NML_SUMMARY"
info "$OUT_NML_MARKERS"

# Warn if summaries are empty despite having raw data
if [[ "$n_leaks" -gt 0 && ! -s "$OUT_SUMMARY" ]]; then
    echo
    echo "warning: raw leak lines found but summary is empty — the log line" >&2
    echo "         format may not match the expected pattern." >&2
fi

if [[ "$n_debug" -gt 0 && ! -s "$OUT_NML_SUMMARY" ]]; then
    echo
    echo "warning: NoMoreLeaks debug lines found but summary is empty — the" >&2
    echo "         log line format may not match the expected pattern." >&2
fi

# Print warnings preview
if [[ "$n_warn" -gt 0 ]]; then
    echo
    echo "Warnings / errors (first ${WARNINGS_PREVIEW_LINES}):"
    echo "---------------------------------------------------"
    head -"$WARNINGS_PREVIEW_LINES" "$OUT_WARNINGS"
    if [[ "$n_warn" -gt "$WARNINGS_PREVIEW_LINES" ]]; then
        echo "... $((n_warn - WARNINGS_PREVIEW_LINES)) more lines — see $OUT_WARNINGS"
    fi
fi

# Nudge if nothing matched at all
if [[ "$n_leaks" -eq 0 && "$n_unhandled" -eq 0 && "$n_scenes" -eq 0 && "$n_debug" -eq 0 && "$n_warn" -eq 0 ]]; then
    echo
    echo "Nothing matched. Possible reasons:"
    echo "  • KSPCF / NoMoreLeaks mods are not installed or not active"
    echo "  • The symlink points to the wrong or an outdated KSP.log"
    echo "  • The log is from a session that had no leaks"
fi
