# Build guide

How to build the C# mods in this repo on a fresh machine.

## Prerequisites

| 必要なもの | 入手元 |
|---|---|
| 7 Days to Die v2.6 | Steam |
| .NET SDK 6.0 以上（`dotnet build` のため） | https://dotnet.microsoft.com/download |
| Mod Organizer 2 (portable mode 推奨) | https://www.modorganizer.org/ |
| HarmonyX (`0Harmony.dll`) | IZY-All in One Gun Pack v5.1 に同梱（Nexus 5458） |

`Microsoft.NETFramework.ReferenceAssemblies.net48` NuGet パッケージは `WakaSleepWorkstationFreeze.csproj` で参照されてるけど、ビルド時に dotnet が自動取得するから手動 install 不要。

## Path 設定

このリポジトリの C# mod は2つのプロパティに依存：

| プロパティ | 内容 | デフォルト |
|---|---|---|
| `SDTDManagedDir` | 7DTD の `7DaysToDie_Data\Managed` フォルダ（`Assembly-CSharp.dll` 等） | `C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed` |
| `SDTDHarmonyDir` | `0Harmony.dll` のあるディレクトリ | `C:\Modding\MO2\mods\IZY-All in One Gun Pack v5.1\0_TFP_Harmony` |

デフォルトは `Directory.Build.props`（リポジトリ root）に書いてあって、別マシンでパスが違う場合は3通りの override 手段：

### 手段1：環境変数（推奨、一時的）

```pwsh
$env:SDTDManagedDir = "D:\Steam\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed"
$env:SDTDHarmonyDir = "D:\Modding\MO2\mods\IZY-All in One Gun Pack v5.1\0_TFP_Harmony"
dotnet build mods/WakaRamNullGuard/Source/WakaRamNullGuard.csproj -c Release
```

### 手段2：`Directory.Build.props.user`（永続、git 無視）

リポジトリ root に `Directory.Build.props.user` を作る（`.gitignore` 済み）：

```xml
<Project>
  <PropertyGroup>
    <SDTDManagedDir>D:\Steam\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed</SDTDManagedDir>
    <SDTDHarmonyDir>D:\Modding\MO2\mods\IZY-All in One Gun Pack v5.1\0_TFP_Harmony</SDTDHarmonyDir>
  </PropertyGroup>
</Project>
```

`Directory.Build.props` は `Directory.Build.props.user` が存在すれば自動 import するので、以後の build で常に効く。

### 手段3：CLI 引数（一回限り）

```pwsh
dotnet build mods/WakaRamNullGuard/Source/WakaRamNullGuard.csproj -c Release `
    -p:SDTDManagedDir="D:\Steam\..." `
    -p:SDTDHarmonyDir="D:\Modding\..."
```

## ビルド

### 個別 mod

```pwsh
cd mods/WakaRamNullGuard/Source
dotnet build -c Release
# 出力: mods/WakaRamNullGuard/WakaRamNullGuard.dll （csproj の OutputPath が `..\` になってるため）
```

`WakaSleepWorkstationFreeze` だけ csproj が Source/ ではなくトップにある：

```pwsh
cd mods/WakaSleepWorkstationFreeze
dotnet build WakaSleepWorkstationFreeze.csproj -c Release
```

### 全 C# mod 一括

```pwsh
cd <repo root>
foreach ($csproj in Get-ChildItem -Path mods -Filter *.csproj -Recurse) {
    dotnet build $csproj.FullName -c Release
}
```

## デプロイ

ビルド済み DLL を MO2 の対応 inner folder にコピー：

```pwsh
pwsh -File scripts/deploy.ps1 -Mo2ModsRoot "C:\Modding\MO2\mods"
```

`-Only` オプションで特定 mod だけ：

```pwsh
pwsh -File scripts/deploy.ps1 -Mo2ModsRoot "C:\Modding\MO2\mods" -Only WakaRamNullGuard
```

XML-only mod もこのスクリプトで `Config/`・`UIAtlases/`・`ModInfo.xml` 等を MO2 へ sync できる。

## トラブルシューティング

### `error MSB3245: Could not resolve this reference. ... Assembly-CSharp.dll`

→ `SDTDManagedDir` が間違ってる。Steam ライブラリの実パスを確認、上記「手段1〜3」で正しく設定。

### `error MSB3245: ... 0Harmony.dll`

→ `SDTDHarmonyDir` が間違ってる。MO2 mods 配下の `IZY-All in One Gun Pack v5.1\0_TFP_Harmony\` を探す。IZY を導入していない場合は別途 HarmonyX を入手して任意のパスに置き、そこを指定。

### `error CS0246: The type or namespace name 'Harmony' could not be found`

→ `0Harmony.dll` のパスは合ってるが NuGet 経由で別の Harmony が混入してる可能性。`obj/` を削除して再ビルド。

### EAC をオフにしたのに DLL が読まれない

→ 7DTD は `ModInfo.xml` の `<SkipWithAntiCheat value="true"/>` で各 mod を判定する。各 Waka mod の ModInfo.xml にこの行が入ってるか確認。MO2 起動時に EAC off になってるかも別途確認。

### Player.log で `Harmony patches applied` が出ない

→ Mod の `Init.cs` 等の `IModApi.InitMod` 実装が正しく走ってない可能性。Player.log を見て：

- `[MODS] Loaded assembly <ModName>` が出てる → DLL は読まれた
- `[MODS] Initialized code in mod` が出てない → InitMod() が例外で死んでる

スタックトレースを Player.log で grep して原因特定。
