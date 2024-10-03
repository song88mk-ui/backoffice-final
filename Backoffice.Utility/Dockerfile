FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /source

ADD Backoffice.Shared Backoffice.Shared
ADD Backoffice.Utility Backoffice.Utility

WORKDIR /source/Backoffice.Utility

RUN dotnet restore

RUN dotnet build --configuration Release

RUN dotnet publish \
  --no-restore \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false

WORKDIR /app/publish
RUN rm -f appsettings.*.json

FROM mcr.microsoft.com/dotnet/aspnet:3.1
EXPOSE 80
WORKDIR /app

COPY --from=build /app/publish .
CMD ["dotnet", "Backoffice.Utility.dll"]

