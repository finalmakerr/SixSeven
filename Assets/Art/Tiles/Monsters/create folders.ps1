$monstersPath = "D:\Documents\GitHub\SixSeven\Assets\Art\Tiles\Monsters"

$states = @(
    "attack",
    "confused",
    "enrage",
    "happy",
    "hurt",
    "idle",
    "move",
    "sad",
    "sleeping",
    "stunned",
    "suprise",
    "angry",
    "calm",
    "thinking"
)

# Create state folders (safe, no overwrite)
Get-ChildItem -Path $monstersPath -Directory | ForEach-Object {
    foreach ($state in $states) {
        $statePath = Join-Path $_.FullName $state
        if (-not (Test-Path $statePath)) {
            New-Item -ItemType Directory -Path $statePath | Out-Null
        }
    }
}

# Add .gitkeep ONLY to truly empty folders
Get-ChildItem -Path $monstersPath -Directory -Recurse | ForEach-Object {
    $hasFiles = Get-ChildItem $_.FullName -Recurse -File -ErrorAction SilentlyContinue
    if (-not $hasFiles) {
        $gitkeep = Join-Path $_.FullName ".gitkeep"
        if (-not (Test-Path $gitkeep)) {
            New-Item -ItemType File -Path $gitkeep | Out-Null
        }
    }
}