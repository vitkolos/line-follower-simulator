# Line Follower Simulator

| Vít Kološ, 2. ročník, IPP | letní semestr 2023/2024 | NPRG038 Pokročilé programování v jazyce C# |
| - | - | - |

## Anotace

Program umožňuje simulovat chování robota jezdícího po čáře. Na vstupu se načte assembly s kódem popisujícím vnitřní fungování robota. Následně se spustí simulace, kdy se několik takových robotů umístí na předem danou mapu a po této mapě určitou dobu jezdí. Výstupem je grafické znázornění jednotlivých trajektorií, z nějž lze vyčíst „stabilitu“ vnitřní logiky robotů.

## Uživatelská dokumentace

### Základní postup

Typické použití programu sestává z několika kroků. Uživatel nejprve načte dráhu (track, map), po níž robot bude jezdit. Dále do ní robota umístí a vhodně konfiguruje rozměry a vzdálenosti, aby co nejlépe odpovídaly realitě. Poté načte assembly s vnitřní logikou robota.

Dále spustí „živou“ simulaci jednoho robota v reálném čase (live simulation), což mu pomůže odhalit ty nejzávažnější problémy. Následně provede paralelní simulaci většího množství robotů s jistými náhodnými odchylkami, čímž ověří, zda bezchybný průjezd dráhy při simulaci jednoho robota nebyl pouze šťastnou náhodou.

Pokud se během simulací projeví nějaký nedostatek, uživatel může nahrát nové assembly a simulaci spustit znova. Obdobně lze měnit soubor s dráhou či konfiguraci pozice, rozměrů a vzdáleností.

### Spuštení

