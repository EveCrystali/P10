Start-Process "powershell" -ArgumentList "dotnet run -debug --project BackendPatient" -NoNewWindow
Start-Process "powershell" -ArgumentList "dotnet run -debug --project ApiGateway" -NoNewWindow
Start-Process "powershell" -ArgumentList "dotnet watch run --project Frontend" -NoNewWindow
