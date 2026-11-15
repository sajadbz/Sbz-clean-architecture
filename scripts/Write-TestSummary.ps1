#Requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$ResultsDirectory,
    [string]$SummaryPath = $env:GITHUB_STEP_SUMMARY
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
