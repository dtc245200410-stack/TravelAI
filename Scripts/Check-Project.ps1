$ErrorActionPreference = "Stop"
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "Restore thất bại." }
    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build thất bại." }
    dotnet test Tests/TravelAI.Tests.csproj
    if ($LASTEXITCODE -ne 0) { throw "Kiểm thử thất bại." }
} finally { Pop-Location }
