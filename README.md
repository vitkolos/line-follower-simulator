# Line Follower Simulator

## Anotace

Program umožňuje simulovat chování robota jezdícího po čáře. Na vstupu se načte assembly s kódem popisujícím vnitřní fungování robota. Následně se spustí simulace, kdy se několik takových robotů umístí na předem danou mapu a po této mapě určitou dobu jezdí. Výstupem je grafické znázornění jednotlivých trajektorií, z nějž lze vyčíst „stabilitu“ vnitřní logiky robotů.

## Uživatelská dokumentace

### Základní postup

Typické použití programu sestává z několika kroků. Uživatel nejprve načte dráhu (track, map), po níž robot bude jezdit. Dále do ní robota umístí a vhodně konfiguruje rozměry a vzdálenosti, aby co nejlépe odpovídaly realitě. Poté načte assembly s vnitřní logikou robota.

Dále spustí „živou“ simulaci jednoho robota v reálném čase (live simulation), což mu pomůže odhalit ty nejzávažnější problémy. Následně provede paralelní simulaci většího množství robotů s jistými náhodnými odchylkami, čímž ověří, zda bezchybný průjezd dráhy při simulaci jednoho robota nebyl pouze šťastnou náhodou.

Pokud se během simulací projeví nějaký nedostatek, uživatel může nahrát nové assembly a simulaci spustit znova. Obdobně lze měnit soubor s dráhou či konfiguraci pozice, rozměrů a vzdáleností.

### Spuštení

