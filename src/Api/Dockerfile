FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Shared/Shared.csproj Shared/
COPY Api/Api.csproj Api/
RUN dotnet restore Api/Api.csproj

COPY . .
RUN dotnet publish Api/Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]