# Yerel yapılandırma

API ve Hangfire ortak ayarları `appsettings.json` dosyalarından okur. Kimlik
bilgileri, her projenin yanındaki **Git tarafından yok sayılan**
`appsettings.Local.json` dosyasında veya ortam değişkenlerinde tutulur.

Yeni makinede ilgili proje dizininde `appsettings.Local.json` oluşturup aşağıdaki
alanları güvenli kaynağınızdan doldurun. Dosya .NET yapılandırmasıyla aynı iç içe
JSON yapısını kullanır; örneğin `PostgresOptions:LingualLoopConnectionString`,
`PostgresOptions` nesnesinin içindeki `LingualLoopConnectionString` alanıdır.

| Proje | Alanlar |
|---|---|
| API | `PostgresOptions:LingualLoopConnectionString`, `JwtOptions:SecretKey`, `AwsOptions:AccessKey`, `AwsOptions:SecretKey` |
| API içerik yönetimi | `KartyAdmin:ApiKey` — boş bırakılırsa içerik girişi ucu kapalı kalır |
| Hangfire | `PostgresOptions:LingualLoopConnectionString`, `HangfireOptions:ConnectionString`, `AwsOptions:AccessKey`, `AwsOptions:SecretKey` |

Yerel dosya derleme çıktısına kopyalanır; yayın paketine dahil edilmez. Ortam
değişkenleri yerel dosyadan, komut satırı da ortam değişkenlerinden önceliklidir.
Ortam değişkenlerinde bölüm ayıracı `__` kullanılır; örneğin
`PostgresOptions__LingualLoopConnectionString`.

API proje dizininden `dotnet run` ile 5214, Hangfire kendi proje dizininden
`dotnet run` ile 5215 portunda çalışır. API'nin içerik kökü uygulama çıktı dizini,
Hangfire'ın içerik kökü çalıştırıldığı dizindir.

`appsettings.Local.json` dosyalarını commit etmeyin. Güncel ortak ayarlardan
kimlik bilgileri çıkarılmıştır; bu işlem önceki Git geçmişini yeniden yazmaz.
