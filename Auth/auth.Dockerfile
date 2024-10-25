# Étape 1 : Utiliser l'image SDK pour construire SharedLibrary et ApiGateway
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copier les fichiers projet et restaurer les dépendances
COPY SharedLibrary/*.csproj SharedLibrary/
COPY ApiGateway/*.csproj ApiGateway/

RUN dotnet restore ApiGateway/ApiGateway.csproj

# Copier tous les fichiers source
COPY . .

# Construire SharedLibrary
RUN dotnet build SharedLibrary/SharedLibrary.csproj -c Release -o /source/build

# Construire ApiGateway
RUN dotnet build ApiGateway/ApiGateway.csproj -c Release -o /source/build

# Étape 2 : Utiliser l'image runtime pour exécuter l'application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /source/build .

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
