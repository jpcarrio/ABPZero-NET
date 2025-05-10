Set-Location "..\..\isu-core-dotnet"

Write-Host -ForegroundColor Magenta -Object "Starting API..."

dotnet run --project src\ISUCore.Web.Host
