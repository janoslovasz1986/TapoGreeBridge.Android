# TapoGreeBridge.Android

Natív .NET for Android kliens (MAUI nélkül) a `TapoGreeBridge` Windows service HTTP API-jához
(`GET /status`, `POST /rooms/{name}/target`).

## Fájlok

- **MainActivity.cs** - a teljes UI-logika: szerver URL beolvasása, `/status` lekérdezés, szobánkénti kártya renderelés, +/- gombok a cél hőfok állításához
- **BridgeApiClient.cs** - vékony `HttpClient` wrapper a két API endponthoz
- **Models.cs** - a `/status` válasz JSON alakjának megfelelő modellek
- **Resources/layout/activity_main.xml** - főképernyő (URL mező, frissítés gomb, szoba-kártyák konténere)
- **Resources/layout/room_card.xml** - egy szoba kártyájának sablonja (név, mérések, +/- gombok)
- **Properties/AndroidManifest.xml** - INTERNET jogosultság + `usesCleartextTraffic="true"` (mivel az API sima HTTP, nem HTTPS)

## Megnyitás / futtatás

Nyisd meg a `TapoGreeBridge.Android.csproj`-t Visual Studio-ban (Android workload-dal), válassz egy emulátort vagy csatlakoztatott telefont, és futtasd F5-tel - pont úgy, mint a FindMe-nél.

Első indításkor írd be a szerver gépének helyi IP-jét és portját (pl. `http://192.168.0.201:5080`) - ez elmentődik a következő indításra.

## Fontos, amire figyelni kell

- **Csak otthoni WiFi-n működik** - a service-nek nincs auth-ja és csak a helyi hálózaton fut, internetről nem érhető el. Az app megpróbálja kiolvasni az aktuális WiFi SSID-t és figyelmeztet, ha nem biztos, hogy otthon vagy, de nem blokkolja a próbálkozást (ha pl. nem ad jogosultságot az SSID olvasásra, vagy hibás a detektálás).
- **`usesCleartextTraffic`** kötelező a manifestben, mert Android 9+ alapból tiltja a sima HTTP-t - ha ezt valaha kiveszed, a kérések szótlanul el fognak hasalni.
- A +/- gombok **0.5°C-os lépésekben** állítják a célt, és minden gomb-nyomás után újra lekérdezi a teljes állapotot, hogy a kártya mindig a szerver által visszaigazolt, valós állapotot mutassa (nem csak optimistán a gombnyomást).

## Ami még hiányzik / lehetséges következő lépés

- Automatikus, időzített frissítés (jelenleg csak kézi "Frissítés" gombbal vagy gombnyomás után történik újralekérdezés)
- Hibakezelés finomítása (pl. külön üzenet timeout vs. "nem ezen a hálózaton vagyunk" esetekre)
- App ikon, splash screen (a Fuse PDF-nél már csináltál ilyet, onnan átemelhető a minta)
