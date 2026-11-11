FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Sobe.Api/Sobe.Api.csproj src/Sobe.Api/
RUN dotnet restore src/Sobe.Api/Sobe.Api.csproj
COPY src/Sobe.Api/ src/Sobe.Api/
RUN dotnet publish src/Sobe.Api/Sobe.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:5080
EXPOSE 5080
USER app
ENTRYPOINT ["dotnet", "Sobe.Api.dll"]
