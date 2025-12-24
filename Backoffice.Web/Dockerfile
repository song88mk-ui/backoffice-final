FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /source

ADD Backoffice.Web Backoffice.Web
ADD Backoffice.Shared Backoffice.Shared

WORKDIR /source/Backoffice.Web

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

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

COPY --from=build /source/Backoffice.Web/publish .
CMD ["/usr/bin/dotnet", "Backoffice.Web.dll"]
