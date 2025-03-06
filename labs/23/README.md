# Lab 23

Cílem tohoto labu je vytvořit CRON job, který bude periodicky zálohovat databázi.

## Krok 1 - Vytvoření jobu

1. Ujistěte se, že jste ve správném namespace:

```
kubectl config set-context --current --namespace mssql
```

2. Nejprve nasaďte nový persistent volume claim, který vytvoří úložiště, do nějž bude SQL Server backupy ukládat.

```
kubectl apply -f sql-volumes-backup.yml
```

3. Upravte deployment SQL Serveru, aby počítal s novým úložištěm:

```
kubectl apply -f sql-deployment.yml
```

4. Prozkoumejte soubor `sql-backup-cronjob.yml` a ujistěte se, že rozumíte tomu, co se v něm děje.

5. Nasaďte CRON job a počkejte, než se spustí. Příkazem `kubectl get jobs` můžete sledovat, jestli se job vytvořil.

6. Upravte CRON job, aby se nespouštěl každých 5 minut, ale jen každou noc ve 3:00 ráno (časové zóny řešit nemusíme).

7. Podívejte se v Azure portálu do Storage accountu v resource group `_MC*` a ověřte, že tam backupy jsou.
