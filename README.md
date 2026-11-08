# YILDIZ — Orman Simülasyonu

YILDIZ, `n × n` boyutlarındaki kare bir ormanda sol üst köşeden başlayan tavşan Yıldız'ın, rastgele yerleştirilmiş engelleri aşarak sağ alt köşedeki yuvasına (tavşan deliğine) ulaşmasını simüle eden bir .NET 10 konsol uygulamasıdır.

---

## 1. Proje Hakkında

Uygulama; orman üretimi, garanti yol oluşturma, komut ayrıştırma ve hareket/çarpışma simülasyonu kurallarını barındıran bağımsız bir çekirdek katman (`Yildiz.Core`), `Spectre.Console` ile görselleştirilmiş etkileşimli bir konsol arayüzü (`Yildiz.Console`) ve birim test projesi (`Yildiz.Tests`) olmak üzere üç projeden oluşur.

---

## 2. Kullanılan Teknolojiler

- **.NET 10 (`net10.0`)** ve **C#** (Nullable reference types etkin)
- **Spectre.Console `0.57.2`** (Yalnızca `Yildiz.Console` projesinde konsol tablosu, durum panelleri ve adım adım canlı animasyon için)
- **xUnit `2.9.3`** (`Yildiz.Tests` projesinde birim testler için)

---

## 3. Gereksinimler

- **.NET 10 SDK** (`10.0.100` veya üzeri)
- UTF-8 karakter kodlamasını destekleyen bir terminal (Windows Terminal, PowerShell, Command Prompt, macOS/Linux terminalleri)

---

## 4. Proje ve Klasör Yapısı

```text
Yildiz/
├── Yildiz.sln
├── src/
│   ├── Yildiz.Core/
│   │   ├── Yildiz.Core.csproj
│   │   ├── Commands/
│   │   │   ├── ScenarioCommand.cs
│   │   │   ├── ScenarioParseResult.cs
│   │   │   └── ScenarioParser.cs
│   │   ├── Generation/
│   │   │   ├── ForestGenerationResult.cs
│   │   │   └── ForestGenerator.cs
│   │   ├── Models/
│   │   │   ├── Direction.cs
│   │   │   ├── ForestBoard.cs
│   │   │   ├── ObstacleType.cs
│   │   │   ├── Position.cs
│   │   │   └── RabbitState.cs
│   │   ├── Pathfinding/
│   │   │   └── GuaranteedPathBuilder.cs
│   │   └── Simulation/
│   │       ├── SimulationEngine.cs
│   │       ├── SimulationResult.cs
│   │       ├── SimulationStatus.cs
│   │       └── SimulationStep.cs
│   └── Yildiz.Console/
│       ├── Yildiz.Console.csproj
│       ├── Rendering/
│       │   └── ForestConsoleRenderer.cs
│       ├── ConsoleApplication.cs
│       └── Program.cs
├── tests/
│   └── Yildiz.Tests/
│       ├── Yildiz.Tests.csproj
│       ├── ForestBoardTests.cs
│       ├── ForestGeneratorTests.cs
│       ├── MovementTests.cs
│       ├── ObstacleTests.cs
│       ├── ScenarioParserTests.cs
│       └── SimulationTests.cs
├── .gitignore
└── README.md
```

---

## 5. Kurulum ve Çalıştırma

Bağımlılıkları geri yüklemek ve projeyi derlemek için çözüm dizininde aşağıdaki komutları çalıştırın:

```bash
dotnet restore Yildiz.sln
dotnet build Yildiz.sln --configuration Release
```

Konsol uygulamasını başlatmak için:

```bash
dotnet run --project src/Yildiz.Console
```

### Windows EXE

Oluşturulan `publish/win-x64/Yildiz.Console.exe` dosyası doğrudan çalıştırılabilir.

---

## 6. Orman Oluşturma Kuralları

