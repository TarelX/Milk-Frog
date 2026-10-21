param([string]$Configuration='Release')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$version='0.1.3'
$environmentNames=@('DOTNET_CLI_HOME','NUGET_PACKAGES','MSBuildEnableWorkloadResolver','DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_SKIP_FIRST_TIME_EXPERIENCE')
$previousEnvironment=@{}
foreach($name in $environmentNames){$previousEnvironment[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
  $env:DOTNET_CLI_HOME=Join-Path $projectRoot '.dotnet-home'
  $env:NUGET_PACKAGES=Join-Path $projectRoot '.nuget\packages'
  $env:MSBuildEnableWorkloadResolver='false'
  $env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
  $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
  & (Join-Path $PSScriptRoot 'Make-AppIcon.ps1') -ProjectRoot $projectRoot
  $artifacts=Join-Path $projectRoot "artifacts\dotnet-$version"
  dotnet build (Join-Path $projectRoot 'tests\MilkFrog.Tests\MilkFrog.Tests.csproj') -c $Configuration --artifacts-path $artifacts -p:RestoreIgnoreFailedSources=true --nologo
  if($LASTEXITCODE -ne 0){throw 'Build failed'}
  dotnet (Join-Path $artifacts "bin\MilkFrog.Tests\$($Configuration.ToLowerInvariant())\MilkFrog.Tests.dll") $projectRoot
  if($LASTEXITCODE -ne 0){throw 'Core checks failed'}
  $output=Join-Path $projectRoot "dist\MilkFrog-$version"
  dotnet publish (Join-Path $projectRoot 'src\MilkFrog.App\MilkFrog.App.csproj') -c $Configuration --artifacts-path $artifacts -r win-x64 --self-contained false -o $output --nologo -p:RestoreIgnoreFailedSources=true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
  if($LASTEXITCODE -ne 0){throw 'Publish failed'}
  Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $output 'README.md') -Force
  $package=Join-Path $projectRoot "dist\MilkFrog-Windows-$version.zip"
  Compress-Archive -LiteralPath $output -DestinationPath $package -Force
  $hash=Get-FileHash -Algorithm SHA256 -LiteralPath $package
  $checksum=Join-Path $projectRoot 'dist\SHA256SUMS.txt'
  "$($hash.Hash) *$([System.IO.Path]::GetFileName($package))" | Set-Content -LiteralPath $checksum -Encoding ascii
  Get-Item -LiteralPath (Join-Path $output 'MilkFrog.exe'),$package,$checksum | Select-Object FullName,Length
} finally {
  foreach($name in $environmentNames){[Environment]::SetEnvironmentVariable($name,$previousEnvironment[$name],'Process')}
}
