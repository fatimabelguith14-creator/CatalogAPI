# Étape 1 : Image de base pour l'exécution
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
USER app

# Étape 2 : Image SDK pour la compilation
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copier le fichier .csproj
COPY ["src/ProductCatalog.Api/ProductCatalog.Api.csproj", "ProductCatalog.Api/"]

# Restaurer les dépendances
RUN dotnet restore "ProductCatalog.Api/ProductCatalog.Api.csproj"

# Copier le reste du code source
COPY src/ProductCatalog.Api/ ProductCatalog.Api/

# Se placer dans le projet
WORKDIR "/src/ProductCatalog.Api"

# Compiler
RUN dotnet build "ProductCatalog.Api.csproj" -c Release -o /app/build

# Étape 3 : Publication
FROM build AS publish
RUN dotnet publish "ProductCatalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Étape 4 : Image finale
FROM base AS final
WORKDIR /app

# Copier les fichiers publiés
COPY --from=publish /app/publish .

# Lancer l'application
ENTRYPOINT ["dotnet", "ProductCatalog.Api.dll"]
