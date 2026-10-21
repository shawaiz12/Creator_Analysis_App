# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Host/CreatorAnalytics.Api/CreatorAnalytics.Api.csproj
RUN dotnet publish src/Host/CreatorAnalytics.Api/CreatorAnalytics.Api.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the non-root user that the base image provides.
USER $APP_UID

ENTRYPOINT ["dotnet", "CreatorAnalytics.Api.dll"]