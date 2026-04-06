# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore WireGuardUI.slnx
RUN dotnet publish src/WireGuardUI.Web/WireGuardUI.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends wireguard-tools iproute2 iptables ca-certificates \
    && printf '#!/bin/sh\n# Docker test shim: skip DNS updates when resolvconf/systemd is unavailable.\nexit 0\n' > /usr/local/bin/resolvconf \
    && chmod +x /usr/local/bin/resolvconf \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WireGuardUI.Web.dll"]
