# Mod Editing

Use focused Waka compatibility patches for local behavior changes. Avoid editing third-party mods directly unless that is the smallest safe surface and the user agrees.

## Before Editing

1. Read the active profile in `profiles\Default\modlist.txt`.
2. Find the nearest existing Waka patch or upstream mod.
3. Inspect `ModInfo.xml`, `Config\*.xml`, source files, and any `README.md`.
4. Check `git status --short` and preserve unrelated changes.
5. Check `overwrite\` before moving generated files into a mod.

Use a neutral workdir if shell startup fails in `C:\Modding\MO2`:

```powershell
pwsh -NoProfile -Command "Get-Content -LiteralPath 'C:\Modding\MO2\mods\...\file.xml'"
```

## Adding A Waka Patch

A normal XML-only patch should have:

- A focused folder under `mods\`, usually with a high `Z` prefix.
- A deployed mod root containing `ModInfo.xml`.
- XML patch files under `Config\`.
- A short `README.md` with Purpose, Changes, Dependencies, Validation, and Safety Notes.

Keep MO2 folder names distinct from deployed 7DTD mod names. `waka-deploy` can deploy a direct `ModInfo.xml` root or child folders with `ModInfo.xml`.

## Editing XML

Match local style:

- Preserve indentation and nearby XPath patterns.
- Prefer narrow XPath edits over broad replacements.
- Add comments only when the XPath choice would be non-obvious later.
- Keep gameplay surface small enough to validate with targeted log checks.

For high-risk surfaces such as inventory size, storage capacity, entity replacement, traders, recipes, progression, loot, or localization, verify the affected XML and then inspect runtime logs after deploy when practical.

## Editing C# Harmony Mods

For DLL-backed Waka mods:

- Read the existing hook class names and `ModInfo.xml` first.
- Keep EAC implications visible in the README.
- Build only when the task requires a behavior change.
- Runtime proof should include loaded-mod lines and absence of mod-specific exceptions.

## Modlist Changes

Before editing `profiles\Default\modlist.txt`, create a timestamped backup next to it unless the user explicitly asked for a direct trivial edit.

Do not rename Waka folders casually. Prefix count is part of load order and conflict resolution.

## README Standard

Use this shape for important Waka patches:

```markdown
# Mod Name

## Purpose

## Changes

## Dependencies

## Validation

## Safety Notes
```

Keep the README operational: describe what to inspect, what can break, and how to prove the patch loaded.