Po stažení obsahu tohoto repozitáře stačí ke spuštení programu použít příkaz `dotnet run` ve složce `SimulatorApp`, k tomu je potřeba disponovat [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Mělo by se otevřít okno s grafickým uživatelským rozhraním.

### Vstupní soubory

Aby bylo možné program rozumně používat, je potřeba disponovat souborem s dráhou (rastrovým obrázkem v jednom z běžných formátů, např. PNG nebo JPEG), příkladem takového souboru je `SimulatorApp/Assets/track.png`. Další vhodné dráhy lze najít [na GitHubu](https://github.com/jaresan/ArduinoSimulator/tree/18106315eedb868713eca6dc190a1462eb5e45d9/public/assets). Kromě lokálního souboru lze vložit i URL adresu obrázku.

Rovněž je nutné získat assembly s kódem robota. Součástí načítaného assembly by vždy měla být právě jedna veřejná třída, která je potomkem třídy `RobotBase` z projektu `CoreLibrary`. Ve složce `UserDefinedRobot` je připravený vzorový kód robota. Z něj se assembly získá klasicky příkazem `dotnet build`, výsledný soubor `UserDefinedRobot.dll` lze rovnou použít v aplikaci.

### Kód robota

Program se zaměřuje na simulaci robota jezdícího po čáře – cílem simulace je tedy posoudit kvalitu převodu mezi binárním vstupem senzorů (každý buď snímá, nebo nesnímá čáru) a rychlostí motorů.

Očekávám, že cílem uživatelů tohoto programu bude naprogramovat robota na platformě Arduino. Proto lze při programování vnitřního fungování robota používat základní funkce, které jsou na této platformě běžně k dispozici.

Jako jednotné rozhraní k programování robota slouží třída `RobotBase` v projektu `CoreLibrary`. Assembly s kódem robota by mělo obsahovat jednoho veřejného potomka této třídy. Ten musí implementovat metody `Setup` a `Loop` (používají se velmi podobně jako odpovídající funkce platformy Arduino), dále pak vlastnosti `MotorsMicroseconds` a `FirstSensorPin`. Je možné rovněž implementovat vlastnost `InternalState` (o té [viz níže](#průběh-simulace-v-reálném-čase)). Kromě toho jsou k dispozici metody `PinMode`, `DigitalRead`, `DigitalWrite` a `Millis`, které [se chovají podle očekávání](https://www.arduino.cc/reference/en/).

#### Setup a Loop

Metoda `Setup` se volá při vytváření instance robota, tedy při jeho umístění na dráhu. Při té příležitosti se také poprvé volá `Loop` (při tomto volání vrací metoda `Millis` hodnotu 0). Dále se `Loop` volá až během simulace, rozestupy mezi jednotlivými voláními jsou však *z pohledu robota* vždy v řádech jednotek milisekund (při simulaci v reálném čase je to 6 ms, při paralelní simulaci 6 ± 3 ms).

#### Povinné vlastnosti

Vlastnost `FirstSensorPin` určuje, na jakých pinech se bude načítat vstup ze senzorů. Těch je dohromady pět, hodnoty z nejlevějšího jsou dostupné na pinu s číslem odpovídajícím hodnotě `FirstSensorPin`, hodnoty z druhého senzoru zleva jsou na následujícím pinu atd. Čteme-li na pinu hodnotu `false`, snímá senzor černou (tmavou) barvu, čteme-li `true`, snímá bílou (světlou) barvu.

Aby bylo jasné, jak se má *virtuální robot* po dráze pohybovat, pro zjištění rychlostí motorů se používá vlastnost `MotorsMicroseconds`. Ta je typu `MotorsState`, což je struktura, která funguje vlastně jako uspořádaná dvojice. První hodnotou je šířka impulsu v mikrosekundách pro PWM levého servomotoru. Druhá hodnota odpovídá pravému motoru. Při 1500 mikrosekundách se motor netočí. Vyšší hodnoty jím otáčejí na jednu stranu, nižší na druhou. Pro jednoduchost je převod mezi šířkou impulzu a rychlostí motoru lineární a nemá omezený rozsah hodnot – [u reálných servomotorů je to však odlišné](https://learn.parallax.com/tutorials/robot/shield-bot/robotics-board-education-shield-arduino/chapter-3-assemble-and-test-5).

### Číselná nastavení

Poznámka: Plátno je oblast ve středu obrazovky, kam se umisťuje obrázek s dráhou a kde se robot obvykle pohybuje.

- konfigurace plátna
    - výška/šířka – určuje maximální rozměr obrázku s dráhou (tedy větší z rozměrů obrázku se přizpůsobí této hodnotě, ten druhý se přizpůsobí, aby nedošlo k deformaci obrázku)
    - zoom – přeškáluje veškerý obsah plátna (dráhu, robota i trajektorie)
- pozice robota
    - X, Y – souřadnice v pixelech, počátek soustavy je vlevo dole, kladný směr vpravo/nahoře
    - R° – úhel natočení robota ve stupních
- konfigurace robota
    - velikost – koeficient, kterým se zvětšuje/zmenšuje samotný robot
    - vzdálenost senzorů – vzdálenost linie senzorů od těla robota v pixelech
    - rychlost – koeficient, jímž se násobí rychlost robota

### Ovládání simulací

Aby bylo možné simulaci spustit, je nutné vše vhodně nastavit. K tomu slouží horní tři sekce v grafickém rozhraní, každá z nich se potvrzuje tlačítkem vpravo.

„Živou“ simulaci lze spustit, pozastavit, obnovit do výchozího stavu nebo také vykreslit její trajektorii (pak ji lze opět skrýt). Při pozastavení simulace se aktuální souřadnice a natočení robota načtou do konfiguračního formuláře.

K ovládání paralelní simulace slouží jedno tlačítko – to nejprve umožňuje simulaci spustit. Tento proces lze v průběhu zrušit. Nakonec se vykreslí trajektorie simulovaných robotů, ty je možné skrýt.

#### Použití myši

Kliknutím myší na libovolné místo na plátně se souřadnice daného bodu načtou do konfiguračního formuláře. Pokud v dané chvíli simulace neprobíhá, tato volba souřadnic se rovnou potvrdí. Tehdy lze rovněž měnit natočení robota – pokud při stisknutém levém nebo pravém tlačítku myši uživatel otáčí kolečkem myši.

#### Průběh simulace v reálném čase

V průběhu „živé“ simulace se robot pohybuje po dráze (respektive po celé ploše okna). Pět kroužků před robotem odpovídá jednotlivým senzorům – kroužek je červený, pokud snímá tmavou (černou) barvu, jinak je zelený. Diagram robota je rozdělen příčnou čarou, to je osa kol (její střed je rovněž vyznačen).

Mají-li některé piny `PinMode` nastaven na `PMode.Output`, v sekci Internal state (dole na obrazovce) se pro každý takový pin objeví „dioda“. Ta se zbarví červeně, pokud je hodnota pinu `true` (HIGH), jinak má bílou barvu.

Pro piny s `PMode.InputPullup` se v téže sekci objeví tlačítko. Je-li stisknuté, na pinu lze číst hodnotu `false` (LOW), jinak má pin hodnotu `true` (HIGH, což odpovídá přítomnosti pull up rezistoru).

Pokud potomek RobotBase implementuje vlastnost `InternalState`, zobrazuje se její aktuální hodnota také v sekci Internal state.

Žádná z těchto možností přístupu k okamžitému stavu robota není k dispozici při paralelní simulaci. Nelze tedy například spoléhat na stisknutí tlačítka ve vhodnou chvíli (a využívat to třeba jako způsob aktivace robota).

## Pokročilejší konfigurace

Pro pokročilého uživatele můžou být zajímavé konstanty na začátku souborů `SimulationLive.cs` a `SimulationParallel.cs` ve složce `SimulatorApp/Simulation` a také v souboru `SimulatorApp/Robot/SimulatedRobot.cs`.

Poznámka: Jedna iterace odpovídá jednomu volání metody `MoveNext`. Ta v sobě obsahuje posunutí času vraceného metodou `Millis`, načtení vstupu (senzorů), zpracování výstupu (motorů) a volání metody `Loop`. Tedy počet iterací = počet volání `Loop` minus 1, jelikož první volání `Loop` je v konstruktoru `SimulatedRobot`.

- SimulationLive
    - IterationLimit – počet iterací, po němž dojde k automatickému pozastavení simulace (počítá se od posledního spuštění simulace)
    - IterationIntervalMs – interval mezi jednotlivými iteracemi
- SimulationParallel
    - základní konfigurace
        - MinPointDistanceMs – při vykreslování trajektorií je tohle minimální vzdálenost (v milisekundách) mezi vykreslovanými body
        - IterationCount – počet iterací jednoho robota, které se provedou během paralelní simulace
        - RobotCount – počet simulovaných robotů
        - IterationIntervalMs – průměrný interval mezi iteracemi
    - nastavení náhodnosti (náhodná rozdělení jsou *uniformní*, „odchylka“ může být kladná i záporná)
        - IterationIntervalDifference – maximální povolená odchylka od IterationIntervalMs
        - PositionDifference – maximální povolená odchylka od nastavených počátečních souřadnic (pro osy X a Y se počítá nezávisle)
        - RotationDifference – maximální povolená odchylka od nastaveného počátečního natočení (v radiánech)
        - SensorErrorLikelihood – pravděpodobnost, že senzor vrátí opačnou hodnotu, než přečetl
        - MotorDifference – maximální povolená odchylka od opravdové šířky impulsu v mikrosekundách (pro každý motor se počítá nezávisle)
    - přepínače náhodnosti – umožňují deaktivovat jednotlivé náhodné prvky paralelní simulace (při deaktivaci všech je výsledkem stejná dráha jako v „živé“ simulaci)
        - RandomInterval
        - RandomPosition
        - RandomSensors
        - RandomMotors
- SimulatedRobot – kromě SensorDistancesY lze tyto hodnoty nepřímo ovlivnit z GUI
    - WheelDistance – vzdálenost koleček robota (šířka osy) v pixelech
    - SpeedCoefficient – koeficient rychlosti v pixelech za sekundu
    - SensorDistancesY – vzdálenosti senzorů od podélné osy procházející středem robota (tedy vlastně Y souřadnice)

Výchozí hodnoty formulářových polí, s nimiž je program spouštěn, lze konfigurovat v horní části souboru `SimulatorApp/App/MainWindow.axaml.cs` pomocí proměnné `_defaultValues`.

## Vývojová dokumentace

- WinExe
- konstanty
