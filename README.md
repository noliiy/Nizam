# Nizam

**Nizam**, inşaat ve proje kontrollü işler için kritik yol (CPM) odaklı bir planlama platformudur. Çekirdek değer döngüsü: proje → WBS → aktiviteler → ilişkiler → zamanlama → Gantt / kritik yol → baseline → ilerleme → sapma.

## Mimari özet

| Katman | Proje | Rol |
|---|---|---|
| Domain | `Nizam.Domain` | Varlıklar, kurallar |
| Scheduling | `Nizam.Scheduling` | CPM motoru (takvim, float, kritik yol) |
| Application | `Nizam.Application` | Komut/sorgu (MediatR) |
| Infrastructure | `Nizam.Infrastructure` | EF Core, JWT, Outbox, seed |
| API | `Nizam.Api` | REST uçları |
| Desktop | `Nizam.Desktop` | ViewModel + API istemcisi + Gantt mantığı (net8.0) |
| Contracts | `Nizam.Automation.Contracts` | n8n olay sözleşmeleri |
| Shared | `Nizam.Shared` | Ortak yardımcılar |

Desktop şu an macOS/CI uyumlu **sınıf kitaplığıdır**; WPF XAML dosyaları `Views/` altında belgeleme amaçlıdır. Windows WPF head için bkz. `src/Nizam.Desktop/WINDOWS.md`.

## Lokal kurulum

### 1. Altyapı (Docker)

```bash
cd deploy
docker compose up -d
```

PostgreSQL `localhost:5432` (`nizam` / `nizam`), Redis, MinIO ve n8n ayağa kalkar.

### 2. API

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet run --project src/Nizam.Api
```

Swagger (Development): `http://localhost:5169/swagger` (port `launchSettings.json` ile değişebilir).

### 3. Desktop mantığı (Mac / CI)

```bash
dotnet build src/Nizam.Desktop/Nizam.Desktop.csproj
dotnet test tests/Nizam.Desktop.Tests
```

Windows’ta WPF uygulaması eklendiğinde API tabanı varsayılanı: `http://localhost:5169/`.

### 4. Testler

```bash
dotnet build Nizam.sln
dotnet test
```

## Demo kullanıcı

| Alan | Değer |
|---|---|
| E-posta | `admin@nizam.local` |
| Şifre | `Admin123!` |

Seed sırasında demo organizasyon ve admin kullanıcısı oluşturulur.

## MVP checklist

Detaylı işaretli liste: [`docs/MVP_CHECKLIST.md`](docs/MVP_CHECKLIST.md).

Özet: Auth, Org, Proje, WBS, Aktivite, İlişki, Takvim, CPM, Gantt mantığı, İlerleme, Veri tarihi, Baseline + karşılaştırma, Gösterge paneli, Audit, Outbox → n8n stub.

## Dokümanlar

- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — mimari temel
- [`docs/PHASE2.md`](docs/PHASE2.md) — Kaynaklar backlog
- [`docs/daily-log.md`](docs/daily-log.md) — geliştirme günlüğü
- [`docs/MVP_CHECKLIST.md`](docs/MVP_CHECKLIST.md) — MVP yolculuğu
