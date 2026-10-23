# ModernCleanCode Step 3 (Infrastructure, EF Core + SQL Server) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (esecuzione Native scelta dall'autore). Steps use checkbox (`- [ ]`) syntax.

**Goal:** Implementare le porte dell'Application con EF Core su SQL Server vero (LocalDB), con migration, test di integrazione e il primo vincolo di idempotenza garantito dal database (chiave primaria sull'`AccountId`).

**Architecture:** `ModernCleanCode.Infrastructure` referenzia Application e Domain. `AppDbContext` mappa `Account`; `EfAccountRepository` implementa `IAccountRepository`; `EfUnitOfWork` implementa `IUnitOfWork` e traduce la violazione di chiave univoca in `ConflictException`. I test girano su un database LocalDB creato e distrutto per ogni run.

**Tech Stack:** .NET 10, EF Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`), `dotnet-ef` come tool locale, xUnit, LocalDB `(localdb)\MSSQLLocalDB`.

**Spec:** `docs/superpowers/specs/2026-10-23-moderncleancode-design.md` (§4, §6 step 3, §7c, §7d)

## Global Constraints

- Root `C:\Progetti\CleanCode`. Il push è autorizzato dall'autore ("in generale pusha"): pubblica ogni commit su `origin main` a fine task.
- Documentazione in italiano; codice e identificatori in inglese.
- SQL Server **vero**, mai SQLite né provider in-memory, nei test di integrazione (§7b del design).
- Connessione con `Trusted_Connection=True;TrustServerCertificate=True`; **nessuna password** nel repo.
- I database di test si chiamano `ModernCleanCode_Test_<guid>`; il cleanup elimina **solo** un database con quel prefisso.
- Infrastructure referenzia solo Application e Domain; il Domain e l'Application restano invariati (verificato dai test architetturali).
- Commit dei pattern: `[RED]` (test che fallisce, committato) poi `[GREEN]`; gli altri commit restano verdi (§7c).
- Ogni guida ha la sezione "Come verificare che sia giusto" (§7d) e dichiara le lacune note.
- Su questa macchina **Bash non funziona**: usare PowerShell e lo strumento Write.
- Limite token: massimo metà della finestra di 4 ore; niente sub-agent.
- Installare pacchetti NuGet e il tool `dotnet-ef` richiede rete: sono dipendenze dichiarate da questo piano.

## Review Focus

- Scala del denaro: `Money` accetta fino a 4 decimali (tariffe come 0,0333); il database non deve arrotondare in silenzio. Colonna `decimal(19,4)`, test di round-trip con 4 decimali.
- Il modello EF deve corrispondere alla migration: nessuna modifica pendente.
- `Add` senza `SaveChangesAsync` non deve persistere nulla (l'unit of work conta).
- Violazione di chiave univoca → `ConflictException`; qualunque **altra** `DbUpdateException` deve uscire invariata, non travestita da conflitto.
- Cleanup dei database di test anche se un test fallisce; mai toccare altri database.

---

### Task 3: Infrastructure con EF Core (Step 3)

**Files:**
- Create: `.config/dotnet-tools.json` (tool locale `dotnet-ef`)
- Create: `src/ModernCleanCode.Infrastructure/ModernCleanCode.Infrastructure.csproj`
- Create: `src/ModernCleanCode.Infrastructure/Persistence/AppDbContext.cs`, `AccountConfiguration.cs`, `EfAccountRepository.cs`, `EfUnitOfWork.cs`
- Create: `src/ModernCleanCode.Infrastructure/Persistence/Migrations/` (generata da `dotnet ef`)
- Create: `tests/ModernCleanCode.Infrastructure.Tests/ModernCleanCode.Infrastructure.Tests.csproj`
- Create: `tests/ModernCleanCode.Infrastructure.Tests/SqlServerFixture.cs`, `AccountRepositoryTests.cs`, `ModelTests.cs`, `ArchitectureTests.cs`
- Create: `docs/03-infrastructure.md`; Modify: `README.md` (riga Step 3)

**Interfaces:**
- Consumes (Application): `IAccountRepository` (`FindAsync(Guid, CancellationToken) : Task<Account?>`, `AddAsync(Account, CancellationToken) : Task`), `IUnitOfWork` (`SaveChangesAsync(CancellationToken) : Task`), `ConflictException(string)`, `OpenAccountHandler`, `ChargeToll...` (per il test end-to-end).
- Consumes (Domain): `Account.Open(Guid, Money)`, `Account.Id`, `Account.Balance`, `Money.Eur(decimal)`, `Money.Amount`.
- Produces (namespace `ModernCleanCode.Infrastructure.Persistence`):
  - `AppDbContext(DbContextOptions<AppDbContext> options) : DbContext` con `DbSet<Account> Accounts`.
  - `EfAccountRepository(AppDbContext db) : IAccountRepository`.
  - `EfUnitOfWork(AppDbContext db) : IUnitOfWork`.
  - Tabella `Accounts(Id uniqueidentifier PK, Balance decimal(19,4) NOT NULL)`.
- Produces (test): `SqlServerFixture : IAsyncLifetime` con `AppDbContext CreateContext()` che apre un **nuovo** contesto sullo stesso database di test; crea il database con `Database.MigrateAsync()` in `InitializeAsync` e lo elimina in `DisposeAsync`.

- [ ] **Step 1: Crea progetto, tool e pacchetti**

Run: `dotnet new tool-manifest`, `dotnet tool install dotnet-ef`; `dotnet new classlib -n ModernCleanCode.Infrastructure -o src/ModernCleanCode.Infrastructure -f net10.0`; `dotnet add src/ModernCleanCode.Infrastructure reference src/ModernCleanCode.Application src/ModernCleanCode.Domain`; `dotnet add src/ModernCleanCode.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer`; `dotnet add src/ModernCleanCode.Infrastructure package Microsoft.EntityFrameworkCore.Design`; `dotnet new xunit -n ModernCleanCode.Infrastructure.Tests -o tests/ModernCleanCode.Infrastructure.Tests -f net10.0`; riferimenti del progetto di test a Infrastructure, Application, Domain; `dotnet sln add` per entrambi; rimuovi `Class1.cs` e `UnitTest1.cs`.
Expected: `dotnet build` senza errori né warning.

- [ ] **Step 2: Scrivi la fixture e i test falliti** (`SqlServerFixture.cs`, `AccountRepositoryTests.cs`)

`AccountRepositoryTests` (ogni test usa **contesti diversi** per scrivere e per rileggere, così la lettura va davvero al database):
`Add_and_save_then_find_in_new_context_returns_same_balance`; `Balance_keeps_four_decimals` (`Money.Eur(10.0333m)` torna identico); `Find_unknown_id_returns_null`; `Add_without_save_is_not_persisted`; `Charge_toll_end_to_end_persists_new_balance` (conto 10, `ChargeTollHandler` con tariffa 0,07, km 10→30; riletto in un nuovo contesto: saldo 8,60).

- [ ] **Step 3: Verifica che falliscano**

Run: `dotnet test tests/ModernCleanCode.Infrastructure.Tests`
Expected: FAIL (errore di compilazione: `AppDbContext` ecc. non esistono).

- [ ] **Step 4: Implementa il mapping**

`AppDbContext` applica `AccountConfiguration`. `AccountConfiguration : IEntityTypeConfiguration<Account>`: chiave `Id` (`ValueGeneratedNever`), tabella `Accounts`, `Balance` con value converter `Money ↔ decimal` (`m => m.Amount`, `d => Money.Eur(d)`) e `HasColumnType("decimal(19,4)")`. `Account` ha costruttore e setter privati: EF usa il costruttore privato `(Guid id, Money balance)` per nome di parametro; **non modificare il Domain**. Se EF non riesce a legarlo, risolvi nel mapping e annota la scelta come `Ruling` nella guida.

- [ ] **Step 5: Implementa repository e unit of work, crea la migration**

`EfAccountRepository` e `EfUnitOfWork` come da Interfaces. Run: `dotnet ef migrations add InitialCreate --project src/ModernCleanCode.Infrastructure --output-dir Persistence/Migrations`. La migration ha bisogno di un modo per creare il contesto a design-time: aggiungi `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` con la connessione LocalDB di sviluppo `Database=ModernCleanCode_Dev`.
Expected: `Persistence/Migrations/*_InitialCreate.cs` generato.

- [ ] **Step 6: Esegui i test di repository**

Run: `dotnet test tests/ModernCleanCode.Infrastructure.Tests --filter AccountRepositoryTests`
Expected: PASS (5 test). Dopo il run, verifica con `sqlcmd` che **non** resti nessun database `ModernCleanCode_Test_*`.

- [ ] **Step 7: Test del modello e architetturale**

`ModelTests.Model_has_no_pending_changes_compared_to_migrations`: `Assert.False(db.Database.HasPendingModelChanges())`. `ArchitectureTests.Infrastructure_references_only_Application_and_Domain_among_project_assemblies`. Verifica che passino.

- [ ] **Step 8: Scrivi `docs/03-infrastructure.md`, aggiorna il README, committa e pubblica**

La guida spiega: cosa implementa l'Infrastructure e perché le frecce puntano verso l'interno; perché test contro SQL Server vero; cosa fa la migration; perché la scala `decimal(19,4)`; perché `EfUnitOfWork` è separato dal contesto. Sezione "Come verificare" con almeno: round-trip con contesti diversi, `Add` senza save, modello vs migration, cleanup dei database di test.

```bash
git add -A
git commit -m "feat: step 3 - Infrastructure con EF Core e SQL Server (migration, test di integrazione)"
git push
```

---

### Task 4: Il database garantisce l'unicità `[RED]` poi `[GREEN]` (Step 3b)

**Files:**
- Create: `tests/ModernCleanCode.Infrastructure.Tests/UniqueIdTests.cs`
- Modify: `src/ModernCleanCode.Infrastructure/Persistence/EfUnitOfWork.cs`
- Create: `docs/03b-unicita-garantita-dal-database.md`; Modify: `README.md`

**Interfaces:**
- Consumes: `EfAccountRepository`, `EfUnitOfWork`, `SqlServerFixture.CreateContext()` (Task 3), `ConflictException`.
- Produces: `EfUnitOfWork.SaveChangesAsync` lancia `ConflictException` quando SQL Server segnala violazione di chiave univoca (`SqlException.Number` 2627 o 2601); ogni altra `DbUpdateException` esce invariata.

- [ ] **Step 1: `[RED]` Scrivi il test che mostra il problema**

`UniqueIdTests.Second_save_with_same_id_throws_conflict_exception`: due contesti, due repository, due unit of work; entrambi aggiungono `Account.Open` con lo **stesso id** e il primo salva; il secondo `SaveChangesAsync` deve lanciare `ConflictException`. Questo simula due richieste che hanno passato entrambe il controllo "esiste già?" dell'handler (il controllo e l'inserimento non sono atomici).

- [ ] **Step 2: Esegui e verifica che fallisca**

Run: `dotnet test tests/ModernCleanCode.Infrastructure.Tests --filter UniqueIdTests`
Expected: FAIL: il test riceve `DbUpdateException` (con `SqlException` 2627 interna) invece di `ConflictException`. La chiave primaria **ha già protetto i dati**: non ci sono due conti con lo stesso id; manca la traduzione dell'errore.

- [ ] **Step 3: Committa il rosso**

```bash
git add -A
git commit -m "[RED] step 3b - due salvataggi con lo stesso id: il database blocca, ma l'errore e' grezzo"
git push
```

- [ ] **Step 4: `[GREEN]` Traduci la violazione in `EfUnitOfWork`**

In `SaveChangesAsync`: cattura `DbUpdateException` quando `InnerException is SqlException { Number: 2627 or 2601 }` e rilancia `ConflictException`; qualunque altra `DbUpdateException` si rilancia con `throw;`. Aggiungi un secondo test `Other_database_errors_are_not_translated_to_conflict` (per esempio provocando un errore diverso, come un valore fuori scala, e verificando che resti `DbUpdateException`; se non riesci a provocarne uno in modo affidabile, annota il limite nella guida come lacuna nota).

- [ ] **Step 5: Esegui tutta la suite**

Run: `dotnet test`
Expected: PASS, 0 warning. Il totale atteso è 28 + 7 (Task 3: 5 repository + 1 modello + 1 architetturale) + 2 (Task 4) = 37; riporta il numero reale.

- [ ] **Step 6: Scrivi la guida, aggiorna il README, committa e pubblica**

`docs/03b-...md`: il problema (controllo + inserimento non atomici), perché basta la chiave primaria a proteggere i dati, perché l'errore va tradotto (l'API dovrà rispondere 409, non 500), mostra i due commit `[RED]`/`[GREEN]`. Sezione "Come verificare": rimuovi la traduzione e guarda il test fallire; l'unicità è garantita dal database o solo dal codice?

```bash
git add -A
git commit -m "[GREEN] step 3b - la violazione di chiave univoca diventa ConflictException"
git push
```

---

## Self-review

- **Copertura dello spec:** Step 3 (EF Core + SQL Server, migration, repository) in Task 3; §7c (RED/GREEN) in Task 4; §7d ("Come verificare") in entrambe le guide; §7b (SQL Server vero, LocalDB) nei vincoli.
- **Coerenza tipi:** `AppDbContext`, `EfAccountRepository`, `EfUnitOfWork`, `SqlServerFixture.CreateContext()` identici in Interfaces e step.
- **Non incluso volutamente:** `ITariffProvider` concreto e la registrazione DI arrivano allo Step 4 (API/configurazione), dove servono davvero.
- **Rischi noti:** l'associazione EF del costruttore privato di `Account` potrebbe richiedere un aggiustamento del mapping (previsto nello Step 4 di questo task, senza toccare il Domain); il conteggio dei test va letto dall'output.
