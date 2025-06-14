# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Tüm kodları kopyala
COPY . .

# Restore işlemi
RUN dotnet restore LingualLoop.Api.sln

# Publish işlemi - API projesi için
RUN dotnet publish LingualLoop.Api/LingualLoop.Api.csproj -c Release -o /app/out

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Derlenmiş dosyaları kopyala
COPY --from=build /app/out .

EXPOSE 80

ENTRYPOINT ["dotnet", "LingualLoop.Api.dll"]