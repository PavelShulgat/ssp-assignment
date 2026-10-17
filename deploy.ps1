param(
    [string]$ResourceGroup = "rg-weather-images",
    [string]$Location = "swedencentral",
    [string]$ImageTag = (Get-Date -Format "yyyyMMddHHmmss")
)

$ErrorActionPreference = "Stop"

# The Docker build compiles the projects itself, but the assignment
# requires publishing with the dotnet CLI, so we do that first.
Write-Host "=== 1. Publishing .NET projects ===" -ForegroundColor Cyan
dotnet publish src/Api/Api.csproj -c Release -o publish/api
dotnet publish src/Worker/Worker.csproj -c Release -o publish/worker

Write-Host "=== 2. Creating resource group ===" -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location --output none

# First pass: storage, queues, registry, logs, environment.
# The container apps are skipped because their images don't exist yet.
Write-Host "=== 3. Deploying infrastructure ===" -ForegroundColor Cyan
$infra = az deployment group create `
    --resource-group $ResourceGroup `
    --template-file infra/main.bicep `
    --parameters imageTag=$ImageTag deployApps=false `
    --query properties.outputs `
    --output json | ConvertFrom-Json

$registryName = $infra.registryName.value
$registryServer = $infra.registryServer.value

Write-Host "Registry: $registryServer" -ForegroundColor Green

Write-Host "=== 4. Building and pushing images ===" -ForegroundColor Cyan
az acr login --name $registryName

docker build -f src/Api/Dockerfile -t "$registryServer/weather-api:$ImageTag" src
docker build -f src/Worker/Dockerfile -t "$registryServer/weather-worker:$ImageTag" src

docker push "$registryServer/weather-api:$ImageTag"
docker push "$registryServer/weather-worker:$ImageTag"

# Second pass: now the images exist, create or update the container apps.
Write-Host "=== 5. Deploying container apps ===" -ForegroundColor Cyan
$apps = az deployment group create `
    --resource-group $ResourceGroup `
    --template-file infra/main.bicep `
    --parameters imageTag=$ImageTag deployApps=true `
    --query properties.outputs `
    --output json | ConvertFrom-Json

Write-Host ""
Write-Host "Deployed: $($apps.apiUrl.value)" -ForegroundColor Green