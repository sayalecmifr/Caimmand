$ErrorActionPreference = 'Stop'

$ContainerName = 'caimmand-postgres-poc'
$PostgresUser = $env:POSTGRES_USER
if ([string]::IsNullOrWhiteSpace($PostgresUser)) { $PostgresUser = 'postgres' }
$PostgresPassword = $env:POSTGRES_PASSWORD
if ([string]::IsNullOrWhiteSpace($PostgresPassword)) { $PostgresPassword = 'postgres' }
$PostgresDb = $env:POSTGRES_DB
if ([string]::IsNullOrWhiteSpace($PostgresDb)) { $PostgresDb = 'caimmand' }
$PostgresPort = $env:POSTGRES_PORT
if ([string]::IsNullOrWhiteSpace($PostgresPort)) { $PostgresPort = '5432' }

Write-Host "Caimmand PoC - Levantando PostgreSQL" -ForegroundColor Cyan
Write-Host "  Contenedor  : $ContainerName"
Write-Host "  Usuario     : $PostgresUser"
Write-Host "  Base        : $PostgresDb"
Write-Host "  Puerto host : $PostgresPort"

$existing = docker ps -a --filter "name=^/$ContainerName$" --format '{{.Names}}' 2>$null
if ($existing -eq $ContainerName)
{
    $status = docker inspect -f '{{.State.Running}}' $ContainerName 2>$null
    if ($status -eq 'true')
    {
        Write-Host "El contenedor ya esta corriendo. No se hace nada." -ForegroundColor Green
        return
    }
    else
    {
        Write-Host "El contenedor existe pero esta parado. Arrancando..." -ForegroundColor Yellow
        docker start $ContainerName | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "No se pudo arrancar el contenedor $ContainerName." }
        Write-Host "Contenedor arrancado." -ForegroundColor Green
        return
    }
}

Write-Host "Creando contenedor nuevo..." -ForegroundColor Yellow
docker run -d `
    -p "${PostgresPort}:5432" `
    --name $ContainerName `
    -e "POSTGRES_USER=$PostgresUser" `
    -e "POSTGRES_PASSWORD=$PostgresPassword" `
    -e "POSTGRES_DB=$PostgresDb" `
    postgres:17 | Out-Null

if ($LASTEXITCODE -ne 0) { throw "No se pudo crear el contenedor $ContainerName." }

Write-Host "Contenedor creado. Esperando a que PostgreSQL este listo..." -ForegroundColor Yellow
$ready = $false
for ($i = 0; $i -lt 30; $i++)
{
    $probe = docker exec $ContainerName pg_isready -U $PostgresUser 2>$null
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    Start-Sleep -Seconds 1
}

if (-not $ready) { throw "PostgreSQL no respondio en 30 segundos." }
Write-Host "PostgreSQL listo en el contenedor $ContainerName." -ForegroundColor Green
Write-Host "Cadena de conexion: Host=localhost;Port=$PostgresPort;Database=$PostgresDb;Username=$PostgresUser;Password=$PostgresPassword" -ForegroundColor Cyan