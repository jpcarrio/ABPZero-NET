Set-Location "..\..\isu-core-dotnet\src\ISUCore.EntityFrameworkCore"

Write-Host -ForegroundColor Magenta -Object "Drop Database"

dotnet ef database drop -f
