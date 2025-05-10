Set-Location "..\..\isu-core-dotnet\src\ISUCore.EntityFrameworkCore"

Write-Host -ForegroundColor Magenta -Object "Create Database"

dotnet ef database update
