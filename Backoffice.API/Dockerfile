FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /source

ADD Backoffice.API Backoffice.API
ADD Backoffice.Shared Backoffice.Shared

WORKDIR /source/Backoffice.API

RUN dotnet restore --verbosity normal

RUN dotnet build \
  --no-restore \
  --configuration Release

RUN dotnet publish \
  --no-restore \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false

RUN cd /app/publish && rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /app/publish .
CMD ["dotnet", "BackOffice.API.dll"]
