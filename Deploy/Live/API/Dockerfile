ARG ASPNET_VERSION=3.1-buster-slim-arm64v8
FROM mcr.microsoft.com/dotnet/core/aspnet:${ASPNET_VERSION}
WORKDIR /app

COPY . ./
ENTRYPOINT ["dotnet", "Peep.API.dll"]