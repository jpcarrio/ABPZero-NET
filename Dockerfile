FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore

RUN dotnet build "ISUCore.sln" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "src/ISUCore.Web.Host/ISUCore.Web.Host.csproj" -c Debug -o /app/publish --no-restore
ENV ASPNETCORE_ENVIRONMENT=Staging
RUN touch "src/ISUCore.Web.Host/appsettings.json"
RUN cat "src/ISUCore.Web.Host/appsettings.Staging.json" > "src/ISUCore.Web.Host/appsettings.json"
RUN dotnet tool install --global dotnet-ef
ENV PATH $PATH:/root/.dotnet/tools
RUN dotnet-ef database update --project src/ISUCore.EntityFrameworkCore/ISUCore.EntityFrameworkCore.csproj --configuration Debug --verbose

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
EXPOSE 5000
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "/app/ISUCore.Web.Host.dll"]

