param(
   [Parameter(Mandatory=$true)]
   [ValidateNotNullOrEmpty()]
   [string]$branch
)

function EnsureFileExists
{
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$FileName
    )

    if (-not (Test-Path $FileName))
    {
      $TemplateFileName = Join-Path -Path (get-location) -ChildPath "\appsettings.json.template"
      Copy-Item $TemplateFileName -Destination $FileName
    }
}

function UpdateSettings
{
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$FileName,
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]$branch
    )

    $settings = Get-Content $FileName -raw | ConvertFrom-Json
    $connectionStrings = $settings.ConnectionStrings.Default

    Write-Host -Object "Old value: $connectionStrings"

    $values = $connectionStrings -split ';'
    $values[1] = 'Database=' + $branch
    $settings.ConnectionStrings.Default = $values -join ';'
    $settings | ConvertTo-Json -depth 32| set-content $FileName

    $connectionStrings = $settings.ConnectionStrings.Default
    Write-Host -Object "New value: $connectionStrings"
}

try
{
    $OldLocation = Get-Location

    Set-Location "..\..\isu-core-dotnet\src\ISUCore.Web.Host"
    $FileName = Join-Path -Path (get-location) -ChildPath "\appsettings.json"

    Write-Host -ForegroundColor Magenta -Object "Updating connection string into the appsettings.json file"
    
    EnsureFileExists -FileName $FileName

    UpdateSettings -FileName $FileName -branch $branch
    
    Write-Host -ForegroundColor Green -Object "Settings file updated successfully!"

    Set-Location $OldLocation
}
catch
{
    Write-Error "$_ $($_.InvocationInfo.PositionMessage)" #Includes stack trace of exception
}
Pop-Location
