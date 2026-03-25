# Windows Publish Guide

## Requirements
- Windows machine
- .NET 8 SDK installed

## Verify build
```bash
dotnet restore
dotnet build -c Release
```

## Run locally
```bash
dotnet run
```

## Publish a Windows executable
Framework-dependent:
```bash
dotnet publish -c Release -r win-x64 --self-contained false -o publish/win-x64
```

Self-contained:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64-selfcontained
```

## Output notes
- database file will be created in `data/billingapp.db` next to the executable
- keep `src/Infrastructure/Data/schema.sql` available in the publish output or adjust startup bootstrapping later to embed schema as a resource

## Recommended next improvement
For production packaging, embed the SQL schema file as a resource so first-run database setup does not depend on source-folder layout.
