# Usa la imagen oficial de .NET SDK 9.0 para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

# Usa la imagen de runtime 9.0 para ejecutar
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "PetShopApi.dll"]
