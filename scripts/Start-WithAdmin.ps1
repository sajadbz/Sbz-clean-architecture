#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

$userName = Read-Host 'Administrator username (press Enter for admin)'
if ([string]::IsNullOrWhiteSpace($userName)) {
    $userName = 'admin'
}
$userName = $userName.Trim()
if ($userName.Length -gt 256 -or $userName -notmatch '^[a-zA-Z0-9_.@+-]+$') {
    throw 'Enter a username using letters, digits, or . _ @ + -'
}

do {
    $securePassword = Read-Host 'Choose a NEW administrator password' -AsSecureString
    try {
        $password = [System.Net.NetworkCredential]::new('', $securePassword).Password
    } finally {
        $securePassword.Dispose()
    }

    $valid = $password.Length -ge 12 -and $password.Length -le 128 -and
        $password -cmatch '[A-Z]' -and $password -cmatch '[a-z]' -and
        $password -match '[0-9]' -and $password -match '[^a-zA-Z0-9]'

    if (!$valid) {
        Write-Host 'Use 12 to 128 characters with uppercase and lowercase letters, a number, and a symbol.'
    }
} until ($valid)

$previousUserName = $env:AdminBootstrap__UserName
$previousPassword = $env:AdminBootstrap__Password
try {
    $env:AdminBootstrap__UserName = $userName
    $env:AdminBootstrap__Password = $password
    Remove-Variable password

    $project = Join-Path $PSScriptRoot '../Sbz.WebUI/Sbz.WebUI.csproj'
    Write-Host "After the app starts, open https://localhost:5001/admin and sign in as $userName."
    & dotnet run --project $project --launch-profile Sbz.WebUI
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
} finally {
    $env:AdminBootstrap__UserName = $previousUserName
    $env:AdminBootstrap__Password = $previousPassword
}
