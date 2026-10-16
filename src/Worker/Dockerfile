FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY Worker/Worker.csproj Worker/
RUN dotnet restore Worker/Worker.csproj

COPY . .
RUN dotnet publish Worker/Worker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
RUN apt-get update && apt-get install -y --no-install-recommends \
    fontconfig fonts-dejavu-core && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Worker.dll"]