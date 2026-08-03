# Backend / Frontend Klasör Ayrımı Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Repo kökündeki `src/DevBlog.Api` (backend) ve `devblog-ui` (frontend) klasörlerini sırasıyla `backend/src/DevBlog.Api` ve `frontend/devblog-ui` altına taşımak; solution dosyasını, `.gitignore`'ı ve kök `README.md`'yi buna göre güncellemek. Kod veya davranış değişikliği yok — sadece dosya konumları ve onlara referans veren yapılandırma.

**Architecture:** Git geçmişini koruyarak `git mv` ile klasör taşıma, ardından taşınan dosyalardaki göreli yol varsayımlarını (solution dosyası, `.gitignore`) güncelleme. Her adımdan sonra derleme/konfigürasyon doğrulaması yapılır.

**Tech Stack:** .NET 10 (DevBlog.Api, `.slnx` solution formatı), Angular (devblog-ui), Git.

## Global Constraints

- Hedef yapı: `backend/src/DevBlog.Api/...` ve `backend/DevBlog.slnx`, `frontend/devblog-ui/...` (spec'te onaylandı).
- Kod, endpoint, model veya bağımlılık değişikliği yapılmayacak.
- `bin/`, `obj/`, `node_modules/`, `.angular/cache/` taşınmayacak (ignore'lu, yeniden üretilecek).
- Taşıma `git mv` ile yapılacak (geçmiş korunacak).
- CI/CD workflow YAML'ı güncellenmeyecek (henüz tanımlı değil, sadece placeholder README var).

---

### Task 1: Backend klasörünü taşı ve solution dosyasını güncelle

**Files:**
- Move: `src/` → `backend/src/` (git mv)
- Move: `DevBlog.slnx` → `backend/DevBlog.slnx` (git mv)
- Verify: `backend/DevBlog.slnx`

**Interfaces:**
- Consumes: yok (ilk task)
- Produces: `backend/src/DevBlog.Api/DevBlog.Api.csproj` yolu, sonraki task'ların `.gitignore` güncellemesinde referans vereceği taban yol.

- [ ] **Step 1: Açık süreçleri durdur**

Çalışan `dotnet run`/`dotnet watch` süreci varsa durdurun (Windows'ta dosya kilidi taşımayı engelleyebilir).

- [ ] **Step 2: src klasörünü backend/src olarak taşı**

```bash
git mv src backend/src
```

- [ ] **Step 3: Solution dosyasını backend altına taşı**

```bash
git mv DevBlog.slnx backend/DevBlog.slnx
```

- [ ] **Step 4: Solution dosyasının içeriğini kontrol et**

`backend/DevBlog.slnx` dosyasını aç. İçerik şu şekilde kalmalı (proje yolu solution dosyasına göre görelidir, ikisi de birlikte taşındığı için değişmemesi gerekir):

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/DevBlog.Api/DevBlog.Api.csproj" />
  </Folder>
</Solution>
```

Eğer yol farklıysa (örn. hâlâ `../src/...` gibi bir şey varsa), `src/DevBlog.Api/DevBlog.Api.csproj` olacak şekilde düzeltin.

- [ ] **Step 5: Backend derlemesini doğrula**

```bash
dotnet build backend/DevBlog.slnx
```

Beklenen: Derleme başarılı, hata yok.

- [ ] **Step 6: Commit**

```bash
git add -A -- backend DevBlog.slnx src
git commit -m "refactor: move backend (src, solution) under backend/"
```

---

### Task 2: Frontend klasörünü taşı

**Files:**
- Move: `devblog-ui/` → `frontend/devblog-ui/` (git mv)

**Interfaces:**
- Consumes: yok
- Produces: `frontend/devblog-ui/` taban yolu, Task 3'ün `.gitignore` güncellemesinde kullanacağı yol.

- [ ] **Step 1: Açık süreçleri durdur**

Çalışan `ng serve` veya benzeri bir süreç varsa durdurun.

- [ ] **Step 2: devblog-ui klasörünü frontend/devblog-ui olarak taşı**

```bash
git mv devblog-ui frontend/devblog-ui
```

- [ ] **Step 3: Frontend konfigürasyonunu doğrula**

```bash
cd frontend/devblog-ui
npm install
npx ng build --configuration development
cd ../..
```

Beklenen: `npm install` ve `ng build` hatasız tamamlanır (mevcut `node_modules` taşınmadığı için yeniden kurulum gerekir).

- [ ] **Step 4: Commit**

```bash
git add -A -- frontend devblog-ui
git commit -m "refactor: move frontend (devblog-ui) under frontend/"
```

---

### Task 3: .gitignore ve README güncellemesi

**Files:**
- Modify: `.gitignore`
- Modify: `README.md`

**Interfaces:**
- Consumes: Task 1'in `backend/src/DevBlog.Api` yolu, Task 2'nin `frontend/devblog-ui` yolu.
- Produces: yok (son task).

- [ ] **Step 1: .gitignore içindeki backend/frontend yollarını güncelle**

`.gitignore` dosyasında şu satırları:

```
src/DevBlog.Api/devblog.db
src/DevBlog.Api/devblog.db-shm
src/DevBlog.Api/devblog.db-wal
```

şu şekilde değiştirin:

```
backend/src/DevBlog.Api/devblog.db
backend/src/DevBlog.Api/devblog.db-shm
backend/src/DevBlog.Api/devblog.db-wal
```

ve:

```
devblog-ui/node_modules/
devblog-ui/dist/
```

şu şekilde değiştirin:

```
frontend/devblog-ui/node_modules/
frontend/devblog-ui/dist/
```

- [ ] **Step 2: .gitignore doğrulaması**

```bash
git status
```

Beklenen: `backend/src/DevBlog.Api/devblog.db` ve `frontend/devblog-ui/node_modules/` görünmüyor (ignore ediliyor); başka beklenmeyen dosya listelenmiyor.

- [ ] **Step 3: Kök README.md'ye proje yapısı bölümü ekle**

`README.md` dosyasının mevcut içeriğinin altına ekleyin:

```markdown

## Proje Yapısı

```
backend/    .NET 10 Web API (DevBlog.Api)
frontend/   Angular uygulaması (devblog-ui)
docs/       Dokümantasyon
```

### Backend'i çalıştırma

```bash
dotnet run --project backend/src/DevBlog.Api/DevBlog.Api.csproj
```

### Frontend'i çalıştırma

```bash
cd frontend/devblog-ui
npm install
npm start
```
```

- [ ] **Step 4: Commit**

```bash
git add .gitignore README.md
git commit -m "docs: update gitignore and README for backend/frontend split"
```

---

### Task 4: Son doğrulama

**Files:**
- Verify only, no changes.

**Interfaces:**
- Consumes: Task 1-3'ün tüm çıktıları.
- Produces: yok.

- [ ] **Step 1: Tam repo durumunu kontrol et**

```bash
git status
```

Beklenen: Temiz working tree (her şey commit edildi), beklenmeyen silinmiş/kaybolmuş dosya yok.

- [ ] **Step 2: Backend'i yeniden derle**

```bash
dotnet build backend/DevBlog.slnx
```

Beklenen: Başarılı.

- [ ] **Step 3: Frontend'i yeniden derle**

```bash
cd frontend/devblog-ui
npx ng build --configuration development
cd ../..
```

Beklenen: Başarılı.

- [ ] **Step 4: Klasör ağacını gözden geçir**

```bash
find . -maxdepth 3 -not -path '*/node_modules/*' -not -path '*/bin/*' -not -path '*/obj/*' -not -path '*/.git/*' -not -path '*/.angular/*' | sort
```

Beklenen: `backend/` ve `frontend/` üst klasörleri altında spec'teki hedef yapıya uygun bir ağaç; kökte artık `src/` veya `devblog-ui/` yok.
