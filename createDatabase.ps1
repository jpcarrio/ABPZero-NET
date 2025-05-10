Set-Location "..\..\..\src\ABPNET.EntityFrameworkCore"

Write-Host -ForegroundColor Magenta -Object "Create Database"

dotnet ef database update


