#Requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$ResultsDirectory,
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY,
    [string]$DesignPath = (Join-Path $PSScriptRoot '../docs/functional-design.md'),
    [string]$ScenarioPath = (Join-Path $PSScriptRoot '../docs/acceptance/design-and-admin.feature'),
    [string]$OwnerPickerOutcome,
    [string]$ReportingOutcome,
    [switch]$RequireCoverage
)

$ErrorActionPreference = 'Stop'
if (!$SummaryPath) {
    throw 'Specify SummaryPath or set GITHUB_STEP_SUMMARY.'
}

$files = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter '*.trx' -File -ErrorAction SilentlyContinue)
$results = foreach ($file in $files) {
    $document = [xml](Get-Content -LiteralPath $file.FullName -Raw)
    $namespaces = [System.Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespaces.AddNamespace('t', $document.DocumentElement.NamespaceURI)
    $categories = @{}
    foreach ($definition in $document.SelectNodes('//t:TestDefinitions/t:UnitTest', $namespaces)) {
        $tags = $definition.SelectNodes('t:TestCategory/t:TestCategoryItem', $namespaces) |
            ForEach-Object { $_.GetAttribute('TestCategory') }
        $categories[$definition.GetAttribute('id')] = $tags -join ', '
    }

    foreach ($result in $document.SelectNodes('//t:Results/t:UnitTestResult', $namespaces)) {
        $name = $result.GetAttribute('testName')
        $category = $categories[$result.GetAttribute('testId')]
        $parts = $name -split ' \| ', 3
        if ($parts.Count -eq 3) {
            $category = $parts[1]
        }
        if (!$category) {
            $category = 'Test suite'
        }

        [pscustomobject]@{
            Test = $name
            Category = $category
            Outcome = $result.GetAttribute('outcome')
            Duration = $result.GetAttribute('duration')
            Started = $result.GetAttribute('startTime')
            Finished = $result.GetAttribute('endTime')
        }
    }
}

$results = @($results | Where-Object { $null -ne $_ })
$lines = [System.Collections.Generic.List[string]]::new()
$requirements = @(Get-Content -LiteralPath $DesignPath | ForEach-Object {
    if ($_ -match '^## (FD-[0-9]+) (.+)$') {
        [pscustomobject]@{ Id = $Matches[1]; Title = $Matches[2] }
    }
})
$feature = Get-Content -LiteralPath $ScenarioPath -Raw
$scenarioIds = @([regex]::Matches($feature, '(?m)^\s*@(FD-[0-9]+)\b') |
    ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
if (!$requirements.Count -or !$scenarioIds.Count -or
    @($requirements.Id | Sort-Object -Unique).Count -ne $requirements.Count -or
    (Compare-Object -ReferenceObject @($requirements.Id) -DifferenceObject $scenarioIds)) {
    throw 'Functional Design IDs must match the requirement tags in the BDD scenarios.'
}

$lines.Add('## Functional Design verification')
$lines.Add('')
$source = 'https://github.com/sajadbz/Sbz-clean-architecture/blob/master'
if ($env:GITHUB_SERVER_URL -and $env:GITHUB_REPOSITORY -and $env:GITHUB_SHA) {
    $source = "$env:GITHUB_SERVER_URL/$env:GITHUB_REPOSITORY/blob/$env:GITHUB_SHA"
}
$lines.Add("[Requirements]($source/docs/functional-design.md) · [BDD scenarios]($source/docs/acceptance/design-and-admin.feature)")
$lines.Add('')
$lines.Add('| ID | Requirement | Result | Evidence |')
$lines.Add('| --- | --- | --- | --- |')

$unverified = foreach ($requirement in $requirements) {
    $id = $requirement.Id
    $matchingTests = @($results | Where-Object { $_.Test.StartsWith("$id |", [StringComparison]::Ordinal) })
    $outcomes = @($matchingTests | ForEach-Object { $_.Outcome.ToLowerInvariant() })
    $evidence = @()
    if ($matchingTests.Count) {
        $evidence += "$($matchingTests.Count) .NET test(s)"
    }

    $absent = $id -ne 'FD-06' -and !$matchingTests.Count
    if ($id -eq 'FD-03') {
        $evidence += 'JavaScript owner picker'
        $outcomes += $OwnerPickerOutcome
        $absent = $absent -or !$OwnerPickerOutcome
    }
    if ($id -eq 'FD-06') {
        $evidence += 'Report verification'
        $outcomes += $ReportingOutcome
        $absent = !$ReportingOutcome
    }

    if ($outcomes | Where-Object { $_ -in 'failed', 'failure', 'error', 'timeout', 'aborted' }) {
        $status = 'Failed'
    } elseif ($absent -or ($outcomes | Where-Object { $_ -notin 'passed', 'success' })) {
        $status = 'Not verified'
    } else {
        $status = 'Passed'
    }

    $title = $requirement.Title.Replace('|', '&#124;')
    $lines.Add("| $id | $title | $status | $($evidence -join ', ') |")
    if ($status -ne 'Passed') { $id }
}
$lines.Add('')

$lines.Add('## Automated test results')
$lines.Add('')
if (!$results.Count) {
    $lines.Add('No test results were produced. See the build and test steps for details.')
} else {
    $counts = $results | Group-Object Outcome | Sort-Object Name
    $outcomes = ($counts | ForEach-Object { '**{0}** {1}' -f $_.Count, $_.Name.ToLowerInvariant() }) -join ' · '
    $lines.Add("**$($results.Count) total** · $outcomes")
    $lines.Add('')
    $lines.Add('The raw TRX file is available in the **test-results** artifact.')
    $lines.Add('')
    $lines.Add('<details>')
    $lines.Add('<summary>Optional test details</summary>')
    $lines.Add('')
    $lines.Add('<table><thead><tr><th>Scenario / test</th><th>Category</th><th>Outcome</th><th>Duration</th><th>Started</th><th>Finished</th></tr></thead><tbody>')
    foreach ($test in ($results | Sort-Object Category, Test)) {
        $cells = foreach ($value in @($test.Test, $test.Category, $test.Outcome, $test.Duration, $test.Started, $test.Finished)) {
            '<td>{0}</td>' -f [System.Net.WebUtility]::HtmlEncode(($value ?? ''))
        }
        $lines.Add('<tr>' + ($cells -join '') + '</tr>')
    }
    $lines.Add('</tbody></table>')
    $lines.Add('</details>')
}
[IO.File]::AppendAllText($SummaryPath, ($lines -join "`n") + "`n")

if ($RequireCoverage -and @($unverified | Where-Object { $_ }).Count) {
    throw "Functional Design requirements did not pass verification: $($unverified -join ', ')."
}
