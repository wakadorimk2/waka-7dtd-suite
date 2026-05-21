"""
Extract cvar=value pairs from a 7DTD player .ttp save file.

Format heuristic: cvars are stored as length-prefixed UTF-8 strings followed
by a float32 (little-endian). String length is a 7-bit-encoded varint (BinaryWriter).
We sweep the file for known cvar prefixes, and for each match we read backwards
to identify the length byte, then read the next 4 bytes after the name as a float.
"""
import struct
import sys
from pathlib import Path

PATH = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(
    r"C:\Users\wakad\AppData\Roaming\7DaysToDie\Saves\North Rumaseza Territory\20260506\Player\EOS_00024c8eea2f403ca8f19352fb2200de.ttp"
)

data = PATH.read_bytes()
print(f"File size: {len(data)} bytes")

# Heuristic: known cvar prefixes that bookmark the cvar dict.
PREFIXES = [
    b"_xpFromQuest", b"_xpFromKill", b"_xpFromLoot", b"_xpFromCrafting",
    b"_xpFromHarvesting", b"_xpFromSelling", b"_xpFromUpgradeBlock",
    b"_xpFromRepairBlock", b"_xpOther",
    b"LastPlayerLevel", b"PlayerLevelBonus",
    b"cvar_scourge_tokens", b"cvar_scourge_trader_points",
    b"tier1_clear", b"tier1_clear_superinfested", b"tier1_fetch",
    b"tier2_clear", b"tier3_clear", b"tier4_clear", b"tier5_clear",
    b"questcomplete", b"questtiercomplete", b"questtradertotradercomplete",
    b"spendskillpoint",
    b"skillagilitycombat", b"skillagilitystealth", b"skillagilityathletics",
    b"skillstrengthcombat", b"skillstrengthconstruction", b"skillstrengthgeneral",
    b"skillfortitudecombat", b"skillfortituderecovery", b"skillfortitudesurvival",
    b"skillintellectcombat", b"skillintellectcraftsmanship", b"skillintellectinfluence",
    b"skillperceptioncombat", b"skillperceptiongeneral", b"skillperceptionscavenging",
    b"attstrength", b"attagility", b"attfortitude", b"attintellect", b"attperception",
    b"attcrafting", b"attbookmastery", b"attbooks", b"attgeneralperks",
    b"perkminer69r", b"perkmotherlode", b"perkluckylooter", b"perkpackmule",
    b"perksalvageoperations", b"perkdaringadventurer",
    b"kills", b"killzombies", b"killwights", b"killbears", b"killcoyotes",
    b"killwolves", b"killmutated", b"killdemolitions", b"killcop", b"killspider",
    b"upgrades", b"repairs", b"readmagazines", b"sellitems", b"purchaseitems",
    b"wakaCarb", b"wakaFat", b"wakaProtein", b"wakaInitialized",
    b"buffrest", b"buffsmell", b"buffsocial",
    b"vitaminAnimalBasedAmount", b"vitaminFruitsAmount", b"vitaminVegetablesOverCap",
    b"BatteryDegradation", b"ClothingDegradationRate",
    b"diseaseCounterCold",
]


def try_read_float_after(name_off: int, name_len: int):
    end = name_off + name_len
    if end + 4 <= len(data):
        f = struct.unpack_from("<f", data, end)[0]
        return f
    return None


def find_all(prefix: bytes):
    results = []
    pos = 0
    while True:
        i = data.find(prefix, pos)
        if i < 0:
            break
        results.append(i)
        pos = i + 1
    return results


def looks_like_length_byte(b: int, expected: int) -> bool:
    return b == expected


print("\n--- cvar value sweep ---")
for prefix in PREFIXES:
    hits = find_all(prefix)
    for i in hits:
        # Heuristic: name is exact match if the byte before it is the length prefix
        # (single-byte varint < 128).
        if i == 0:
            continue
        # We don't actually know exact name length without reading next non-printable
        # boundary; assume name == prefix when prefix preceded by length byte == len(prefix)
        if data[i - 1] == len(prefix):
            v_float = try_read_float_after(i, len(prefix))
            v_int = struct.unpack_from("<i", data, i + len(prefix))[0] if i + len(prefix) + 4 <= len(data) else None
            print(f"  [{i:6d}] {prefix.decode():40s} f={v_float!r:>14}  i={v_int}")
        else:
            # Possibly an inflected name (e.g. cvar with suffix). Print 16 bytes context.
            tail = data[i:i + len(prefix) + 24]
            print(f"  [{i:6d}] {prefix.decode():40s} <prefix-only, ctx={tail!r}>")

print("\nDone.")
