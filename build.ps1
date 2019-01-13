dotnet clean
dotnet restore
dotnet build --output "$((Get-Item -Path ".\").FullName)\bin\Release\win10-x64" --configuration Release --runtime win10-x64 --verbosity diag
