# Step 0 — Setup

## Problema
Prima di scrivere codice serve un ambiente riproducibile: stessa versione dell'SDK, stesse regole del compilatore, un database su cui provare.

## Strumenti richiesti
- .NET SDK **10.0.401** (fissato in `global.json`, con `rollForward: latestFeature`).
- git.
- SQL Server: serve da Step 3. Nel PC dell'autore sono presenti LocalDB (`(localdb)\MSSQLLocalDB`) e un'istanza `MSSQLSERVER`.

Verifica:

```bash
dotnet --version
sqlcmd -S "(localdb)\MSSQLLocalDB" -C -Q "SELECT @@VERSION"
```

Con LocalDB funzionante vedrai "Microsoft SQL Server 2025 ...". L'opzione `-C` accetta il certificato locale.

## Come è organizzato il repo
- `src/` — il codice di produzione, un progetto per livello.
- `tests/` — un progetto di test per ogni progetto di `src/`.
- `docs/` — queste guide, una per step.
- `Directory.Build.props` — regole comuni a tutti i progetti.

## Perché `TreatWarningsAsErrors`
Un warning ignorato oggi diventa un bug domani. Con i warning trattati come errori il compilatore è la prima rete di sicurezza: `Nullable` attivo segnala i possibili `null` prima dell'esecuzione.

## Come provarlo
```bash
dotnet build
```
Atteso: `0 Warning(s), 0 Error(s)`.

## Cosa può rompersi
- SDK diverso da 10.0.x: `global.json` rifiuta la build.
- LocalDB non avviata: `sqllocaldb start MSSQLLocalDB`.
