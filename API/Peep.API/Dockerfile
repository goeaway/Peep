#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["API/Peep.API/Peep.API.csproj", "API/Peep.API/"]
COPY ["Core/Peep/Peep.csproj", "Core/Peep/"]
COPY ["API/Peep.API.Application/Peep.API.Application.csproj", "API/Peep.API.Application/"]
COPY ["Core/Peep.Core.API/Peep.Core.API.csproj", "Core/Peep.Core.API/"]
COPY ["Core/Peep.Core.Infrastructure/Peep.Core.Infrastructure.csproj", "Core/Peep.Core.Infrastructure/"]
COPY ["API/Peep.API.Persistence/Peep.API.Persistence.csproj", "API/Peep.API.Persistence/"]
COPY ["API/Peep.API.Models/Peep.API.Models.csproj", "API/Peep.API.Models/"]
RUN dotnet restore "API/Peep.API/Peep.API.csproj"
COPY . .
WORKDIR "/src/API/Peep.API"
RUN dotnet build "Peep.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Peep.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Peep.API.dll"]
