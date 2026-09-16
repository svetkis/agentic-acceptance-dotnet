# Version Audit Checklist

## Pre-flight
- [ ] List of changed files obtained (`global.json`, `*.csproj`, `package.json`, `Dockerfile`)
- [ ] Team standard for TargetFramework is known (e.g., `net9.0`)
- [ ] `dotnet list package --outdated` output is available (for .NET projects)

## SDK & Runtime
- [ ] `global.json` — version without `preview`/`rc`/`beta`
- [ ] `global.json` — `rollForward` != `disable`
- [ ] All projects use a single `TargetFramework`

## NuGet Packages
- [ ] No `preview`/`rc`/`beta` in `PackageReference`/`PackageVersion`
- [ ] Microsoft.* package versions match TargetFramework (e.g., `net9.0` → EF Core 9.x)
- [ ] No packages more than 2 major releases behind current LTS
- [ ] `dotnet list package --vulnerable` — clean or all vulnerabilities documented

## Frontend (if applicable)
- [ ] `package.json` — no `alpha`/`beta`/`rc` in `dependencies`
- [ ] React/Vue/Angular — lag no more than 1 major version
- [ ] TypeScript — compatible with framework

## Infrastructure
- [ ] `Dockerfile` — base image is not `preview`, not `rc`
- [ ] GitHub Actions — `actions/setup-dotnet` with version specified or `global.json`
- [ ] GitHub Actions — `actions/checkout@v5`, not `v2`-`v4`
