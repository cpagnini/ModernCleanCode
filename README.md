# ModernCleanCode

Corso passo passo su una soluzione **.NET con Clean Architecture** il cui database e i cui messaggi rispettano **idempotenza e persistenza** verso servizi esterni.

## Il dominio

Una rete autostradale con dispositivi IoT (varchi di pedaggio, sensori, pannelli a messaggio variabile). Nomi generici, nessun marchio reale. Tre flussi:

1. **Ingresso (IoT → backend):** passaggi ai varchi, con duplicati, ritardi e disordine.
2. **Uscita (backend → IoT):** incidente → Transactional Outbox → bus → consumer (Emergenze, Pannelli, Reporting).
3. **Servizi esterni inaffidabili:** pagamenti e emergenze, con timeout e risposte doppie.

I dispositivi sono **simulati** in .NET, quindi il corso gira offline, senza Docker né Azure.

## Come leggere il corso

Ogni step è un commit con la sua guida in `docs/`. Per ogni pattern il corso mostra prima il problema:

- `[RED]` il codice ingenuo con un test che **fallisce**;
- `[GREEN]` la correzione che lo rende verde.

Gli altri commit sono sempre verdi.

## Indice

| Step | Guida |
|------|-------|
| 0 | [Setup](docs/00-setup.md) |
| 1 | [Domain](docs/01-domain.md) |
| 2 | [Application](docs/02-application.md) |

Il progetto completo è descritto in [docs/superpowers/specs](docs/superpowers/specs/2026-10-23-moderncleancode-design.md).
