[README.md](https://github.com/user-attachments/files/27177373/README.md)

Konzolová aplikace v C# pro zobrazování NHL hokejových výsledků a statistik.
Data jsou stahována z oficiálního, bezplatného NHL Stats API (`api-web.nhle.com`) – nevyžaduje žádný API klíč.

## Funkce aplikace

| Volba | Popis |
|-------|-------|
| 1 | Výsledky posledních 7 dní |
| 2 | Tabulka týmových statistik (výhry, góly, win%, průměr) |
| 3 | Top 5 nejgólovějších zápasů |
| 4 | Souhrnná statistika gólů (průměr, max, min, celkem) |

## Architektura a SOLID

SportsDashboard/
├── Models/         – datové třídy (GameScore, TeamStats, PlayoffSeries)
├── Services/       – IHockeyService, NhlApiService (HTTP, async)
├── Processors/     – StatsProcessor (agregace, výpočty, filtrování)
├── UI/             – ConsoleUI (veškerý výstup na konzoli)
├── Exceptions/     – vlastní výjimky (ApiException, Timeout, Unavailable, ParseException)
└── Program.cs      – composition root, hlavní smyčka

### SOLID principy

- **Single Responsibility:** Každá třída má jednu zodpovědnost. `NhlApiService` pouze komunikuje s API, `StatsProcessor` pouze počítá, `ConsoleUI` pouze vykresluje.
- **Open/Closed:** Nový sport nebo zdroj dat → nová třída implementující `IHockeyService`, žádná změna existujícího kódu.
- **Liskov Substitution:** `NhlApiService` je zaměnitelná za libovolnou jinou implementaci `IHockeyService` (např. `MockHockeyService` pro testy).
- **Interface Segregation:** `IHockeyService` obsahuje pouze metody relevantní pro hokejová data.
- **Dependency Inversion:** `Program.cs` závisí na rozhraní `IHockeyService`, nikoli na konkrétní implementaci.
- 
## Asynchronní zpracování

- `GetRecentScoresAsync` spouští stahování dat pro každý den **paralelně** pomocí `Task.WhenAll` – výrazně zkracuje dobu odezvy.
- Všechna HTTP volání (`HttpClient.GetAsync`) jsou `async/await` – vlákno není blokováno čekáním na I/O.
- `CancellationToken` je propagován přes celou call chain (od uživatelského `Ctrl+C` přes `Program.cs` až do `HttpClient`).
- Výjimky uvnitř tasků jsou korektně zachyceny – selhání jednoho dne neukončí celý request.
- 
## Zpracování dat (StatsProcessor)

Veškerá logika je **čistá funkce**:

- **Agregace:** výpočet výher, proher, gólů pro každý tým z raw dat zápasů
- **Výpočty:** winRate, průměr gólů, goal differential
- **Třídění:** tabulka seřazena dle winRate, pak dle goal diff
- **Filtrování:** výsledky pro konkrétní tým
- **Min/max/avg:** souhrnná statistika gólů přes všechny zápasy
  
## Výjimky a chybové stavy

| Výjimka | Situace |
|---------|---------|
| `ApiTimeoutException` | HTTP požadavek vypršel (TaskCanceledException) |
| `ApiUnavailableException` | Nelze se připojit k API (HttpRequestException) |
| `ApiException` | HTTP chyba (404, 500 apod.) |
| `DataParseException` | Neplatný JSON v odpovědi |

Výjimky jsou **vyvolány v servisní vrstvě** a **zachyceny v Program.cs** – správná propagace napříč vrstvami. UI nikdy nezachycuje výjimky sám – pouze je dostane zobrazit přes `ConsoleUI.PrintError()`.

## Spuštění

Vyžaduje: připojení k internetu.
