FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /source

ADD Backoffice.Web Backoffice.Web
ADD Backoffice.Shared Backoffice.Shared

WORKDIR /source/Backoffice.Web

RUN dotnet restore

RUN dotnet build --configuration Release

RUN dotnet publish -o publish && \
  cd publish && rm -f appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:7.0
EXPOSE 80
WORKDIR /app

COPY --from=build /source/Backoffice.Web/publish .
CMD ["/usr/bin/dotnet", "Backoffice.Web.dll"]
