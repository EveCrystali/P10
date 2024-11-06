# Étape 1 : Construire l'application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copier les fichiers projet et restaurer les dépendances
COPY SharedLibrary/*.csproj SharedLibrary/
COPY BackendNote/*.csproj BackendNote/
RUN dotnet restore BackendNote/BackendNote.csproj

# Copier tous les fichiers source
COPY . .

# Publier l'application en mode Release
RUN dotnet publish BackendNote/BackendNote.csproj -c Release -o /app

# Étape 2 : Construire l'image runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copier les fichiers publiés depuis l'étape de build
COPY --from=build /app ./

# Exposer le port
EXPOSE 7202

# Définir le point d'entrée
ENTRYPOINT ["dotnet", "BackendNote.dll"]
