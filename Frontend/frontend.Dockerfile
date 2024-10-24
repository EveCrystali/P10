# Étape 1 : Construire l'application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copier les fichiers projet et restaurer les dépendances
COPY *.csproj ./
RUN dotnet restore

# Copier tous les fichiers et construire l'application
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Étape 2 : Créer l'image finale
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Exposer le port sur lequel ton Frontend écoute (port 7000 d'après tes infos)
EXPOSE 7000

# Démarrer l'application
ENTRYPOINT ["dotnet", "Frontend.dll"]
