# Démarrer BackendPatient
Start-Process "powershell" -ArgumentList "dotnet run --project BackendPatient" -NoNewWindow

# Pause pour donner du temps aux services de démarrer (ajuster le délai si nécessaire)
Start-Sleep -Seconds 1

# Démarrer Auth
Start-Process "powershell" -ArgumentList "dotnet run --project Auth" -NoNewWindow

# Pause pour donner du temps aux services de démarrer (ajuster le délai si nécessaire)
Start-Sleep -Seconds 2

# Démarrer ApiGateway
Start-Process "powershell" -ArgumentList "dotnet run --project ApiGateway" -NoNewWindow

# Pause pour donner du temps aux services de démarrer (ajuster le délai si nécessaire)
Start-Sleep -Seconds 4

# Démarrer Frontend avec dotnet watch run
Start-Process "powershell" -ArgumentList "dotnet watch run --project Frontend" -NoNewWindow

# Pause pour donner du temps aux services de démarrer (ajuster le délai si nécessaire)
Start-Sleep -Seconds 6



