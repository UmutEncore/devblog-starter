---
name: migration-guvenlik-kontrolu
description: DevBlog backend'inde yeni bir EF Core migration oluşturulurken veya mevcut bir migration dosyası düzenlenirken tetiklenir. DropColumn/DropTable gibi riskli işlemler için kullanıcı onayı zorunlu kılar, yeni eklenen alanlarda varsayılan değer/nullability kontrolü yapar. "migration oluştur", "migration ekle", "dotnet ef migrations add", "kolon sil", "tablo sil" gibi isteklerde veya backend/src/DevBlog.Api/Migrations/ altında dosya oluşturulacağında kullanılır.
---

# Migration Güvenlik Kontrolü

DevBlog'da migration'lar `Program.cs` içindeki `db.Database.Migrate()` ile **uygulama
her başladığında otomatik uygulanıyor** (bkz. CLAUDE.md → Common commands). Bu yüzden
`dotnet ef database update` ile elle "önizleme" yapma fırsatı çoğu zaman yoktur; asıl
güvenlik kapısı, migration **oluşturulduğu** an devreye girmelidir. Bu skill o kapıyı
tanımlar.

## Ne zaman tetiklenir

- `dotnet ef migrations add <Name>` çalıştırılmadan önce.
- `backend/src/DevBlog.Api/Migrations/` altında yeni bir migration dosyası oluşturulacağında
  veya mevcut bir migration dosyası elle düzenleneceğinde.
- `AppDbContext` üzerindeki entity'lerde (`Users`, `Posts`, `Comments`) alan/kolon/tablo
  ekleme, kaldırma veya tip değiştirme talebi geldiğinde.

## Akış

1. **Migration'ı oluşturmadan önce değişikliğin ne tür bir şema değişikliği olduğunu
   sınıflandır**: yeni alan ekleme / alan kaldırma / tablo kaldırma / tip veya nullability
   değişikliği / yeniden adlandırma.
2. Migration dosyası oluşturulduktan sonra (ya `dotnet ef migrations add` ile ya da elle),
   **`Up()` metodunu satır satır oku** ve aşağıdaki "Riskli işlemler" listesiyle eşleştir.
3. Riskli bir işlem varsa, migration'ı **uygulamadan** (yani `dotnet ef database update`
   çalıştırmadan, hatta uygulamayı yeniden başlatmadan) `AskUserQuestion` ile açık onay al.
   Onay isteğinde şunları netleştir:
   - Hangi tablo/kolon etkileniyor.
   - Veri kaybı senaryosu ne (örn. "Posts.Summary kolonu silinirse mevcut 3 seed post'un
     özet verisi kalıcı olarak kaybolur").
   - Geri alınabilir mi (`Down()` metodu veriyi geri getirebiliyor mu, yoksa sadece şema mı
     geri alıyor)?
4. Kullanıcı onayı vermezse migration dosyasını commit'leme/uygulama; alternatif bir yaklaşım
   öner (örn. kolonu silmek yerine önce nullable yapıp bir sonraki sürümde kaldırmak).
5. Riskli olmayan (sadece ekleme) durumlarda bile "Yeni alan kontrolü" bölümündeki
   default value / nullability kontrolünü atlamadan uygula.

## Riskli işlemler — mutlaka kullanıcı onayı gerektirir

Migration'ın `Up()` metodunda şu çağrılardan biri varsa **dur ve onay al**:

- `DropColumn`, `DropTable`
- `RenameColumn`, `RenameTable` (veri kaybetmez ama kod/DTO senkronizasyonunu bozabilir —
  bkz. CLAUDE.md → "Contract between frontend and backend")
- `DropForeignKey`, `DropIndex` (özellikle `Post.Slug` üzerindeki unique index gibi bir
  bütünlük kısıtı kaldırılıyorsa — CLAUDE.md'de bahsedilen slug benzersizliği kuralını
  bozar)
- `AlterColumn` ile: tip küçültme (örn. `text` → `varchar(50)`), `nullable: true` →
  `nullable: false` geçişi, veya bir string/numeric kolonun mevcut veriyle uyumsuz hale
  gelebileceği başka bir dönüşüm

Onay isteği formatı (AskUserQuestion ile):
- Soru: "Bu migration [tablo/kolon] üzerinde [işlem] yapıyor ve mevcut veriyi [somut etki]
  şekilde etkileyebilir. Devam edilsin mi?"
- Seçenekler: "Onaylıyorum, devam et" / "Migration'ı düzenle (örn. önce nullable yap)" /
  "İptal et"

## Yeni alan eklenirken kontrol edilecekler

Migration `AddColumn<T>` içeriyorsa:

1. **Non-nullable + default yok** → mevcut kayıtlar için sorun. EF Core, `defaultValue`/
   `defaultValueSql` verilmeden non-nullable bir kolon eklerse mevcut satırlara CLR
   varsayılanını (`0`, `""`, `false`...) yazar; bu genelde anlamsızdır (örn. bir
   `Post.ViewCount` alanı `0` alabilir ama bir `Post.AuthorEmail` alanı boş string almamalı).
   Bu durumda:
   - Kullanıcıya somut varsayılan değeri sor (`AskUserQuestion`), **veya**
   - Kolonu `nullable: true` yapıp uygulamanın/servis katmanının null'ı ele almasını öner,
     **veya**
   - Anlamlı bir `defaultValueSql`/`defaultValue` öner ve kullanıcıdan onay al.
2. **Default değer verilmiş ama semantik olarak şüpheli** (örn. tarih alanına
   `DateTime.MinValue`, ilişkisel bir ID alanına `0`) → bunu da onaya sun, sessizce kabul
   etme.
3. **Foreign key alanı ekleniyor** (örn. `Post` tablosuna yeni bir `CategoryId`) → mevcut
   satırlarda karşılık gelen satır yoksa FK kısıtı migration'ı başarısız kılar; nullable FK
   veya var olan bir kayda işaret eden bir default önerilmeli.
4. Nullable + default yok, gerçekten opsiyonel bir alan ise → ek onaya gerek yok, ama bunun
   neden güvenli olduğunu (mevcut kayıtlar `NULL` alacak ve bu kabul edilebilir) kısaca
   belirt.

## Down() metodunu unutma

Riskli bir `Up()` işlemi varsa, `Down()`'ın gerçekten şemayı (ve mümkünse veriyi) geri
getirip getiremediğini kontrol et. `DropColumn` sonrası `Down()` kolonu geri ekler ama
**veri geri gelmez** — bunu onay isteğinde açıkça belirt, "geri alınabilir" gibi yanlış bir
güven vermemek için.

## Özet kontrol listesi

- [ ] Migration `Up()` içinde `Drop*`/riskli `AlterColumn`/`Rename*` var mı? → varsa onay al.
- [ ] Yeni non-nullable kolon var mı ve default değeri var mı? → yoksa onay al ya da nullable
      öner.
- [ ] Yeni FK var mı ve mevcut satırlarla uyumlu mu?
- [ ] `Down()` gerçekten geri alabiliyor mu, yoksa sadece şemayı mı?
- [ ] Kullanıcı onayı alınmadan migration uygulanmadı / commit edilmedi mi?
