<#
  Writes src\Kairos\app.ico (the exe, Explorer and Start menu icon) from the drawing the app itself uses for its
  window (src\Kairos\App\AppIconArt.cs): the warm plate, and the ring and dot in the theme's rose.
  Build the solution first, then build again so the exe takes the new icon.
#>
param([string]$Out = (Join-Path $PSScriptRoot '..\src\Kairos\app.ico'))
$exe = Join-Path $PSScriptRoot '..\src\Kairos\bin\x64\Debug\net9.0-windows\win-x64\Kairos.exe'
if (-not (Test-Path $exe)) { throw "Build Kairos.sln first: $exe not found" }
& $exe --write-icon ([System.IO.Path]::GetFullPath($Out)) | Out-String | Write-Output
