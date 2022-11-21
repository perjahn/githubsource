FROM mcr.microsoft.com/dotnet/sdk as build

WORKDIR /app

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV POWERSHELL_TELEMETRY_OPTOUT=1

RUN apt-get update && \
    apt-get -y upgrade

COPY . .

RUN ls -la && \
    dotnet publish -c Release


FROM mcr.microsoft.com/dotnet/aspnet as runtime

WORKDIR /app

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV POWERSHELL_TELEMETRY_OPTOUT=1

COPY --from=build /app/bin/Release/*/publish/* /app/

RUN ls -la

RUN cat appsettings.json
RUN cat githubsource.runtimeconfig.json
RUN cat web.config

ENTRYPOINT ["/app/githubsource"]
