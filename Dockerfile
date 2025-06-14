# sdk image ile build yapıyoruz
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# csproj ve solution kopyala ve restore yap
COPY *.csproj ./
RUN dotnet restore

# Tüm dosyaları kopyala ve publish et
COPY . .
RUN dotnet publish -c Release -o out

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out ./

EXPOSE 80

ENTRYPOINT ["dotnet", "LingualLoop.Api.dll"]