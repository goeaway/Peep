#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Crawler/Peep.Crawler/Peep.Crawler.csproj", "Crawler/Peep.Crawler/"]
COPY ["Core/Peep.Core.API/Peep.Core.API.csproj", "Core/Peep.Core.API/"]
COPY ["Core/Peep/Peep.csproj", "Core/Peep/"]
COPY ["Core/Peep.Core.Infrastructure/Peep.Core.Infrastructure.csproj", "Core/Peep.Core.Infrastructure/"]
RUN dotnet restore "Crawler/Peep.Crawler/Peep.Crawler.csproj"
COPY . .
WORKDIR "/src/Crawler/Peep.Crawler"
RUN dotnet build "Peep.Crawler.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Peep.Crawler.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app

# install chromium for puppeteer sharp
RUN apt-get update
RUN apt-get install chromium -y

# ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome"
ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/chromium"

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Peep.Crawler.dll"]
