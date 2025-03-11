## Lab 24

Cílem tohoto labu je vyzkoušet si vytvoření Helm chartů.

## Krok 1 - Triviální chart

1. Ujistěte se, že jste v adresáři `labs/24`

2. Spusťte příkaz `helm create app`

3. Prohlédněte si obsah vytvořeného adresáře.

4. Následujícím PowerShell skriptem smažte věci, které nebudeme potřebovat:

```
rmdir app/templates/tests -Recurse -Force
rm app/templates/hpa.yaml
rm app/templates/NOTES.txt
rm app/templates/serviceaccount.yaml
```

5. Příkazem `helm install --dry-run test app/` si vypište vygenerované YAML soubory.

## Krok 2 - Úprava service a deploymentu

1. Porovnejte nejprve část __Service__ se souborem `labs/21/app-service.yml`.

> Až na odlišné `labels` a `selector` by měla být služba stejná, takže nepotřebujeme nic dělat.

2. Nyní porovnejme část __Deployment__ se souborem `labs/14/app-deployment.yml`.

> Zde je změn více, pojďme je odbavovat jednu po druhé.

3. V elementu `spec` nepotřebujeme `serviceAccountName` ani `securityContext`:

    * Odeberte tyto sekce ze souboru `app/templates/deployment.yaml`

    * Odeberte tyto sekce ze souboru `values.yaml`

4. V elementu `container` nepotřebujeme `securityContext`:

    * Odeberte tuto sekci ze souboru `app/templates/deployment.yaml`

    * Odeberte sekci `podSecurityContext` ze souboru `values.yaml`

5. V elementu `container` nepotřebujeme `readinessProbe`:

    * Odeberte tuto sekci ze souboru `app/templates/deployment.yaml`

6. Liveness probe by se nám naopak hodila konfigurovatelná ve `values.yaml`.

    * Sekci `livenessProbe` přesuňte do `values.yaml` a zrušte její odsazení.

    * V souboru `app/templates/deployment.yaml` vytvořte sekci `livenessProbe` stejným způsobem, jako se dosazuje sekce `nodeSelector`.

    > Pozor na odsazení u příkazu `nindent` - mělo by být 12 znaků. Zároveň pozor, ať máte v souboru pouze mezery a ne tabulátory.

7. Stejným způsobem pod element `containers` doplňte sekci `env`. Zajistěte, ať je možné konfigurovat ji ve `values.yaml`.

8. Stejným způsobem pod element `containers` doplňte sekci `volumeMounts`. Zajistěte, ať je možné konfigurovat ji ve `values.yaml`.

9. Podobně doplňte sekci `volumes`. Pozor na její umístění.

10. V souboru `values.yaml` upravte sekci `image`:

    * Do `repository` dosaďte cestu k image (bez tagu)

    * Změňte `pullPolicy` na `Always`

11. Ze souboru `values.yaml` odeberte sekci `autoscaling`.

12. Odeberte odkazy na tuto sekci z `app/templates/deployment.yaml`.

12. Zkuste spustit `helm install --dry-run test app/` a porovnat výsledek nyní.

## Krok 3 - Úprava ingressu

1. Nejprve ve `values.yaml` nastavte `ingress.enabled` na `true`.

2. Doplňte hodnoty podle souboru `labs/22/app-ingress-test.yaml`:

    * `className` na `nginx`

    * `host` na `test.northwindstore.local`

    * `pathType` na `Prefix`

    * Zkopírujte celou sekci `tls`

3. Spusťte dry-run a porovnejte výstupy.

## Krok 4 - Persistent volume claim

1. Do složky `templates` přidejte ještě soubor `persistentVolumeClaim.yaml`, který bude řešit persistent volume claim.

2. Vykopírujte do něj __Persistent volume claim__ i __Storage class__ ze souboru `labs/14/app-volumes.yml`. 

3. Do `values.yaml` přidejte následující sekci, kterou budeme volume claim konfigurovat:

```
persistentVolumeClaim:
  name: app-files
  storageClass: app-files
```

4. Jako název volume claimu v šabloně použijte výraz `{{ .Values.persistentVolumeClaim.name }}`

5. Jako storage class použijte `{{ .Values.persistentVolumeClaim.storageClass }}`

> Pozor, dosazuje se na dvě místa - jednou do classy samotné a jednou do persistent volume claimu.

6. Podobně jako má `ingress` vlastnost `enabled`, přidejte ji i k persistent volume claimu:

```
persistentVolumeClaim:
  enabled: true
  name: app-files
  storageClass: app-files
```

7. Soubor `persistentVolumeClaim.yaml` obalte ifem:

```
{{- if .Values.persistentVolumeClaim.enabled -}}
...
{{- end }}
```

## Krok 5 - Nasazení aplikace

1. V souboru `Chart.yaml` změňte `appVersion` na hodnotu `1.0`.

2. Spusťte následující příkaz, kterým aplikaci nainstalujete:

```
helm upgrade -i --create-namespace --namespace northwindstore-helm `
  --set ingress.hosts[0].host=helm.northwindstore.local `
  --set ingress.hosts[0].paths[0].path=/ `
  --set ingress.hosts[0].paths[0].pathType=Prefix `
  --set ingress.tls[0].hosts[0]=helm.northwindstore.local `
    northwindstore app/
```

3. Přidejte do namespace následující secret (ty nám helm nevytváří):

```
kubectl create secret generic app --from-literal=ConnectionStrings__DB="Data Source=tcp:mssql-service.mssql.svc.cluster.local,1433; Initial Catalog=NorthwindTest; User ID=NorthwindTestLogin; Password=AppPass_1" --namespace=northwindstore-helm
```

4. Přidejte do souboru `C:\Windows\system32\drivers\etc\hosts` záznam pro doménu `helm.northwindstore.local`.

5. Ověřte, že aplikace funguje - https://helm.northwindstore.local 