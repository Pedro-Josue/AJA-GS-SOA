[CmdletBinding()]
param(
    [string]$DestinationRoot,
    [switch]$Force,
    [switch]$SkipVerification
)

$ErrorActionPreference = 'Stop'
$sourceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($DestinationRoot)) {
    $DestinationRoot = Join-Path $sourceRoot 'entregas'
}
$destination = [System.IO.Path]::GetFullPath($DestinationRoot)
$sourcePrefix = $sourceRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (-not $destination.StartsWith($sourcePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'O destino deve ficar dentro da raiz do projeto.'
}

$csharpTarget = Join-Path $destination 'AJA-CSharp'
$soaTarget = Join-Path $destination 'AJA-SOA'
$allowedTargets = @([System.IO.Path]::GetFullPath($csharpTarget), [System.IO.Path]::GetFullPath($soaTarget))

foreach ($target in $allowedTargets) {
    if (Test-Path -LiteralPath $target) {
        if (-not $Force) {
            throw "O destino já existe: $target. Use -Force para regenerar somente as entregas validadas."
        }
        $resolved = [System.IO.Path]::GetFullPath($target)
        if (-not $resolved.StartsWith($destination.TrimEnd('\') + '\', [System.StringComparison]::OrdinalIgnoreCase) -or
            ([System.IO.Path]::GetFileName($resolved) -notin @('AJA-CSharp', 'AJA-SOA'))) {
            throw "Recusa de remoção fora dos dois destinos conhecidos: $resolved"
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

New-Item -ItemType Directory -Force -Path $csharpTarget, $soaTarget | Out-Null
$excludedSegments = @('.git', 'bin', 'obj', 'docs', 'entregas', 'tmp', 'verification', 'TestResults', '.vs', '.idea', '.data-protection-keys', '.local-backups')
$sourceFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File | Where-Object {
    $relative = [System.IO.Path]::GetRelativePath($sourceRoot, $_.FullName)
    $segments = $relative -split '[\\/]'
    -not ($segments | Where-Object { $_ -in $excludedSegments }) -and
    $relative -notin @('README.md', 'README.CSharp.md', 'README.SOA.md') -and
    $_.Extension -notin @('.db', '.db-shm', '.db-wal', '.user') -and
    $_.Name -ne '.env' -and
    $_.Name -ne 'appsettings.Local.json'
}

function Copy-Delivery([string]$target) {
    foreach ($file in $sourceFiles) {
        $relative = [System.IO.Path]::GetRelativePath($sourceRoot, $file.FullName)
        $output = Join-Path $target $relative
        $parent = Split-Path -Parent $output
        if (-not (Test-Path -LiteralPath $parent)) {
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
        }
        Copy-Item -LiteralPath $file.FullName -Destination $output
    }
}

Copy-Delivery $csharpTarget
Copy-Delivery $soaTarget
Copy-Item -LiteralPath (Join-Path $sourceRoot 'README.CSharp.md') -Destination (Join-Path $csharpTarget 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot 'README.SOA.md') -Destination (Join-Path $soaTarget 'README.md') -Force

$manifestLines = $sourceFiles | Sort-Object FullName | ForEach-Object {
    $relative = [System.IO.Path]::GetRelativePath($sourceRoot, $_.FullName).Replace('\', '/')
    "$relative`:$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
}
$sha = [System.Security.Cryptography.SHA256]::Create()
try {
    $snapshotBytes = [System.Text.Encoding]::UTF8.GetBytes(($manifestLines -join "`n"))
    $snapshotId = ([System.BitConverter]::ToString($sha.ComputeHash($snapshotBytes))).Replace('-', '').ToLowerInvariant()
}
finally {
    $sha.Dispose()
}

@("AJA C#", "Snapshot: $snapshotId", "Gerado em UTC: $([DateTimeOffset]::UtcNow.ToString('O'))") |
    Set-Content -LiteralPath (Join-Path $csharpTarget 'DELIVERY-METADATA.txt') -Encoding utf8
@("AJA SOA", "Snapshot: $snapshotId", "Gerado em UTC: $([DateTimeOffset]::UtcNow.ToString('O'))") |
    Set-Content -LiteralPath (Join-Path $soaTarget 'DELIVERY-METADATA.txt') -Encoding utf8

function Get-CodeManifest([string]$root) {
    $result = @{}
    Get-ChildItem -LiteralPath $root -Recurse -File | ForEach-Object {
        $relative = [System.IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/')
        if ($relative.StartsWith('src/') -or $relative.StartsWith('tests/') -or
            $relative -eq 'AJA.sln' -or $relative -eq 'global.json' -or
            $relative -eq 'NuGet.Config' -or $relative -eq '.config/dotnet-tools.json') {
            $result[$relative] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    }
    return $result
}

$left = Get-CodeManifest $csharpTarget
$right = Get-CodeManifest $soaTarget
if ($left.Count -ne $right.Count) { throw 'As entregas possuem quantidades diferentes de arquivos de código.' }
foreach ($key in $left.Keys) {
    if (-not $right.ContainsKey($key) -or $left[$key] -ne $right[$key]) {
        throw "Código divergente entre as entregas: $key"
    }
}

function Invoke-Checked([string]$commandDescription, [scriptblock]$command) {
    & $command
    if ($LASTEXITCODE -ne 0) { throw "Falha em $commandDescription (código $LASTEXITCODE)." }
}

function Remove-BuildArtifacts([string]$target) {
    $targetPrefix = [System.IO.Path]::GetFullPath($target).TrimEnd('\') + '\'
    Get-ChildItem -LiteralPath $target -Recurse -Directory | Where-Object { $_.Name -in @('bin', 'obj', 'TestResults', '.data-protection-keys') } |
        Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
            $full = [System.IO.Path]::GetFullPath($_.FullName)
            if ($full.StartsWith($targetPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                Remove-Item -LiteralPath $full -Recurse -Force
            }
        }
}

if (-not $SkipVerification) {
    foreach ($target in $allowedTargets) {
        Push-Location $target
        try {
            Invoke-Checked 'restore' { dotnet restore AJA.sln --configfile NuGet.Config }
            Invoke-Checked 'tool restore' { dotnet tool restore --configfile NuGet.Config }
            Invoke-Checked 'build' { dotnet build AJA.sln --no-restore }
            Invoke-Checked 'tests' { dotnet test AJA.sln --no-build --logger 'console;verbosity=minimal' }
            $verificationDb = Join-Path $target 'verification.db'
            $previousDesignConnection = $env:AJA_DESIGN_CONNECTION
            try {
                $env:AJA_DESIGN_CONNECTION = "Data Source=$verificationDb"
                Invoke-Checked 'database update' {
                    dotnet ef database update --project src/Aja.Api --startup-project src/Aja.Api --no-build
                }
            }
            finally {
                $env:AJA_DESIGN_CONNECTION = $previousDesignConnection
                foreach ($dbFile in @($verificationDb, "$verificationDb-shm", "$verificationDb-wal")) {
                    if (Test-Path -LiteralPath $dbFile) { Remove-Item -LiteralPath $dbFile -Force }
                }
            }
        }
        finally {
            Pop-Location
            Remove-BuildArtifacts $target
        }
    }
}

Write-Output "Entregas geradas com código idêntico. Snapshot: $snapshotId"
Write-Output "C#:  $csharpTarget"
Write-Output "SOA: $soaTarget"
