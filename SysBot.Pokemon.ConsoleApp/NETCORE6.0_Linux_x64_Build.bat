dotnet clean
dotnet restore
dotnet publish --configuration release --framework net6.0 --runtime linux-x64
PAUSE