# TapoGreeBridge

**TapoGreeBridge** egy saját fejlesztésű okosotthon-vezérlő rendszer, amely összeköti a **Tapo hőmérőket** és a **Gree klímákat**, és egy **Android alkalmazáson** keresztül teszi vezérelhetővé őket – mindezt a **helyi hálózaton**, felhőszolgáltatás nélkül.

A projekt két részből áll:
- **TapoGreeBridge** – Windows PC-n futó háttérszolgáltatás (.NET 10, ASP.NET Core)
  https://github.com/janoslovasz1986/TapoGreeBridge.Service
- **TapoGreeBridge.Android** – natív Android vezérlő app (.NET for Android, MAUI nélkül)

---

## Motiváció

A Gree klímák beépített hőmérője pontatlan – a plafonhoz közel méri a hőmérsékletet, és nem azt adja vissza, amit a szobában valóban érzünk. A hivatalos Gree+ app ezen nem segít.

A megoldás: külső Tapo T310 hőmérő szobánként, és egy saját vezérlési logika, amely a valós hőmérséklet alapján kapcsolja a klímát hűtés és ventilátor üzemmód között – automatikusan, 24/7, a háttérben.

---

## Funkciók

### Háttérszolgáltatás (PC)
- 🌡️ **Tapo H100 hub** leolvasása (T310/T315 szenzor, KLAP protokoll)
- ❄️ **Gree klíma vezérlés** helyi UDP protokollon (AES-128 ECB + GCM auto-fallback)
- 🔄 **Automatikus hűtés↔ventilátor váltás** a valós hőmérséklet alapján, aszimmetrikus hiszterézissel
- 🛡️ **Biztonságos leállás** – service leállításkor visszaállítja a klíma setpointját, hogy ne maradjon túlhűtve a szoba
- 📅 **Időzítő** – egyszeri és napi ismétlődő ütemezés (ki/bekapcsolás üzemmóddal és hőfokkal)
- 💾 **SQLite adatbázis** – időzítők perzisztens tárolása
- 🌐 **HTTP API** – Minimal API a helyi hálózaton, az Android app ezen keresztül kommunikál
- 🔧 **Szobánként konfigurálható** – cél hőfok, hiszterézis küszöbök, SafeSetTem szobánként

### Android app
- 📱 Natív .NET for Android (MAUI nélkül), Activity/View architektúra
- 🏠 Szobánkénti kártyák – valós hőfok, klíma szenzor, üzemmód, be/ki állapot
- 🎯 Cél hőfok állítása ±0.5°C lépésekben
- ⚡ Klíma be/kikapcsolás, hűtés/ventilátor váltás közvetlenül az appból
- 🔘 Aktív/inaktív kapcsoló szobánként (ha egy klímát nem szeretnél automatikusan vezérelni)
- 🕐 Időzítő kezelés szobánként – hozzáadás, törlés, be/ki kapcsolás
- 💾 Szerver URL megjegyzése (SharedPreferences)

---

## Architektúra

```
Tapo H100 hub (T310 szenzor)
        │ KLAP / HTTP
        ▼
ThermostatBackgroundService  ──►  RoomStateStore  ◄──  SchedulerService
        │                              │
        │ UDP / AES                    │ HTTP API
        ▼                              ▼
   Gree klíma              Android app (BridgeApiClient)
```

---

## Technológiák

| Réteg | Technológia |
|-------|-------------|
| Háttérszolgáltatás | .NET 10, ASP.NET Core Minimal API, Worker Service |
| Adatbázis | SQLite, Entity Framework Core |
| Tapo protokoll | KLAP (SHA256 + AES-128/CBC), reverse-engineered |
| Gree protokoll | UDP/7000, AES-128/ECB + GCM, reverse-engineered |
| Android app | .NET for Android (net10.0-android), natív Activity/View |

---

## Projektstruktúra

```
TapoGreeBridge/                     # PC háttérszolgáltatás
├── TapoHubClient.cs                # Tapo H100 hub KLAP kliens
├── GreeAcClient.cs                 # Gree klíma UDP kliens
├── TapoPlugClient.cs               # Tapo P110/P115 plug kliens (előkészítve)
├── ThermostatController.cs         # Hűtés↔ventilátor döntési logika
├── ThermostatBackgroundService.cs  # Fő vezérlési loop
├── EnergyPollingService.cs         # P110 energiamérés (előkészítve)
├── SchedulerService.cs             # Időzítő végrehajtás
├── RoomStateStore.cs               # Szálbiztos megosztott állapot
├── EnergyDbContext.cs              # EF Core DbContext
├── EnergyRecord.cs                 # Energiamérési adatmodell
├── ScheduleEntry.cs                # Időzítő adatmodell
├── Options.cs                      # Konfigurációs modellek
├── Program.cs                      # Host + Minimal API endpontok
├── DiagnosticsRunner.cs            # Interaktív hardware teszt
└── appsettings.json                # Konfiguráció

TapoGreeBridge.Android/             # Android kliens app
├── MainActivity.cs                 # Főképernyő – szoba kártyák
├── ScheduleActivity.cs             # Időzítők kezelése
├── BridgeApiClient.cs              # HTTP kliens az API-hoz
├── Models.cs                       # Adatmodellek
└── Resources/
    └── layout/
        ├── activity_main.xml       # Főképernyő layout
        ├── activity_schedule.xml   # Időzítők képernyő layout
        ├── room_card.xml           # Szoba kártya sablon
        └── schedule_item.xml       # Időzítő lista elem sablon
```