1. Kullanıcıdan alınan pozitif `n` tam sayısı için `n × n` boyutunda kare bir orman oluşturulur (`3`, `7`, `16`, `33`, `40`, `50` örnek boyutlardır; uygulama pozitif tam sayı boyutlarını destekler, büyük ormanlarda konsol görünümünü otomatik olarak hizalı kompakt ızgaraya veya görünüm kesitine uyarlar ve 32-bit dizi/bellek sınırlarını kontrollü biçimde denetler).
2. **Başlangıç Konumu ve Yönü:** Tavşan her zaman sol üst köşede (`(0, 0)`, kullanıcı ekranında `(1, 1)`) ve **Güney (`South`, `Y↓`)** yönüne bakarak başlar.
3. **Hedef (Tavşan Deliği):** Her zaman sağ alt köşededir (`(n - 1, n - 1)`, kullanıcı ekranında `(n, n)`).
4. **Engel Türleri ve Sayıları:**
   - Kurt (`Wolf` - `W`): Ölümcül engel
   - Tilki (`Fox` - `F`): Ölümcül engel
   - Dikenli Tel (`BarbedWire` - `T`): `İ` hazırlık komutuyla aşılabilen engel
   - Çit (`Fence` - `C`): `J` hazırlık komutuyla aşılabilen engel
   - Her engel türünden ormanda **0 ile 4 adet** arasında rastgele sayıda bulunur.
   - Bir hücrede yalnızca tek bir engel bulunabilir; başlangıç ve hedef hücrelerine engel yerleştirilmez.
   - Oluşturulan her ormanda başlangıçtan hedefe ulaşan **en az bir geçerli yol** bulunur.

### Konsol Sembolleri

| Sembol | Anlamı |
|---|---|
| `Y↓` / `Y↑` / `Y→` / `Y←` | Tavşan (Güney / Kuzey / Doğu / Batı yönünde) |
| `X↓` / `X↑` / `X→` / `X←` | Ölü Tavşan (son baktığı yön korunur) |
| `H` | Tavşan Deliği (Hedef) |
| `W` | Kurt |
| `F` | Tilki |
| `T` | Dikenli Tel |
| `C` | Çit |
| `•` | Geçilmiş Boş Hücre (Hareket İzi) |
| `·` | Boş Hücre |

---

## 7. Komutlar ve Açıklamaları

Kullanıcı komut dizisini virgülle ayırarak tek satırda girer (örnek: `N,N,L,J,N,İ,N`).

| Komut | Model Karşılığı | Açıklama |
|---|---|---|
| `N` | `MoveForward` | Tavşanın baktığı yönde 1 hücre ileri hareket eder. |
| `P` | `MoveBackward` | Tavşanın baktığı yönün tersine 1 hücre geri hareket eder; **baktığı yön değişmez**. |
| `R` | `TurnRight` | Bulunduğu hücrede 90° sağa (saat yönünde) döner (`South → West → North → East → South`). |
| `L` | `TurnLeft` | Bulunduğu hücrede 90° sola (saat yönünün tersine) döner (`South → East → North → West → South`). |
| `J` | `Jump` | Bir sonraki `N` veya `P` hareketinde çit (`Fence`) engelini aşmak için tek kullanımlık zıplama hazırlığı yapar. |
| `İ` | `Duck` | Bir sonraki `N` veya `P` hareketinde dikenli tel (`BarbedWire`) engelini aşmak için tek kullanımlık eğilme hazırlığı yapar (`İ`, `i`, `I`, `ı` desteklenir). |

### Örnek Komut Kombinasyonları

- `L,J,N` = Sola dön, zıplamaya hazırlan, ileri git.
- `L,İ,N` = Sola dön, eğilmeye hazırlan, ileri git.

> **Not:** Önce `R` veya `L` ile yön değiştirilebilir, ardından `J` veya `İ` ile geçiş hazırlığı yapılabilir ve hemen sonraki `N` veya `P` hareketiyle ilgili engel (`J` için çit, `İ` için dikenli tel) aşılabilir. `J` ve `İ` hazırlıkları tek kullanımlıktır; boş hücreye yapılan hareketlerde de tüketilerek sıfırlanır. Hedef hücrede hazırlık türüyle uyuşmayan bir engel varsa `Blocked`, kurt veya tilki varsa `Dead` durumu oluşur.

---

## 8. `J` ve `İ` Komutları

1. **Hazırlık Eylemi:** `J` ve `İ` komutları tek başına tavşanın konumunu veya yönünü değiştirmez; yalnızca hemen ardından gelen tek bir `N` veya `P` hareketi için geçiş izni (`PendingBypass`) tanımlar.
2. **Zorunlu Komut Sırası:** `J` veya `İ` komutundan hemen sonra **mutlaka `N` veya `P`** gelmelidir:
   - Araya `R`, `L` veya ikinci bir `J`/`İ` giremez (`J,L,N` veya `J,İ,N` geçersizdir).
   - Senaryo `J` veya `İ` ile sonlanamaz (`N,J` geçersizdir).
   - Bu kural ihlalleri simülasyon başlatılmadan önce `ScenarioParser` tarafından yakalanır ve hatalı komut sırası numarasıyla birlikte kullanıcıya bildirilir.
