# Dockerfile pour SharedLibrary
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copier les fichiers de SharedLibrary et restaurer les dépendances
COPY *.csproj ./
RUN dotnet restore

# Copier tous les fichiers et construire la bibliothèque
COPY . ./
RUN dotnet build -c Release -o /app/build
