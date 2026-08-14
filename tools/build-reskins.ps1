param(
    [string]$Root = "C:\Users\Umka\Documents\Default Project\ReskinManager",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$srcRoot = Join-Path $Root "addons-src"
$outRoot = Join-Path $Root "reskins"
$zipRoot = Join-Path $outRoot "zips"
$previewRoot = Join-Path $outRoot "previews"
$avatarRoot = Join-Path $outRoot "avatars"

New-Item -ItemType Directory -Force -Path $zipRoot, $previewRoot, $avatarRoot | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression

$catalog = [ordered]@{
    version = "1"
    updatedAt = (Get-Date).ToString("yyyy-MM-dd")
    addons = @()
}

$addons = Get-ChildItem -LiteralPath $srcRoot -Directory
foreach ($dir in $addons) {
    $jsonPath = Join-Path $dir.FullName "addon.json"
    if (-not (Test-Path -LiteralPath $jsonPath)) { continue }

    $json = Get-Content -LiteralPath $jsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $id = $dir.Name

    # -- zip содержимого (без addon.json и preview) --
    $zipPath = Join-Path $zipRoot "$id.zip"
    if ((Test-Path -LiteralPath $zipPath) -and -not $Force) {
        Write-Host "skip $id (exists, use -Force)"
        continue
    }
    if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

    $tmpZip = Join-Path $env:TEMP "$id.zip"
    if (Test-Path -LiteralPath $tmpZip) { Remove-Item -LiteralPath $tmpZip -Force }

    $fs = [System.IO.File]::Create($tmpZip)
    $zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create)

    try {
        $files = Get-ChildItem -LiteralPath $dir.FullName -Recurse -File |
            Where-Object { $_.Name -ne "addon.json" -and $_.Name -notlike "preview.*" }
        foreach ($f in $files) {
            $rel = $f.FullName.Substring($dir.FullName.Length).TrimStart("\", "/").Replace("\", "/")
            $entry = $zip.CreateEntry($rel, [System.IO.Compression.CompressionLevel]::Optimal)
            $es = $entry.Open()
            $bs = [System.IO.File]::OpenRead($f.FullName)
            $bs.CopyTo($es)
            $bs.Dispose()
            $es.Dispose()
        }
    } finally {
        $zip.Dispose()
        $fs.Dispose()
    }

    Copy-Item -LiteralPath $tmpZip -Destination $zipPath -Force
    Remove-Item -LiteralPath $tmpZip -Force

    $size = (Get-Item -LiteralPath $zipPath).Length

    # -- preview --
    $previewName = ""
    $previewFile = Get-ChildItem -LiteralPath $dir.FullName -File |
        Where-Object { $_.Name -like "preview.*" } | Select-Object -First 1
    if ($previewFile) {
        $ext = $previewFile.Extension
        $dest = Join-Path $previewRoot "$id$ext"
        Copy-Item -LiteralPath $previewFile.FullName -Destination $dest -Force
        $previewName = "previews/$id$ext"
    }

    # -- avatar --
    $avatarName = ""
    if ($json.AuthorAvatar) {
        $avatarFile = $json.AuthorAvatar -replace "^/reskins/", ""
        $avatarSrc = Join-Path $srcRoot ($avatarFile.Replace("/", "\"))
        if (Test-Path -LiteralPath $avatarSrc) {
            $avatarRel = Split-Path $avatarFile -Leaf
            $avatarDest = Join-Path $avatarRoot $avatarRel
            Copy-Item -LiteralPath $avatarSrc -Destination $avatarDest -Force
            $avatarName = "avatars/$avatarRel"
        }
    }

    $catalog.addons += [ordered]@{
        id = $id
        title = $json.Title
        author = $json.Author
        authorAvatar = $avatarName
        type = $json.Type
        tags = @($json.Tags)
        description = $json.Description
        workshopUrl = $json.WorkshopUrl
        preview = $previewName
        archive = "zips/$id.zip"
        ignore = @($json.Ignore)
        sizeBytes = $size
    }

    Write-Host "packed $id ($([math]::Round($size / 1KB)) KB)"
}

$catalogJson = $catalog | ConvertTo-Json -Depth 5
$catalogPath = Join-Path $outRoot "catalog.json"
[System.IO.File]::WriteAllText($catalogPath, $catalogJson, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "catalog.json written: $catalogPath"
