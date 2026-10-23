# ModernCleanCode Steps 0-1 (Setup e Domain) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Creare il repo con la soluzione vuota e il livello Domain (Money, Account, GatePassage, tariffa), ognuno con la sua guida in italiano.

**Architecture:** Clean Architecture; il Domain non dipende da nulla. Soluzione in `src/` e `tests/`. Ogni step è un commit con `docs/NN-titolo.md` (problema, soluzione, perché, come provarlo, cosa può rompersi).

**Tech Stack:** .NET SDK 10.0.401, C#, xUnit, SQL Server (LocalDB `(localdb)\MSSQLLocalDB` e istanza `MSSQLSERVER`, usati dallo Step 3 in poi).

**Spec:** `docs/superpowers/specs/2026-10-23-moderncleancode-design.md`

## Global Constraints

- Root del repo: `C:\Progetti\CleanCode`. Solo commit locali; **nessun push** finché l'autore non lo chiede. Remote futuro: `https://github.com/cpagnini/ModernCleanCode.git`.
- Documentazione in italiano; codice e identificatori in inglese. Nessun marchio reale.
- Ogni commit è compilabile e con test verdi, salvo i commit `[RED]` dei pattern (Step 6 in poi): qui non ce ne sono.
- Messaggi di commit terminano con la riga `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Il Domain non referenzia nessun altro progetto né pacchetto NuGet.
- Sub-agent e ricerche solo se indispensabili; limite token: massimo metà della finestra di 4 ore.

## Review Focus

- `Money` con importo negativo o valuta diversa: deve lanciare, mai produrre un risultato silenzioso.
- `Account.Charge` di un importo pari esattamente al saldo: deve riuscire e lasciare saldo 0.
- Tariffa con varco di uscita uguale a quello di ingresso o chilometraggio decrescente: deve lanciare.
- Arrotondamento dei centesimi: la tariffa si arrotonda a 2 decimali "away from zero", mai troncata.
- `GatePassage` con timestamp senza offset o `PassageId` vuoto: deve essere rifiutato.

---

### Task 0: Repo, soluzione e guida di setup (Step 0)

**Files:**
- Create: `.gitignore`, `global.json`, `Directory.Build.props`, `ModernCleanCode.slnx`, `README.md`, `docs/00-setup.md`

**Interfaces:**
- Produces: soluzione vuota apribile con `dotnet build`; `Directory.Build.props` con `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `LangVersion=latest`.

- [ ] **Step 1: Verifica gli strumenti**

Run: `dotnet --version` e `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION"`
Expected: `10.0.401` e una versione di SQL Server stampata senza errori. Se LocalDB non risponde, prova `sqlcmd -S localhost -E -Q "SELECT @@VERSION"` e annota quale istanza funziona in `docs/00-setup.md`.

- [ ] **Step 2: Inizializza git e crea i file di base**

Run: `git init -b main`, `dotnet new gitignore`, `dotnet new globaljson --sdk-version 10.0.401 --roll-forward latestFeature`, `dotnet new sln -n ModernCleanCode`. Scrivi `Directory.Build.props` con le quattro proprietà sopra.
Expected: `dotnet build` termina con "0 Warning(s), 0 Error(s)".

- [ ] **Step 3: Scrivi `README.md` e `docs/00-setup.md`**

README: cos'è il corso, il dominio (rete autostradale con IoT, nomi generici), indice degli step linkato, regola `[RED]`/`[GREEN]`. Guida 00: strumenti richiesti, come verificare SQL Server, come è organizzato il repo, perché `TreatWarningsAsErrors`.

- [ ] **Step 4: Commit (include lo spec e questo piano nella storia)**

```bash
git add -A
git commit -m "chore: step 0 - repo, soluzione vuota e guida di setup"
```

---

### Task 1: Domain (Step 1)

**Files:**
- Create: `src/ModernCleanCode.Domain/ModernCleanCode.Domain.csproj` (classlib net10.0)
- Create: `src/ModernCleanCode.Domain/Money.cs`, `Account.cs`, `GatePassage.cs`, `TollTariff.cs`, `DomainException.cs`
- Create: `tests/ModernCleanCode.Domain.Tests/ModernCleanCode.Domain.Tests.csproj` (xUnit, riferisce il Domain)
- Create: `tests/ModernCleanCode.Domain.Tests/MoneyTests.cs`, `AccountTests.cs`, `GatePassageTests.cs`, `TollTariffTests.cs`
- Create: `docs/01-domain.md`