Po stažení obsahu tohoto repozitáře stačí ke spuštení programu použít příkaz `dotnet run` ve složce `SimulatorApp`, k tomu je potřeba disponovat [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Mělo by se otevřít okno s grafickým uživatelským rozhraním.

### Vstupní soubory

Aby bylo možné program rozumně používat, je potřeba disponovat souborem s dráhou (rastrovým obrázkem v jednom z běžných formátů, např. PNG nebo JPEG), příkladem takového souboru je `SimulatorApp/Assets/track.png`. Další vhodné dráhy lze najít [na GitHubu](https://github.com/jaresan/ArduinoSimulator/tree/18106315eedb868713eca6dc190a1462eb5e45d9/public/assets). Kromě lokálního souboru lze vložit i URL adresu obrázku.

Rovněž je nutné získat assembly s kódem robota. Součástí načítaného assembly by vždy měla být právě jedna veřejná třída, která je potomkem třídy `RobotBase` z `CoreLibrary`. Ve složce `UserDefinedRobot` je připravený vzorový kód robota. Z něj se assembly získá klasicky příkazem `dotnet build`, výsledný soubor `UserDefinedRobot.dll` lze rovnou použít v aplikaci.

### Kód robota

Program se zaměřuje na simulaci robota jezdícího po čáře – cílem simulace je tedy posoudit kvalitu převodu mezi binárním vstupem senzorů (každý buď snímá, nebo nesnímá čáru) a rychlostí motorů.

Očekávám, že cílem uživatelů tohoto programu bude naprogramovat robota na platformě Arduino. Proto lze při programování vnitřního fungování robota používat základní funkce, které jsou na této platformě běžně k dispozici.

Jako jednotné rozhraní k programování robota slouží třída `RobotBase` v `CoreLibrary`. Assembly s kódem robota by mělo obsahovat jednoho veřejného potomka této třídy. Ten musí implementovat metody `Setup` a `Loop` (používají se velmi podobně jako odpovídající funkce platformy Arduino), dále pak vlastnosti `MotorsMicroseconds` a `FirstSensorPin`. Je možné rovněž implementovat vlastnost `InternalState` (o té [viz níže](#průběh-simulace-v-reálném-čase)). Kromě toho jsou k dispozici metody `PinMode`, `DigitalRead`, `DigitalWrite` a `Millis`, které [se chovají podle očekávání](https://www.arduino.cc/reference/en/).

#### Setup a Loop

Metoda `Setup` se volá při vytváření instance robota, tedy při jeho umístění na dráhu. Při té příležitosti se také poprvé volá `Loop` (při tomto volání vrací metoda `Millis` hodnotu 0). Dále se `Loop` volá až během simulace, rozestupy mezi jednotlivými voláními jsou však *z pohledu robota* vždy v řádech jednotek milisekund (při simulaci v reálném čase je to 6 ms, při paralelní simulaci 6 ± 3 ms).

#### Povinné vlastnosti

Vlastnost `FirstSensorPin` určuje, na jakých pinech se bude načítat vstup ze senzorů. Těch je dohromady pět, hodnoty z nejlevějšího jsou dostupné na pinu s číslem odpovídajícím hodnotě `FirstSensorPin`, hodnoty z druhého senzoru zleva jsou na následujícím pinu atd. Čteme-li na pinu hodnotu `false`, snímá senzor černou (tmavou) barvu, čteme-li `true`, snímá bílou (světlou) barvu.

Aby bylo jasné, jak se má *virtuální robot* po dráze pohybovat, pro zjištění rychlostí motorů se používá vlastnost `MotorsMicroseconds`. Ta je typu `MotorsState`, což je struktura, která funguje vlastně jako uspořádaná dvojice. První hodnotou je šířka impulsu v mikrosekundách pro PWM levého servomotoru. Druhá hodnota odpovídá pravému motoru. Při 1500 mikrosekundách se motor netočí. Vyšší hodnoty jím otáčejí na jednu stranu, nižší na druhou. Pro jednoduchost je převod mezi šířkou impulzu a rychlostí motoru lineární a nemá omezený rozsah hodnot – [u reálných servomotorů je to však odlišné](https://learn.parallax.com/tutorials/robot/shield-bot/robotics-board-education-shield-arduino/chapter-3-assemble-and-test-5).

#### Třída Servo

Třídu `Servo` z `CoreLibrary` není nutné použít, ale může se hodit jako náhrada třídy `Servo` ze stejnojmenné Arduino knihovny.

### Číselná nastavení

Poznámka: Plátno je oblast ve středu obrazovky, kam se umisťuje obrázek s dráhou a kde se robot obvykle pohybuje.

- konfigurace plátna
    - výška/šířka – určuje maximální rozměr obrázku s dráhou (tedy větší z rozměrů obrázku se přizpůsobí této hodnotě, ten druhý se přizpůsobí, aby nedošlo k deformaci obrázku)
    - zoom – přeškáluje veškerý obsah plátna (dráhu, robota i trajektorie)
- pozice robota
    - X, Y – souřadnice v pixelech, počátek soustavy je vlevo dole, kladný směr vpravo/nahoře
    - R° – úhel natočení robota ve stupních
- konfigurace robota
    - velikost – koeficient, kterým se zvětšuje/zmenšuje samotný robot
    - vzdálenost senzorů – vzdálenost linie senzorů od těla robota v pixelech
    - rychlost – koeficient, jímž se násobí rychlost robota

### Ovládání simulací

Aby bylo možné simulaci spustit, je nutné vše vhodně nastavit. K tomu slouží horní tři sekce v grafickém rozhraní, každá z nich se potvrzuje tlačítkem vpravo.

„Živou“ simulaci lze spustit, pozastavit, obnovit do výchozího stavu nebo také vykreslit její trajektorii (pak ji lze opět skrýt). Při pozastavení simulace se aktuální souřadnice a natočení robota načtou do konfiguračního formuláře.

K ovládání paralelní simulace slouží jedno tlačítko – to nejprve umožňuje simulaci spustit. Tento proces lze v průběhu zrušit. Nakonec se vykreslí trajektorie simulovaných robotů, ty je možné skrýt.

#### Použití myši

Kliknutím myší na libovolné místo na plátně se souřadnice daného bodu načtou do konfiguračního formuláře. Pokud v dané chvíli simulace neprobíhá, tato volba souřadnic se rovnou potvrdí. Tehdy lze rovněž měnit natočení robota – pokud při stisknutém levém nebo pravém tlačítku myši uživatel otáčí kolečkem myši.

#### Průběh simulace v reálném čase

V průběhu „živé“ simulace se robot pohybuje po dráze (respektive po celé ploše okna). Pět kroužků před robotem odpovídá jednotlivým senzorům – kroužek je červený, pokud snímá tmavou (černou) barvu, jinak je zelený. Diagram robota je rozdělen příčnou čarou, to je osa kol (její střed je rovněž vyznačen).

Mají-li některé piny `PinMode` nastaven na `PMode.Output`, v sekci Internal state (dole na obrazovce) se pro každý takový pin objeví „dioda“. Ta se zbarví červeně, pokud je hodnota pinu `true` (HIGH), jinak má bílou barvu.

Pro piny s `PMode.InputPullup` se v téže sekci objeví tlačítko. Je-li stisknuté, na pinu lze číst hodnotu `false` (LOW), jinak má pin hodnotu `true` (HIGH, což odpovídá přítomnosti pull up rezistoru).

Pokud potomek RobotBase implementuje vlastnost `InternalState`, zobrazuje se její aktuální hodnota také v sekci Internal state.

Žádná z těchto možností přístupu k okamžitému stavu robota není k dispozici při paralelní simulaci. Nelze tedy například spoléhat na stisknutí tlačítka ve vhodnou chvíli (a využívat to třeba jako způsob aktivace robota).

## Pokročilejší konfigurace

Pro pokročilého uživatele můžou být zajímavé konstanty na začátku souborů `SimulationLive.cs` a `SimulationParallel.cs` ve složce `SimulatorApp/Simulation` a také v souboru `SimulatorApp/Robot/SimulatedRobot.cs`.

Poznámka: Jedna iterace odpovídá jednomu volání metody `MoveNext`. Ta v sobě obsahuje posunutí času vraceného metodou `Millis`, načtení vstupu (senzorů), zpracování výstupu (motorů) a volání metody `Loop`. Tedy počet iterací = počet volání `Loop` minus 1, jelikož první volání `Loop` je v konstruktoru `SimulatedRobot`.

- SimulationLive
    - IterationLimit – počet iterací, po němž dojde k automatickému pozastavení simulace (počítá se od posledního spuštění simulace)
    - IterationIntervalMs – interval mezi jednotlivými iteracemi
    - TimeCorrectionIterations – počet iterací, po němž se provede korekce času (aby se čas robota nezpožďoval oproti skutečnému času simulace)
- SimulationParallel
    - základní konfigurace
        - MinPointDistanceMs – při vykreslování trajektorií je tohle minimální vzdálenost (v milisekundách) mezi vykreslovanými body
        - IterationCount – počet iterací jednoho robota, které se provedou během paralelní simulace
        - RobotCount – počet simulovaných robotů
        - IterationIntervalMs – průměrný interval mezi iteracemi
    - nastavení náhodnosti (náhodná rozdělení jsou *uniformní*, „odchylka“ může být kladná i záporná)
        - IterationIntervalDifference – maximální povolená odchylka od IterationIntervalMs
        - PositionDifference – maximální povolená odchylka od nastavených počátečních souřadnic (pro osy X a Y se počítá nezávisle)
        - RotationDifference – maximální povolená odchylka od nastaveného počátečního natočení (v radiánech)
        - SensorErrorLikelihood – pravděpodobnost, že senzor vrátí opačnou hodnotu, než přečetl
        - MotorDifference – maximální povolená odchylka od opravdové šířky impulsu v mikrosekundách (pro každý motor se počítá nezávisle)
    - přepínače náhodnosti – umožňují deaktivovat jednotlivé náhodné prvky paralelní simulace (při deaktivaci všech je výsledkem stejná dráha jako v „živé“ simulaci)
        - RandomInterval
        - RandomPosition
        - RandomSensors
        - RandomMotors
- SimulatedRobot – kromě SensorDistancesY lze tyto hodnoty nepřímo ovlivnit z grafického rozhraní aplikace
    - WheelDistance – vzdálenost koleček robota (šířka osy) v pixelech
    - SpeedCoefficient – koeficient rychlosti v pixelech za sekundu
    - SensorDistancesY – vzdálenosti senzorů od podélné osy procházející středem robota (tedy vlastně Y souřadnice)

Výchozí hodnoty formulářových polí, s nimiž je program spouštěn, lze konfigurovat v horní části souboru `SimulatorApp/App/MainWindow.axaml.cs` pomocí proměnné `_defaultValues`.

## Vývojová dokumentace

### Struktura

Projekt sestává ze čtyř částí.

- SimulatorApp – hlavní aplikace s grafickým uživatelským rozhraním umožňující spouštět simulace a zobrazovat jejich výsledky
- UserDefinedRobot – obsahuje vzorový kód robota (assemblies s roboty však mohou pocházet i odjinud)
- CoreLibrary – prostřednictvím typu RobotBase stanovuje rozhraní umožňující hlavní aplikaci načítat různá assemblies s roboty; dále pak poskytuje pomocnou třídu Servo
- TestSuite – sada testů pro třídy BoolBitmap a SimulatedRobot

### Vztah mezi C\# a C++ kódem

Jedním z mých cílů bylo, aby se kód robota v C# mohl co nejvíce blížit kódu napsaném v C++, který lze spustit na hardwaru reálného robota. Rozhodl jsem se však zachovat jmenné konvence C#, takže např. názvy metod začínají velkými písmeny. Dalším výrazným rozdílem je globálnost základních funkcí (`digitalRead` aj.), ty jsem učinil veřejnými instančními metodami třídy `RobotBase`.

Třída `RobotBase` tedy obsahuje veřejné členy, ty jsou dostupné v potomkovi a některé z nich se používají při simulaci. Dále pak obsahuje soukromé členy, k některým z nich se přistupuje při simulaci pomocí reflection. Objektový návrh je tedy poměrně bezpečný.

### Třídy v SimulatorApp

- MainWindow – slouží k vykreslování uživatelského rozhraní a ke zpracovávání akcí uživatele
- AppState – spravuje vnitřní stav aplikace, zajišťuje jeho oddělení od stavu uživatelského rozhraní
- Map – zde je načten obrázek s dráhou, zajišťuje jeho vykreslení
- BoolBitmap – wrapper pro SKBitmap, poskytuje hodnoty senzorům, umožňuje cachování a rychlou duplikaci při paralelní simulaci
- SimulationLive – slouží ke spuštění a ovládání „živé“ simulace
- SimulationParallel – umožňuje spuštění paralelní simulace
- SimulatedRobot – „robot v prostředí“, je to wrapper pro konkrétního potomka RobotBase, udržuje informace o jeho poloze a stavu vůči simulaci
- DummyRobot – potomek třídy RobotBase, nic nedělá; je použit jako záloha, pokud uživatel nedodá vlastního robota, aby bylo možné robota umístit na dráhu
- RobotException – zabalí se do ní výjimka vyvolaná z vnitřního kódu robota, aby se mohla zpracovat na vyšší úrovni

### SimulatedRobot

Třída `SimulatedRobot` je na pomezí mezi simulací (simulační třídou) a robotem (vnitřní logikou), uchovává stav robota v prostředí simulace.

V konstruktoru se pomocí reflection zpřístupňují některé soukromé členy třídy `RobotBase`, konkrétně metoda `AddMillis` a pole `_pinModes` a `_pinValues`. Pomocí `AddMillis` se jakoby posouvají vnitřní hodiny robota o daný počet milisekund vpřed. Dvě pole pak slouží k manipulaci s hodnotami jednotlivých pinů.

Metoda `GetRobotPosition` slouží k výpočtu změny pozice robota s pohonem typu „differential drive“. Použil jsem vzorec [z tohoto paperu](https://rossum.sourceforge.net/papers/DiffSteer/DiffSteer.html#d5), konkrétně \[5].

Kromě pozice robota je také potřeba také udržovat pozice senzorů. K tomu slouží metoda `GetSensorPosition`, pro snížení výpočetní náročnosti se v ní používají předgenerovaná pole `_sensorDistances` a `_sensorAngles` popisující relativní polohu senzorů vůči robotovi.

Jelikož i vnitřní kód robota může vyvolat výjimku, ke spouštění takového kódu se používají metody `SafelyRun` a `SafelyGetNewRobot`, které případnou výjimku zabalí do `RobotException`. Ta je pak zachycena v `AppState` a zobrazena v chybovém okně.

### Souřadnice

Prvky uživatelského rozhraní a obrázky obvykle používají takový systém souřadnic, kde počátek (0, 0) je v levém horním rohu a kladný směr je vpravo/dole. Jelikož je mi však bližší soustava s počátkem vlevo dole a kladným směrem doprava/nahoru, rozhodl jsem se souřadnice převádět na tuto soustavu. Místa, kde se tento převod provádí, jsou v komentáři označeny jako `#coordinates`.

### Specifika simulací

Vzhledem k časování „živé“ simulace pomocí metody `Task.Delay`, která nezaručuje přesný čas, docházelo k tomu, že interní čas robota běžel pomaleji než ten reálný. Proto jsem implementoval korekční mechanismus, který zajišťuje, že se opoždění drží v jistých mezích.

Paralelizaci paralelní simulace zajišťuje metoda `Parallel.For`. Aby ji však bylo možné použít, musí každý robot disponovat vlastní bitmapou, kterou čte svými senzory. Jelikož vytváření mnoha kopií bitmapy je poměrně časově náročné, implementoval jsem vlastní třídu `BoolBitmap`, která v základu zprostředkovává „hezké“ rozhraní pro senzory (takže ji používá i neparalelní „živá“ simulace), umožňuje však také jednorázové načtení celé bitmapy a její uložení do pole booleovských hodnot. Toto pole pak lze efektivně duplikovat, takže není problém, aby měl každý z paralelních robotů vlastní instanci `BoolBitmap`.

Aby paralelní simulace dávala nějaký smysl, působí na roboty náhodné jevy podobné těm, které se mohou vyskytovat v reálném světě (chybovost senzorů, prokluzování koleček, odlišná počáteční poloha, rozdílné intervaly spouštění funkce `loop`). V konstruktoru `SimulationParallel` se generuje seed, od nějž se odvíjí veškerá náhodnost v dané simulaci, takže v případě potřeby lze aplikaci upravit tak, aby bylo možná seed získat a zafixovat.

### Volba frameworku

Aplikaci jsem původně vytvořil pomocí WPF, následně jsem však přešel na framework Avalonia, aby byl program multiplatformní. Z toho vyplývá také volba balíčku SkiaSharp pro práci s bitmapou a rovněž použití balíčku MessageBox.Avalonia pro zobrazování dialogových oken.

### Poznámka ke spuštění aplikace

`SimulatorApp` lze také sestavit příkazem `dotnet build` a následně spustit pomocí vzniklého spustitelného souboru (na OS Windows to bude `SimulatorApp.exe`). Pokud se toto spuštění neprovádí z konzole, může se kromě grafického rozhraní otevřít také okno s konzolí. Tomu lze zabránit tak, že se v souboru `SimulatorApp/SimulatorApp.csproj` do tagu `OutputType` místo `Exe` napíše `WinExe`. Tím se ale rovněž znemožní vypisování do konzole.
