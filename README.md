# Line Follower Simulator

## Anotace

Program umožňuje simulovat chování robota jezdícího po čáře. Na vstupu se načte assembly s kódem popisujícím vnitřní fungování robota. Následně se spustí simulace, kdy se několik takových robotů umístí na předem danou mapu a po této mapě určitou dobu jezdí. Výstupem je grafické znázornění jednotlivých trajektorií, z nějž lze vyčíst „stabilitu“ vnitřní logiky robotů.

## Uživatelská dokumentace

### Základní postup

Typické použití programu sestává z několika kroků. Uživatel nejprve načte dráhu (track, map), po níž robot bude jezdit. Dále do ní robota umístí a vhodně konfiguruje rozměry a vzdálenosti, aby co nejlépe odpovídaly realitě. Poté načte assembly s vnitřní logikou robota. Dále spustí „živou“ simulaci jednoho robota v reálném čase (live simulation), což mu pomůže odhalit ty nejzávažnější problémy. Následně provede paralelní simulaci většího množství robotů s jistými náhodnými odchylkami, čímž ověří, zda bezchybný průjezd dráhy při simulaci jednoho robota nebyl pouze šťastnou náhodou. Pokud se během simulací projeví nějaký nedostatek, uživatel může nahrát nové assembly a simulaci spustit znova. Obdobně lze měnit soubor s dráhou či konfiguraci pozice, rozměrů a vzdáleností.

### Spuštení

Po stažení obsahu tohoto repozitáře stačí ke spuštení programu použít příkaz `dotnet run` ve složce `SimulatorApp`, k tomu je potřeba disponovat [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Mělo by se otevřít okno s grafickým uživatelským rozhraním.

### Vstupní soubory

Aby bylo možné program rozumně používat, je potřeba disponovat souborem s dráhou (rastrovým obrázkem v jednom z běžných formátů, např. PNG nebo JPEG), příkladem takového souboru je `SimulatorApp/Assets/track.png`. Další vhodné dráhy lze najít [na GitHubu](https://github.com/jaresan/ArduinoSimulator/tree/18106315eedb868713eca6dc190a1462eb5e45d9/public/assets). Kromě lokálního souboru lze vložit i URL adresu obrázku.

Rovněž je nutné získat assembly s kódem robota. Součástí načítaného assembly by vždy měla být právě jedna veřejná třída, která je potomkem třídy `RobotBase` z projektu `CoreLibrary`. Ve složce `UserDefinedRobot` je připravený vzorový kód robota. Z něj se assembly získá klasicky příkazem `dotnet build`, výsledný soubor `UserDefinedRobot.dll` lze rovnou použít v aplikaci.

### Kód robota

### Průběh simulace

## Vývojová dokumentace

- WinExe
- konstanty
