# Lab 31

Cílem tohoto labu je připravit Dockerfile pro projekty, které obsahují testy, a nachystat `docker-compose.ci.yml`, který umožní rozběhnutí aplikace a SQL databáze v kontejneru.

## Krok 0 - Fork repozitáře

1. Na githubu si kliknutím na tlačítko __Fork__ vytvořte svůj fork repozitáře - budete do něj muset commitovat změny, aby je CI vidělo.

2. Naklonujte si váš fork vedle stávajícího adresáře (máme v něm nějaké změny, které budeme chtít přenášet do vlastního forku).

3. Nyní budeme pracovat ve vašem forku - ujistěte se, že jste ve větvi `day4`

## Krok 1 - Unit test project

1. Ujistěte se, že je jako defaultní branch v repozitáři nastavena větev `day4`.

2. Otevřete ve Visual Studiu hlavní solution `src/NorthwindStore.sln`.

3. Pravým tlačítkem klikněte na projekt `NorthwindStore.Tests.UnitTests` a vyberte možnost __Add > Docker Support__.

4. Vygenerovaný Dockerfile nedělá to, co bychom potřebovali.

    * Zrušte v něm všechny reference na runtimový kontejner - ten nepotřebujeme. Stačí nám stage `build`, která se odehrává v kontejneru SDK.

    * `dotnet build` nemusí výstupy generovat do adresáře `/app/build` - odeberme tento option.

    * `dotnet build` nemusí mít konfiguraci `Release`.

    * Před `dotnet restore` doplňme nakopírování adresáře `packages`, podobně jako u aplikačního Dockerfile.

    * Příkazu `dotnet build` můžeme smazat option pro build konfiguraci Release a option pro změnu cesty pro výstup.
    
    * Přidejme řádek `ENTRYPOINT` s příkazem `dotnet test NorthwindStore.Tests.UnitTests.csproj`.

4. Pravým tlačítkem klikněte na `Dockerfile` a vyberte možnost __Build Docker Image__.

5. Následujím příkazem kontejner spusťte a ověřte, že testy projdou.

```
docker run --rm northwindstoretestsunittests
```

6. Ověřte, že když test selže (třeba tak, že do něj přidáte `throw new Exception();`), tak spuštění kontejneru skončí s nenulovým exit kódem:

```
$LASTEXITCODE
```

## Krok 2 - Integration test project

1. Stejným postupem jako v kroku 1 vytvořte `Dockerfile` pro integrační testy v projektu  `NorthwindStore.Tests.IntegrationTests`.

2. Abychom mohli integrační testy spustit lokálně, měl by kontejner integračních testů vidět na databázový kontejner. Ten je hostován v Docker network vytvořené pomocí Docker compose - tato síť má ale automaticky generovaný název. Do `docker-compose.override.yml` tedy doplňte tuto instrukci, která založí síť s explicitním názvem `northwindstore-dev`:

```
networks:
  northwindstore-dev:
    name: northwindstore-dev
```

3. Oběma službám v `docker-compose.override.yml` doplňte instrukci, která je do této sítě přiřadí:

```
    networks:
    - northwindstore-dev
```

4. Pomocí __Rebuild solution__ a spuštění projektu se ujistěte, že se kontejnery vytvořily znovu a že jsou ve správné network. Jde to udělat například pomocí příkazu `docker inspect`.

5. Pravým tlačítkem klikněte na `Dockerfile` a vyberte __Build Docker Image__.

6. Ověřte, že kontejner s testy funguje:

```
docker run --rm -e "ConnectionStrings__DB=Data Source=tcp:northwindstore.db,1433; Initial Catalog=Northwind; User ID=sa; Password=DevPass_1" --network northwindstore-dev northwindstoretestsintegrationtests
```

## Krok 3 - Příprava docker-compose.ci.yml

V rámci buildu budeme chtít vytvořit kontejnery pro aplikaci i pro databázi, a zároveň je budeme chtít spustit. Proti aplikaci můžeme chtít v budoucnu spouštět UI testy (např. pomocí knihovny Selenium). Databázi zase budeme potřebovat na integrační testy.

1. V rootu repozitáře vytvořte adresář `ci` a zkopírujte do něj soubor `src/docker-compose.override.yml`. 
Pojmenujte jej jako `docker-compose.ci.yml`.

2. Prověďte v něm tyto změny:

    * Odstraňte z proměnné `ASPNETCORE_URLS` URL pro https - v buildu nechceme řešit development HTTPS certifikáty.

    * Odstraňte ze sekce ports port `443`.

    * U aplikace úplně odeberte sekci `volumes`.

    * U databáze zrušte sekci `ports` - nechceme port exposovat navenek, pokud by na jednom stroji běželo více buildů najednou, došlo by ke kolizi.

    * U databáze zrušte sekci `volume` - zde si chceme databázi vždy inicializovat nanovo.

3. Commitněte a pushněte do větve `day4`.

4. V __Azure DevOps__ vytvořte nový projekt.

5. Otevřete sekci __Pipelines__ a vytvořte novou pipeline.

6. Na obrazovce __Where is your code__ vyberte __GitHub__.

7. Proveďte autentizaci vůči GitHub repozitáři.

8. V templatách vyberte možnost __Starter pipeline__ (druhá odspoda) - nechceme nic předgenerovat, chceme vše definovat sami.

