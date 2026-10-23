# ModernCleanCode: design del corso

Data: 2026-10-23 · Autore: Claudio Pagnini (con Claude) · Repo di destinazione: https://github.com/cpagnini/ModernCleanCode.git

## 1. Intento

Un corso **per l'autore stesso** (poi pubblicabile) per capire, con calma, come si costruisce una soluzione .NET con **Clean Architecture** il cui database e i cui messaggi rispettano **idempotenza e persistenza** nei confronti di servizi esterni.

Il corso è una guida fatta di **commit successivi**: ogni commit introduce un solo concetto, è compilabile, ha i test che passano, ed è accompagnato da un documento markdown che spiega il perché passo passo. Il criterio di successo principale: **l'autore, rileggendo la guida, capisce ogni pezzo e saprebbe rifarlo.** Niente fretta.

## 2. Dominio: rete autostradale con IoT

Scelto perché è "una realtà grossa" (volumi alti, soldi veri, sistemi esterni) e perché i dispositivi in campo rendono naturale il tema IoT. Nomi generici, nessun marchio reale.

Contesto: una rete autostradale ha dispositivi in campo (varchi di pedaggio, sensori di traffico/meteo, pannelli a messaggio variabile) che parlano con un backend .NET.

Tre flussi che il corso deve mostrare, ciascuno con il proprio problema di idempotenza:

1. **Ingresso (IoT → backend): passaggi ai varchi.** I varchi inviano passaggi duplicati, in ritardo e fuori ordine. Il backend li deduplica, calcola il pedaggio, addebita il conto del viaggiatore.
2. **Uscita (backend → IoT): incidenti e pannelli.** La creazione di un incidente scrive nella stessa transazione l'incidente e il messaggio in outbox (come nel diagramma di partenza); un servizio in background lo pubblica e i consumer (Emergenze, Pannelli, Reporting) lo gestiscono. I comandi ai pannelli devono essere ripetibili senza effetti doppi.
3. **Servizi esterni inaffidabili.** Un sistema di pagamento e un sistema emergenze esterni possono andare in timeout o rispondere due volte. Si usa idempotency key, retry con Polly, timeout, circuit breaker.

Riferimento originale: il diagramma `download.svg` (API → transazione unica Incidents+OutboxMessages → BackgroundService → Service Bus → 3 consumer). È il flusso 2 e resta nel corso.

## 3. Obiettivi, non-obiettivi, anti-obiettivi

**Obiettivi**
- Clean Architecture con dipendenze verso l'interno: Domain, Application, Infrastructure, API.
- Pattern: Transactional Outbox, Inbox/deduplica nei consumer, Idempotency-Key sull'API, concorrenza ottimistica, gestione out-of-order.
- Database SQL Server con vincoli che *garantiscono* l'idempotenza (indici univoci), non solo il codice.
- Test a ogni step, inclusi test di integrazione sul database e test che riproducono i guasti (duplicato, timeout, crash a metà).
- Ogni commit ha una spiegazione in italiano in `docs/`.

**Non-obiettivi**
- Non è un prodotto: nessuna UI, nessuna autenticazione completa, nessun deploy in produzione.
- Non copre microservizi distribuiti reali: un solo repo, host separati solo dove serve.
- Non insegna Azure/IoT Hub come tecnologia: sono capitoli opzionali alla fine.

**Anti-obiettivi (sarebbe un fallimento anche se funziona)**
- Codice corretto ma che l'autore non riesce a spiegare.
- Pattern aggiunti "perché si usano" senza prima mostrare il problema che risolvono.
- Commit enormi che mescolano più concetti.

## 4. Vincoli

- .NET SDK 10.0.401 installato; git 2.55; `sqlcmd` presente. **Docker non c'è.**
- Il corso deve girare **offline** su Windows 11 senza Azure né Docker.
- SQL Server locale (LocalDB o istanza installata: da verificare allo step 0).
- Lingua: documentazione in italiano; codice e identificatori in inglese.
- Il repo `C:\Progetti\CleanCode` diventa la root del repo `ModernCleanCode` (decisione del progettista, da confermare).

## 5. Approcci considerati

**Dominio.** (A) Biglietteria eventi, (B) E-commerce, (C) Ride-sharing, (D) Banca/bonifici, (E) Autostrade/telepedaggio. **Scelto E**, perché l'autore voleva una realtà grossa con IoT. D resta possibile come secondo corso.

**Ingresso dati dai dispositivi.**
- IoT Hub reale: fedele, ma richiede Azure e non è offline. Scartato come base.
- Broker MQTT (Mosquitto): fedele all'IoT vero, ma richiede un'installazione in più. Rinviato a un capitolo avanzato.
- **Simulatore .NET dietro la porta `IDeviceGateway`: scelto dall'autore.** Gli adapter reali si aggiungono dopo senza toccare Domain/Application.

