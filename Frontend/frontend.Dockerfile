# Étape 1 : Utiliser l'image pour construire SharedLibrary et Frontend
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /source
EXPOSE 7000

# Copier les fichiers projet et restaurer les dépendances
COPY SharedLibrary/*.csproj SharedLibrary/
COPY Frontend/*.csproj Frontend/

RUN dotnet restore Frontend/Frontend.csproj

# Copier tous les fichiers source
COPY . .

# Construire SharedLibrary
RUN dotnet build SharedLibrary/SharedLibrary.csproj -c Release -o /source/build

# Publier l'application Frontend
RUN dotnet publish Frontend/Frontend.csproj -c Release -o /source/publish

# Étape 2 : Utiliser l'image runtime pour exécuter l'application
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /source/publish .

ENTRYPOINT ["dotnet", "Frontend.dll"]
