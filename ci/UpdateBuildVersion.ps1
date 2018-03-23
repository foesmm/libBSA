$token = $env:API_TOKEN

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-type" = "application/json"
}

$apiURL = "https://ci.appveyor.com/api/projects/$env:APPVEYOR_ACCOUNT_NAME/$env:APPVEYOR_PROJECT_SLUG"
$history = Invoke-RestMethod -Uri "$apiURL/history?recordsNumber=2" -Headers $headers -Method Get

$gitDescribe = git describe --abbrev=0
$version = $gitDescribe -replace "^v", ""

$previousVersion = [Version]"0.0.0"
$currentVersion = [Version]$version
if ($history.builds.Count -eq 2)
{
    $previous = $history.builds[1].version
    $previousVersion = [Version]$previous.Substring(0,$previous.LastIndexOf("."))
}

Write-Host "Previous version: $previousVersion"
Write-Host "Current version: $currentVersion"

if ($currentVersion -ne $previousVersion)
{
    Write-Host "Version has been changed, resetting build number and version format"
    $versionFormat = "$currentVersion.{build}"

    $s = Invoke-RestMethod -Uri "https://ci.appveyor.com/api/projects/$env:APPVEYOR_ACCOUNT_NAME/$env:APPVEYOR_PROJECT_SLUG/settings" -Headers $headers  -Method Get
    $s.settings.versionFormat = $versionFormat
    $s.settings.nextBuildNumber = "1"
    Invoke-RestMethod -Uri 'https://ci.appveyor.com/api/projects' -Headers $headers  -Body ($s.settings | ConvertTo-Json -Depth 10) -Method Put

    $env:APPVEYOR_BUILD_NUMBER = "0"
    Update-AppveyorBuild -Version "$currentVersion.$env:APPVEYOR_BUILD_NUMBER"
}
