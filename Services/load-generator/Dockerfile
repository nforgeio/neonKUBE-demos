FROM        mcr.microsoft.com/dotnet/aspnet:6.0.6-jammy-amd64
MAINTAINER  marcus@neonforge.com
STOPSIGNAL  SIGTERM

ENV TZ=UTC
ENV DEBIAN_FRONTEND noninteractive
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY bin/Release/net6.0     /usr/bin/load-generator.dotnet/
COPY docker-entrypoint.sh   /

RUN chmod 700 /*.sh \
    && echo "exec dotnet /usr/bin/load-generator.dotnet/load-generator.dll \"\$@\"" >> /usr/bin/load-generator \
    && chmod 770 /usr/bin/load-generator \
    && ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone \
    && sed -i 's!^#precedence ::ffff:0:0/96  10$!precedence ::ffff:0:0/96  100!g' /etc/gai.conf

ENTRYPOINT ["/docker-entrypoint.sh"]
