---
name: security-audit
description: DevBlog reposuna özel security audit / güvenlik denetimi skill'i. Her endpoint'in Endpoint → Service/DbContext akışını satır satır okuyup OWASP Top 10, input validation, CORS ve bu repoya özgü senaryoları (hardcoded JWT secret, Base64 "hashing", localStorage token, layering bypass, CommentsEndpoint spoofing) tarar; bulguları Bulgu/Senaryo/Öneri/Severity formatında raporlar. "security audit", "güvenlik denetimi", "OWASP", "penetrasyon testi", "bu endpoint güvenli mi", "CORS kontrolü", "JWT güvenliği", "kimlik doğrulama incele" isteklerinde veya yeni bir endpoint eklenip/değiştirildiğinde kullanılır.
---

# DevBlog Security Audit

Bu skill, DevBlog backend'indeki (ve gerektiğinde frontend'in tükettiği kısımlarındaki)
endpoint'leri **kaynak koddan** — çalışan bir instance'a saldırmadan — denetler. Amaç,
her endpoint'in gerçek veri akışını (`Endpoint → Service → Repository → AppDbContext`,
CLAUDE.md'deki hedef katmanlaşmaya bkz.) satır satır takip ederek OWASP Top 10, input
validation ve CORS başlıklarında somut, dosya:satır referanslı bulgular üretmektir.

## Ne zaman tetiklenir

- Kullanıcı "security audit", "güvenlik denetimi", "OWASP taraması", "penetrasyon testi",
  "bu endpoint/API güvenli mi", "CORS kontrolü", "JWT güvenliği", "auth flow'unu incele"
  gibi bir istek yaptığında.
- Yeni bir endpoint (`Endpoints/*.cs` altında yeni bir `Map` metodu) eklendiğinde veya
  mevcut bir endpoint'in auth/validation/veri akışı değiştirildiğinde — proaktif olarak
  bu skill'i öner.
- `Program.cs` içindeki CORS/JWT/middleware pipeline konfigürasyonu değiştirildiğinde.

## Kapsam

- `backend/src/DevBlog.Api/Program.cs` — global config: CORS policy, JWT config,
  middleware pipeline sırası, HTTPS redirection, migration/seed otomasyonu.
- `backend/src/DevBlog.Api/Endpoints/*.cs` — her `Map` metodu bir denetim birimi.
- `backend/src/DevBlog.Api/Services/*.cs`, `Repositories/*.cs`, `Data/AppDbContext.cs`,
  `Data/DataSeeder.cs` — endpoint'in çağırdığı her katman.
- `backend/src/DevBlog.Api/Models/*.cs` ve DTO record'ları (`IPostService.cs` içindeki
  `CreatePostRequest`/`CreateCommentRequest`/`LoginRequest` gibi) — validation açığı burada
  yakalanır.
- `backend/src/DevBlog.Api/appsettings*.json`, `DevBlog.Api.csproj` — config sızıntısı ve
  bağımlılık sürümleri.
- Frontend tarafı sadece **backend'in ürettiği/tükettiği veri nereye gidiyor** sorusuyla
  sınırlı: `frontend/devblog-ui/src/app/services/auth.service.ts` (token nerede saklanıyor)
  ve stored content'i render eden template'ler (örn. `post-detail.component.html` — `{{ }}`
  interpolation mu, `[innerHTML]` mi).

## Akış

1. **`Program.cs`'i oku.** CORS policy, JWT `TokenValidationParameters`, middleware sırası
   (`UseCors`/`UseAuthentication`/`UseAuthorization` sırası doğru mu — auth öncesi CORS
   olmalı), `UseHttpsRedirection()`/HSTS var mı, global exception handler var mı, rate
   limiter (`AddRateLimiter`/`UseRateLimiter`) var mı, migration'ların otomatik uygulanması
   (`db.Database.Migrate()`) prod'a taşınma riski açısından not et.
