ARG SDK_VERSION=3.1-buster-arm64v8
ARG NET_VERSION=3.1-buster-slim-arm64v8
FROM mcr.microsoft.com/dotnet/core/sdk:${SDK_VERSION} AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore ./Crawler/Peep.Crawler/Peep.Crawler.csproj
RUN dotnet publish ./Crawler/Peep.Crawler/Peep.Crawler.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:${NET_VERSION}
ARG PLAYWRIGHT_SCRIPT=unix/native/playwright.sh
WORKDIR /app

COPY --from=build-env /app/out .
COPY --from=build-env /app/.playwright ./.playwright

RUN bash ./.playwright/${PLAYWRIGHT_SCRIPT} install
RUN echo "deb http://ftp.uk.debian.org/debian buster main" | tee -a /etc/apt/sources.list
RUN apt-get update
RUN apt-get install -y \ 
libwoff1 libopus0 libwebp6 libwebpdemux2 libenchant1c2a libgudev-1.0-0 libsecret-1-0 libhyphen0 libgdk-pixbuf2.0-0 libegl1 libgles2 libevent-2.1-6 libnotify4 libxslt1.1 \
libdbus-glib-1-2 \
xvfb

ENTRYPOINT ["dotnet", "Peep.Crawler.dll"]