FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

ADD Backoffice.API Backoffice.API
ADD Backoffice.Shared Backoffice.Shared

WORKDIR /source/Backoffice.API

ARG NUGET_PASSWORD

RUN dotnet nuget add source \
  --username FlyingCockatoo \
  --password ${NUGET_PASSWORD} \
  --store-password-in-clear-text \
  --name github "https://nuget.pkg.github.com/FlyingCockatoo/index.json"

RUN dotnet restore
RUN dotnet build \
  --no-restore \
  --configuration Release
RUN dotnet publish \
  --no-restore \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false

RUN cd /app/publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .
CMD ["dotnet", "BackOffice.API.dll"]
