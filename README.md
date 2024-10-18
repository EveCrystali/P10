# P10 Project

## Description

Ce projet est une application web composée de plusieurs microservices backend et frontend. Il utilise **ASP.NET Core 8** pour les services backend et un frontend basé sur **ASP.NET MVC**.

## Sommaire

- [Structure du Projet](#structure-du-projet)
- [Configuration](#configuration)
- [Installation](#installation)
- [Exécution](#ex%C3%A9cution)

## Structure du Projet

- **ApiGateway** : Contient le projet de la passerelle API avec **Ocelot**.
- **Auth** : Contient le projet d'authentification avec **Identity** et **JWT Bearer**.
- **BackendNote** : Contient le projet de gestion des notes avec **MongoDB Driver** et **Elasticsearch**.
- **BackendPatient** : Contient le projet de gestion des patients avec **Entity Framework** et SQL Server.
- **Frontend** : Contient le projet frontend avec **ASP.NET Core 8 MVC**.
- **SharedLibrary** : Contient les bibliothèques partagées entre les différents projets.

Frontend (port 7000) interagit avec l'API Gateway (port 5000). API Gateway redirige les requêtes vers :

- BackendPatient (port 7200),
- BackendNote (port 7202),
- Auth Service (port 7201).

BackendNote utilise MongoDB et Elasticsearch pour la gestion des notes et les recherches. BackendPatient utilise SQL Server via Entity Framework pour la gestion des patients. Auth Service gère l'authentification de tous les microservices avec JWT Bearer.

## Configuration

1. Configurez les fichiers `appsettings.json` pour chaque projet backend (`ApiGateway`, `Auth`, `BackendNote`, `BackendPatient`, `Frontend`).
    
    ### Exemple de `appsettings.json` pour BackendNote :
    ```json
   {
    "MongoDB": {
      "ConnectionString": "mongodb://mongo:27017",
      "DatabaseName": "NoteDB",
      "NotesCollectionName": "Notes"
    },
    "Elasticsearch": {
      "Uri": "http://elasticsearch:9200"
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "AllowedHosts": "*"
    }
    ```
    
2. Pour l'authentification dans **Auth** :
    
    - Assurez-vous de configurer la clé secrète pour le JWT :
      ```json 
      {
        "JWT": {
          "Secret": "YourJWTSecretKey",
          "Issuer": "your-app",
          "Audience": "your-users"
        }
      }
      ```
        
## Installation

1. Clonez le repository :
    
    `git clone https://github.com/votre-utilisateur/votre-repository.git cd votre-repository`
    
2. Restaurez les packages NuGet pour tous les services :
    
    `dotnet restore`
    

## Exécution

### Option 1 : Lancer tous les microservices avec `launch.json`

Voici un fichier `launch.json` pour Visual Studio Code, permettant de démarrer et déboguer tous les microservices simultanément :
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "C#: ApiGateway Debug",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/ApiGateway/bin/Debug/net8.0/ApiGateway.dll",
            "args": [],
            "cwd": "${workspaceFolder}/ApiGateway",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        },
        {
            "name": "C#: Auth Debug",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/Auth/bin/Debug/net8.0/Auth.dll",
            "args": [],
            "cwd": "${workspaceFolder}/Auth",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        },
        {
            "name": "C#: BackendPatient Debug",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/BackendPatient/bin/Debug/net8.0/BackendPatient.dll",
            "args": [],
            "cwd": "${workspaceFolder}/BackendPatient",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        },
        {
            "name": "C#: BackendNote Debug",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/BackendNote/bin/Debug/net8.0/BackendNote.dll",
            "args": [],
            "cwd": "${workspaceFolder}/BackendNote",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        },
        {
            "name": "C#: Frontend Debug",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/Frontend/bin/Debug/net8.0/Frontend.dll",
            "args": [],
            "cwd": "${workspaceFolder}/Frontend",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        }
    ],
    "compounds": [
        {
            "name": "Launch all microservices",
            "configurations": [
                "C#: ApiGateway Debug",
                "C#: Auth Debug",
                "C#: BackendPatient Debug",
                "C#: BackendNote Debug",
                "C#: Frontend Debug"
            ]
        }
    ]
}

```


### Option 2 : Utiliser Docker (si applicable)

Si vous utilisez **Docker** pour vos services, vous pouvez utiliser le fichier `docker-compose.yml` pour lancer tous les services ensemble
