# GLaDOS Discord Bot - AI Agent Instructions

## Project Overview

A .NET 10 Discord bot tracking Old School RuneScape (OSRS) player data with scheduled jobs for birthday notifications and hiscores updates. Built as a learning project for modern .NET, PostgreSQL, and cloud deployment.

## Architecture

### Multi-Project Structure

- **GLaDOS.Core**: Domain entities (`Entity` base class with `Id`, `CreatedDate`, `ModifiedDate`) and service registration orchestration
- **GLaDOS.Discord**: Discord.Net integration with `IHostedService` background workers (see `HelloWorld.cs`)
- **GLaDOS.Infra**: Entity Framework Core + PostgreSQL data layer (`ApplicationDbContext`)
- **GLaDOS.Scheduler**: ASP.NET Core Web API entry point - **use this as startup project for EF migrations**

### Key Dependencies

- .NET 10.0 target framework across all projects
- Discord.Net 3.18.0 for bot functionality
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0 for PostgreSQL
- Entity Framework Core 10.0.0

## Critical Workflows

### Database Migrations

**Always use GLaDOS.Scheduler as startup project:**

```bash
dotnet ef migrations add MigrationName --project GLaDOS.Infra --startup-project GLaDOS.Scheduler
dotnet ef database update --project GLaDOS.Infra --startup-project GLaDOS.Scheduler
```

Connection string in `GLaDOS.Scheduler/appsettings.json` under `ConnectionStrings:DefaultConnection`.

### Service Registration Pattern

Services are registered via extension methods in `ServiceCollection/ServiceCollectionExtensions.cs`:

- Core project calls `AddDiscordServices()` from Discord project
- Discord services registered as both `AddSingleton<IHelloWorld, HelloWorld>()` and `AddHostedService<HelloWorld>()`
- Entry point: `Program.cs` calls `AddCoreServices()` and configures DbContext

### Discord Bot Pattern

Discord bots implement `IHostedService` to run as background workers:

- Inherit from `IHostedService` (see `HelloWorld.cs`)
- Inject `DiscordSocketClient` (registered as singleton in DI)
- `StartAsync`: Login with bot token and start client
- `StopAsync`: Stop and logout client
- **Security note**: Bot token currently hardcoded in `HelloWorld.cs:38` - move to configuration

## Domain Model Conventions

### Entity Base Class

All domain entities inherit from `GLaDOS.Domain.Entity`:

```csharp
public class Entity {
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
```

### Planned Data Model (from README)

- **DiscordUser** ↔ **DiscordUserOsrsUser** (junction) ↔ **OsrsUser**
- **OsrsUser** has many: **OsrsSkill**, **OsrsQuest**, **OsrsMusic**, **OsrsDiary**
- Track OSRS player progress: skills (level/rank/xp), quests (enum state), music (unlocked), diaries (region/difficulty/completed)

## Scheduled Jobs (Planned)

1. **BirthdayCheck**: Daily check for user birthdays (`DiscordUser.dateOfBirth`), send congratulations message
2. **Fetch OSRS Hiscores**: Poll hiscores API, update database, log changes
3. **Fetch OSRS Wiki**: (Details not yet specified)

## Development Notes

- Currently uses `net10.0` (early preview) - consider stability for production
- `GLaDOS.Scheduler` has OpenAPI/Swagger enabled in development mode
- Git ignores `bin/`, `obj/`, `.idea/` (Rider IDE), and `.env` files
- README shows incomplete Mermaid ER diagram - refer to text description above for entity relationships

## TODO Items

- Implement audit logging (noted in README line 1)
- Move Discord bot token to `appsettings.json` or user secrets
- Complete OSRS entity models (currently only `DiscordUser` exists)
- Implement scheduled background jobs using hangfire
- Define "What does it do" and "What is its purpose" sections in README
