# Fredsholm Værktøjsbibliotek

En webapplikation til registrering og styring af fælles værktøj på Fredsholmsvej. Beboere kan se hvilke værktøjer der findes, og hvem der har lånt hvad.

## Teknologi

- **Backend:** ASP.NET Core 8 (Blazor)
- **Databaser:** PostgreSQL (brugere og udlån) + MongoDB (værktøjsdata)
- **Containerisering:** Docker Compose

## Lokal udvikling

Kræver [Docker](https://www.docker.com/) og [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
# Kopier og udfyld miljøvariable
cp .env.example .env   # eller opret .env manuelt

# Start alle services
docker compose up --build
```

Applikationen kører herefter på [http://localhost:8080](http://localhost:8080).

### Miljøvariable

| Variabel | Beskrivelse | Standard |
|---|---|---|
| `ADMIN_PASSWORD` | Adgangskode til admin-bruger (**påkrævet**) | — |
| `ADMIN_EMAIL` | E-mail til admin-bruger | `admin@toollib.dk` |
| `DATABASE_URL` | PostgreSQL connection string | `Host=postgres;Database=toollib;Username=postgres;Password=postgres123` |
| `MONGODB_URL` | MongoDB connection string | `mongodb://mongo:27017` |
| `APP_BASE_URL` | Applikationens offentlige URL | `http://localhost:8080` |
| `SMTP_HOST` | SMTP-server til e-mail | — |
| `SMTP_PORT` | SMTP-port | `587` |
| `SMTP_USE_SSL` | Brug SSL/TLS | `true` |
| `SMTP_FROM` | Afsenderadresse | — |
| `SMTP_USERNAME` | SMTP-brugernavn | — |
| `SMTP_PASSWORD` | SMTP-adgangskode | — |

## Deploy til fly.io

Projektet er sat op til at deploye som en multi-container applikation via Docker Compose på [fly.io](https://fly.io). Se [fly.io dokumentationen](https://fly.io/docs/machines/guides-examples/multi-container-machines/) for baggrundsinformation.

### Forudsætninger

- [flyctl](https://fly.io/docs/flyctl/install/) installeret og logget ind (`fly auth login`)
- En fly.io-konto

### Første gang

```bash
# Opret applikationen (hvis den ikke allerede eksisterer)
fly launch --no-deploy

# Opret persistent storage til databaserne
fly volumes create postgres_data --size 3 --region ams
fly volumes create mongo_data --size 3 --region ams

# Sæt påkrævede hemmeligheder
fly secrets set ADMIN_PASSWORD=<din-adgangskode>

# Valgfrit: sæt admin-e-mail og SMTP-konfiguration
fly secrets set ADMIN_EMAIL=<din-email>
fly secrets set SMTP_HOST=<smtp-server> SMTP_FROM=<afsender> SMTP_USERNAME=<brugernavn> SMTP_PASSWORD=<adgangskode>

# Deploy
fly deploy
```

### Efterfølgende deploys

```bash
fly deploy
```

### Nyttige kommandoer

```bash
fly status          # Vis kørende maskiner
fly logs            # Se applikationslog
fly ssh console     # SSH ind i app-containeren
fly secrets list    # Vis satte hemmeligheder (uden værdier)
```

### Konfiguration

`fly.toml` indeholder deploy-konfigurationen:

- **Region:** Amsterdam (`ams`)
- **VM:** 1 delt CPU, 1 GB RAM
- **HTTP:** Port 8080, HTTPS tvinges automatisk
- **Auto-stop:** Maskinen stopper ved inaktivitet og starter igen ved trafik (min. 1 maskine kørende)
- **Mounts:** `postgres_data` og `mongo_data` er persistente volumes tilknyttet databasernes datamapper
