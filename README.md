# KioskRewards — B2B Loyalty Platform

[![CI](https://github.com/xoulios/B2B-Loyalty-Platform---Umbraco-training-project/actions/workflows/ci.yml/badge.svg)](https://github.com/xoulios/B2B-Loyalty-Platform---Umbraco-training-project/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=xoulios_B2B-Loyalty-Platform---Umbraco-training-project&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=xoulios_B2B-Loyalty-Platform---Umbraco-training-project)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=xoulios_B2B-Loyalty-Platform---Umbraco-training-project&metric=coverage)](https://sonarcloud.io/summary/new_code?id=xoulios_B2B-Loyalty-Platform---Umbraco-training-project)

A mini **B2B loyalty platform**: a company rewards the kiosk owners who sell its products. Owners
log in, see their **points balance + transaction history**, and **redeem** points for rewards from a
catalog. Bilingual (EL/EN). Built as a hands-on practice project for **Umbraco 17 LTS on Clean
Architecture**.

## Tech stack

- **.NET 10**, **Umbraco 17 LTS**, **EF Core 10**, **SQLite** (dev), **xUnit**
- Clean Architecture, 5 projects: `Web` (Umbraco host/presentation) → `Application` (ports/DTOs) →
  `Domain` (entities/rules) · `Infrastructure` (EF Core) · `Tests` (xUnit, 29 tests)

## Architecture at a glance

Two worlds, one bridge: **Umbraco (CMS)** owns pages/rewards/members/languages; a **pure .NET
loyalty core** owns points accounts and transactions, bridged only by the Umbraco Member's `Key`
(Guid). Full write-up: [docs/KIOSKREWARDS-FULL-ANALYSIS.md](docs/KIOSKREWARDS-FULL-ANALYSIS.md).

## Quality & security tooling

This project doubles as practice for professional CI/CD and AppSec tooling:

- **CI** — GitHub Actions: build + test on every push/PR to `main`
- **SonarCloud** — code quality Quality Gate + coverage, CI-based analysis
- **Dependabot** + **CodeQL** + **Secret scanning** — GitHub-native SCA/SAST/secret detection
- **Snyk** — independent second-vendor SCA + SAST (Snyk Code)
- **Branch protection (Ruleset)** on `main` — required PR + required status checks + no force pushes

Full write-up with concepts explained: [docs/KIOSKREWARDS-DEVOPS-ANALYSIS.md](docs/KIOSKREWARDS-DEVOPS-ANALYSIS.md),
status tracker: [docs/DEVOPS-TOOLING.md](docs/DEVOPS-TOOLING.md).

## Running locally

- VS 2022 → open `KioskRewards.sln` → **F5** (startup project `KioskRewards.Web`, IIS Express).
- Or CLI: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project KioskRewards.Web --no-launch-profile --urls "https://localhost:44315"`
- Backoffice: `https://localhost:44315/umbraco`
- Tests: `dotnet test`

Full session/context handoff for continuing development: [docs/PROJECT-CONTEXT.md](docs/PROJECT-CONTEXT.md).
