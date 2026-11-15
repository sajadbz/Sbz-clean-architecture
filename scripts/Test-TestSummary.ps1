#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$directory = Join-Path ([IO.Path]::GetTempPath()) ('sbz-test-summary-' + [guid]::NewGuid())
New-Item -ItemType Directory -Path $directory | Out-Null
$trx = Join-Path $directory 'fixture.trx'
$summary = Join-Path $directory 'summary.md'
$missing = Join-Path $directory 'missing.md'
$mismatch = Join-Path $directory 'mismatch.feature'

try {
    $results = 1..5 | ForEach-Object {
        $id = 'FD-{0:00}' -f $_
        "<UnitTestResult testId='$id' testName='$id | Acceptance | Example &amp; result' outcome='Passed' duration='00:00:01' startTime='2026-11-15T10:00:00Z' endTime='2026-11-15T10:00:01Z' />"
    }
    $contents = "<TestRun xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010'><Results>$($results -join '')</Results></TestRun>"
    [IO.File]::WriteAllText($trx, $contents)

    & "$PSScriptRoot/Write-TestSummary.ps1" -ResultsDirectory $directory -SummaryPath $summary `
        -OwnerPickerOutcome success -ReportingOutcome success -RequireCoverage
    $output = Get-Content -LiteralPath $summary -Raw
    foreach ($text in 'Functional Design verification', 'FD-01', 'FD-06', 'Optional test details',
        '2026-11-15T10:00:01Z', 'Example &amp; result', 'JavaScript owner picker', 'Report verification') {
        if (!$output.Contains($text)) {
            throw "Summary is missing expected content: $text"
        }
    }
    if (@([regex]::Matches($output, '\| Passed \|')).Count -ne 6) {
        throw 'Expected six passed requirement rows.'
    }

    [IO.File]::WriteAllText($trx, $contents.Replace($results[2], ''))
    $reportedMissing = $false
    try {
        & "$PSScriptRoot/Write-TestSummary.ps1" -ResultsDirectory $directory -SummaryPath $missing `
            -OwnerPickerOutcome success -ReportingOutcome success -RequireCoverage
    } catch {
        if ($_.Exception.Message -notmatch 'FD-03') { throw }
        $reportedMissing = (Get-Content -LiteralPath $missing -Raw) -match '\| FD-03 \|.*\| Not verified \|'
    }
    if (!$reportedMissing) {
        throw 'Missing .NET evidence should fail even though the JavaScript check passed.'
    }

    $featurePath = Join-Path $PSScriptRoot '../docs/acceptance/design-and-admin.feature'
    [IO.File]::WriteAllText($mismatch, ([IO.File]::ReadAllText($featurePath).Replace('@FD-06', '@FD-99')))
    $rejectedMismatch = $false
    try {
        & "$PSScriptRoot/Write-TestSummary.ps1" -ResultsDirectory $directory -SummaryPath $missing `
            -ScenarioPath $mismatch
    } catch {
        if ($_.Exception.Message -notmatch 'must match') { throw }
        $rejectedMismatch = $true
    }
    if (!$rejectedMismatch) {
        throw 'Mismatched Functional Design and Gherkin IDs should fail.'
    }

    Write-Output 'FD-06 passed: summary, test metadata, and missing evidence checks.'
} finally {
    foreach ($path in $trx, $summary, $missing, $mismatch) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    Remove-Item -LiteralPath $directory
}