---

## Telepítés és konfiguráció

### Előfeltételek
- .NET 10 SDK
- Windows PC (a háttérszolgáltatás Windowson fut)
- Android telefon (API 26+)
- Tapo H100 hub + T310/T315 hőmérők
- Gree klíma (Gree+ kompatibilis)

### 1. Konfiguráció

Szerkeszd az `appsettings.json`-t:

```json
{
  "Tapo": {
    "HubIp": "192.168.0.x",
    "Email": "tapo-fiok@email.com",
    "Password": "tapo-jelszo"
  },
  "Thermostat": {
    "PollIntervalSeconds": 300,
    "Rooms": [
      {
        "Name": "Nappali",
        "TapoDeviceId": "a szenzor device_id-je",
        "GreeIp": "192.168.0.x",
        "PlugIp": "",
        "TargetTemperatureCelsius": 22.0,
        "LowerHysteresisCelsius": 0.5,
        "UpperHysteresisCelsius": 1.0,
        "SafeSetTem": 24
      }
    ]
  }
}
```

> 💡 **TapoDeviceId meghatározása**: indítsd el a szolgáltatást üres `TapoDeviceId` mezővel – a hibaüzenetben felsorolja az összes talált szenzort az azonosítójukkal.

> 💡 **Gree MAC automatikus felderítés**: a klíma MAC-jét a program induláskor automatikusan kideríti a megadott IP alapján, nem kell kézzel beírni.

### 2. Háttérszolgáltatás indítása

```bash
cd TapoGreeBridge
dotnet run
```

**Diagnosztikai mód** (hardware teszt):
```bash
dotnet run -- test
```

### 3. Windows tűzfal

Az Android app eléréséhez engedélyezd az 5080-as portot (Private profil):

```powershell
New-NetFirewallRule -DisplayName "TapoGreeBridge API" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow -Profile Private
```

### 4. Android app

Nyisd meg a `TapoGreeBridge.Android.csproj`-t Visual Studio-ban, és telepítsd a telefonra F5-tel. Első indításkor add meg a PC helyi IP-jét és portját (pl. `http://192.168.0.201:5080`).

---

## HTTP API

Az API a helyi hálózaton érhető el (`http://<PC-IP>:5080`).

| Metódus | Endpoint | Leírás |
|---------|----------|--------|
| GET | `/status` | Minden szoba aktuális állapota |
| POST | `/rooms/{name}/target` | Cél hőfok beállítása `{"celsius": 22.5}` |
| POST | `/rooms/{name}/active` | Vezérlés be/ki `{"active": false}` |
| POST | `/rooms/{name}/power` | Klíma be/ki `{"on": true}` |
| POST | `/rooms/{name}/mode` | Üzemmód váltás `{"mod": 1}` (1=Hűtés, 3=Ventilátor) |
| GET | `/rooms/{name}/schedules` | Időzítők listája |
| POST | `/rooms/{name}/schedules` | Új időzítő létrehozása |
| DELETE | `/schedules/{id}` | Időzítő törlése |
| PATCH | `/schedules/{id}/enabled` | Időzítő be/ki `{"enabled": false}` |
| GET | `/energy/{roomName}` | Energiafogyasztási előzmény (dátumszűréssel) |

---

## Vezérlési logika

A `ThermostatController` aszimmetrikus hiszterézissel dolgozik, kizárólag a külső Tapo szenzor mérése alapján – a klíma saját (pontatlan) szenzorát csak megjelenítésre használja:

```
valós hőfok ≤ cél - LowerHysteresis  →  Ventilátor módra vált
valós hőfok ≥ cél + UpperHysteresis  →  Hűtés módra vált
közötte                               →  nem változtat
```

Minimum váltási idő (alapból 5 perc) védi a kompresszort a rövid ciklusoktól.

---

## Ismert korlátok

- **Tapo P110 firmware 1.4.x**: a lokális API ezen a firmware verzión nem működik (403 Forbidden). A P115 jelenleg még elérhető lokálisan.
- **Gree GCM**: néhány újabb/rebrandelt egység (pl. Cooper & Hunter) AES-GCM titkosítást használ a bind-hoz – a kliens automatikusan próbálkozik ECB után GCM-mel.
- **KLAP session**: a Tapo session ~24 óránként lejár, a kliens automatikusan újracsatlakozik.
- **Csak helyi hálózat**: az API nincs autentikálva, kizárólag megbízható otthoni hálózaton használd.

---

## Jövőbeli tervek

- Perzisztens szoba beállítások SQLite-ban (cél hőfok, hiszterézis szobánként, Android appból szerkeszthetően)
- Fűtés mód támogatás (jelenleg csak hűtés és ventilátor)
- Energiafogyasztás grafikon az Android appban
- Windows Service telepítés (automatikus indítás rendszerinduláskor)

---

## Licensz

Ez egy személyes, nem kereskedelmi projekt. A protokoll implementációk nyilvánosan elérhető reverse engineering munkákon alapulnak:
- Tapo KLAP: [python-kasa](https://github.com/python-kasa/python-kasa)
- Gree UDP: [gree-remote](https://github.com/tomikaa87/gree-remote), [greeclimate](https://github.com/cmroche/greeclimate)
