FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Medika.Api/Medika.Api.csproj", "src/Medika.Api/"]
COPY ["src/Medika.Application/Medika.Application.csproj", "src/Medika.Application/"]
COPY ["src/Medika.Domain/Medika.Domain.csproj", "src/Medika.Domain/"]
COPY ["src/Medika.Infrastructure/Medika.Infrastructure.csproj", "src/Medika.Infrastructure/"]
RUN dotnet restore "src/Medika.Api/Medika.Api.csproj"
COPY . .
RUN dotnet build "src/Medika.Api/Medika.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Medika.Api/Medika.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Medika.Api.dll"]
