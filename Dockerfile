FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GLaDOS.Discord/GLaDOS.Discord.csproj", "GLaDOS.Discord/"]
COPY ["GLaDOS.Scheduler/GLaDOS.Scheduler.csproj", "GLaDOS.Scheduler/"]
COPY ["GLaDOS.Core/GLaDOS.Core.csproj", "GLaDOS.Core/"]
COPY ["GLaDOS.Infra/GLaDOS.Infra.csproj", "GLaDOS.Infra/"]
COPY ["GLaDOS.OldschoolRunescape/GLaDOS.OldschoolRunescape.csproj", "GLaDOS.OldschoolRunescape/"]
COPY ["GLaDOS.OsrsWiki/GLaDOS.OsrsWiki.csproj", "GLaDOS.OsrsWiki/"]

RUN dotnet restore "GLaDOS.Discord/GLaDOS.Discord.csproj"
RUN dotnet restore "GLaDOS.Scheduler/GLaDOS.Scheduler.csproj"

COPY . .

FROM build AS publish-discord
WORKDIR "/src/GLaDOS.Discord"
RUN dotnet publish "GLaDOS.Discord.csproj" -c Release -o /app/publish/discord

FROM build AS publish-scheduler
WORKDIR "/src/GLaDOS.Scheduler"
RUN dotnet publish "GLaDOS.Scheduler.csproj" -c Release -o /app/publish/scheduler

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final-discord
WORKDIR /app
# libsodium23 + libopus0 are required by Discord.Net to open voice connections on Linux
RUN apt-get update && \
    apt-get install -y libsodium23 libopus0 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
COPY --from=publish-discord /app/publish/discord .
ENTRYPOINT ["dotnet", "GLaDOS.Discord.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final-scheduler
WORKDIR /app
COPY --from=publish-scheduler /app/publish/scheduler .

RUN apt-get update && \
    apt-get install -y curl git libsodium23 libopus0 && \
    curl -fsSL https://opencode.ai/install | bash && \
    ln -s /root/.opencode/bin/opencode /usr/local/bin/opencode && \
    curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg && \
    chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null && \
    apt-get update && \
    apt-get install -y gh && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

RUN git config --global user.email "glados-bot@discord.bot" && \
    git config --global user.name "GLaDOS Feature Bot" && \
    git config --global credential.https://github.com.helper '!f() { echo "username=x-access-token"; echo "password=$GITHUB_TOKEN"; }; f'

ENTRYPOINT ["dotnet", "GLaDOS.Scheduler.dll"]