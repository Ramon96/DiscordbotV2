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
COPY --from=publish-discord /app/publish/discord .
ENTRYPOINT ["dotnet", "GLaDOS.Discord.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final-scheduler
WORKDIR /app
COPY --from=publish-scheduler /app/publish/scheduler .

RUN apt-get update && \
    apt-get install -y curl git && \
    curl -fsSL https://opencode.ai/install | bash && \
    ln -s /root/.opencode/bin/opencode /usr/local/bin/opencode && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

RUN git config --global user.email "glados-bot@discord.bot" && \
    git config --global user.name "GLaDOS Feature Bot" && \
    git config --global credential.https://github.com.helper '!f() { echo "username=x-access-token"; echo "password=$GITHUB_TOKEN"; }; f'

ENTRYPOINT ["dotnet", "GLaDOS.Scheduler.dll"]