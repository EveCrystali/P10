# Démarrer BackendPatient
Start-Process "powershell" -ArgumentList "dotnet run --project BackendPatient" -NoNewWindow

# Pause pour donner du temps aux services de démarrer (ajuster le délai si nécessaire)
Start-Sleep -Seconds 3

# Démarrer ApiGateway
Start-Process "powershell" -ArgumentList "dotnet run --project ApiGateway" -NoNewWindow

# Pause pour donner du temps aux services de démarrer (ajuster le délai si nécessaire)
Start-Sleep -Seconds 6

# Démarrer Frontend avec dotnet watch run
Start-Process "powershell" -ArgumentList "dotnet watch run --project Frontend" -NoNewWindow

# Pause pour donner du temps aux services de démarrer (ajuster le délai si nécessaire)
Start-Sleep -Seconds 9

# Ouvrir le navigateur une fois le frontend démarré
Start-Process "https://localhost:7200/swagger/index.html"
Start-Process "https://localhost:5000/"
Start-Process "https://localhost:7000/"
