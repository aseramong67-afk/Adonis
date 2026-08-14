param(
    [string]$PublishDir = "C:\Users\Umka\Documents\Default Project\ReskinManager\publish",
    [string]$ZipPath = "C:\Users\Umka\Documents\Default Project\ReskinManager\Adonis.zip"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$exclude = @("auth.json", "binds.json", "sessions.json", "settings.json", "Adonis.pdb", "web.config", "appsettings.Development.json")

if (Test-Path -LiteralPath $ZipPath) { Remove-Item -LiteralPath $ZipPath -Force }

$zip = [System.IO.Compression.ZipFile]::Open($ZipPath, [System.IO.Compression.ZipArchiveMode]::Create)
$count = 0
try {
    $files = Get-ChildItem -LiteralPath $PublishDir -Recurse -File |
        Where-Object { $exclude -notcontains $_.Name -and $_.FullName -notmatch "\\Adonis\.exe\.WebView2\\" }
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($PublishDir.Length).TrimStart("\").Replace("\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, $rel, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        $count++
    }
}
finally { $zip.Dispose() }

Write-Host "Adonis.zip: $([math]::Round((Get-Item $ZipPath).Length / 1MB, 1)) MB, files: $count"
