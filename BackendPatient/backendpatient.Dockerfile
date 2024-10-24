# Étape 1 : Construire l'application BackendPatient
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copier les fichiers projet et restaurer les dépendances
COPY ./*.csproj ./
RUN dotnet restore

# Copier tous les fichiers source et les fichiers partagés
COPY . ./
COPY ./SharedLibrary/*.csproj ../SharedLibrary/  

# Construire le projet
RUN dotnet publish -c Release -o /app/publish

# Étape 2 : Créer l'image finale
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copier les fichiers compilés du BackendPatient
COPY --from=build /app/publish .

# Copier la bibliothèque partagée
COPY /app/libs/SharedLibrary.dll /app/libs/

# Exposer le port
EXPOSE 7200

# Démarrer l'application
ENTRYPOINT ["dotnet", "BackendPatient.dll"]
