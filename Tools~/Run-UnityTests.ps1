param(
    [Parameter(Mandatory = $true)] [string] $UnityPath,
    [ValidateSet('EditMode', 'StandaloneWindows64', 'Android')] [string] $Platform = 'EditMode',
    [string] $ProjectPath,
    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $PSScriptRoot '..\UnityProject~'
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot '..\UnityProject~\TestResults'
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity executable was not found: $UnityPath"
}
$project = [IO.Path]::GetFullPath($ProjectPath)
$output = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force -Path $output | Out-Null

$result = Join-Path $output "$Platform.xml"
$log = Join-Path $output "$Platform.log"
$arguments = @(
    '-batchmode', '-nographics', '-projectPath', $project,
    '-runTests', '-testPlatform', $Platform,
    '-testResults', $result, '-logFile', $log
)

if ($Platform -ne 'EditMode') {
    & $UnityPath -batchmode -nographics -projectPath $project `
        -executeMethod FerryKit.CI.PlayerConfiguration.Configure -quit `
        -logFile (Join-Path $output "$Platform-configure.log")
    if ($LASTEXITCODE -ne 0) { throw "Player configuration failed with exit code $LASTEXITCODE." }

    $buildName = if ($Platform -eq 'Android') { 'FerryKitTests.apk' } else { 'FerryKitTests.exe' }
    $settings = Join-Path $project "ProjectSettings\TestSettings-$Platform.json"
    $arguments += @('-testSettingsFile', $settings, '-buildPlayerPath', (Join-Path $output $buildName))
}

if (Test-Path -LiteralPath $result) {
    Remove-Item -LiteralPath $result -Force
}
& $UnityPath @arguments
if ($LASTEXITCODE -ne 0) { throw "Unity $Platform tests failed with exit code $LASTEXITCODE. See $log" }
if (-not (Test-Path -LiteralPath $result)) { throw "Unity did not create the expected result file: $result" }

try {
    $resultDocument = New-Object System.Xml.XmlDocument
    $resultDocument.Load($result)
}
catch {
    throw "Unity created an invalid test result file: $result. $($_.Exception.Message)"
}

$testRun = $resultDocument.SelectSingleNode('/test-run')
if ($null -eq $testRun) {
    throw "Unity test result does not contain an NUnit test-run element: $result"
}

$total = 0
$failed = 0
$inconclusive = 0
if (-not [int]::TryParse($testRun.GetAttribute('total'), [ref] $total) -or
    -not [int]::TryParse($testRun.GetAttribute('failed'), [ref] $failed) -or
    -not [int]::TryParse($testRun.GetAttribute('inconclusive'), [ref] $inconclusive)) {
    throw "Unity test result contains invalid NUnit counters: $result"
}
if ($total -le 0) {
    throw "Unity did not run any $Platform tests. See $log"
}
if ($testRun.GetAttribute('result') -ne 'Passed' -or $failed -ne 0 -or $inconclusive -ne 0) {
    throw "Unity $Platform tests did not pass (result=$($testRun.GetAttribute('result')), failed=$failed, inconclusive=$inconclusive). See $log"
}
