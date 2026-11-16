# Version update 1.6 -> 1.7: ApexMechanoids
Game installed: 1.7.5005 rev1082 (unstable)   Refs: GameRefs/1.6 (1.6.4871 rev590)   Unity: gone (UnityStubs.dll, Waylith.dll)
Harmony: HarmonyRef/1.6 2.4.3.0 net472 (chain today: none, csproj uses NuGet Lib.Harmony 2.4.2 compile-only)   HarmonyRef/1.7 3.0.0.0 net10
ManagedDir: D:\Steam\steamapps\common\RimWorld   Framework: net48 -> net10.0   Runtime: CoreCLR (Waylith)   GC: unverified
UnityStubs.dll: yes   Toolchain changed: yes   SDK: 10.0.403
Layout today: LoadFolders, two version folders, each with its own copy of Source, Defs, Patches   Missing 1.7 dependencies: none (Combat Extended lists only 1.6 in About.xml but runs on 1.7)
1.6 after the update: one source, both built (decided by the user 2026-11-15: "1.6 und 1.7 supporten weiterhin, fixes/features in 1.7 auch in 1.6")
Save surface vs last tag: no tag in the repo, nothing to compare; hotfix-1 hediff defNames equal the names 1.6 created at runtime, so 1.6 saves hold

## Findings

- Two copies of the source (`1.6/Source`, `1.7/Source`), split on 2026-10-14 (`13522b0 1.7 copy paste`). They drifted both ways, no line changed on both sides:
  - only in 1.6 (CE merge `e9137e7`, 2026-10-15): `DebugActions/ApexMechanoidDebugActions.cs`, `Verb_Whip.GetArmorPenetration` (virtual, used by `Verb_WhipCE`), `MeleeScaledProjectileUtility.FindStrongestMeleeVerb` public, the whole `ApexMechanoids_CombatExtended` project.
  - only in 1.7 (hotfix-1 `01d5b06`, 2026-11-04): spawn/map guards in `CompAbilityEffect_ConvertToBuilding`, `CompMechWirelessCharger`; `CompStatFactorsFromHealth` reads `hediff` from XML instead of creating defs at runtime; 15 def files (`APM_MechanoidWeaponBase`, `Hediffs_StatFactorsFromHealth.xml`, weapon parents, Maul race).
  - 1.6 players have none of hotfix-1; 1.7 players have no debug actions.
- The 1.7 DLL is a 1.6 build: `1.7/Source/ApexMechanoids/ApexMechanoids.csproj` is net48 against `Krafs.Rimworld.Ref 1.6.4871`. It was never compiled against 1.7.
- `1.6/Patches` and `1.7/Patches` are identical; `1.7/Defs` = `1.6/Defs` + hotfix-1, plain XML that 1.6 can load (`BaseGun` parent).
- `LoadFolders.xml` `<v1.7>` loads `1.6/CombatExtended` (net48 CE DLL built against CE 1.6). CE's About.xml lists only 1.6, but CE runs on 1.7, so this line was needed.
- References come from NuGet (`Krafs.Rimworld.Ref`, `Krafs.Publicizer` on Assembly-CSharp, `Lib.Harmony`) plus checked-in `ReferenceAssembly/VEF-reference.dll`, `PipeSystem.dll` (1.6 builds, identical in both copies). CI builds on ubuntu without a game, so the workspace pattern (`GameRefs/`, `HarmonyRef/`, `new_old_version_project.py`, explicit Compile list) cannot be used as is. NuGet has `Krafs.Rimworld.Ref 1.7.5005-unstable` (= installed game) and `Lib.Harmony 3.0.0-rc.5`.
- `.github/workflows/build-csharp.yml`: .NET 9 (net10 needs 10.x), builds and commits only `1.6/...` DLLs, `rsync --exclude '1.6/Source/'`, PR comment says "Built against RimWorld <first supportedVersions entry>" (always 1.6).
- Code: 39 Harmony patch classes, 54 attributes, 0 transpilers, no `#if`, no Prepatcher, no AssetBundles, only vanilla `ShaderDatabase` shaders, no string `GetHashCode`. 8 `AccessTools.Field/Method` lookups to probe.
- Upstream repo (mort1s1234/Mechanoids, several contributors): the layout change and net10 (VS 2026 / Rider 2026 needed for the 1.7 project) affect everybody who works on it.

