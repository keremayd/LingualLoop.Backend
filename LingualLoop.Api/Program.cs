using LingualLoop.Api.Middlewares;
using Postgres.Extensions;
using Service.Extensions;
using Service.Handlers.Queries;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddIdentity();
builder.Services.AddJwt(builder.Configuration);
builder.Services.AddPostgres(builder.Configuration);
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg=>cfg.RegisterServicesFromAssemblies(typeof(GetUserByIdQueryHandler).Assembly));


var app = builder.Build();

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

app.Run();