ARG NET_VERSION=3.1-buster-slim-arm64v8
FROM mcr.microsoft.com/dotnet/core/runtime:${NET_VERSION}
WORKDIR /app

COPY . ./

# install chromium for puppeteer sharp
RUN apt-get update
RUN apt-get install chromium -y

# ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome"
ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/chromium"

ENTRYPOINT ["dotnet", "Peep.Crawler.dll"]