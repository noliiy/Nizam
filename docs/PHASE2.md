# Phase 2 — Kaynaklar ve kapasite

MVP tamamlandıktan sonraki dikey dilim. SoT yine Nizam; n8n yalnızca bildirim/orkestrasyon.

## Hedefler

- [ ] Kaynak sözlüğü (işgücü, ekipman, malzeme) — org + proje kapsamı
- [ ] Aktivite kaynak talepleri (miktar / birim / takvim)
- [ ] Kaynak histogramları (Desktop + API)
- [ ] Kapasite vs talep görünümü
- [ ] Basit leveling motoru (`Nizam.Scheduling.Resources`) — opsiyonel / açıkça tetiklenen
- [ ] Kaynak atama çakışma uyarıları
- [ ] Audit + Outbox olayları (`RESOURCE_*`)
- [ ] Desktop: Kaynaklar navigasyon sekmesinin gerçek içeriği

## Bilinçli olarak Phase 2 dışı

- Tam EVM / maliyet (Phase 3)
- Monte Carlo / risk (Phase 4–5)
- XER içe aktarma
- Offline senkron

## Teknik notlar

- Leveling, CPM sonuçlarını girdi alır; SoT yazımı Application komutları üzerinden yapılır.
- Histogram sorguları okuma modelleri olarak Application’da tutulur; Desktop yalnızca API tüketir.
- Mevcut `MainViewModel` “Kaynaklar” menü öğesi yer tutucudur — Phase 2’de `ResourceListViewModel` bağlanır.
