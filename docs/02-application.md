# Step 2 — Application

## Problema
Il Domain sa *cosa* è permesso, ma qualcuno deve dire *quando*: "apri questo conto", "addebita questo pedaggio". Quel qualcuno deve caricare i dati, chiamare le regole e salvare, senza sapere se i dati stanno in SQL Server o in memoria, e senza contenere regole di business.

## Soluzione
Il progetto `ModernCleanCode.Application` referenzia **solo** il Domain e contiene:

| Cosa | Ruolo |
|------|-------|
| `OpenAccountHandler`, `TopUpAccountHandler`, `ChargeTollHandler` | un caso d'uso ciascuno: carica → applica le regole del Domain → salva una volta |
| `IAccountRepository`, `IUnitOfWork`, `ITariffProvider` | le **porte**: cosa serve all'Application dal mondo esterno |
| `NotFoundException`, `ConflictException` | errori dell'Application (le regole violate restano `DomainException`) |

## Perché così
- **Gli handler non contengono regole.** `ChargeTollHandler` non sa che il saldo non può andare sotto zero: lo sa `Account.Charge`. Se l'handler duplicasse il controllo, due posti potrebbero divergere.
- **Le porte stanno qui, le implementazioni fuori.** È la Dependency Inversion: l'Application definisce l'interfaccia, l'Infrastructure (Step 3) la implementa. Le frecce di dipendenza puntano sempre verso il Domain.
- **`IUnitOfWork` separato dal repository.** Un caso d'uso = un solo `SaveChangesAsync`. Questo è il mattone dell'Outbox (Step 7): conto e messaggio verranno salvati nella stessa transazione.
- **Nessun salvataggio su errore.** Nei test, ogni percorso di errore verifica `SaveCount == 0`: se la regola fallisce, nulla viene scritto.
- **Niente MediatR.** Gli handler si chiamano direttamente: il flusso si legge dall'inizio alla fine. Si può aggiungere un mediator in futuro senza toccare il Domain.
- **Doppioni scritti a mano** (`InMemoryAccountRepository`, `FakeUnitOfWork`): 15 righe l'uno, nessuna magia, e si vede cosa fa il test.
- **Test architetturale.** `ArchitectureTests` fallisce se il Domain referenzia un altro progetto o se l'Application referenzia qualcosa oltre al Domain. Le regole di Clean Architecture diventano un test, non una convenzione.

## Come provarlo
```bash
dotnet test
```
Atteso: 28 test superati (15 Domain + 13 Application), 0 warning.

Per vedere l'architecture test in azione: aggiungi un riferimento da `ModernCleanCode.Domain` a `ModernCleanCode.Application` e usa un tipo di quest'ultimo; il test fallisce (o la compilazione si rompe per il riferimento circolare, che è già un segnale).

## Come verificare che sia giusto (tocca a te)
1. **L'handler salva anche quando fallisce?** Leggi `ChargeToll.cs`: `SaveChangesAsync` deve stare *dopo* `Charge`, mai prima. Poi guarda che ogni test d'errore verifichi `SaveCount == 0`.
2. **Un solo salvataggio per caso d'uso?** Cerca `SaveChangesAsync` negli handler: una chiamata ciascuno. Due salvataggi vorrebbero dire due transazioni, quindi un possibile stato a metà.
3. **L'handler contiene regole di business?** Non deve: se vedi `if (saldo < ...)` in un handler, quella regola va nel Domain.
4. **Il test fallirebbe se rompessi il codice?** Prova una mutazione: commenta la riga `account.Charge(toll)` e lancia i test. Devono fallire.
5. **I casi non coperti.** La cancellazione è testata solo in `OpenAccountHandler`, non negli altri due. È una lacuna nota, non un errore nascosto.
6. **Cosa non protegge ancora.** Due richieste simultanee sullo stesso conto: nessun test lo prova, perché oggi non è risolto (vedi sotto).

## Cosa può rompersi
- Due `ChargeToll` simultanei sullo stesso conto: entrambi leggono lo stesso saldo. Soluzione allo Step 12 (`rowversion`).
- `OpenAccount` ripetuto dà `ConflictException`: è corretto per ora, ma un client che riprova dopo un timeout vorrebbe la *stessa* risposta. Lo risolve lo Step 4 con `Idempotency-Key`.
- Il controllo "esiste già?" + "aggiungi" non è atomico: due richieste simultanee possono passare entrambe. Sarà l'indice univoco del database (Step 3) a garantirlo davvero.
