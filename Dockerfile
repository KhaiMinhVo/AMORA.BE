FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Amora.BE.sln ./
COPY src/Amora.Domain/Amora.Domain.csproj src/Amora.Domain/
COPY src/Amora.Application/Amora.Application.csproj src/Amora.Application/
COPY src/Amora.Infrastructure/Amora.Infrastructure.csproj src/Amora.Infrastructure/
COPY src/Amora.Api/Amora.Api.csproj src/Amora.Api/
RUN dotnet restore src/Amora.Api/Amora.Api.csproj
COPY src/ src/
RUN dotnet publish src/Amora.Api/Amora.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Amora.Api.dll"]
