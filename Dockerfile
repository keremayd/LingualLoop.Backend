# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Solution dosyasını ve projeleri kopyala
COPY LingualLoop.Api.sln ./
COPY LingualLoop.Api/*.csproj ./LingualLoop.Api/
COPY LingualLoop.Hangfire/*.csproj ./LingualLoop.Hangfire/

# Restore işlemi (tüm solution için)
RUN dotnet restore

# Projeleri kopyala (kodlar ve diğer dosyalar)
COPY . .

# Publish işlemi - API projesi için
RUN dotnet publish LingualLoop.Api/LingualLoop.Api.csproj -c Release -o out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Build aşamasından derlenen dosyaları kopyala
COPY --from=build /app/out ./

EXPOSE 80

ENTRYPOINT ["dotnet", "LingualLoop.Api.dll"]
