FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

ADD Backoffice.Shared Backoffice.Shared
ADD Backoffice.Utility Backoffice.Utility

WORKDIR /source/Backoffice.Utility

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

WORKDIR /app/publish
RUN rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app

COPY --from=build /app/publish .
CMD ["dotnet", "Backoffice.Utility.dll"]
