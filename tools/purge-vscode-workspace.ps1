param (
    [Parameter(Mandatory = $true)]
    [string]$FolderPath
)

# Verify folder exists
if (-not (Test-Path -Path $FolderPath -PathType Container)) {
    Write-Error "The specified folder '$FolderPath' does not exist."
    exit 1
}

$FolderPath = Resolve-Path -Path $FolderPath

Write-Host "Purging VS Code workspace data for: $FolderPath" -ForegroundColor Cyan

# 1. Remove .vscode folder in the target directory
$vscodeFolderPath = Join-Path -Path $FolderPath -ChildPath ".vscode"
if (Test-Path -Path $vscodeFolderPath -PathType Container) {
    Write-Host "Removing .vscode folder..." -ForegroundColor Yellow
    Remove-Item -Path $vscodeFolderPath -Recurse -Force
    Write-Host "Removed .vscode folder." -ForegroundColor Green
}
else {
    Write-Host "No .vscode folder found in the specified directory." -ForegroundColor Gray
}

# 2. Clean VS Code workspace storage
$appDataPath = $env:APPDATA
$workspaceStoragePath = Join-Path -Path $appDataPath -ChildPath "Code\User\workspaceStorage"

if (Test-Path -Path $workspaceStoragePath -PathType Container) {
    Write-Host "Checking VS Code workspace storage..." -ForegroundColor Yellow
    
    # Get folder name to use in search
    $folderName = Split-Path -Path $FolderPath -Leaf
    
    # Find workspace storage folders that might be related to this folder
    $workspaceFolders = Get-ChildItem -Path $workspaceStoragePath -Directory
    
    $foldersRemoved = 0
    
    foreach ($folder in $workspaceFolders) {
        # Check if the workspace has a reference to the target folder
        $workspaceJsonPath = Join-Path -Path $folder.FullName -ChildPath "workspace.json"
        
        if (Test-Path $workspaceJsonPath) {
            $content = Get-Content -Path $workspaceJsonPath -Raw
            
            # Check if the content contains the folder path (normalize path separators)
            $normalizedPath = $FolderPath.Replace('\', '\\')
            if ($content -match [regex]::Escape($normalizedPath) -or $content -match [regex]::Escape($folderName)) {
                Write-Host "Found related workspace storage: $($folder.Name)" -ForegroundColor Yellow
                Remove-Item -Path $folder.FullName -Recurse -Force
                $foldersRemoved++
            }
        }
    }
    
    if ($foldersRemoved -gt 0) {
        Write-Host "Removed $foldersRemoved workspace storage folder(s)." -ForegroundColor Green
    }
    else {
        Write-Host "No related workspace storage folders found." -ForegroundColor Gray
    }
}

# 3. Clear VS Code history entries for the folder
$historyPath = Join-Path -Path $appDataPath -ChildPath "Code\User\History"
if (Test-Path -Path $historyPath -PathType Container) {
    Write-Host "Checking VS Code history entries..." -ForegroundColor Yellow
    
    # Try to clean entries in various history files
    $historyFiles = @(
        "fileHistory.json",
        "recentlyOpened.json",
        "workspaces.json"
    )
    
    foreach ($file in $historyFiles) {
        $filePath = Join-Path -Path $historyPath -ChildPath $file
        if (Test-Path $filePath) {
            # Backup the file first
            $backupFile = "$filePath.backup"
            Copy-Item -Path $filePath -Destination $backupFile -Force
            
            try {
                $fileContent = Get-Content -Path $filePath -Raw | ConvertFrom-Json
                $modified = $false
                
                # Process depending on file type
                if ($file -eq "recentlyOpened.json" -and $fileContent.entries) {
                    $originalCount = $fileContent.entries.Count
                    $fileContent.entries = $fileContent.entries | Where-Object {
                        -not ($_.folderUri -and ($_.folderUri -match [regex]::Escape($FolderPath) -or $_.folderUri -match [regex]::Escape($folderName)))
                    }
                    if ($fileContent.entries.Count -lt $originalCount) {
                        $modified = $true
                    }
                }
                
                if ($modified) {
                    $fileContent | ConvertTo-Json -Depth 10 | Set-Content -Path $filePath
                    Write-Host "Cleaned entries from $file" -ForegroundColor Green
                }
                else {
                    Write-Host "No relevant entries found in $file" -ForegroundColor Gray
                }
            }
            catch {
                Write-Warning "Failed to process $file. Restoring backup."
                Copy-Item -Path $backupFile -Destination $filePath -Force
            }
            finally {
                if (Test-Path $backupFile) {
                    Remove-Item -Path $backupFile -Force
                }
            }
        }
    }
}

Write-Host "`nVS Code workspace purge completed for: $FolderPath" -ForegroundColor Cyan
Write-Host "You may need to restart VS Code if it's currently running." -ForegroundColor Yellow