## Pass A - One package layout
- [x] `git mv 1.7/Source/ApexMechanoids` changes into the 1.6 tree: take the 1.7 version of `CompAbilityEffect_ConvertToBuilding.cs`, `CompMechWirelessCharger.cs`, `CompStatFactorsFromHealth.cs`; everything else from 1.6
- [x] `git mv 1.6/Source Source` (repo root), `git rm -r 1.7/Source`
- [x] `git mv 1.7/Defs Defs`, `git mv 1.7/Patches Patches`, `git rm -r 1.6/Defs 1.6/Patches`; version folders keep only `Assemblies`
- [x] LoadFolders.xml `<v1.7>`: dropped the `1.6/CombatExtended` line on the wrong assumption that CE had no 1.7 release; this switched CE off on 1.7. Fixed upstream (`7b9f093` mort1s1234, `8d5d043` SaltyKarl): CE content lives in `Mods/CombatExtended`, loaded by `<v1.6>` and `<v1.7>`; `31968f0` points the CE project output and CI there and removes `1.6/CombatExtended` and the stale `1.7/CombatExtended`
- [x] About.xml: unchanged (1.6 and 1.7 already listed)
- [x] load_folders.py 1.6 and 1.7 without warnings
## Pass T - 1.7 toolchain (`Source/ApexMechanoids/ApexMechanoids.csproj`)
- [x] net10.0, LangVersion 7.3 (shared with net48), ImplicitUsings/Nullable off, GenerateDependencyFile/RuntimeConfig off
- [x] `Krafs.Rimworld.Ref 1.7.5005-unstable`, `Lib.Harmony 3.0.0-rc.5` compile-only, Publicizer kept (CoreCLR access-check bypass: unverified, checked in game)
- [x] VEF/PipeSystem refs: `ReferenceAssembly/1.7/` from Workshop VEF `1.7/Assemblies`, the 1.6 ones to `ReferenceAssembly/1.6/`
- [x] OutputPath `../../1.7/Assemblies/`; Compile items: keep SDK globbing (see open points)
- [x] build 0/0, output folder holds only `ApexMechanoids.dll` (+pdb in Debug)
## Pass M - 1.6 from the same source
- [x] `Source/ApexMechanoids_1_6/ApexMechanoids_1_6.csproj`: net48, LangVersion 7.3, `Krafs.Rimworld.Ref 1.6.4871`, `Lib.Harmony 2.4.2` compile-only (as released), Publicizer, `RW_1_6`, compiles `../ApexMechanoids/**/*.cs` (same glob, same order as the 1.7 project), refs `ReferenceAssembly/1.6`, OutputPath `../../1.6/Assemblies/`
- [x] `ApexMechanoids_CombatExtended` references `ApexMechanoids_1_6` (stays 1.6 only)
- [x] solution `Source/ApexMechanoids.slnx` (CI, VS 2026) with all three projects, 1.7 with a build dependency on 1.6
- [x] solution builds 0/0, both DLLs; `#if RW_1_6` per API difference: none needed; removed two unused usings (`NVorbis.NAudioSupport`, `RuntimeAudioClipLoader`) in `CompAbilityEffect_Bash.cs`, `PatchAll()` -> `PatchAll(typeof(StartUp).Assembly)` (Harmony 3 CS0618)
- [x] same type order in both DLLs (patch order): 624 types, 0 mismatches
- [x] patch_probe 1.6 DLL against GameRefs/1.6
## Pass C - CI
- [x] `dotnet-version: 10.0.x`, restore/build/test `./Source/ApexMechanoids.sln`
- [x] commit `1.6/Assemblies/ApexMechanoids.dll`, `1.7/Assemblies/ApexMechanoids.dll`, CE DLL; upload both
- [x] rsync `--exclude 'Source/'`; PR comment lists all supportedVersions
## Pass B - Port to 1.7
- [x] build 0/0 against 1.7.5005
- [x] patch_probe (54 patches, Harmony 3 injected-parameter types) + the 8 AccessTools lookups by hand: 53 targets + 1 TargetMethod, 0 problems on 1.7 and 1.6; KCSG names (`ScenPart_AddStartingStructure.PostMapGenerate`, `PrepareCarefully_Util.pcScenariosSave`, `chooseFrom`, `nearMapCenter`) present in VEF 1.6 and 1.7, `Patch_KcsgStartingStructureGate` Prepare checked in game
- [x] Defs/Patches XPaths against 1.7 Data: 0 vanilla defNames/abstract names of 1.6 missing in 1.7.5005; rw-smith validate_mod 0 errors
- [ ] game started with `-mod=brrainz.harmony;OskarPotocki.VanillaFactionsExpanded.Core;Mortis.ApexMechanoids`, Player.log worked top to bottom (deploy: user)
- [x] rwtest-refresh for `Tests/` handed over (2026-11-15, against 1.7.5005 rev1082: 24 tests checked, no line changed; drafts until the first 1.7 run)
## After every beta patch (until 1.7 is on main)
- [x] 2026-11-15 1.7.5005 rev1082: build ok, 54 patches probed, log not read yet (game not started)
## Open points
- Compile items: the workspace wants an explicit list; this repo globs. Kept for the upstream contributors, both projects glob the same folder so the order matches.
- References via NuGet instead of `GameRefs`/`HarmonyRef` (CI has no game). `Krafs.Rimworld.Ref` 1.7 tracks the unstable branch: bump it with every beta patch.
- Contributors need VS 2026 / Rider 2026 for the net10 project; `dotnet build` works everywhere.
- Versions block: decided 2026-11-15, only this file. One source: `Source/ApexMechanoids/` is built twice, `ApexMechanoids.csproj` for 1.7 (net10, `1.7/Assemblies`), `ApexMechanoids_1_6/ApexMechanoids_1_6.csproj` for 1.6 (net48, `RW_1_6`, `1.6/Assemblies`); CE project: one net48 build against `ApexMechanoids_1_6`, output `Mods/CombatExtended/Assemblies`, loaded on 1.6 and 1.7 (on 1.7 it binds to the net10 `ApexMechanoids.dll` by name: unverified, checked in game). API differences go into `#if RW_1_6 ... #else ... #endif` at the line that touches the game. Defs/Patches live at the root for both.
- `WeaponTweakData/` is loaded by LoadFolders but excluded from the CI zip (older than this update).
- Leftover untracked `1.6/Source`, `1.7/Source` (`.vs`, `obj`), locked by Visual Studio: delete after closing VS.
- 1.6 is not installed here: tests in game on 1.6 need a second install or a Steam branch switch.
