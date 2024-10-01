# Arrêtez les services en cours d'exécution
dotnet stop

# Arrêtez les services en cours d'exécution
dotnet build

# Lancez les services en mode debug sans reconstruire les projets
Start-Process dotnet -ArgumentList "run --launch-profile ""ApiGateway"" --project ApiGateway --no-build"
Start-Process dotnet -ArgumentList "run --launch-profile ""BackendPatient"" --project BackendPatient --no-build"
Start-Process dotnet -ArgumentList "run --launch-profile ""Auth"" --project Auth --no-build"
Start-Process dotnet -ArgumentList "run --launch-profile ""Frontend"" --project Frontend --no-build"