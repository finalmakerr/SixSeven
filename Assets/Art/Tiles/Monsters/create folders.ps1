# Run script from inside the parent folder that contains monster folders

$basePath = Get-Location

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
    "thinking",
    "video"   # added video folder
)

# Create state folders inside each first-level subfolder
Get-ChildItem -Path $basePath -Directory | ForEach-Object {

    foreach ($state in $states) {
        $statePath = Join-Path $_.FullName $state
        if (-not (Test-Path $statePath)) {
            New-Item -ItemType Directory -Path $statePath | Out-Null
        }
    }
}

# Add .gitkeep ONLY to truly empty folders
Get-ChildItem -Path $basePath -Directory -Recurse | ForEach-Object {

    $hasFiles = Get-ChildItem $_.FullName -Recurse -File -ErrorAction SilentlyContinue

    if (-not $hasFiles) {
        $gitkeep = Join-Path $_.FullName ".gitkeep"
        if (-not (Test-Path $gitkeep)) {
            New-Item -ItemType File -Path $gitkeep | Out-Null
        }
    }
}

Write-Host "Done. State folders + video folder ensured in all subdirectories." -ForegroundColor Green
