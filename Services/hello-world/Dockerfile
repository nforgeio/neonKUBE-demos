FROM        mcr.microsoft.com/dotnet/aspnet:6.0.6-jammy-amd64
MAINTAINER  marcus@neonforge.com
STOPSIGNAL  SIGTERM

# Environment

ENV TZ=UTC
ENV DEBIAN_FRONTEND noninteractive
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY bin/Release/net6.0     /usr/bin/hello-world.dotnet/
COPY docker-entrypoint.sh   /

RUN chmod 700 /*.sh \
    && echo "exec dotnet /usr/bin/hello-world.dotnet/hello-world.dll \"\$@\"" >> /usr/bin/hello-world \
    && chmod 770 /usr/bin/hello-world \
    && ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone \
    && sed -i 's!^#precedence ::ffff:0:0/96  10$!precedence ::ffff:0:0/96  100!g' /etc/gai.conf

ENTRYPOINT ["/docker-entrypoint.sh"]
