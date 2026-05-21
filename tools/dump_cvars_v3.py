"""Parse cvars allowing optional $ type prefix."""
import struct
import re
from pathlib import Path

PATH = Path(r"C:\Users\wakad\AppData\Roaming\7DaysToDie\Saves\North Rumaseza Territory\20260506\Player\EOS_00024c8eea2f403ca8f19352fb2200de.ttp")
data = PATH.read_bytes()

cvars = {}
i = 0
NAME_RE = re.compile(rb"\$?[A-Za-z_][A-Za-z0-9_]*")

while i < len(data) - 5:
    L = data[i]
    if 4 <= L <= 60 and i + 1 + L + 4 <= len(data):
        name = data[i + 1:i + 1 + L]
        if NAME_RE.fullmatch(name):
            payload = data[i + 1 + L:i + 1 + L + 4]
            v_int = struct.unpack("<i", payload)[0]
            v_flt = struct.unpack("<f", payload)[0]
            n = name.decode().lstrip("$")
            cvars.setdefault(n, []).append((v_int, v_flt))
            i += 1 + L
            continue
    i += 1

def show(k, fmt="auto"):
    if k not in cvars:
        return f"{k}: <missing>"
    vi, vf = cvars[k][0]
    if fmt == "int":
        return f"{k}: {vi}"
    if fmt == "float":
        return f"{k}: {vf:.2f}"
    # heuristic
    if -1000 < vi < 100000 and abs(vf) < 1e-30:
        return f"{k}: {vi}"
    if vf > 1 and vf < 1e8:
        return f"{k}: {vf:.1f}"
    return f"{k}: int={vi} float={vf:.3g}"

print("=== PROGRESSION ===")
for k in ["LastPlayerLevel", "PlayerLevelBonus"]:
    print("  " + show(k, "float"))

print("\n=== XP SOURCES ===")
total = 0
for k in ["_xpFromQuest", "_xpFromKill", "_xpFromLoot", "_xpFromCrafting",
          "_xpFromHarvesting", "_xpFromSelling", "_xpFromUpgradeBlock",
          "_xpFromRepairBlock", "_xpOther"]:
    if k in cvars:
        v = cvars[k][0][1]
        total += v
        print(f"  {k:30s} {v:>10.0f} ({v / 1:>7.0f})")
print(f"  {'TOTAL':30s} {total:>10.0f}")

print("\n=== ATTRIBUTES (points spent) ===")
for k in ["attstrength", "attagility", "attfortitude", "attintellect", "attperception",
          "attcrafting", "attbookmastery", "attbooks", "attgeneralperks"]:
    print("  " + show(k, "int"))

print("\n=== POI SCOURGE ===")
for k in ["cvar_scourge_tokens", "cvar_scourge_trader_points"]:
    print("  " + show(k, "float"))

print("\n=== TIER COUNTERS (count of entries) ===")
for k in ["tier1_clear", "tier1_clear_superinfested", "tier1_fetch",
          "tier2_clear", "tier3_clear"]:
    cnt = len(cvars.get(k, []))
    print(f"  {k:35s} entries={cnt}")

print("\n=== QUEST COUNTERS ===")
for k in ["questcomplete", "questtiercomplete", "questtradertotradercomplete"]:
    print("  " + show(k))

print("\n=== KILL COUNTERS (raw int=count) ===")
for k in ["killzombies", "killwights", "killbears", "killcoyotes", "killwolves",
          "killmutated", "killdemolitions", "killcop", "killspider", "killbigmama",
          "killtourist", "killvultures", "killchickens", "killboars", "killdeer",
          "killrabbits", "killsnakes", "killmountainlion", "killlumberjack",
          "killanyzombies", "killanyanimal", "huntanimals"]:
    if k in cvars:
        # Often format: int < 0x10000 with low byte = count
        vi = cvars[k][0][0]
        # Try interpreting as 2 little-endian shorts: count + flag
        lo = vi & 0xFFFF
        hi = (vi >> 16) & 0xFFFF
        print(f"  {k:25s} raw=0x{vi:08x} lo={lo} hi={hi}")

print("\n=== BODY LAYER (Waka mod) ===")
for k in ["wakaCarb", "wakaFat", "wakaProtein", "wakaInitialized",
          "wakaCarbFill", "wakaFatFill", "wakaProteinFill",
          "vitaminAnimalBasedAmount", "vitaminFruitsAmount"]:
    if k in cvars:
        print("  " + show(k, "float"))
