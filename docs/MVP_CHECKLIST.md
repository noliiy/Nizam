# MVP Checklist

Kaynak yolculuk: Login → Org → Proje → WBS → Aktiviteler → İlişkiler → Takvim → Zamanlama → Gantt/Kritik → Baseline → Veri tarihi → İlerleme → Yeniden hesap → Sapma / gecikme.

| # | Madde | Durum | Not |
|---|---|---|---|
| 1 | Auth (login / JWT) | [x] | `POST /api/auth/login`, demo `admin@nizam.local` |
| 2 | Organizasyon | [x] | Create + seed demo org |
| 3 | Kullanıcı / temel rol | [x] | Seed admin + membership |
| 4 | Proje oluşturma | [x] | Kök WBS + varsayılan 5x8 takvim yan etkisi |
| 5 | WBS CRUD (liste/oluştur/güncelle) | [x] | API + `WbsTreeViewModel` (yeniden adlandırma yerel taslak) |
| 6 | Aktiviteler | [x] | Create/Update/List + `ActivityTableViewModel` |
| 7 | İlişkiler (FS vb.) | [x] | Döngü kontrolü + FS komutu Desktop’ta |
| 8 | Takvim | [x] | Proje varsayılanı Standard 5x8 |
| 9 | Scheduling Engine (CPM) | [x] | `Nizam.Scheduling` + `RunSchedule` |
| 10 | Aktivite tablosu (Desktop) | [x] | Kolonlar TR: Kod, Ad, Süre, Başlangıç, Bitiş, Toplam Bolluk, Kritik |
| 11 | Gantt mantığı / sanallaştırma | [x] | `VirtualizedGanttLayout` / `GanttLayoutEngine` — WPF çizim Windows head’de |
| 12 | İlerleme girişi | [x] | API + `ProgressEntryViewModel` |
| 13 | Veri tarihi (Data Date) | [x] | API + Gantt VM |
| 14 | Baseline + karşılaştırma | [x] | Sapma gün cinsinden TR etiketler |
| 15 | Proje gösterge paneli | [x] | `GetDashboard` + `DashboardViewModel` |
| 16 | Audit log | [x] | Create/schedule/progress/baseline yazımları |
| 17 | Outbox → n8n temeli | [x] | Writer + `OutboxDispatcherHostedService` (webhook opsiyonel) |
| 18 | Desktop kabuk / navigasyon | [x] | `MainViewModel` TR menü; XAML belge |
| 19 | Entegrasyon duman testi | [x] | `Nizam.Api.IntegrationTests` schedule smoke |
| 20 | Dokümantasyon (TR README) | [x] | README, PHASE2, daily-log, bu checklist |

**MVP dışı (bilinçli):** Kaynak leveling, tam EVM, Monte Carlo, XER, offline sync, network diyagramı, AI, portföy zekâsı → Phase 2+.
