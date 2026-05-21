"""
Better cvar parser. Walk the file looking for length-prefixed strings of cvar names
followed by a 4-byte int / float, where the byte preceding the name == name length.
"""
import struct
import sys
import re
from pathlib import Path

PATH = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(
    r"C:\Users\wakad\AppData\Roaming\7DaysToDie\Saves\North Rumaseza Territory\20260506\Player\EOS_00024c8eea2f403ca8f19352fb2200de.ttp"
)

data = PATH.read_bytes()


def is_printable_name(b):
    return all(0x20 <= c < 0x7F for c in b)


cvars = {}
i = 0
while i < len(data) - 5:
    L = data[i]
    if 4 <= L <= 60 and i + 1 + L + 4 <= len(data):
        name = data[i + 1:i + 1 + L]
        if is_printable_name(name) and re.fullmatch(rb"[A-Za-z_][A-Za-z0-9_]*", name):
            payload = data[i + 1 + L:i + 1 + L + 4]
            v_int = struct.unpack("<i", payload)[0]
            v_flt = struct.unpack("<f", payload)[0]
            n = name.decode()
            # Pick the most plausible: int if small (<10k absolute) else float
            if -100000 < v_int < 100000 and v_flt != v_int:
                # Could be either; store both
                cvars.setdefault(n, []).append((v_int, v_flt))
            else:
                cvars.setdefault(n, []).append((v_int, v_flt))
            i += 1 + L  # skip past name
            continue
    i += 1

# Print interesting cvars
INTERESTING = [
    "LastPlayerLevel", "PlayerLevelBonus",
    "_xpFromQuest", "_xpFromKill", "_xpFromLoot", "_xpFromCrafting",
    "_xpFromHarvesting", "_xpFromSelling", "_xpFromUpgradeBlock",
    "_xpFromRepairBlock", "_xpOther",
    "cvar_scourge_tokens", "cvar_scourge_trader_points",
    "tier1_clear", "tier1_clear_superinfested", "tier1_fetch",
    "tier2_clear", "tier3_clear",
    "questcomplete", "questtiercomplete", "questtradertotradercomplete",
    "spendskillpoint",
    "attstrength", "attagility", "attfortitude", "attintellect", "attperception",
    "attcrafting", "attbookmastery", "attbooks", "attgeneralperks",
    "perkminer69r", "perkmotherlode", "perkluckylooter", "perkpackmule",
    "perksalvageoperations", "perkdaringadventurer", "perkbetterbarter",
    "perkmasterchef", "perklivingofftheland",
    "perklightarmor", "perkmediumarmor", "perkheavyarmor",
    "perkparkour", "perkruleonecardio", "perksecondwind",
    "perkphysician", "perkpaintolerance", "perkhealingfactor",
    "perksniperdamage", "perkrangerscomplete", "perkpistolpetedamage",
    "perkbatteruparmormastery", "perkbatterupbighits", "perkbatterupcomplete",
    "perksledgesagacomplete",
    "kills", "killzombies", "killwights", "killbears", "killcoyotes",
    "killwolves", "killmutated", "killdemolitions", "killcop", "killspider",
    "killbigmama", "killtourist", "killvultures",
    "upgrades", "repairs", "readmagazines", "sellitems", "purchaseitems",
    "groupcompletechallenge",
    "wakaCarb", "wakaFat", "wakaProtein",
    "diseaseCounterCold",
    "Burns", "MaxRepairs",
    "huntanimals", "killanyzombies", "killanyanimal",
    "harvestseedSkillXP", "harvestbowsSkillXP", "harvestrepairToolsXP",
]

print(f"Cvar dictionary has {sum(len(v) for v in cvars.values())} entries\n")
print(f"{'cvar':40s} {'int':>14s} {'float':>14s}")
print("-" * 70)
for k in INTERESTING:
    if k in cvars:
        for vi, vf in cvars[k]:
            # Pretty print
            if abs(vf) < 1e-30 or abs(vf) > 1e10:
                shown = f"{vi}"
            elif vi < 1000 and vf > 1:
                shown = f"i={vi} | f={vf:.2f}"
            else:
                shown = f"f={vf:.2f}"
            print(f"{k:40s} {shown}")
    else:
        print(f"{k:40s} <missing>")

print("\n--- All perk/skill cvars (non-zero) ---")
for k, vlist in sorted(cvars.items()):
    if not (k.startswith("perk") or k.startswith("skill") or k.startswith("att")):
        continue
    for vi, vf in vlist:
        if vi == 0:
            continue
        print(f"  {k:40s} i={vi:>5d} f={vf:.3g}")
