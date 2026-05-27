param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDir
)

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    Write-Error "PublishDir is empty."
    exit 1
}

$exe = [System.IO.Path]::Combine($PublishDir, "WasteAccountingClient.exe")
if (-not (Test-Path -LiteralPath $exe)) {
    Write-Error "Executable not found: $exe"
    exit 1
}

$desktop = [Environment]::GetFolderPath('Desktop')
if ([string]::IsNullOrWhiteSpace($desktop)) {
    Write-Error "Desktop folder not found."
    exit 1
}

$lnkPath = [System.IO.Path]::Combine($desktop, "Учет отходов.lnk")
$legacy = [System.IO.Path]::Combine($desktop, "WasteAccounting.lnk")
if (Test-Path -LiteralPath $legacy) { Remove-Item -LiteralPath $legacy -Force -ErrorAction SilentlyContinue }
$w = New-Object -ComObject WScript.Shell
$s = $w.CreateShortcut($lnkPath)
$s.TargetPath = $exe
$s.WorkingDirectory = $PublishDir
$s.Description = "Учет отходов"
$s.IconLocation = "$exe,0"
$s.Save()

Write-Host "Shortcut: $lnkPath"
Write-Host "Target:   $exe"
