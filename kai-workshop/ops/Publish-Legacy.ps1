param([string]$Destination="C:\kai-workshop\legacy",[int]$Port=8081,[string]$BindAddress="127.0.0.1")
$ErrorActionPreference="Stop"
Import-Module WebAdministration
$Root=Split-Path $PSScriptRoot -Parent
if(Test-Path "IIS:\Sites\KaiWorkshopLegacy"){throw "KaiWorkshopLegacy already exists. Review it before replacing anything."}
New-Item -ItemType Directory -Force -Path "$Destination\bin" | Out-Null
dotnet build "$Root\src\RailBooking.Legacy" -c Release -o "$Destination\bin"
if($LASTEXITCODE -ne 0){throw "Legacy build failed"}
Copy-Item "$Root\src\RailBooking.Legacy\Global.asax" $Destination
Copy-Item "$Root\src\RailBooking.Legacy\Web.config" $Destination
New-WebAppPool -Name KaiWorkshopLegacy
Set-ItemProperty "IIS:\AppPools\KaiWorkshopLegacy" managedRuntimeVersion v4.0
New-Website -Name KaiWorkshopLegacy -Port $Port -IPAddress $BindAddress -PhysicalPath $Destination -ApplicationPool KaiWorkshopLegacy
Write-Host "Set the three appSettings in the deployed Web.config locally. Do not upload secrets. No firewall rule was added."
