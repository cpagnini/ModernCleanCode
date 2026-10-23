# Step 1 — Domain

## Problema
Il cuore del sistema sono le regole del business: un pedaggio non è mai negativo, un conto non va sotto zero, un passaggio al varco ha sempre un identificativo. Se queste regole vivono sparse tra controller, database e script, prima o poi qualcuno le salta.

## Soluzione
Un progetto `ModernCleanCode.Domain` che **non dipende da nulla**: niente EF Core, niente ASP.NET, nessun pacchetto NuGet. Contiene solo:

| Tipo | Cosa garantisce |
|------|-----------------|
| `Money` | importo mai negativo, valuta sempre EUR, sottrazione oltre il disponibile impossibile |
| `Account` | `Charge` rifiuta un importo superiore al saldo e lascia il saldo intatto; addebito pari al saldo → saldo 0 |
| `GatePassage` | `PassageId`, varco e transponder obbligatori; l'orario è sempre normalizzato in UTC |
| `TollTariff` | pedaggio = tariffa/km × distanza, arrotondato a 2 decimali *away from zero* |
| `DomainException` | l'unico modo in cui il Domain dice "questa regola è violata" |

## Perché così
- **Il Domain non dipende da nulla** perché è la parte che vive più a lungo. Database e framework cambiano; le regole del pedaggio no. In Clean Architecture le dipendenze puntano sempre verso il Domain.
- **`Money` è un value object, non un `decimal`.** Un `decimal` accetta `-5` e non sa di che valuta è. `Money` rende impossibile creare uno stato sbagliato: il costruttore è privato e si passa da `Money.Eur(...)`.
- **Le regole stanno nelle entità**, non in un servizio. `Account.Charge` sa da solo di non poter andare sotto zero, quindi nessun chiamante può dimenticarsi il controllo.
- **Orario in UTC** perché lo Step 6 dovrà ordinare eventi che arrivano da dispositivi in fusi diversi: meglio normalizzare subito.
- **Arrotondamento esplicito.** `Math.Round` di default arrotonda al pari ("banker's rounding"): 0,035 diventerebbe 0,04 ma 0,025 diventerebbe 0,02. Per soldi usiamo `AwayFromZero`, che è quello che si aspetta chi guarda uno scontrino.

## Come provarlo
```bash
dotnet test
```
Atteso: 14 test superati, 0 warning.

## Cosa può rompersi
- `default(Money)` esiste sempre ed è zero euro: va bene, ma ricordalo se un giorno aggiungi altre valute.
- Nessun controllo di concorrenza qui: due `Charge` simultanei sullo stesso conto in memoria si scavalcano. Lo risolviamo allo Step 12 con `rowversion`.
- `GatePassage` non sa ancora riconoscere i duplicati: lo fa il database allo Step 6.
