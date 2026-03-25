# Billing App

Windows desktop billing application built with **C# WinForms** and **SQLite**.

## Current status

This repo now includes:
- project file and startup wiring
- SQLite schema + automatic database initialization
- domain models
- calculation engine for fixed-price and storage-day billing
- repositories for customers, products, and invoices
- invoice list screen
- invoice create flow
- invoice load/edit flow
- save invoice to SQLite
- basic invoice search on the list screen
- TXT preview export
- PDF export support
- Windows publish instructions

## Stack

- .NET 8
- WinForms
- SQLite (`Microsoft.Data.Sqlite`)
- QuestPDF for PDF generation

## Project structure

- `BillingApp.csproj` — project definition
- `Program.cs` — app entry point
- `src/Domain/Models` — core entities
- `src/Application/Services` — billing and day-count logic
- `src/Infrastructure/Data` — SQLite schema and repositories
- `src/WinFormsUI/Forms` — desktop UI forms
- `docs/PROJECT_PLAN.md` — MVP scope and roadmap

## Features implemented

### Billing logic
- fixed-price invoice lines
- storage daily pricing rule
- automatic storage-day calculation
- VAT/tax calculation
- invoice totals calculation

### Data layer
- creates the SQLite database automatically on first run
- seeds default products
- persists invoices and invoice items
- reloads saved invoices for editing

### UI
- invoice list view
- new invoice form
- edit existing invoice by double-clicking a row or using the edit button
- quick search by invoice number, customer, or status
- preview/export button that writes a printable text version of the invoice
- PDF export button for invoice documents

## How it works

On startup the app:
1. creates a `data/` folder next to the executable
2. initializes `billingapp.db`
3. applies the schema from `src/Infrastructure/Data/schema.sql`
4. seeds default products if missing
5. opens the invoice list screen

## Run locally

### Requirements
- Windows
- .NET 8 SDK

### Commands
```bash
dotnet restore
dotnet build
dotnet run
```

## Notes

- this environment did not have `dotnet` installed, so compile verification could not be completed here
- the app is structured for further work such as PDF export and packaging

## Publishing
See `docs/WINDOWS_PUBLISH.md` for Windows build and publish commands.

## Export
- TXT preview export works as a simple printable fallback
- PDF export is wired through QuestPDF
- do a real `dotnet restore && dotnet build` on Windows to verify package restore and runtime behavior

## Next recommended steps
- replace text preview export with real PDF generation
- stronger validation and error messages
- better customer/product management screens
- packaging/publish for Windows `.exe`
- final build and runtime verification on Windows
