FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

ADD Backoffice.WebSocket.Service Backoffice.WebSocket.Service
ADD Backoffice.Shared Backoffice.Shared

WORKDIR /source/Backoffice.WebSocket.Service

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
  -o publish && \
  cd publish && \
  rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /source/Backoffice.WebSocket.Service/publish .
CMD ["/usr/bin/dotnet", "Backoffice.WebSocket.Service.dll"]
