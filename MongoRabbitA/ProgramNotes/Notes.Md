# ASP.NET Core: IHostedService & Database Seeding

## 1. Nedir?

`IHostedService`, ASP.NET Core uygulamalarında web sunucusu (Kestrel) HTTP isteklerini kabul etmeye başlamadan önce veya uygulama ömrü boyunca arka planda çalışması gereken görevleri yöneten temel arayüzdür (`interface`).

Klasik API denetleyicileri (Controllers) sadece dışarıdan gelen HTTP isteklerine yanıt verirken, `IHostedService` doğrudan uygulama yaşam döngüsüne (lifecycle) bağlanır.

---

## 2. Arayüz Tanımı ve Yaşam Döngüsü

```csharp
public interface IHostedService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

### Çalışma Sırası

1. **Konteyner Kilitlenir:** `builder.Build()` çalıştırılır.
2. **Kancalar Tetiklenir:** `builder.Services.AddHostedService<T>()` ile kaydedilen tüm servislerin `StartAsync` metotları eklenme sırasına göre art arda yürütülür.
3. **Portlar Açılır:** Bütün `StartAsync` işlemleri tamamlandıktan sonra web sunucusu (Kestrel) HTTP isteklerini dinlemeye başlar.
4. **Kapanış:** Kapanma sinyali (`Ctrl+C` veya `SIGTERM`) geldiğinde `StopAsync` metotları kayıt sırasının tersi yönde çağrılır.

---

## 3. Neden Startup/Program.cs Yerine IHostedService?

| Kriter                       | `Program.cs` İçinde Elle Çağırma                                 | `IHostedService` Yaklaşımı                          |
| ---------------------------- | ---------------------------------------------------------------- | --------------------------------------------------- |
| **Kod Temizliği**            | `GetRequiredService` çağrıları ile `Program.cs` kirlenir         | `Program.cs` yalnızca tek satır kayıt içerir        |
| **Bağımlılık Yönetimi (DI)** | Servisler elle çekilmek zorundadır                               | Constructor Injection otomatik çalışır              |
| **Yaşam Döngüsü Kontrolü**   | Kırılgan ve manuel bir akış oluşur                               | Framework garantisiyle sunucu başlamadan önce koşar |
| **Genişletilebilirlik**      | İptal belirteci (`CancellationToken`) ve kapanış yönetimi zordur | Başlangıç ve kapanış için standart iki metot sunar  |

---

## 4. Kullanım Alanları

- **Database Seeding:** Uygulama açılışında veritabanı boşsa tohumlama verisi aktarma.
- **Message Queue Tüketimi:** RabbitMQ, Kafka veya Azure Service Bus dinleyicileri.
- **Zamanlanmış İşler (Cron):** Arka planda periyodik çalışan temizlik veya senkronizasyon servisleri.
- **Sistem Ön Isıtma (Warm-up):** Uygulama trafiğe açılmadan önce önbellek veya kritik bağlantıları hazır hale getirme.

---

## 5. Kritik Hatalar ve Kaçınma Yolları

### A. Scoped Servis Enjeksiyon Hatası

- `IHostedService` varsayılan olarak **Singleton** ömürlüdür.
- Constructor üzerinden doğrudan **Scoped** bir servis (örneğin Entity Framework `DbContext`) enjekte edilirse runtime hatası oluşur.
- **Çözüm:** `IServiceScopeFactory` ile manuel scope açılmalıdır:
  ```csharp
  using var scope = _scopeFactory.CreateScope();
  var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
  ```

### B. StartAsync İçinde Bloke Edici Sonsuz Döngüler

- `StartAsync` içinde sonsuz bir `while` döngüsü çalıştırılır ve `await` ile bloke edilirse, bu metot tamamlanamayacağı için web sunucusu asla HTTP isteklerini kabul etmeye başlayamaz.
- **Çözüm:** Sürekli döngü gerektiren arka plan işleri için `IHostedService` yerine doğrudan `BackgroundService` sınıfından türetme yapılıp `ExecuteAsync` metodu ezilmelidir.

---

## 6. Referans Uygulama: MongoDB DbSeeder

### Sınıf Tanımı (`Services/DbSeeder.cs`)

```csharp
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using MongoRabbitA.DTOs;

namespace MongoRabbitA.Services
{
    public class DbSeeder : IHostedService
    {
        private readonly MongoDbService _mongoDbService;
        private readonly IHostEnvironment _env;

        public DbSeeder(MongoDbService mongoDbService, IHostEnvironment env)
        {
            _mongoDbService = mongoDbService;
            _env = env;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 1. Verify if database is empty
            var count = await _mongoDbService.CountDocumentAsync();
            if (count > 0)
            {
                return;
            }

            // 2. Resolve JSON seed file path
            var filePath = Path.Combine(_env.ContentRootPath, "Data", "Seed", "products.json");
            if (!File.Exists(filePath))
            {
                return;
            }

            // 3. Deserialize stream
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            await using var stream = File.OpenRead(filePath);
            var products = await JsonSerializer.DeserializeAsync<List<CreateProductDTO>>(stream, options, cancellationToken);

            // 4. Bulk insert into collection
            if (products is not null && products.Count > 0)
            {
                await _mongoDbService.InsertManyProductAsync(products);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
```

### Konteyner Kaydı (`Program.cs`)

```csharp
builder.Services.AddHostedService<DbSeeder>();
```
