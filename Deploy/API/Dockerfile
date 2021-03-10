ARG SDK_VERSION=3.1-buster-arm64v8
ARG ASPNET_VERSION=3.1-buster-slim-arm64v8
FROM mcr.microsoft.com/dotnet/core/sdk:${SDK_VERSION} AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore ./API/Peep.API/Peep.API.csproj
RUN dotnet publish ./API/Peep.API/Peep.API.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:${ASPNET_VERSION}

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Peep.API.dll"]