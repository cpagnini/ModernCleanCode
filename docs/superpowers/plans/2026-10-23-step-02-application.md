# ModernCleanCode Step 2 (Application) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (scelto dall'autore: esecuzione Native). Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Creare il livello Application: casi d'uso (handler) e porte (interfacce), testati con doppioni scritti a mano, senza database.

**Architecture:** `ModernCleanCode.Application` referenzia solo il Domain. Gli handler orchestrano: caricano l'aggregato tramite una porta, invocano le regole del Domain, salvano tramite `IUnitOfWork`. Le implementazioni delle porte arrivano allo Step 3 (Infrastructure). Niente MediatR: handler scritti a mano, chiamati direttamente.

**Tech Stack:** .NET 10, C#, xUnit, doppioni di test a mano (nessuna libreria di mocking).

**Spec:** `docs/superpowers/specs/2026-10-23-moderncleancode-design.md` (§5, §6 step 2)

## Global Constraints

- Root: `C:\Progetti\CleanCode`. Solo commit locali, **nessun push**.
- Documentazione in italiano; codice e identificatori in inglese. Nessun marchio reale.
- Application referenzia **solo** `ModernCleanCode.Domain`; nessun pacchetto NuGet di produzione.
- Il Domain non referenzia nulla (già vero dallo Step 1; l'architecture test lo verifica).
- Commit sempre verde (lo Step 2 non introduce un pattern, quindi niente `[RED]`).
- Il messaggio di commit termina con `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Su questa macchina **Bash non funziona**: usare PowerShell e lo strumento Write.
- Limite token: massimo metà della finestra di 4 ore; niente sub-agent.

## Review Focus

- Conto inesistente: l'handler lancia `NotFoundException`, mai `NullReferenceException`.
- `ChargeToll` con saldo insufficiente: la `DomainException` esce e `SaveChangesAsync` **non** viene chiamato.
- `OpenAccount` su un id già esistente: `ConflictException`, il conto esistente resta intatto.
- Saldo iniziale o ricarica negativi: `DomainException` dal Domain, senza salvataggio.
- `CancellationToken` già annullato: l'handler lancia `OperationCanceledException` prima di salvare.

---

### Task 2: Livello Application (Step 2)

**Files:**
- Create: `src/ModernCleanCode.Application/ModernCleanCode.Application.csproj` (classlib net10.0, riferisce Domain)
- Create: `src/ModernCleanCode.Application/Abstractions/ICommandHandler.cs`, `IAccountRepository.cs`, `IUnitOfWork.cs`, `ITariffProvider.cs`
- Create: `src/ModernCleanCode.Application/Accounts/AccountDto.cs`, `OpenAccount.cs`, `TopUpAccount.cs`, `ChargeToll.cs`
- Create: `src/ModernCleanCode.Application/Errors/NotFoundException.cs`, `ConflictException.cs`
- Create: `tests/ModernCleanCode.Application.Tests/ModernCleanCode.Application.Tests.csproj` (xUnit; riferisce Application e Domain)
- Create: `tests/ModernCleanCode.Application.Tests/Fakes/InMemoryAccountRepository.cs`, `FakeUnitOfWork.cs`, `FixedTariffProvider.cs`
- Create: `tests/ModernCleanCode.Application.Tests/OpenAccountHandlerTests.cs`, `TopUpAccountHandlerTests.cs`, `ChargeTollHandlerTests.cs`, `ArchitectureTests.cs`
- Create: `docs/02-application.md`
- Modify: `README.md` (riga dello Step 2 nell'indice)

**Interfaces:**
- Consumes (Domain, Step 1): `Account.Open(Guid, Money)`, `Account.TopUp(Money)`, `Account.Charge(Money)`, `Account.Id`, `Account.Balance`, `Money.Eur(decimal)`, `Money.Amount`, `TollTariff.Calculate(int, int)`, `DomainException`.
- Produces (namespace `ModernCleanCode.Application...`):
  - `Abstractions.ICommandHandler<in TCommand, TResult>` con `Task<TResult> HandleAsync(TCommand command, CancellationToken ct)`.
  - `Abstractions.IAccountRepository`: `Task<Account?> FindAsync(Guid id, CancellationToken ct)`; `Task AddAsync(Account account, CancellationToken ct)`.
  - `Abstractions.IUnitOfWork`: `Task SaveChangesAsync(CancellationToken ct)`.
  - `Abstractions.ITariffProvider`: `TollTariff Current { get; }`.
  - `Errors.NotFoundException(string message)` e `Errors.ConflictException(string message)`, entrambe `sealed`, derivate da `Exception`.
  - `Accounts.AccountDto(Guid Id, decimal Balance)`.
  - `Accounts.OpenAccount(Guid AccountId, decimal InitialBalance)` e `OpenAccountHandler(IAccountRepository, IUnitOfWork) : ICommandHandler<OpenAccount, AccountDto>`.
  - `Accounts.TopUpAccount(Guid AccountId, decimal Amount)` e `TopUpAccountHandler(IAccountRepository, IUnitOfWork) : ICommandHandler<TopUpAccount, AccountDto>`.
  - `Accounts.ChargeToll(Guid AccountId, int EntryKm, int ExitKm)`, `ChargeTollResult(decimal Charged, decimal Balance)` e `ChargeTollHandler(IAccountRepository, ITariffProvider, IUnitOfWork) : ICommandHandler<ChargeToll, ChargeTollResult>`.
- Per tutti gli handler: se `ct.IsCancellationRequested` all'ingresso → `ct.ThrowIfCancellationRequested()` prima di toccare repository o unit of work.

- [ ] **Step 1: Crea i progetti e collegali**

Run: `dotnet new classlib -n ModernCleanCode.Application -o src/ModernCleanCode.Application -f net10.0`, `dotnet new xunit -n ModernCleanCode.Application.Tests -o tests/ModernCleanCode.Application.Tests -f net10.0`; `dotnet add src/ModernCleanCode.Application reference src/ModernCleanCode.Domain`; `dotnet add tests/ModernCleanCode.Application.Tests reference src/ModernCleanCode.Application src/ModernCleanCode.Domain`; `dotnet sln add` per i due progetti. Rimuovi `Class1.cs` e `UnitTest1.cs`.
Expected: `dotnet build` senza errori né warning.

- [ ] **Step 2: Scrivi i doppioni di test** (`Fakes/`)

`InMemoryAccountRepository` (dizionario per `Guid`, espone `Count`), `FakeUnitOfWork` (contatore `SaveCount`), `FixedTariffProvider(TollTariff)`. Sono doppioni, non test: nessuna asserzione.

- [ ] **Step 3: Scrivi i test falliti di `OpenAccountHandler`**

`OpenAccountHandlerTests`: `Opens_account_and_saves_once` (risultato `AccountDto` con saldo 25, `SaveCount == 1`, repository `Count == 1`), `Existing_id_throws_conflict_and_keeps_account` (secondo open con stesso id → `ConflictException`, saldo originale intatto, `SaveCount` ancora 1), `Negative_initial_balance_throws_domain_exception_without_saving`, `Cancelled_token_throws_before_saving`.

- [ ] **Step 4: Esegui i test per verificare che falliscano**

Run: `dotnet test tests/ModernCleanCode.Application.Tests`
Expected: FAIL (errore di compilazione: tipi non definiti).

- [ ] **Step 5: Implementa le interfacce, le eccezioni, `AccountDto`, `OpenAccount` e `OpenAccountHandler`** come da Interfaces. L'handler cerca l'id con `FindAsync`: se esiste → `ConflictException`; altrimenti `Account.Open`, `AddAsync`, `SaveChangesAsync`.

- [ ] **Step 6: Esegui `OpenAccountHandlerTests`**

Run: `dotnet test tests/ModernCleanCode.Application.Tests --filter OpenAccountHandlerTests`
Expected: PASS (4 test).

- [ ] **Step 7: Test falliti e implementazione di `TopUpAccountHandler`**

`TopUpAccountHandlerTests`: `Tops_up_and_saves_once` (10 + 5 = 15, `SaveCount == 1`), `Unknown_account_throws_not_found_without_saving`, `Negative_amount_throws_domain_exception_without_saving`. Verifica FAIL, implementa, verifica PASS con `--filter TopUpAccountHandlerTests`.

- [ ] **Step 8: Test falliti e implementazione di `ChargeTollHandler`**

`ChargeTollHandlerTests` con tariffa fissa 0,07 €/km: `Charges_tariff_for_distance_and_saves_once` (conto 10, km 10→30 → `Charged == 1.40m`, `Balance == 8.60m`), `Unknown_account_throws_not_found`, `Insufficient_funds_throws_domain_exception_and_does_not_save` (conto 1, km 0→100 → `DomainException`, `SaveCount == 0`, saldo ancora 1), `Invalid_km_range_throws_domain_exception_without_saving` (entry = exit). Verifica FAIL, implementa, verifica PASS con `--filter ChargeTollHandlerTests`.

- [ ] **Step 9: Test architetturale**

`ArchitectureTests` (usa `Assembly.GetReferencedAssemblies()`): `Domain_references_no_project_assembly` (nessun assembly il cui nome inizia con `ModernCleanCode`), `Application_references_only_Domain_among_project_assemblies` (tra gli assembly `ModernCleanCode*` referenziati c'è solo `ModernCleanCode.Domain`). Verifica che passino subito (il vincolo è già rispettato) e annota in `docs/02-application.md` come si romperebbe aggiungendo un riferimento sbagliato.

- [ ] **Step 10: Esegui tutta la suite**

Run: `dotnet test`
Expected: PASS, 0 warning. Il totale atteso è 14 (Domain) + 13 (Application: 4 + 3 + 4 + 2 architetturali) = 27; riporta nella guida il numero reale letto dall'output.

- [ ] **Step 11: Scrivi `docs/02-application.md`, aggiorna il README e committa**

La guida spiega: cosa fa un handler (carica, applica le regole del Domain, salva) e perché **non** contiene regole di business; cosa sono le porte e perché le implementa l'Infrastructure (Dependency Inversion); perché `IUnitOfWork` è separato dal repository (un solo `SaveChanges` per caso d'uso: base della transazione unica dell'outbox allo Step 7); perché niente MediatR; come provarlo (`dotnet test`); cosa può rompersi (due `Charge` concorrenti → Step 12; `OpenAccount` doppio non ancora idempotente "vero" → Step 4 con `Idempotency-Key`).

```bash
git add -A
git commit -m "feat: step 2 - livello Application (handler, porte, test architetturali)"
```

---

## Self-review

- **Copertura:** Step 2 del §6 (casi d'uso, handler, porte) coperto da Task 2; i 5 punti del Review Focus hanno un test ciascuno (not found, nessun save su errore, conflitto, importi negativi, cancellazione).
- **Coerenza tipi:** nomi `OpenAccount`, `TopUpAccount`, `ChargeToll`, `AccountDto`, `ChargeTollResult`, `IAccountRepository`, `IUnitOfWork`, `ITariffProvider` identici in Interfaces e step.
- **Da correggere in esecuzione:** il conteggio dei test nello Step 10 va letto dall'output reale, non dedotto.
