<#
Rename files that start with "file_" to blue1, blue2, ...
Usage:
  -Run in target folder:
      powershell -ExecutionPolicy Bypass -File .\rename-files-to-blue.ps1
  -Or specify path:
      powershell -ExecutionPolicy Bypass -File .\rename-files-to-blue.ps1 -Path 'C:\path\to\images'
  -Dry-run:
      powershell -ExecutionPolicy Bypass -File .\rename-files-to-blue.ps1 -WhatIf
#>

param(
    [string]$Path = ".",
    [switch]$WhatIf
)

try {
    $targetPath = Resolve-Path -Path $Path -ErrorAction Stop
}
catch {
    Write-Error "Путь не найден: $Path"
    exit 1
}

$files = Get-ChildItem -Path $targetPath -File -Filter 'file_*' | Sort-Object Name
if ($files.Count -eq 0) {
    Write-Output "Файлы, соответствующие 'file_*', не найдены в $($targetPath.Path)"
    exit 0
}

$counter = 1
foreach ($f in $files) {
    $ext = $f.Extension
    $newName = "blue$counter$ext"
    $newFull = Join-Path $f.DirectoryName $newName
    while (Test-Path $newFull) {
        $counter++
        $newName = "blue$counter$ext"
        $newFull = Join-Path $f.DirectoryName $newName
    }

    if ($WhatIf) {
        Write-Output "Будет переименован: '$($f.Name)' -> '$newName'"
    }
    else {
        Rename-Item -Path $f.FullName -NewName $newName
        Write-Output "Переименован: '$($f.Name)' -> '$newName'"
    }

    $counter++
}
