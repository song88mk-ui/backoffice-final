FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /source

ADD Backoffice.WebSocket.API Backoffice.WebSocket.API
ADD Backoffice.Shared Backoffice.Shared

WORKDIR /source/Backoffice.WebSocket.API

RUN dotnet restore

RUN dotnet build --configuration Release

RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:7.0
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Backoffice.WebSocket.API/publish .
CMD ["/usr/bin/dotnet", "Backoffice.WebSocket.API.dll"]