**Interfaces:**
- Produces (namespace `ModernCleanCode.Domain`):
  - `class DomainException : Exception` con costruttore `(string message)`.
  - `readonly record struct Money(decimal Amount, string Currency)`; `static Money Eur(decimal amount)`; `Money Add(Money other)`; `Money Subtract(Money other)`; `static Money Zero`. Importo >= 0, valuta sempre "EUR"; violazioni → `DomainException`. Sottrarre più del disponibile → `DomainException`.
  - `sealed class Account`: `Guid Id`, `Money Balance`; `static Account Open(Guid id, Money initialBalance)`; `void TopUp(Money amount)`; `void Charge(Money amount)` (lancia `DomainException` se `amount > Balance`).
  - `enum GateDirection { Entry, Exit }`; `sealed record GatePassage(Guid PassageId, string GateId, string TransponderId, DateTimeOffset OccurredAt, GateDirection Direction)` con validazione nel costruttore tramite factory `static GatePassage Create(...)`: `PassageId` non vuoto, `GateId` e `TransponderId` non vuoti, `OccurredAt.Offset` accettato ma convertito in UTC.
  - `sealed record TollTariff(Money RatePerKm)`; `Money Calculate(int entryKm, int exitKm)` = `RatePerKm * (exitKm - entryKm)` arrotondato a 2 decimali `MidpointRounding.AwayFromZero`; lancia `DomainException` se `exitKm <= entryKm`.

- [ ] **Step 1: Crea i progetti e aggiungili alla soluzione**

Run: `dotnet new classlib -n ModernCleanCode.Domain -o src/ModernCleanCode.Domain -f net10.0`, `dotnet new xunit -n ModernCleanCode.Domain.Tests -o tests/ModernCleanCode.Domain.Tests -f net10.0`, `dotnet add tests/ModernCleanCode.Domain.Tests reference src/ModernCleanCode.Domain`, `dotnet sln add` per entrambi. Rimuovi `Class1.cs` e il test di esempio.
Expected: `dotnet build` verde.

- [ ] **Step 2: Scrivi i test falliti di `Money`**

`MoneyTests`: `Eur_negative_amount_throws` (Eur(-0.01m) → DomainException), `Add_sums_amounts` (Eur(1.10m)+Eur(2.20m) = Eur(3.30m)), `Subtract_more_than_available_throws`, `Subtract_exact_gives_zero`.

- [ ] **Step 3: Esegui i test per verificare che falliscano**

Run: `dotnet test --filter MoneyTests`
Expected: FAIL (tipi non definiti, errore di compilazione).

- [ ] **Step 4: Implementa `Money` e `DomainException`** come da Interfaces. Il record struct valida in un factory; il costruttore primario non è pubblico per le istanze invalide (usa `Eur`).

- [ ] **Step 5: Esegui i test di `Money`**

Run: `dotnet test --filter MoneyTests`
Expected: PASS (4 test).

- [ ] **Step 6: Test falliti di `Account`**

`AccountTests`: `Charge_equal_to_balance_leaves_zero`, `Charge_above_balance_throws_and_keeps_balance`, `TopUp_increases_balance`. Poi implementa `Account` come da Interfaces e verifica con `dotnet test --filter AccountTests` (FAIL poi PASS).

- [ ] **Step 7: Test falliti di `GatePassage`**

`GatePassageTests`: `Create_with_empty_passage_id_throws`, `Create_with_blank_gate_throws`, `Create_normalizes_time_to_utc` (input `+02:00` → `OccurredAt.Offset == TimeSpan.Zero` e stesso istante). Implementa e verifica FAIL poi PASS.

- [ ] **Step 8: Test falliti di `TollTariff`**

`TollTariffTests`: `Calculate_multiplies_rate_by_distance` (0.07 €/km, km 10→30 = Eur(1.40m)), `Calculate_rounds_away_from_zero` (0.0333 €/km × 1 km → Eur(0.03m); 0.035 €/km × 1 km → Eur(0.04m)), `Calculate_exit_not_after_entry_throws` (uguali e decrescenti). Implementa e verifica FAIL poi PASS.

- [ ] **Step 9: Esegui tutta la suite**

Run: `dotnet test`
Expected: tutti PASS, 0 warning.

- [ ] **Step 10: Scrivi `docs/01-domain.md` e committa**

La guida spiega: perché il Domain non dipende da nulla, perché `Money` è un value object e non un `decimal`, perché le regole stanno nelle entità e non nei servizi, come provarlo (`dotnet test`), cosa può rompersi (arrotondamento, valute).

```bash
git add -A
git commit -m "feat: step 1 - livello Domain (Money, Account, GatePassage, tariffa)"
```

---

## Self-review

- **Copertura dello spec:** il piano copre Step 0 e Step 1 del §6; gli altri step avranno piani propri, come da §7b ("uno alla volta").
- **Coerenza tipi:** nomi `Money`, `Account`, `GatePassage`, `TollTariff`, `DomainException` identici in Interfaces, test e file.
- **Proporzione:** i corpi di codice non sono scritti; ci sono firme, test e valori.
