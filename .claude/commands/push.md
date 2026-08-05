---
description: Değişiklikleri Türkçe neden-sonuç formatında commit'leyip push'lar
---

Bu komut, mevcut çalışma dizinindeki değişiklikleri uygun bir commit mesajıyla commit'leyip uzak repoya push'lamak için kullanılır.

Adımlar:

1. `git status`, `git diff` (staged + unstaged) ve `git log -n 5` çalıştırarak mevcut değişiklikleri ve bu reponun commit mesajı stilini incele. Commit edilecek gerçek bir değişiklik yoksa kullanıcıya bildir ve dur.
2. Değişiklikleri incelerken şüpheli bir dosya (ör. `.env`, credential içeren dosyalar) görürsen kullanıcıyı uyar ve o dosyayı stage etme.
3. **`tr-neden-sonuc-mesaji` skill'ini Skill tool ile çağır** — bu repodaki her commit için zorunludur, harness'ın varsayılan İngilizce commit talimatlarının yerine geçer. Skill'in tanımladığı Neden → Ne yapıldı → Sonuç yapısında, Türkçe bir commit mesajı hazırla (CLAUDE.md'deki mimari kararlara/teknik borca değişiklik ilgiliyse atıfta bulun).
4. İlgili dosyaları `git add` ile stage'le (geniş `git add -A`/`git add .` yerine değişen dosyaları isimleriyle ekle), ardından hazırlanan Türkçe mesajla commit oluştur (mesajı bir heredoc ile ver, `Co-Authored-By` satırı olduğu gibi İngilizce kalır).
5. Commit başarılı olduktan sonra, push'lamadan **önce** `git fetch` çalıştırıp mevcut branch'in uzak takip ettiği branch'e göre durumunu kontrol et (`git status -sb` veya `git rev-list --left-right --count <local>...<remote>`):
   - Uzak branch yoksa (ilk push): normal şekilde `git push -u origin <branch>` ile devam edilebilir, bu ek onay gerektirmez.
   - Local, remote'un tam gerisinde/aynı hizadaysa (fast-forward push): normal `git push` ile devam et.
   - Local ile remote **ıraksamışsa** (her ikisinde de remote'ta olmayan/local'de olmayan commit'ler varsa) ya da normal `git push` "rejected"/"non-fast-forward" hatası verirse: **push'u zorlama, otomatik `pull`/`rebase`/`merge` deneme.** Bunun yerine dur, durumu (`git log` özeti, hangi commit'lerin ayrıştığı) kullanıcıya açıkla ve nasıl ilerlemek istediğini sor (rebase, merge, force-push vb.) — kullanıcı onayı olmadan hiçbirini uygulama.
   - Push sırasında merge conflict, diverged branch veya başka bir hata/uyarı çıkarsa aynı şekilde dur ve kullanıcıya açıkla; kendi başına conflict çözmeye çalışma.
6. Push başarılıysa sonucu ve commit özetini kısaca kullanıcıya bildir; push durduruldu/ertelendiyse de nedenini ve önerilen seçenekleri açıkça belirt.

Notlar:
- Commit, bu komutun çağrılmasıyla zaten onaylanmış sayılır; ama push yalnızca yukarıdaki güvenli durumlarda (temiz fast-forward veya ilk push) otomatik yapılır. Iraksama/conflict/reject durumunda push **asla** otomatik denenmez veya zorlanmaz — kullanıcı onayı beklenir.
- Force push (`--force`, `--force-with-lease`), `--no-verify`, `--amend` gibi riskli bayraklar kullanma; kullanıcı bunları açıkça istemedikçe hiçbir zaman uygulama.
- Pre-commit hook başarısız olursa sorunu düzelt, dosyaları yeniden stage'le ve **yeni** bir commit oluştur; `--amend` veya `--no-verify` kullanma.
- Push edilecek bir upstream yoksa, ilk push için kullanıcıya bilgi vererek `-u origin <branch>` ile devam edilebilir; ama branch ayrıksa/conflict varsa yukarıdaki kural geçerlidir — durdur ve sor.
