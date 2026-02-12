1) Ordered `git mv` operations

```bash
# Case-only folder renames first (using __tmp)
git mv Assets/art Assets/__tmp_art
 git mv Assets/__tmp_art Assets/art

git mv Assets/audio Assets/__tmp_audio
 git mv Assets/__tmp_audio Assets/audio

git mv Assets/prefabs Assets/__tmp_prefabs
 git mv Assets/__tmp_prefabs Assets/prefabs

git mv Assets/scenes Assets/__tmp_scenes
 git mv Assets/__tmp_scenes Assets/scenes

git mv Assets/scripts Assets/__tmp_scripts
 git mv Assets/__tmp_scripts Assets/scripts

git mv Assets/settings Assets/__tmp_settings
 git mv Assets/__tmp_settings Assets/settings

git mv Assets/gamecore Assets/__tmp_gamecore
 git mv Assets/__tmp_gamecore Assets/gamecore
```

```bash
# Hard-coded duplicate case folder normalizations
# (No safe non-art duplicate-case folders selected; deep artwork duplicates excluded by policy.)
```

```bash
# Top-level folder lowercase renames complete; now safe subfolder lowercase renames
# assets/audio
git mv Assets/audio/Audio Assets/audio/__tmp_audio_sub
 git mv Assets/audio/__tmp_audio_sub Assets/audio/audio

git mv Assets/audio/Explosions Assets/audio/__tmp_explosions
 git mv Assets/audio/__tmp_explosions Assets/audio/explosions

git mv Assets/audio/Level Assets/audio/__tmp_level
 git mv Assets/audio/__tmp_level Assets/audio/level

git mv Assets/audio/Match Assets/audio/__tmp_match
 git mv Assets/audio/__tmp_match Assets/audio/match

git mv Assets/audio/Meta Assets/audio/__tmp_meta
 git mv Assets/audio/__tmp_meta Assets/audio/meta

git mv Assets/audio/Monsters Assets/audio/__tmp_monsters
 git mv Assets/audio/__tmp_monsters Assets/audio/monsters

git mv Assets/audio/Movement Assets/audio/__tmp_movement
 git mv Assets/audio/__tmp_movement Assets/audio/movement

git mv Assets/audio/Music Assets/audio/__tmp_music
 git mv Assets/audio/__tmp_music Assets/audio/music

git mv Assets/audio/Power-ups Assets/audio/__tmp_power-ups
 git mv Assets/audio/__tmp_power-ups Assets/audio/power-ups

git mv Assets/audio/Progression Assets/audio/__tmp_progression
 git mv Assets/audio/__tmp_progression Assets/audio/progression

git mv Assets/audio/UI Assets/audio/__tmp_ui
 git mv Assets/audio/__tmp_ui Assets/audio/ui

# assets/gamecore
git mv Assets/gamecore/Prefabs Assets/gamecore/__tmp_prefabs
 git mv Assets/gamecore/__tmp_prefabs Assets/gamecore/prefabs

git mv Assets/gamecore/Scenes Assets/gamecore/__tmp_scenes
 git mv Assets/gamecore/__tmp_scenes Assets/gamecore/scenes

git mv Assets/gamecore/Scripts Assets/gamecore/__tmp_scripts
 git mv Assets/gamecore/__tmp_scripts Assets/gamecore/scripts

git mv Assets/gamecore/scripts/BonusMiniGames Assets/gamecore/scripts/__tmp_bonusminigames
 git mv Assets/gamecore/scripts/__tmp_bonusminigames Assets/gamecore/scripts/bonusminigames

git mv Assets/gamecore/scripts/Levels Assets/gamecore/scripts/__tmp_levels
 git mv Assets/gamecore/scripts/__tmp_levels Assets/gamecore/scripts/levels

# assets/scripts
git mv Assets/scripts/Bonus Assets/scripts/__tmp_bonus
 git mv Assets/scripts/__tmp_bonus Assets/scripts/bonus

git mv Assets/scripts/Systems Assets/scripts/__tmp_systems
 git mv Assets/scripts/__tmp_systems Assets/scripts/systems

# assets/settings
git mv Assets/settings/Scenes Assets/settings/__tmp_scenes
 git mv Assets/settings/__tmp_scenes Assets/settings/scenes
```

2) Patch diffs for C# hardcoded paths

```diff
diff --git a/Assets/Editor/SixSevenSetup.cs b/Assets/Editor/SixSevenSetup.cs
@@
-    private const string ScenesFolder = "Assets/scenes";
-    private const string PrefabsFolder = "Assets/prefabs";
-    private const string TilePrefabPath = "Assets/prefabs/TileUI.prefab";
+    private const string ScenesFolder = "Assets/scenes";
+    private const string PrefabsFolder = "Assets/prefabs";
+    private const string TilePrefabPath = "Assets/prefabs/TileUI.prefab";
@@
-        if (!AssetDatabase.IsValidFolder("Assets/scripts"))
-            AssetDatabase.CreateFolder("Assets", "Scripts");
+        if (!AssetDatabase.IsValidFolder("Assets/scripts"))
+            AssetDatabase.CreateFolder("Assets", "scripts");
```

3) Manual Unity validation tasks

- Open Unity and wait for full asset reimport/refresh.
- Confirm no missing scripts/components in open scenes and prefabs.
- Verify all expected folders exist with lowercase names under `Assets/` (`art`, `audio`, `prefabs`, `scenes`, `scripts`, `settings`, `gamecore`) while `Assets/Editor`, `Assets/Resources`, and `Assets/!Downloads` remain unchanged.
- Confirm no `.meta` file conflicts in the Console and no GUID churn in version control.
- Open key prefabs from `Assets/prefabs` and `Assets/gamecore/prefabs` and confirm sprite/audio/material references are intact.
- Play the main gameplay scene and validate tile rendering, audio playback, level loading, and UI flows.
- Run project build (development build) and confirm no path-related runtime load failures.
