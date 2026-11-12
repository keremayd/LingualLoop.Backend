using AwsService.Extensions;
using Common.Options;
using LingualLoop.Api.Middlewares;
using Postgres.Extensions;
using Service.Extensions;
using Service.Handlers.Queries;

Console.WriteLine("[startup] entering Program");
var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
Console.WriteLine("[startup] builder created");

// Yerel kimlik bilgileri Git'e girmez; ortam ve komut satırı önceliği korunur.
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Add services to the container.
builder.Services.AddIdentity();
Console.WriteLine("[startup] identity registered");
builder.Services.AddJwt(builder.Configuration);
Console.WriteLine("[startup] jwt registered");
builder.Services
    .AddPostgres(builder.Configuration)
    .AddAwsS3Service(builder.Configuration);
Console.WriteLine("[startup] postgres/aws registered");

// İçerik girişi ucunun paylaşılan anahtarı. Bölüm yoksa anahtar boş kalır ve
// uç kendini kapatır (bkz. KartyAdminOptions).
builder.Services.Configure<KartyAdminOptions>(
    builder.Configuration.GetSection(KartyAdminOptions.SectionName));

// Apple ile giriş: token'ın alıcısı paket kimliği; Apple'ın anahtarları
// bellekte tutulur, bu yüzden doğrulayıcı tek örnek.
builder.Services.Configure<AppleOptions>(
    builder.Configuration.GetSection(AppleOptions.SectionName));
builder.Services.AddSingleton<Service.Helpers.AppleIdentityValidator>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});
builder.Services.AddControllers();
Console.WriteLine("[startup] controllers registered");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
Console.WriteLine("[startup] swagger registered");
builder.Services.AddMediatR(cfg=>cfg.RegisterServicesFromAssemblies(typeof(GetUserByIdQueryHandler).Assembly));
Console.WriteLine("[startup] mediatR registered");


var app = builder.Build();
Console.WriteLine("[startup] app built");

app.UseCors("AllowAllOrigins");

// Configure the HTTP byIdRequest pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
Console.WriteLine("[startup] controllers mapped");

app.Run();
