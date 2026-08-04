---
name: tr-neden-sonuc-mesaji
description: DevBlog reposunda commit mesajı yazarken veya code review yaparken Türkçe, neden-sonuç ilişkisi kuran açıklayıcı bir mesaj üretir. Kullanıcı "commit mesajı yaz", "code review yap", "bu değişikliği açıkla" dediğinde veya bir git commit/PR incelemesi yapılacağında kullanılır.
---

# Türkçe Neden-Sonuç Mesajı

Bu skill, DevBlog reposunda **commit mesajları** ve **code review** çıktıları için
Türkçe, açıklayıcı ve neden-sonuç ilişkisi kuran bir yazım biçimi tanımlar.
Amaç, "ne değişti"yi değil "neden değişti" ve "bunun sonucu ne oldu/olacak"ı öne çıkarmaktır.

## Ne zaman kullanılır

- Kullanıcı bir commit oluşturmasını istediğinde (`git commit`).
- Kullanıcı bir code review istediğinde (`/code-review`, `/review` veya elle inceleme).
- Kullanıcı bir değişikliği/PR'ı Türkçe olarak açıklamasını istediğinde.

## Ortak ilke: Neden → Değişiklik → Sonuç

Her mesaj üç unsuru içermeli, ama rapor şablonu gibi başlıklandırılmadan doğal bir
anlatımla birleştirilmeli:

1. **Neden**: Bu değişikliğe ihtiyaç neden doğdu? (bug, eksik davranış, teknik borç,
   CLAUDE.md'de tanımlı bir mimari karar, kullanıcı talebi vb.)
2. **Ne yapıldı**: Değişiklik somut olarak ne yaptı? (dosya/katman seviyesinde, "what" değil
   "how" — kodun kendisi zaten "what"ı gösteriyor.)
3. **Sonuç**: Bu değişiklik olmadan ne olurdu / bu değişiklikle ne mümkün oldu ya da hangi
   risk ortadan kalktı?

Diff'e bakıp sadece "ne değiştiğini" listelemek yeterli değildir; commit mesajı veya review
yorumu, ilgili diff'in *arkasındaki motivasyonu* göstermelidir (git log, ilgili endpoint/servis
kodu, CLAUDE.md'deki mimari kararlar ve bilinen teknik borç listesi taranarak).

## Commit mesajı için biçim

Türkçe, imperative olmayan (Türkçe'de zaten doğal olan) açıklayıcı bir başlık + gövde:

```
<kısa özet, 50-72 karakter>

<Neden bu değişikliğe gerek duyuldu — hangi sorunu/ihtiyacı çözüyor.>
<Bunun için ne yapıldı (katman/dosya seviyesinde, kısa).>
<Sonuç: bu değişiklik olmadan ne olurdu ya da artık ne mümkün.>
```

Kurallar:
- Başlık satırı Türkçe ve kısa: neyin değiştiğini değil, hangi ihtiyacı karşıladığını özetler
  (örn. "fix:", "feat:", "refactor:" gibi conventional-commit önekleri korunabilir, ama açıklama
  Türkçe olur).
- Gövdede "what" tekrarı yapılmaz (kod zaten gösteriyor); odak "why" ve "so what" üzerindedir.
- CLAUDE.md'de tanımlı mimari kararlara (örn. `Endpoint → Service → Repository` katmanlaşması,
  teknik borç listesi) atıfta bulunmak, neden-sonuç bağını netleştiriyorsa eklenir.
- `Co-Authored-By` satırı gibi harness gereksinimleri olduğu gibi korunur (Türkçe'ye çevrilmez).

## Code review için biçim

Her bulgu için Türkçe, tek paragraflık bir açıklama:

```
<Dosya:satır> — <Sorunun/riskin ne olduğu>.
Neden: <bu kod satırının/deseninin neden bir soruna yol açtığı veya açabileceği>.
Sonuç: <düzeltilmezse ortaya çıkacak somut etki — hata senaryosu, performans kaybı,
CLAUDE.md'deki bir kuralın ihlali vb.>.
```

Kurallar:
- Sadece "bu satır yanlış" demek yeterli değildir; *neden* yanlış olduğu ve düzeltilmediğinde
  *ne olacağı* somut bir senaryoyla belirtilir.
- CLAUDE.md'deki mevcut teknik borç listesiyle çakışan bulgular ("zaten bilinen borç" olanlar)
  ayrıca işaretlenir, tekrar aynı bulguyu yeni bir sorun gibi raporlamamak için.
- Review bulguları varsa ve host `ReportFindings` aracını bekliyorsa, `summary` ve
  `failure_scenario` alanları Türkçe ve bu neden-sonuç yapısında doldurulur.

## Örnek — Commit mesajı

```
fix: post slug'larının benzersizliğini repository katmanında garanti et

Aynı slug'a sahip iki post oluşturulabiliyordu çünkü benzersizlik kontrolü
sadece frontend'de yapılıyordu ve backend'e doğrudan istek atıldığında bu
kontrol devre dışı kalıyordu. PostService artık kayıttan önce
IPostRepository.ExistsBySlugAsync ile kontrol yapıyor. Bu sayede API'ye
doğrudan istek gönderilse dahi çakışan slug'lı post oluşturulamıyor ve
GET /posts/{slug} sorgularının tekil sonuç döndürmesi garanti ediliyor.
```

## Örnek — Code review bulgusu

```
Endpoints/CommentsEndpoint.cs:34 — Endpoint, AppDbContext'i doğrudan enjekte
ediyor.
Neden: CLAUDE.md'deki hedef katmanlaşma (Endpoint → Service → Repository)
ihlal ediliyor; iş mantığı endpoint içine sızıyor.
Sonuç: Yorum ekleme mantığı test edilemez hale geliyor (DbContext mock'lamak
gerekir) ve PostsEndpoint'te kurulan servis/repository deseninden sapma,
kod tabanında iki farklı katmanlaşma stiline yol açıyor.
```
