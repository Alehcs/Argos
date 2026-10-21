<#
  Writes src\Argos\app.ico (the exe, Explorer and Start menu icon) from the drawing the app itself uses for its
  window (src\Argos\App\AppIconArt.cs): the warm plate, and the ring and dot in the theme's rose.
  Build the solution first, then build again so the exe takes the new icon.
#>
param([string]$Out = (Join-Path $PSScriptRoot '..\src\Argos\app.ico'))
$exe = Join-Path $PSScriptRoot '..\src\Argos\bin\x64\Debug\net10.0-windows\win-x64\Argos.exe'
if (-not (Test-Path $exe)) { throw "Build Argos.sln first: $exe not found" }
& $exe --write-icon ([System.IO.Path]::GetFullPath($Out)) | Out-String | Write-Output