3. **Tek Kullanımlık Tüketim:**
   - `J,N` (veya `J,P`): Hedef hücrede çit (`C`) varsa tavşan o hücreye geçer; tek başına `N` veya `P` ile çite girilirse `Blocked` olur.
   - `İ,N` (veya `İ,P`): Hedef hücrede dikenli tel (`T`) varsa tavşan o hücreye geçer; tek başına `N` veya `P` ile dikenli tele girilirse `Blocked` olur.
   - Geçiş izni yalnızca takip eden ilk `N`/`P` hareketinde geçerlidir ve hedef hücre boş olsa dahi o hareket sonunda sıfırlanır (`J,N,N` dizisinde ikinci `N` normal harekettir).
4. **Yanlış Hazırlık ve Ölümcül Engeller:**
   - Dikenli tel önünde `J,N` veya çit önünde `İ,N` kullanılması `Blocked` ile sonuçlanır.
   - Kurt (`W`) veya tilki (`F`) hücresine `J,N` ya da `İ,N` ile girilmesi ölümü (`Dead`) engellemez.

---

## 9. Oyun Durumları ve Sonuçları

| Durum (`SimulationStatus`) | Açıklama ve Davranış |
|---|---|
| `Running` | Komut başarıyla işlendi, simülasyon sonraki komutla devam ediyor. |
| `Success` | Tavşan `(n - 1, n - 1)` hedef deliğine ulaştı. Simülasyon hemen durur; fazladan kalan komutlar çalıştırılmaz. (`n = 1` boyutunda başlangıç ile hedef aynı olduğundan simülasyon doğrudan `Success` ile sonuçlanır.) |
| `Dead` | Tavşan kurt (`W`) veya tilki (`F`) hücresine girdi (`X↓` vb. olarak gösterilir, ormandaki engel korunur). Simülasyon hemen durur; kalan komutlar işlenmez. |
| `Blocked` | Tavşan çit (`C`) veya dikenli tel (`T`) hücresine uygun hazırlık komutu olmadan girmeye çalıştı. Tavşan engelden önceki hücrede kalır ve simülasyon hemen durur. |
| `OutOfBounds` | `N` veya `P` hareketi orman sınırları dışına çıktı. Tavşan son geçerli konumunda kalır ve simülasyon hemen durur. |
| `Incomplete` | Tüm komutlar hatasız işlendi ancak tavşan henüz hedef deliğe ulaşmadı. |

Başarısız senaryoların (`Dead`, `Blocked`, `OutOfBounds`, `Incomplete`) ardından kullanıcıya aynı orman üzerinde hedefe ulaşan örnek bir başarılı komut dizisi gösterilir ve kullanıcı isterse (`E`) aynı ormanda başlangıç durumundan itibaren adım adım oynatılır.

---

## 10. Örnek Giriş ve Çıkış Senaryosu

```text
╭───┬────┬───┬───╮
│ # │ 1  │ 2 │ 3 │
├───┼────┼───┼───┤
│ 1 │ Y↓ │ F │ C │
│ 2 │ ·  │ · │ · │
│ 3 │ C  │ T │ H │
╰───┴────┴───┴───╯

Komut dizisini virgülle ayırarak giriniz (örnek: N,N,L,N,N): N,J,N,L,İ,N,N
```

- `1. Adım (N)`: `(2, 1)` hücresine ilerler (`Güney Y↓`).
- `2. Adım (J)`: Çit için zıplama hazırlığı yapar (`Konum: (2, 1)`).
- `3. Adım (N)`: `(3, 1)` hücresindeki çiti (`C`) aşar.
- `4. Adım (L)`: Sola dönerek yönünü `Doğu (Y→)` yapar.
- `5. Adım (İ)`: Dikenli tel için eğilme hazırlığı yapar (`Konum: (3, 1)`).
- `6. Adım (N)`: `(3, 2)` hücresindeki dikenli teli (`T`) aşar (`(3, 1)` hücresindeki `C` sembolü yeniden görünür).
- `7. Adım (N)`: `(3, 3)` hedef deliğine (`H`) ulaşır (`(3, 2)` hücresindeki `T` sembolü yeniden görünür) ve `TEBRİKLER! YILDIZ YUVASINA ULAŞTI.` mesajı gösterilir.

---
