FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY . ./
RUN dotnet restore Peep.CLI/Peep.CLI.csproj
RUN dotnet publish Peep.CLI/Peep.CLI.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1

#####################
#PUPPETEER RECIPE
#####################
# Install latest chrome dev package and fonts to support major charsets (Chinese, Japanese, Arabic, Hebrew, Thai and a few others)
# Note: this installs the necessary libs to make the bundled version of Chromium that Puppeteer
# installs, work.
ARG CHROME_VERSION="81.0.4044.138-1"
RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils
RUN wget --no-verbose -O /tmp/chrome.deb http://dl.google.com/linux/chrome/deb/pool/main/g/google-chrome-stable/google-chrome-stable_${CHROME_VERSION}_amd64.deb \
&& apt-get update \
&& apt-get install -y /tmp/chrome.deb --no-install-recommends --allow-downgrades fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf \
&& rm /tmp/chrome.deb

# Add user, so we don't need --no-sandbox.
# same layer as npm install to keep re-chowned files from using up several hundred MBs more space    
# RUN groupadd -r pptruser && useradd -r -g pptruser -G audio,video pptruser \
#     && mkdir -p /home/pptruser/Downloads \
#     && chown -R pptruser:pptruser /home/pptruser

# # Run everything after as non-privileged user.
# USER pptruser
#####################
#END PUPPETEER RECIPE
#####################

ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome"

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Peep.CLI.dll"];