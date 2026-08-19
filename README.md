# WoW Addon Lab

A standalone World of Warcraft addon runtime for developing and testing installed addons without launching the game.
!Currently in "early" development!

## What works

- Native Lua 5.1 through Lua.NET, including WoW compatibility helpers.
- Per product addon catalog.
- Optional official Blizzard UI loading, enabled by default and loaded before user addons.
- Multi-addon Lua loading.
- Emulator-only, account-wide, and per-character SavedVariables selections under `%LOCALAPPDATA%\WoWAddonLab\SavedVariables`.
- Inspector, Lua console, logs, reload controls, and a localhost automation API.

## Requirements

- .NET 10 SDK
- A locally installed World of Warcraft product

## Run

Desktop GUI:

```powershell
dotnet run --project src\WoWAddonLab
```

Headless for an AI agent, test runner, or CI:

```powershell
dotnet run --project src\WoWAddonLab --headless --product wow --enable MyAddonName
```

`--product` accepts a product code such as `wow`, a product folder such as `_retail_`, or a full product path. `--enable` is repeatable or comma-separated.
Use `--profile account:MYACCOUNTNAME --import-saved` to copy that profile's data into the isolated emulator profile before startup.

Use `--no-blizzard-ui` for isolated addon development, or `--blizzard-ui` to  force the official bootstrap regardless of the saved product setting.
`--no-tact` also prevents Blizzard UI extraction, so pair it with
`--no-blizzard-ui` for a fully local isolated run.

The automation API listens only on `127.0.0.1:43117`. Change it with
`--port 43118`. Use `--ticks N` for a smoke run.

More info coming soon