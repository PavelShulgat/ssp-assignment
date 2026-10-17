# Weather Images

An Azure Container Apps pipeline that generates images stamped with live Dutch weather data. One HTTP request starts a background job that fans out to one image per weather station and stores the results in Blob Storage.

Built for the Inholland Cloud Computing (CCD) assessment.

**Live API:** https://weatherimg-api.agreeablepond-18b8b7df.swedencentral.azurecontainerapps.io

## How it works

`POST /images` returns a job id immediately and puts one message on the **job-start** queue.

`JobStarterWorker` picks it up, fetches the current measurements from the [Buienradar feed](https://data.buienradar.nl/2.0/feed/json), and writes one message per weather station (~40) to the **image-tasks** queue. That is the fan-out.

`ImageWorker` drains that queue. For each station it fetches a photo from picsum.photos, draws the station name, temperature and weather description onto it with SkiaSharp, and uploads the result to Blob Storage as `images/{jobId}/{stationId}.png`.

`GET /images/{id}` lists the blobs under that job's prefix and returns their URLs.

Two queues are used deliberately: the first carries a single "start job" message, the second carries one message per station, which is what allows the image work to run in parallel.

## API

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/images` | Starts a job. Returns `202` with a job id. |
| GET | `/images/{id}` | Returns the generated image URLs for that job. |
| GET | `/health` | Liveness check. |

A job takes roughly 30–60 seconds. Calling `GET /images/{id}` before then returns however many images have finished so far.

See [`requests.http`](requests.http) for runnable examples against both the local and deployed API.

## Layout

```
src/Api       Minimal API — receives requests, reads results
src/Worker    Two background services — the fan-out and the image generation
src/Shared    Queue and blob names, plus the message contracts both sides use
infra         Bicep template for all Azure resources
deploy.ps1    Publish, provision, build, push, deploy
```

## Running locally

Needs the .NET 8 SDK and [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) standing in for Azure Storage.

Start Azurite (blob and queue services), then in two terminals:

```
dotnet run --project src/Api
dotnet run --project src/Worker
```

Set `@host = {{local}}` in `requests.http` and send the requests from there.

The storage connection string comes from `ConnectionStrings:Storage` and falls back to `UseDevelopmentStorage=true`, so no configuration is needed locally.

## Deploying

Needs the Azure CLI (logged in), Docker, and PowerShell.

```
.\deploy.ps1
```

The script publishes both projects with the dotnet CLI, provisions the infrastructure from `infra/main.bicep`, builds and pushes the container images to Azure Container Registry, then deploys the container apps and prints the resulting URL.

Bicep runs twice: once without the container apps so the registry exists to push to, then again to create the apps now that their images are available.

Resources are deployed to **swedencentral** — the subscription policy restricts which regions are allowed.

## Notes

The images are drawn with SkiaSharp rather than ImageSharp.Drawing, which now requires a paid Six Labors licence key at build time.

Some weather stations report wind but no temperature, so temperature is nullable throughout and those images read "no reading" rather than claiming 0 °C.

Queue messages are deleted only after the work succeeds. Delivery is at-least-once, so a message can be redelivered after a crash; blob names are deterministic, so a retry overwrites the same file rather than creating a duplicate.
