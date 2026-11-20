# Derleme aşaması (.NET 9; global.json 9.0.313 ister, aynı ana sürümde ileri gider)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Stelinda.Api/Stelinda.Api.csproj
RUN dotnet publish Stelinda.Api/Stelinda.Api.csproj -c Release -o /app/out --no-restore

# Çalışma aşaması
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Cloud Run dinlenecek portu PORT ile verir (varsayılan 8080).
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Stelinda.Api.dll"]
