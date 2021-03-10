ARG SDK_VERSION=3.1-buster-arm64v8
ARG NET_VERSION=3.1-buster-slim-arm64v8
FROM mcr.microsoft.com/dotnet/core/sdk:${SDK_VERSION} AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore ./Crawler/Peep.Crawler/Peep.Crawler.csproj
RUN dotnet publish ./Crawler/Peep.Crawler/Peep.Crawler.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:${NET_VERSION}

# install chromium for puppeteer sharp
RUN apt-get update
RUN apt-get install chromium -y

# ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome"
ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/chromium"

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Peep.Crawler.dll"]