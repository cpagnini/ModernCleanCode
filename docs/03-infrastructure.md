# Step 3 — Infrastructure (EF Core + SQL Server)

## Problema
L'Application dice "mi serve qualcuno che carichi e salvi i conti" (`IAccountRepository`, `IUnitOfWork`), ma non sa nulla di database. Serve il pezzo che parla davvero con SQL Server, senza che il Domain né l'Application ne vengano contaminati.

## Soluzione
Il progetto `ModernCleanCode.Infrastructure` referenzia Application e Domain (mai il contrario) e contiene:

| Cosa | Ruolo |
|------|-------|
| `AppDbContext` | la sessione con il database; espone `Accounts` |
| `AccountConfiguration` | come `Account` diventa una riga: tabella `Accounts`, `Id` come chiave, `Balance` come `decimal(19,4)` |
| `EfAccountRepository` | implementa `IAccountRepository` |
| `EfUnitOfWork` | implementa `IUnitOfWork`: un solo `SaveChangesAsync` per caso d'uso |
| `Migrations/InitialCreate` | lo script che crea la tabella, generato da `dotnet ef` |
| `AppDbContextFactory` | serve solo a `dotnet ef` per creare il contesto a design-time |

## Perché così
- **Le frecce puntano verso l'interno.** L'Infrastructure conosce l'Application, non il contrario. Se domani cambi database, tocchi solo questo progetto. Un test architetturale lo verifica.
- **Test su SQL Server vero, non in memoria.** L'obiettivo del corso è che il *database* garantisca l'idempotenza (chiavi, indici). Un database finto non ha quei vincoli: i test passerebbero e il sistema reale no. Ogni classe di test crea un database LocalDB suo (`ModernCleanCode_Test_<guid>`) e lo elimina alla fine.
- **Contesti diversi per scrivere e rileggere.** Con lo stesso contesto EF restituisce l'oggetto che ha già in memoria e il database non viene mai interrogato. Con un contesto nuovo la lettura va davvero al database.
- **`decimal(19,4)`.** `Money` accetta fino a 4 decimali (tariffe come 0,0333). Con 2 decimali il database arrotonderebbe in silenzio e perderesti centesimi di centesimo senza accorgertene.
- **Il Domain non è stato toccato.** `Account` ha costruttore e setter privati. EF riesce a ricostruirlo usando il costruttore privato (i parametri `id` e `balance` coincidono per nome con le proprietà). Se non avesse funzionato, la correzione andava nel mapping, non nel Domain.
- **Una dipendenza vulnerabile.** Il build è stato fermato da `TreatWarningsAsErrors`: SQL Server portava `System.Security.Cryptography.Pkcs` 9.0.11, con vulnerabilità note. Abbiamo aggiunto un riferimento diretto alla versione corretta (10.0.13) invece di spegnere l'avviso. È il motivo per cui esiste quella regola.

## Come provarlo
```bash
dotnet test
```
Atteso: 35 test superati (15 Domain + 13 Application + 7 Infrastructure), 0 warning. Serve LocalDB avviata.

## Come verificare che sia giusto (tocca a te)
1. **I dati arrivano davvero al database?** Nei test, scrittura e lettura usano contesti diversi. Se li unissi in uno, il test passerebbe comunque: è il tipo di test "bugiardo" da riconoscere.
2. **`Add` senza `SaveChanges` non deve persistere.** C'è un test apposta (`Add_without_save_is_not_persisted`): senza unit of work nessun dato è al sicuro.
3. **Il modello coincide con la migration?** `Model_has_no_pending_changes_compared_to_migrations` fallisce se cambi il mapping e dimentichi di generare una nuova migration.
4. **Il cleanup è sicuro?** Dopo `dotnet test` controlla che non restino database `ModernCleanCode_Test_*` (`sqlcmd -S "(localdb)\MSSQLLocalDB" -C -Q "SELECT name FROM sys.databases"`). Il cleanup rifiuta di eliminare database senza il prefisso di test.
5. **Mutazione.** Cambia `decimal(19,4)` in `decimal(19,2)` nel mapping e rigenera: `Balance_keeps_four_decimals` deve fallire.
6. **Cosa NON è ancora protetto.** Il controllo "esiste già?" + "aggiungi" non è atomico; la chiave primaria protegge i dati, ma l'errore non è ancora tradotto. Lo vediamo nello Step 3b.

## Cosa può rompersi
- LocalDB non avviata → i test falliscono in connessione (`sqllocaldb start MSSQLLocalDB`).
- Il mapping usa il costruttore privato per nome: se rinomini un parametro di `Account`, EF non riesce più a ricostruirlo.
- `Money` accetta più di 4 decimali: il database li arrotonderebbe. Oggi nessuna regola del Domain lo impedisce: lacuna nota.
