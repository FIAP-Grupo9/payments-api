FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/FCG.Contracts/FCG.Contracts.csproj shared/FCG.Contracts/
COPY shared/FCG.BuildingBlocks/FCG.BuildingBlocks.csproj shared/FCG.BuildingBlocks/
COPY src/FCG.Payments.Api/FCG.Payments.Api.csproj src/FCG.Payments.Api/
RUN dotnet restore src/FCG.Payments.Api/FCG.Payments.Api.csproj

COPY shared/ shared/
COPY . .
RUN dotnet publish src/FCG.Payments.Api/FCG.Payments.Api.csproj \
    -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FCG.Payments.Api.dll"]