2. **Her endpoint grubunu bul.** `Program.cs`'in sonundaki `XxxEndpoint.Map(app)`
   çağrılarından başla; yeni eklenmiş bir endpoint dosyası varsa onu da listeye ekle.
3. **Her endpoint için veri akışını takip et**: Endpoint dosyasından başlayıp
   çağırdığı servis/repository'ye, oradan `AppDbContext`'e kadar oku. Kim `AppDbContext`'e
   doğrudan erişiyor (CLAUDE.md → "Technical debt"'te bilinen: `CommentsEndpoint`,
   `AuthEndpoint`) tespit et — bu durum sadece mimari borç değil, doğrulama/iş
   mantığının ad-hoc ve tutarsız yazılmasına yol açtığı için güvenlik açısından da
   işaretlenmesi gereken bir zemin.
4. **Aşağıdaki checklist kategorilerini** (OWASP eşlemeli + Validation + CORS +
   repo'ya özgü senaryolar) her endpoint'e uygula. Bir kategori bir endpoint'e uygulanamıyorsa
   (örn. SSRF şu an hiçbir yerde yok) "N/A" olarak geç, sessizce atlama.
5. Bulguları [Çıktı formatı](#çıktı-formatı)na göre raporla.
6. Her bulgu için CLAUDE.md → "Technical debt" bölümünde zaten yazılı mı kontrol et; öyleyse
   bulguyu **yine raporla** (audit'in işi ciddiyetini somutlaştırmak) ama "Not" alanında
   "CLAUDE.md'de bilinen borç" olarak işaretle — yeni bir keşif gibi sunma.
7. Mümkünse bağımlılık taraması çalıştır: `dotnet list package --vulnerable --project backend/src/DevBlog.Api/DevBlog.Api.csproj`
   ve (frontend değişmişse) `npm audit --prefix frontend/devblog-ui`. Ağ erişimi yoksa veya
   komut başarısız olursa "bağımlılık taraması çalıştırılamadı" diye açıkça belirt, tahmin
   etme.

## Checklist kategorileri (OWASP Top 10 eşlemeli)

Bu skill **OWASP Top 10:2025** taksonomisini kullanır — 2021 listesinden sonraki ilk
güncelleme; Kasım 2025'te OWASP Global AppSec Washington D.C.'de duyuruldu, nihai sürüm
Ocak 2026'da yayınlandı (owasp.org/Top10/2025). 2021'e göre başlıca değişiklikler:
**Security Misconfiguration** 5.'ten 2.'ye yükseldi, **Cryptographic Failures** 2.'den
4.'e indi, **Injection** 3.'ten 5.'e indi, **Insecure Design** 4.'ten 6.'ya indi;
**Software Supply Chain Failures** (eski "Vulnerable and Outdated Components"'in
genişletilmiş hali) ve **Mishandling of Exceptional Conditions** yeni eklendi; eski A10
**SSRF** artık ayrı bir kategori değil, **Broken Access Control**'e (A01) dahil edildi.
OWASP bu listeyi yeniden güncellemiş olabilir — yeni bir audit'e başlamadan önce
owasp.org/Top10 üzerinden kategori sırasının/isimlerinin hâlâ aşağıdaki gibi olduğunu
doğrula, eskimiş bir sürümü sessizce kullanma.

### A01 — Broken Access Control (SSRF dahil)
- Her route için `.RequireAuthorization()` var mı, olmalı mı? (`POST /posts` var;
  `POST /posts/{slug}/comments` yok — bunun kasıtlı bir "herkes yorum yazabilir" tasarımı mı
  yoksa gözden kaçmış bir açık mı olduğunu netleştir.)
- **Rol kontrolü eksik**: `POST /posts` sadece `RequireAuthorization()` kullanıyor;
  `User.Role` claim'i taşınıyor (`AuthEndpoint.cs`) ama hiçbir policy/`RequireRole` ile
  kontrol edilmiyor. Şu an tek kullanıcı (`admin`) olduğu için görünmüyor, ama yeni bir
  "Author" rolü eklenirse **her authenticated kullanıcı, rolünden bağımsız post
  oluşturabilir** — bunu somut bir bulgu olarak yaz.
- IDOR: route parametresiyle (`{slug}`) erişilen kaynak, isteği yapanın sahip olduğu bir
  kaynak mı diye bir sahiplik kontrolü var mı? (Şu an update/delete endpoint'i yok; biri
  eklenirse `authorId == currentUserId` kontrolünün şart olduğunu not et.)
- Mass assignment: DTO'lar (`CreatePostRequest`, `CreateCommentRequest`) entity'nin tamamını
  mı yansıtıyor, yoksa yalnız izinli alanları mı taşıyor? (Şu an `AuthorId`/`Id` request'ten
  alınmıyor — iyi bir pattern; yeni alan eklenirken bunun bozulmadığını doğrula.)
- **SSRF** (2025'ten önce ayrı A10 kategorisiydi, şimdi buraya dahil): şu an dışa HTTP
  isteği atan bir kod yolu yok → **N/A**. Yeni bir "URL'den görsel/OG metadata çek" gibi bir
  özellik eklenirse bu maddeyi burada tekrar aktif değerlendir.

### A02 — Security Misconfiguration
- **CORS**: `Program.cs:21-25` → `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`,
  zaten `// TODO: restrict in production` ile işaretli. Detaylı analiz için
  [CORS kontrol listesi](#cors-kontrol-listesi)'ne bak.
- Güvenlik header'ları (CSP, `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`) hiçbir middleware ile eklenmemiş.
- OpenAPI/Scalar sadece `IsDevelopment()` altında expose ediliyor — doğru pattern; ama
  `ASPNETCORE_ENVIRONMENT` production'da yanlış set edilirse (örn. "Development" kalırsa)
  API şeması ve olası stack trace'ler açığa çıkar — bunu bir deploy-config riski olarak not
  et, kod değişikliği önerme.
- `/auth/login`'in generic `Results.Unauthorized()` dönmesi (kullanıcı var/yok bilgisini
  sızdırmaması) **iyi bir uygulama** — bunu bulgu olarak değil, "korunması gereken doğru
  davranış" olarak not et; birisi "daha açıklayıcı hata mesajı" isterse buna karşı uyar.
- Bu kategori 2025'te 5.'ten 2.'ye yükseldi — CORS bulgusunun severity'sini buna göre
  (bkz. [CORS kontrol listesi](#cors-kontrol-listesi)) yeniden değerlendir, mekanik olarak
  eski severity'yi kopyalama.

### A03 — Software Supply Chain Failures
2025'te eski "Vulnerable and Outdated Components"'i genişleterek tedarik zinciri
bütünlüğünü de kapsayan bir kategori:
- `DevBlog.Api.csproj`'daki paket sürümlerini (`Microsoft.EntityFrameworkCore*`,
  `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`,
  `Scalar.AspNetCore`) `dotnet list package --vulnerable` ile kontrol et.
- Frontend `package.json`/`package-lock.json` için `npm audit` öner (çalıştırabiliyorsan
  çalıştır).
- Ek olarak (genişletilmiş kapsam): lockfile'lar (`package-lock.json`, varsa
  `packages.lock.json`) commit edilmiş mi; `index.html` veya component template'lerinde
  CDN'den pinlenmemiş/imzasız bir üçüncü taraf script yükleniyor mu? Şu an böyle bir script
  görülmedi, ama yeni bir bağımlılık/script eklenirse burada tekrar kontrol et.

### A04 — Cryptographic Failures
- **Şifre "hashleme"**: `AuthEndpoint.cs` ve `DataSeeder.cs`, `Convert.ToBase64String` ile
  şifreyi encode ediyor — bu hashing değil, tersinir bir encoding. Salt yok, iterasyon yok.
  DB sızıntısı = düz metin şifre sızıntısı. Somut öneri: OWASP'ın 2026 itibarıyla birincil
  önerisi **Argon2id** (memory-hard, GPU/ASIC saldırılarını maliyetli kılar; örn.
  `Konscious.Security.Cryptography.Argon2` ile `m=19MiB, t=2, p=1` veya `m=46MiB, t=1, p=1`).
  `BCrypt.Net` (cost ≥ 12) hâlâ kabul edilebilir bir alternatiftir; `PBKDF2`
  (`Rfc2898DeriveBytes`) sadece FIPS uyumluluğu zorunluysa tercih edilmeli.
- **Hardcoded JWT secret**: `Program.cs:28` ve `AuthEndpoint.cs:24` içinde aynı literal
  string (`"devblog-super-secret-key-2024-dev"`) iki kez tanımlı ve repoya commit edilmiş.
  Kaynak koduna erişimi olan herkes, istediği claim/role ile geçerli bir token
  imzalayabilir → tam yetki devralma. Ayrıca iki dosyada duplike olduğu için biri değişip
  diğeri unutulursa token doğrulama kırılır. Öneri: secret'ı `IConfiguration`/user-secrets/
  ortam değişkeninden oku, tek bir yerden inject et.
- **HTTPS enforcement yok**: `Program.cs`'de `UseHttpsRedirection()`/HSTS çağrısı yok —
  trafik düz HTTP üzerinden gidebilir, JWT bearer token ağ dinlemesine (sniffing) açık.

### A05 — Injection
- EF Core LINQ (parametrize sorgular) kullanılıyor — SQL injection riski şu an düşük.
  Yeni eklenen her kod yolunda `FromSqlRaw`/string interpolation ile SQL kullanılmadığını
  doğrula.
- Log injection: kullanıcı girdisi (username, comment body) ham haliyle loglara yazılıyor mu?
  Şu an structured logging kullanımı görülmedi; eklenirse CRLF/log injection'a dikkat çek.

### A06 — Insecure Design
- **Rate limiting yok**: Hiçbir endpoint'te `AddRateLimiter`/`UseRateLimiter` yok.
  `/auth/login` brute-force/credential stuffing'e, `/posts/{slug}/comments` spam/flood'a
  tamamen açık.
- **Input uzunluk sınırı yok**: `CreatePostRequest`/`CreateCommentRequest`/`LoginRequest`
  alanlarında max length yok — aşırı büyük body ile depolama/DoS riski.
- **Yorum sahiplik doğrulaması yok** (bkz. [Repo'ya özgü senaryolar](#repoya-özgü-senaryolar) →
  Comment spoofing).

### A07 — Authentication Failures
(2025'te "Identification and Authentication Failures"tan yeniden adlandırıldı.)
- Şifre politikası (min uzunluk/karmaşıklık) hiçbir yerde yok; seed'de `admin`/`admin` gibi
  zayıf bir kimlik bilgisi DB'ye yazılıyor (`DataSeeder.cs`).
- **Token yaşam döngüsü 2026 pratiğinden geride**: token expiration 8 saat
  (`AuthEndpoint.cs:37`), revocation/refresh mekanizması yok — çalınan bir token 8 saat
  boyunca hiçbir şekilde iptal edilemez (JWT stateless). Güncel yaklaşım kısa ömürlü
  (5-15 dk) access token + ayrı saklanan, her kullanımda rotate edilen refresh token'dır;
  bunu somut bir bulgu olarak yaz, sadece "revocation yok" demekle sınırlı kalma.
- Brute-force koruması yok (bkz. A06 rate limiting).
- `ValidateIssuer = false, ValidateAudience = false` (`Program.cs:36-37`) — bu API tek
  servis olduğu için düşük risk, ama başka bir sistemin aynı secret'ı bilmesi durumunda
  üretilen bir token'ın burada da kabul edilebileceğini not et.
- `HS256` + paylaşılan secret, tek bir monolitik API için kabul edilebilir; API başka
  servislere bölünürse (mikroservis) her doğrulayan servisin token da üretebilmesi riski
  doğar — o noktada asimetrik (`RS256`) imzalamaya geçişi öner, şimdiden zorunlu değil.

### A08 — Software or Data Integrity Failures
(2025'te "and" → "or" olarak küçük bir isim değişikliği; kapsam aynı.)
- Migration'lar `db.Database.Migrate()` ile otomatik uygulanıyor (`Program.cs:50-55`) —
  review edilmemiş bir migration'ın deploy anında doğrudan production'a gitme riski
  (bkz. [[migration-guvenlik-kontrolu]] skill'i ile çapraz kontrol et, tekrar aynı bulguyu
  üretme, referans ver).
- JWT imzalama anahtarının kaynak kodda olması (A04) aynı zamanda bir bütünlük sorunu:
  repoya erişimi olan herkes geçerli/imzalı token üretebilir.

### A09 — Security Logging and Alerting Failures
(2025'te "Monitoring" → "Alerting" vurgusuyla yeniden adlandırıldı: sadece loglamak değil,
şüpheli aktiviteye *alarm üretmek* de bu kategorinin kapsamında.)
- Başarısız login denemeleri loglanmıyor — brute-force/credential stuffing tespiti mümkün
  değil.
- Loglama eklense bile (henüz eklenmedi) tek başına yeterli değil: art arda başarısız login
  gibi bir eşiği aşan durumları işaretleyen bir alerting mekanizması da yok — bu ikisini
  tek bir "logging yok" bulgusu gibi değil, ayrı ayrı not et.
- Post/comment oluşturma dışında bir audit trail yok (silme/güncelleme endpoint'i henüz
  yok; eklenince bu maddeyi tekrar değerlendir).

### A10 — Mishandling of Exceptional Conditions
(2025'te tamamen yeni eklenen kategori; eski A10 SSRF'nin yerini aldı — SSRF şimdi A01'e
taşındı. Hata yönetimi, mantık hataları ve "fail open" davranışına odaklanır.)
- **Global exception handler yok**: `Program.cs`'de `UseExceptionHandler`/`IExceptionHandler`
  kaydı yok — production'da unhandled exception'ların generic bir hata dönmesi (stack trace
  sızdırmadan) garanti edilmiyor; `ASPNETCORE_ENVIRONMENT` doğru ayarlanmazsa bilgi
  sızıntısı riski (bkz. A02 deploy-config notu).
- **Fail-open riski**: bir endpoint içinde try/catch ile bir hata yutulup istek "başarılı"
  gibi mi devam ettiriliyor (fail open), yoksa hata isteği durduruyor mu (fail closed)? Şu
  an endpoint'lerde açık bir try/catch görülmedi — hata varsayılan ASP.NET Core davranışına
  (500 dönmesi) bırakılıyor, bu doğru yönde. Yeni bir catch bloğu eklenirse hatayı sessizce
  yutup 200 dönmediğinden emin ol; bunu her yeni endpoint incelemesinde kontrol listesine ekle.

## Validation (girdi doğrulama) kontrol listesi

- Hiçbir DTO'da (`CreatePostRequest`, `CreateCommentRequest`, `LoginRequest`) data
  annotation (`[Required]`, `[MaxLength]`, `[EmailAddress]`) veya FluentValidation gibi bir
  doğrulama katmanı yok.
- `CreatePostRequest.Slug`: format kontrolü yok — boşluk/büyük harf/özel karakter
  içerebilir; `PostsEndpoint`'in slug tabanlı route'u (`GET /posts/{slug}`) için hem
  kullanılabilirlik hem güvenlik açısından (URL encoding sürprizleri) sorun olabilir.
- `CreateCommentRequest.AuthorName`/`Body`: boş/whitespace-only string kabul edilebilir,
  üst uzunluk sınırı yok.
- `LoginRequest.Username`/`Password`: null/boş kontrolü yok — model binding boş string
  geçirebilir; kırılmaz ama anlamsız DB sorgusuna yol açar.
- Genel öneri: minimal API'lerde guard clause'lar veya FluentValidation entegrasyonu; hangi
  alanların gerçekten "gerekli" olduğunu üründen (ürün sahibinden) doğrulat, tahmini sınır
  koyma.

## CORS kontrol listesi

- `Program.cs:21-25`'teki `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()` her audit'te
  flaglenir. Severity'yi mekanik olarak "Critical" yazma — şu an `AllowCredentials()`
  kullanılmadığı için (JWT `Authorization` header ile taşınıyor, cookie değil) doğrudan
  CSRF'e yol açmaz; severity'yi **High** olarak gerekçelendir: herhangi bir origin API'ye
  istek atabilir ve yanıtı okuyabilir (public post verisi için düşük etki, ama gelecekte
  hassas bir alan eklenirse etki büyür).
- Frontend `AuthService`'in TODO'sundaki gibi httpOnly cookie'ye geçilirse, bu CORS policy'si
  **anında CSRF'e tam açık** hale gelir (`AllowCredentials` + `AllowAnyOrigin` birlikte
  ASP.NET Core'da zaten derleme/çalışma zamanı hatası verir, ama geliştirici bunu fark
  edip origin whitelist'e geçmek zorunda kalır) — bu geçişten önce mutlaka spesifik origin
  listesine (örn. sadece dev `http://localhost:4200` + prod domain) kısıtlanması gerektiğini
  vurgula.

## Repo'ya özgü senaryolar

- **Layering bypass = validation bypass**: `CommentsEndpoint`/`AuthEndpoint` `AppDbContext`'e
  doğrudan erişiyor (CLAUDE.md → bilinen borç). Bu, doğrulama/iş mantığının her endpoint'te
  ayrı ayrı ve tutarsız yazılmasına yol açıyor — `PostsEndpoint`'teki merkezi slug-conflict
  kontrolü (`PostService.CreateAsync`) gibi bir eşdeğer, diğer ikisinde yok. Yeni bir
  endpoint eklerken bu tutarsızlığı büyütmemek için `PostsEndpoint` deseni referans alınmalı.
- **Frontend token storage → gecikmeli XSS zinciri**: `AuthService`, JWT'yi `localStorage`'da
  saklıyor (kodda zaten TODO: httpOnly cookie). `post-detail.component.html` şu an sadece
  `{{ }}` interpolation kullanıyor (Angular'ın otomatik HTML escape'i) — yani bugün
  doğrudan çalışan bir XSS yolu **yok**. Ama backend hiçbir input sanitization/output
  encoding yapmıyor; DB'deki her `Post.Content`/`Comment.Body`/`Comment.AuthorName` zaten
  kontrolsüz kullanıcı girdisi. İleride Markdown render gibi bir sebeple `[innerHTML]`
  kullanılırsa, mevcut kayıtlar anında stored XSS'e döner ve `localStorage`'daki token
  çalınabilir → hesap devralma. Bunu "şu an sömürülemez ama gizli/gecikmeli risk" olarak
  raporla, "şu an aktif XSS var" deme — abartma.
- **Comment spoofing**: `CommentsEndpoint`, `AuthorName`'i request body'den serbestçe alıyor,
  giriş yapmış kullanıcıyla eşleştirmiyor — herkes (login olmuş ya da olmamış) "admin" veya
  başka bir gerçek kullanıcı adına yorum yazabilir.
- **Seed admin credentials**: `DataSeeder.cs`, `admin`/`admin` (Base64) ile bir admin hesabı
  oluşturuyor. Migration'lar otomatik uygulanan bir ortamda (`Program.cs`) bu seed'in
  production'a taşınmaması ya da production'da devre dışı bırakılması gerektiğini vurgula.

## Severity ölçeği

- **Critical** — Auth bypass, tam kimlik/yetki devralma, secret/credential sızıntısı
  yoluyla sistemin tamamen ele geçirilebilmesi. (Örn: hardcoded JWT secret, Base64
  "hashing".)
- **High** — Yetki yükseltme, brute-force'a tamamen açık login, CORS/misconfig yoluyla veri
  sızıntısı, gecikmeli ama gerçek bir XSS zinciri.
- **Medium** — Rate limiting yokluğu, eksik input validation, güvenlik header'larının
  yokluğu, HTTPS redirection eksikliği.
- **Low** — Somut istismar senaryosu zayıf best-practice eksiklikleri (örn. audit log
  yokluğu, token revocation mekanizması yokluğu).
- **Info** — Hardening önerisi; henüz bir istismar senaryosu olmayan gözlem veya "doğru
  yapılmış, böyle kalsın" notu.

## Çıktı formatı

Rapor başında özet tablo:

| # | Endpoint/Alan | OWASP | Severity | Bulgu (kısa) |
|---|---|---|---|---|

Ardından her bulgu için:

```
### [SEVERITY] [OWASP-Kategori] Başlık

**Konum:** dosya:satır (örn. Program.cs:21-25)
**Bulgu:** Ne bulundu, somut kod referansıyla.
**Senaryo:** Bu açık istismar edilirse ne olur (gerçekçi bir saldırgan senaryosu).
**Öneri:** Somut, uygulanabilir düzeltme (kod/yaklaşım düzeyinde).
**Not:** CLAUDE.md → Technical debt'te zaten var mı? (Evet/Hayır)
```

Rapor sonunda, en yüksek üç bulgu "Hemen yapılması gerekenler" başlığı altında tekrar
listelenir (severity + tek satır gerekçe).

Her bulgu Critical/High için tek satırlık **somut kod önerisi** ekle (örn. gerçek bir
`Argon2id.HashPassword(...)` çağrısı, gerçek bir `policy.WithOrigins(...)` satırı) — genel
"iyi bir hashing algoritması kullanın" gibi soyut önerilerle geçme.

## Kapsam dışı / not düşülecekler

- Bu bir statik/kaynak-kod denetimidir; çalışan bir instance'a karşı gerçek bir pentest
  (network taraması, fuzzing, canlı saldırı) yapılmaz.
- Kod okuyarak tespit edilemeyen çalışma zamanı/deploy-ortamı davranışları (gerçek
  production'daki HTTPS termination, firewall, `ASPNETCORE_ENVIRONMENT` değeri) için
  "kod bunu garanti etmiyor, deploy config'i doğrulanmalı" diye not düş — tahmin etme.
- Bağımlılık taraması (`dotnet list package --vulnerable`, `npm audit`) çalıştırılamazsa
  (ağ erişimi yok, komut başarısız) bunu raporda açıkça "çalıştırılamadı" olarak belirt.

## Özet kontrol listesi (audit sonunda)

- [ ] `Program.cs` (CORS, JWT, middleware sırası, HTTPS, rate limiting, exception handling)
      incelendi mi?
- [ ] Her endpoint için Endpoint → Service/Repository → `AppDbContext` zinciri takip edildi
      mi, kim DbContext'e direkt erişiyor not edildi mi?
- [ ] OWASP Top 10'un her kategorisi için en az bir bulgu ya da açık "N/A" gerekçesi yazıldı
      mı?
- [ ] Validation ve CORS özel bölümleri dolduruldu mu?
- [ ] Frontend token storage (`auth.service.ts`) ve stored-content render path'i
      (`{{ }}` mi `[innerHTML]` mi) kontrol edildi mi?
- [ ] Her bulgu Bulgu/Senaryo/Öneri/Severity + dosya:satır formatında mı?
- [ ] CLAUDE.md teknik borç listesiyle çapraz kontrol yapıldı mı (Not: bilinen/yeni)?
- [ ] Özet tablo + "Hemen yapılması gerekenler" bölümü eklendi mi?
- [ ] Bağımlılık taraması çalıştırıldı mı, çalıştırılamadıysa açıkça belirtildi mi?
