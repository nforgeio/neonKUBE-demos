FROM        mcr.microsoft.com/dotnet/aspnet:7.0.3-jammy-amd64
MAINTAINER  marcus@neonforge.com
STOPSIGNAL  SIGTERM

ENV TZ=UTC
ENV DEBIAN_FRONTEND noninteractive
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY bin/Release/7.0/publish     /load-generator
COPY docker-entrypoint.sh           /

RUN chmod 700 /*.sh \
    && ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone \
    && sed -i 's!^#precedence ::ffff:0:0/96  10$!precedence ::ffff:0:0/96  100!g' /etc/gai.conf \
    && sed -i 's/\r$//' docker-entrypoint.sh

ENTRYPOINT ["/docker-entrypoint.sh"]
