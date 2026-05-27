# C:\Modding\MO2 Workspace

この repository は、**7 Days to Die 用の live Mod Organizer 2 workspace / Waka mod suite** です。
Mod Organizer 2 本体のソースコードではありません。

目的は、現在の modlist、Waka 系の互換・調整パッチ、deploy helper、運用メモを同じ場所で管理し、次の作業者やエージェントが安全に現状確認、mod 編集、deploy、検証へ入れるようにすることです。

## まず読む場所

- [AGENTS.md](AGENTS.md): この workspace の作業ルール、shell 回避策、deploy、remote dedicated server の注意。
- [CURRENT_STATE.md](CURRENT_STATE.md): ある時点の作業 snapshot。最新状態の断定には使わず、必要に応じて refresh してください。
- [docs/workflows/state-and-triage.md](docs/workflows/state-and-triage.md): 現状確認、git status、overwrite、log 確認の導線。
- [docs/workflows/mod-editing.md](docs/workflows/mod-editing.md): Waka patch 追加、XML / C# mod 編集、modlist 変更時の基本手順。
- [docs/workflows/deploy-and-validation.md](docs/workflows/deploy-and-validation.md): local client、local dedicated server、notebook dedicated server の deploy と log 検証。
- [docs/next-save-plan.md](docs/next-save-plan.md): 次セーブの設計方針、採用 / 弱体化 / 維持候補。
- [docs/balance-audit-bloodfall.md](docs/balance-audit-bloodfall.md): Bloodfall + weapon mods 周辺の balance audit と判断材料。

## 主な構成

- `mods/`: MO2 管理下の mod folders。Waka 系の local patch や compatibility mod はここにあります。
- `profiles/Default/`: 通常プレイ用 profile。`modlist.txt` が有効 / 無効と load order の主要な source of truth です。
- `profiles/NotebookServer/`: separate notebook dedicated server 用 profile。client-only mod との分離に注意してください。
- `tools/waka-deploy/`: enabled mods を 7DTD の `Mods` folder へ反映する deploy helper。
- `docs/workflows/`: よく使う運用手順。
- `_analysis/`, `_archive/`, `_decomp/`, `_tools/`: 調査、退避、decompile、補助ツール用。明示がない限り shipped mods として扱いません。
- `overwrite/`: MO2 overwrite output。移動や整理の前に必ず中身を確認します。

## よく使う導線

### 現状確認

```powershell
git status --short
Get-Content -LiteralPath "profiles\Default\modlist.txt"
Get-ChildItem -LiteralPath "mods" -Directory | Sort-Object Name | Select-Object Name, LastWriteTime
```

より詳しい初動は [docs/workflows/state-and-triage.md](docs/workflows/state-and-triage.md) を参照してください。

### Mod 編集

既存 mod の構造、patch style、load order 上の位置を確認してから編集します。
原則として third-party mod を直接書き換えず、必要最小限の Waka patch を `mods/` 配下に追加します。

手順は [docs/workflows/mod-editing.md](docs/workflows/mod-editing.md) を参照してください。

### Deploy と検証

不確実な変更では dry-run を先に実行します。

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -DryRun
& "tools\waka-deploy\deploy.ps1" "profiles\Default"
```

deploy 後は client / dedicated server の最新 log で `ERR`, `WRN`, `XPath`, `XML`, `ModInfo`, `Exception` を確認します。
詳しくは [docs/workflows/deploy-and-validation.md](docs/workflows/deploy-and-validation.md) を参照してください。

## Notebook Dedicated Server

Notebook dedicated server は local client とは別環境です。
`profiles/NotebookServer/` と `tools/waka-deploy/deploy-notebook.ps1` を使い、専用手順に従って mirror、restart、log verification を行います。

注意点:

- client-only mod を notebook server へ不用意に同期しない。
- `deploy-notebook.ps1 -FromProfile` は `profiles\NotebookServer\modlist.txt` を読む。
- remote path や LAN IP は変わる可能性があるため、作業前に現在値を確認する。
- restart は専用 helper / scheduled task 経由で行い、最新 `output_log_dedi__*.txt` で起動完了を確認する。

## Safety

- 既存の `modlist.txt`、profiles、deploy 先、`overwrite/`、game files を勝手に整理しない。
- `profiles/Default/modlist.txt` や `profiles/NotebookServer/modlist.txt` を編集する前に、原則として同じ folder に timestamped backup を作る。
- deploy は dry-run で対象を確認してから本実行する。
- `mods/` の folder 名や高 `Z` prefix は load order の一部なので、軽率に rename しない。
- third-party mod への直接編集が必要な場合は、理由と影響範囲を明記する。
- `CURRENT_STATE.md` は replaceable snapshot であり、最新状態の証拠ではない。必要な時点で profile、deploy log、runtime log を再確認する。

## Shell Notes

この workspace は `cwd=C:\Modding\MO2` からの command 実行で失敗することがあります。
その場合は `C:\tmp` など neutral working directory から PowerShell を起動し、absolute path と `-LiteralPath` を使ってください。

```powershell
pwsh -NoProfile -Command "Get-Content -LiteralPath 'C:\Modding\MO2\profiles\Default\modlist.txt'"
```

## Non-Goals

- ここは公開宣伝用 README ではありません。
- MO2 本体の開発手順や一般的な 7DTD modding 入門は扱いません。
- 明示された task なしに modlist、XML、deploy target、remote server、archive を変更しません。