9. V záhlaví upravte cestu k souboru na `ci/pipelines/build.yml`.

10. Prověďte následující kroky v pipeline:

    * Smažte předgenerované kroky v pipeline - nechte jen řádek `steps:`

    * Přidejte krok __Docker Compose_

    * Jako hlavní soubor použijte `src/docker-compose.yml`

    * Jako další soubory přidejte `../ci/docker-compose.ci.yml` (cesta je relativní vůči hlavnímu souboru)

    * Jako příkaz zvolme `up`

    * Argumenty nastavte na `--build --force-recreate -d`

> Option `--build` řekne, že se před startem kontejnerů má provést build.
>
> Option `--force-recreate` smaže kontejnery, pokud tam existovaly, a vytvoří je znovu.
>
> Option `-d` je důležitý - nezablokuje nám terminál tím, že bude čekat, než kontejnery skončí.

11. Klikněte na __Save and run__ a vyberte, že se to bude commitovat do nové větve - pojmenujte ji `day4-ci`.

12. Počkejte, než pipeline doběhne, a ověřte, že příkaz __Docker compose__ nevyhodil chybu. 

## Krok 4 - Spuštění testů

První krok v pipeline nám ověřil, že zdrojové kódy jdou zkompilovat, že umíme postavit kontejner s vývojářskou databází a že to celé jde spustit. Nyní je čas na testy.

1. Přidejte do pipeline krok __Docker__

    * Vyberte akci `build`

    * Container repository (název kontejneru) nastavte na `northwindstoretestsunittests` (jedná se o název výsledného kontejneru)

    * Vyberte cestu k Dockerfile `src/NorthwindStore.Tests.UnitTests/Dockerfile`

    * Složka s kontextem bude `src`

    * Image tag nechme `$(Build.BuildId)`, budeme jej používat dále (je to unikátní a rostoucí číslo buildu)

2. Přidejte další krok __Docker__

    * Vyberte akci `run` (je potřeba potvrdit, že zadáváte vlastní hodnotu)

    * Jako argument dejte `--rm northwindstoretestsunittests:$(Build.BuildId)`

3. To samé proveďme s integračními testy. 

    * Název kontejneru je `northwindstoretestsintegrationtests`

    * Cesta k Dockerfile je `src/NorthwindStore.Tests.IntegrationTests/Dockerfile`

    * Při spuštění nezapomeňte přidat optiony (dávají se před název kontejneru)

```
--rm -e "ConnectionStrings__DB=Data Source=tcp:northwindstore.db,1433; Initial Catalog=Northwind; User ID=sa; Password=DevPass_1" --network northwindstore-dev northwindstoretestsintegrationtests:$(Build.BuildId)
```

4. Uložte, spusťte pipeline a zkontrolujte logy, že se testy opravdu spustily.

## Krok 5 - Přidání logování

Příkazu `dotnet test` můžeme dát parametry, které mu přikážou, aby vytvořil XML soubor s výsledky testů. Ten umí Azure DevOps načíst a vizualizovat v rámci výsledků pipeline.

1. Do Dockerfile unit testů přidejte příkazu `dotnet test` dodatečný argument:

```
--logger trx;LogFileName=/src/testresults/unittests-results.trx
```

2. Do Dockerfile integračních testů přidejte příkazu `dotnet test` dodatečný argument:

```
--logger trx;LogFileName=/src/testresults/integrationtests-results.trx
```

3. Commitněte a pushněte.

4. V pipeline v kroku spuštění obou kontejnerů s testy přidejte argument mapování volume:

```
-v $(Build.ArtifactStagingDirectory)/testresults:/src/testresults
```

5. Oběma krokům, které spouští testy, přidejte příznak, aby se pokračovalo i v případě chyby:

```
  continueOnError: true
```

> Pozor na indentaci - nepatří to do sekce `inputs`, ale o úroveň výše (na stejný level jako `inputs` nebo `task`).

6. Přidejte poslední step __Publish test results__.

    * Formát `VSTest`

    * Jako pattern zvolte `*.trx`

    * Search directory nastavme jako `$(Build.ArtifactStagingDirectory)/testresults`

    * Zaškrtněte __Fail if there are test failures__

7. Spusťte build a ověřte, že agent najde soubor s výstupem textů a zobrazí ve výsledcích 2 úspěšné testy.

## Krok 6 - Úklid

Pokud používáte Hosted agenty, není třeba běžící kontejnery (aplikaci a databázi) vypínat, protože virtuální stroj, na kterém to běželo, stejně zaniká.

V případě, že máte vlastního build agenta, kterého po skončení pipeline neresetujete do výchozího stavu, je nutné úklid provést.

1. Zkopírujte krok __Docker compose__ a upravte následující:

    * Action na `down`

    * Zrušte argumenty, nepotřebujeme je.

    * Jako `condition` nastavte, že se má spustit i v případě, že předchozí kroky zfailovaly nebo došlo ke zrušení pipeline:

```
  condition: always()
```

> Pozor na indentaci, mělo by to být na stejné úrovni jako `task` nebo `inputs`.

2. Spusťte pipeline a ověřte, že se krok provedl.

3. Optional - doplňte jednotlivým krokům vlastnost `displayName`, aby se na obrazovce s logy zobrazovaly rozumné názvy jednotlivých kroků.