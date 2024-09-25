Start-Process "powershell" -ArgumentList "dotnet run --debug --project BackendPatient -NoNewWindow" 
Start-Process "powershell" -ArgumentList "dotnet run --debug --project Auth -NoNewWindow" 
Start-Process "powershell" -ArgumentList "dotnet run --debug --project ApiGateway -NoNewWindow" 
Start-Process "powershell" -ArgumentList "dotnet run --debug --project Frontend -NoNewWindow" 