**Message bus** (scelta del progettista, non esplicitamente discussa con l'autore). Porta `IMessageBus` con due adapter: uno locale basato su SQL Server, uno per Azure Service Bus. Il locale è il default del corso, Service Bus è un capitolo opzionale.

**Persistenza** (scelta del progettista): EF Core + SQL Server, migration, indici univoci per la deduplica.

**Mediator** (scelta del progettista): handler scritti a mano, senza MediatR, per non nascondere il flusso a chi sta imparando.

## 6. Struttura proposta del corso (ordine e confini degli step sono modificabili)

Ogni step = un commit + `docs/NN-titolo.md` con: problema, soluzione, perché così, come provarlo, cosa può rompersi.

0. Setup repo, soluzione, README, verifica strumenti.
1. Domain: entità e value object (viaggiatore, conto, passaggio, tariffa, incidente), test unitari.
2. Application: casi d'uso e handler, porte.
3. Infrastructure: EF Core + SQL Server, migration, repository.
4. API: endpoint, validazione, `Idempotency-Key`.
5. Simulatore dispositivi e `IDeviceGateway`.
6. Ingresso passaggi idempotente: duplicati, indice univoco, out-of-order.
7. Transactional Outbox + `BackgroundService`.
8. `IMessageBus`: adapter locale SQL.
9. Consumer con Inbox/deduplica (Emergenze, Pannelli, Reporting).
10. Servizio pagamenti esterno finto: idempotency key, Polly, timeout, circuit breaker.
11. Incidente → pannelli: comandi ripetibili.
12. Concorrenza ottimistica (rowversion) e addebiti sul conto.
13. Test di integrazione dei guasti, osservabilità (log, correlation id).
14. Capitoli avanzati opzionali: adapter Azure Service Bus, MQTT, IoT Hub.

## 7. Cosa è lasciato al progettista

- Nomi esatti di entità, progetti, namespace.
- Formato preciso dei documenti oltre la struttura indicata al §6.
- Scelta tra LocalDB e istanza SQL Server installata, dopo verifica allo step 0.
- Libreria di test (xUnit di default) e di asserzioni.
- Quanti e quali step suddividere ulteriormente se uno risulta troppo grosso.

## 7b. Scelte del progettista dopo il controllo (da verificare)

- **SQL Server:** verificato sul PC, sono presenti sia LocalDB (`MSSQLLocalDB`) sia un'istanza `MSSQLSERVER` in esecuzione. Il corso usa SQL Server vero (LocalDB di default) in tutti gli step, anche nei test di integrazione; niente SQLite, perché la tesi "il database garantisce l'idempotenza" e `rowversion` richiedono SQL Server.
- **Repo:** la root è `C:\Progetti\CleanCode`; lo spec fa parte della storia. Si fanno solo commit locali; il push su GitHub avviene solo su richiesta esplicita dell'autore.
- **Percorso:** tutti i 14 step sono tracciati nel piano, ma si dettagliano e costruiscono uno alla volta, a ritmo dell'autore. Lo step 14 è un'appendice opzionale, non costruita nel piano iniziale.
- **Bus locale su SQL:** tabella-coda con polling e visibility timeout, consegna *at-least-once*, ordinamento per chiave di partizione, retry con contatore e dead letter. Si spiega nella guida cosa lo differenzia da un broker vero.
- **Dominio:** tariffa per tratta (varco di ingresso e di uscita, due eventi accoppiati per viaggio); ordine degli eventi dato dal timestamp del dispositivo, non dall'arrivo; finestra di arrivo tardivo configurabile. Addebito al conto con possibilità di rimborso.
- **Librerie:** minimal API, `Microsoft.Extensions.Http.Resilience` (Polly v8), xUnit, doppioni di test scritti a mano, `TimeProvider` per rendere il simulatore riproducibile, eccezioni di dominio invece del Result pattern.

## 7c. Decisione dell'autore sulla forma dei commit

Per ogni pattern il corso mostra **prima il problema**: un commit con il codice ingenuo e un test che *fallisce* (rosso, marcato chiaramente nel messaggio e nella guida), poi un commit con la correzione che lo rende verde. Gli step che non introducono un pattern restano un solo commit sempre verde. Il messaggio di commit dei commit rossi inizia con `[RED]`, quelli di correzione con `[GREEN]`.

## 7d. Decisione dell'autore: l'AI scrive, l'umano verifica

Il codice lo scrive un'AI; l'umano deve saper giudicare se è giusto. Ogni guida `docs/NN-*.md` ha quindi una sezione **"Come verificare che sia giusto"** con le domande da porsi (regole di business corrette, errori senza stati a metà, test che falliscono davvero con una mutazione, lacune note). Le lacune trovate vanno dichiarate nella guida, non nascoste.

## 8. Rischi e punti aperti

- Il simulatore deve produrre in modo *riproducibile* duplicati, ritardi e disordine, altrimenti i test sono instabili.
- Il bus locale su SQL Server rischia di sembrare "un trucco"; la guida deve spiegare chiaramente cosa differisce da un broker vero.
- Il corso è lungo: va confermato che l'autore vuole davvero tutti i 14 step o preferisce un percorso più corto.
