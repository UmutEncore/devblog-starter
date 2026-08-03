# Backend / Frontend Klasör Ayrımı — Tasarım

**Tarih:** 2026-08-03
**Durum:** Onaylandı

## Amaç

Repo kökünde dağınık duran backend (`src/DevBlog.Api`) ve frontend (`devblog-ui`) klasörlerini, sorumluluğu net iki üst klasör altında (`backend/`, `frontend/`) toplamak. Solution ve proje dosyalarının göreli konumları korunacak şekilde sadece taşıma yapılacak; herhangi bir kod veya yapılandırma mantığı değişmeyecek.

## Mevcut Yapı

```
repo-root/
  DevBlog.slnx
  src/
    DevBlog.Api/
      DevBlog.Api.csproj
      Program.cs
      Data/  Endpoints/  Migrations/  Models/
      appsettings.json, appsettings.Development.json
      devblog.db (gitignored)
  devblog-ui/
    angular.json, package.json, tsconfig.json
    src/
  docs/
  .github/
```

## Hedef Yapı

```
repo-root/
  backend/
    DevBlog.slnx
    src/
      DevBlog.Api/
        DevBlog.Api.csproj
        Program.cs
        Data/  Endpoints/  Migrations/  Models/
        appsettings.json, appsettings.Development.json
  frontend/
    devblog-ui/
      angular.json, package.json, tsconfig.json
      src/
  docs/
  .github/
```

## Değişiklik Adımları

1. `git mv src backend/src` — backend kaynak kodu, geçmişi koruyarak taşınır.
2. `git mv DevBlog.slnx backend/DevBlog.slnx` — solution dosyası backend altına taşınır.
3. `backend/DevBlog.slnx` içindeki proje yolu ve klasör etiketleri gözden geçirilir; proje dosyasına göreli yol (`src/DevBlog.Api/DevBlog.Api.csproj`) aynı kalır çünkü solution dosyası da aynı üst klasöre taşındı.
4. `git mv devblog-ui frontend/devblog-ui` — frontend, geçmişi koruyarak taşınır.
5. `.gitignore` güncellenir:
   - `src/DevBlog.Api/devblog.db*` → `backend/src/DevBlog.Api/devblog.db*`
   - `devblog-ui/node_modules/`, `devblog-ui/dist/` → `frontend/devblog-ui/node_modules/`, `frontend/devblog-ui/dist/`
6. Kök `README.md` içine backend/frontend klasörlerinin konumunu ve nasıl çalıştırılacağını açıklayan kısa bir bölüm eklenir.
7. `bin/`, `obj/`, `node_modules/`, `.angular/cache/` gibi üretilen/ignore'lu klasörler taşınmaz; yeni konumlarında yeniden oluşturulacaklardır.

## Kapsam Dışı

- Kod, endpoint, model veya bağımlılık değişikliği yapılmayacak.
- CI/CD pipeline'ı henüz tanımlı değil (`.github/workflows/README.md` sadece placeholder); bu nedenle güncellenecek workflow YAML'ı yok.
- Solution/proje dosya adları (`DevBlog.slnx`, `DevBlog.Api.csproj`) değişmeyecek.

## Doğrulama Planı

- `dotnet build backend/DevBlog.slnx` ile backend'in taşıma sonrası derlendiği doğrulanır.
- `frontend/devblog-ui` içinde bağımlılıklar (`node_modules`) yeni konumda çalışır durumda olmalı; `npm install` sonrası temel bir `ng build` veya konfigürasyon kontrolü ile frontend'in bozulmadığı doğrulanır.
- `git status` ile beklenmeyen silinme/kaybolma olmadığı teyit edilir.

## Riskler ve Notlar

- Windows dosya sistemi üzerinde `git mv` ile klasör taşıma sırasında açık dosya kilitleri (ör. çalışan `dotnet` veya `ng serve` süreçleri) taşımayı engelleyebilir; taşımadan önce ilgili süreçlerin durdurulmuş olması gerekir.
- `bin/`/`obj/`/`node_modules` içeriği taşınmayacağı için taşıma sonrası ilk derleme/`npm install` biraz daha uzun sürebilir.
