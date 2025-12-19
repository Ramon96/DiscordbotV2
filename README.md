## Why This Project Exists

This project is a personal learning initiative designed to enhance my programming skills across multiple domains:

- *Modern .NET Development* – Building robust, scalable applications using the latest .NET ecosystem
- *Relational Database Design* – Implementing and optimizing data models for real-world scenarios
- *Cloud Infrastructure* – Deploying and managing applications in cloud environments

By combining my professional goals with a personal interest in Discord bots and Old School RuneScape tracking, this project provides a practical, engaging way to develop skills that are directly applicable to my career.

## What does it do

## What is its purpose

## Arhcitecture

mermaid
erDiagram
    DiscordUser {
        int id PK
        string username
        date dateOfBirth
        date created
        date modified
    }

    DiscordUserOsrsUser {
        int discord_id FK
        int osrs_id FK
        date created
        date modified
    }

    OsrsUser {
        int id PK
        string username
        date created
        date modified
    }

    OsrsSkill {
        int id PK
        int osrs_user_id FK
        string name
        int level
        int rank
        int xp
        date created
        date modified
    }

    OsrsQuest {
        int osrs_user_id FK
        int id PK
        string name
        enum state
        date created
        date modified
    }

    OsrsMusic {
        int osrs_user_id FK
        int id PK
        string name
        bool unlocked
        date created
        date modified
    }

    OsrsDiary {
        int id pk
        int osrs_user_id Fk
        date created
        date modified
        string region
        enum diffeculty
        bool completed
    }

    DiscordUser ||--o{ DiscordUserOsrsUser : links
    DiscordUserOsrsUser ||--|| OsrsUser : maps

    OsrsUser ||--o{ OsrsSkill : trains
    OsrsUser ||--o{ OsrsQuest : completes
    OsrsUser ||--o{ OsrsMusic : unlocks
    OsrsUser ||--o{ OsrsDiary : unlocks


## BirthdayCheck

This job checks whether it's someones birthday today. If it is you get a congratations from the bot

## Fetch osrs Hiscores

This job checks if the hiscores are updated; updates the database and will display a log of the changes.

## Fetch osrs wiki

This job checks for diary changes. music unlocks and quest progression. stores it in the database and display the changes.
```
