# Step 3b — L'unicità la garantisce il database

Questo è il primo pattern del corso, quindi ha due commit: `[RED]` (il problema) e `[GREEN]` (la correzione).

## Problema
`OpenAccountHandler` fa due cose: "il conto esiste già?" e poi "aggiungilo". Fra le due passa del tempo. Se due richieste identiche arrivano insieme (un client che riprova, un messaggio consegnato due volte), **entrambe** vedono "non esiste" e **entrambe** provano a inserire. Il controllo in codice non può chiudere questa finestra: solo il database può.

## Il commit `[RED]`
`UniqueIdTests.Second_save_with_same_id_throws_conflict_exception` simula la gara in modo deterministico: due contesti aggiungono lo stesso id, il primo salva, il secondo salva dopo. Il test **fallisce**:

```
Expected: ConflictException
Actual:   DbUpdateException
```

Due cose da notare:
- **I dati sono al sicuro.** Il database ha rifiutato il secondo inserimento grazie alla chiave primaria su `Id`: non esistono due conti con lo stesso id. L'idempotenza dei *dati* è già garantita.
- **L'errore però è grezzo.** Un `DbUpdateException` con un `SqlException` dentro è un dettaglio tecnico. Se arrivasse fino all'API, l'utente vedrebbe un 500 (errore del server) invece di un 409 (conflitto).

## Il commit `[GREEN]`
`EfUnitOfWork.SaveChangesAsync` cattura `DbUpdateException` **solo** quando l'errore interno è un `SqlException` con numero 2627 (violazione di chiave primaria/univoca) o 2601 (indice univoco), e lo traduce in `ConflictException`. Ogni altro errore esce invariato.

## Perché così
- **Il database è l'arbitro finale.** Il controllo nell'handler è comodo (risponde subito "conflitto" nel caso normale), ma la garanzia vera è il vincolo. Sono due difese a livelli diversi.
- **Si traduce solo l'errore giusto.** Catturare ogni `DbUpdateException` come "conflitto" nasconderebbe errori veri (disco pieno, trigger che fallisce, timeout) dietro un 409 fuorviante.
- **La traduzione sta nell'Infrastructure.** I numeri 2627/2601 sono un dettaglio di SQL Server: l'Application conosce solo `ConflictException`.

## Come provarlo
```bash
dotnet test
```
Atteso: 37 test superati (15 Domain + 13 Application + 9 Infrastructure), 0 warning.

## Come verificare che sia giusto (tocca a te)
1. **Vedi il rosso.** Torna al commit `[RED]` (`git checkout a467c43`) e lancia `dotnet test --filter UniqueIdTests`: deve fallire con `DbUpdateException`. Poi `git checkout main`.
2. **Mutazione sul filtro.** In `EfUnitOfWork.cs` togli la condizione sul numero (`when (ex.InnerException is SqlException)`): `Other_database_errors_are_not_translated_to_conflict` **deve** fallire. Se non fallisce, quel test non protegge nulla.
3. **Mutazione sulla traduzione.** Togli il `try/catch` intero: `Second_save_with_same_id_throws_conflict_exception` deve fallire.
4. **L'unicità è davvero nel database?** Elimina la riga `HasKey(a => a.Id)` dal mapping, rigenera la migration e riprova: senza chiave primaria i due conti con lo stesso id esisterebbero entrambi.

**Un caso reale di test bugiardo.** La prima versione del secondo test usava un importo enorme (`1e16`) per provocare "un altro errore del database". Passava, ma la mutazione del punto 2 **non la faceva fallire**: l'errore veniva respinto dal driver *prima* di arrivare a SQL Server (era un `ArgumentException`, non un `SqlException`), quindi il test non esercitava il caso che dichiarava. Ora un trigger di test lancia un vero errore SQL 50000 solo per un saldo particolare (666,6666), e la mutazione viene catturata. Lezione: un test che passa non dimostra niente finché non hai visto fallire la versione rotta.

## Cosa può rompersi
- I numeri 2627/2601 sono specifici di SQL Server: con un altro database vanno cambiati (per questo vivono in un solo punto).
- Questo risolve il **doppio inserimento**. Non risolve ancora due *addebiti* simultanei sullo stesso conto (Step 12) né un client che ripete la richiesta e vuole la *stessa risposta* (Step 4, `Idempotency-Key`).
- Il trigger `TestBoom` esiste solo nel database di test di quella classe e viene distrutto con esso.
