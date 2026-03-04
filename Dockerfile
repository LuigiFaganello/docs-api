FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DocsApi.sln .
COPY src/DocsApi.Domain/DocsApi.Domain.csproj src/DocsApi.Domain/
COPY src/DocsApi.Application/DocsApi.Application.csproj src/DocsApi.Application/
COPY src/DocsApi.Infrastructure/DocsApi.Infrastructure.csproj src/DocsApi.Infrastructure/
COPY src/DocsApi.WebApi/DocsApi.WebApi.csproj src/DocsApi.WebApi/
RUN dotnet restore src/DocsApi.WebApi/DocsApi.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/DocsApi.WebApi/DocsApi.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Copy default services.yml as fallback (overridable via volume mount)
COPY services.yml .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV SERVICES_FILE=/app/services.yml
EXPOSE 8080

ENTRYPOINT ["dotnet", "DocsApi.WebApi.dll"]
