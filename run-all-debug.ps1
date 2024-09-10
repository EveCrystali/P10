Start-Process "powershell" -ArgumentList "dotnet run -debug --project BackendPatient" 
Start-Process "powershell" -ArgumentList "dotnet run -debug --project ApiGateway" 
Start-Process "powershell" -ArgumentList "dotnet run -debug --project Frontend" 