param([switch]$History)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding
Set-Location (Split-Path $PSScriptRoot -Parent)
# Print locations only, never matching credentials.
$pattern = 'AIza[0-9A-Za-z_-]{30,}|gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----|(?im)^[ \t]*DeveloperSecretKey:[ \t]*["'']?[A-Za-z0-9_-]{10,}'
$findings = 0
$paths = @(git -c core.quotepath=false ls-files --cached --others --exclude-standard | Sort-Object -Unique)
if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed' }
foreach ($path in $paths) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $bytes = [IO.File]::ReadAllBytes((Join-Path (Get-Location) $path))
    # Binary files are not covered by this text scan.
    if ($bytes -contains 0) { continue }
    $content = [Text.Encoding]::UTF8.GetString($bytes)
    foreach ($hit in [regex]::Matches($content, $pattern)) {
        $line = 1 + ([regex]::Matches($content.Substring(0, $hit.Index), "`n")).Count
        Write-Output ("RISK {0}:{1} (value hidden)" -f $path, $line)
        $findings++
    }
}
if ($History) {
    $historyPattern = 'AIza[0-9A-Za-z_-]{30,}|gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----'
    foreach ($revision in @(git rev-list --all)) {
        $locations = @(git grep -I -l -E $historyPattern $revision --)
        $grepExit = $LASTEXITCODE
        if ($grepExit -gt 1) { throw 'History scan failed' }
        foreach ($location in $locations) { Write-Output "HISTORY RISK $location (value hidden)"; $findings++ }
    }
}
if ($findings -gt 0) { Write-Output "$findings findings. Keep the repository private until resolved."; exit 1 }
Write-Output 'PASS: no matching credentials in Git-eligible text files. This is a pattern scan, not a security guarantee.'
exit 0
