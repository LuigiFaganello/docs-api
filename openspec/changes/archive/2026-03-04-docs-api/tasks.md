## 1. Scaffolding da solução

- [x] 1.1 Criar solução `DocsApi.sln` com os projetos: `DocsApi.Domain`, `DocsApi.Application`, `DocsApi.Infrastructure`, `DocsApi.WebApi`
- [x] 1.2 Criar projetos de teste: `DocsApi.Unit`, `DocsApi.Integration`
- [x] 1.3 Adicionar referências entre projetos conforme Clean Architecture (WebApi → Application + Infrastructure → Domain)
- [x] 1.4 Adicionar dependências NuGet: `YamlDotNet` (Infrastructure), `Scalar.AspNetCore` (WebApi), `Microsoft.AspNetCore.OpenApi` (WebApi)
- [x] 1.5 Criar arquivo `services.yml` de exemplo na raiz com 2-3 serviços fictícios

## 2. Domain

- [x] 2.1 Criar entidade `ServiceDefinition` com propriedades: `Id`, `Name`, `SpecUrl`, `Description`, `Auth`, `Headers`, `TtlSeconds`, `Insecure`
- [x] 2.2 Criar entidade `CachedSpec` com propriedades: `RawJson`, `CachedAt` e método `IsStale(int ttlSeconds)`
- [x] 2.3 Criar record `ServiceAuth` com `Type` (Basic), `Username`, `Password`
- [x] 2.4 Criar interface `IServiceRegistry` com métodos: `GetAll()`, `GetById(string id)`
- [x] 2.5 Criar interface `ISpecFetcher` com método: `FetchAsync(ServiceDefinition service)`
- [x] 2.6 Criar interface `ISpecCache` com métodos: `TryGet(string id)`, `Set(string id, CachedSpec spec)`, `Remove(string id)`, `Clear()`

## 3. Application (casos de uso)

- [x] 3.1 Criar `GetServiceListUseCase` — retorna lista de `ServiceSummaryDto` (sem credenciais)
- [x] 3.2 Criar `GetServiceSpecUseCase` — orquestra cache → fetch → cache stale com fallback
- [x] 3.3 Criar `RefreshServiceSpecUseCase` — invalida cache de um serviço específico
- [x] 3.4 Criar `ClearCacheUseCase` — invalida todos os caches
- [x] 3.5 Criar DTO `ServiceSummaryDto` com `Id`, `Name`, `Description`

## 4. Infrastructure

- [x] 4.1 Implementar `YamlServiceRegistry`: lê e deserializa `services.yml` via `YamlDotNet`
- [x] 4.2 Validar config no carregamento: `id` único, `specUrl` não vazia, falhar com mensagem clara
- [x] 4.3 Implementar `HttpSpecFetcher`: `HttpClient` com suporte a Basic Auth, headers customizados e flag `Insecure` (bypass TLS)
- [x] 4.4 Implementar conversão YAML → JSON no fetcher para specs retornadas em formato YAML
- [x] 4.5 Configurar timeout por requisição (padrão: 10 segundos, configurável)
- [x] 4.6 Implementar `InMemorySpecCache` com `ConcurrentDictionary<string, CachedSpec>` e TTL por serviço

## 5. WebApi

- [x] 5.1 Configurar `Program.cs`: DI dos use cases e infraestrutura, `AddOpenApi()`, `MapOpenApi()`, `MapScalarApiReference()`
- [x] 5.2 Implementar `ServicesController`: `GET /api/services` e `GET /api/services/{id}/spec`
- [x] 5.3 Implementar `CacheController`: `POST /api/services/{id}/refresh` e `POST /api/cache/clear`
- [x] 5.4 Implementar `GET /health` como minimal API endpoint
- [x] 5.5 Configurar CORS via `appsettings.json` (origens permitidas)
- [x] 5.6 ~~Configurar rota `GET /docs/{id}` com HTML customizado e CDN~~ (substituído pela task 8.1)
- [x] 5.7 Configurar leitura do path do `services.yml` via `appsettings.json` com fallback para variável de ambiente `SERVICES_FILE`
- [x] 5.8 Retornar header `X-Cache: HIT | MISS | STALE` nas respostas de spec

## 6. Testes

- [x] 6.1 Testes unitários de `InMemorySpecCache`: hit, miss, expiração de TTL
- [x] 6.2 Testes unitários de `GetServiceSpecUseCase`: fluxo cache hit, cache miss, fallback stale
- [x] 6.3 Testes unitários de `YamlServiceRegistry`: config válida, id duplicado, yaml inválido
- [x] 6.4 Testes de integração: `GET /api/services`, `GET /api/services/{id}/spec`, `GET /health`

## 7. Entrega

- [x] 7.1 Criar `Dockerfile` multi-stage (build + runtime) para .NET 10
- [x] 7.2 Atualizar `README.md` com instruções de configuração do `services.yml`, variáveis de ambiente e execução

## 9. Suporte a Docker

- [x] 9.1 Criar `.dockerignore` excluindo `bin/`, `obj/`, `tests/`, `.git/`, `.vscode/` do build context
- [x] 9.2 Criar `docker-compose.yml` com serviço `docs-api`, porta `8080:8080` e volume `./services.yml:/app/services.yml`
- [x] 9.3 Validar que o `Dockerfile` copia o `services.yml` padrão como fallback e define `SERVICES_FILE=/app/services.yml`
- [x] 9.4 Atualizar `README.md` com seção de uso via Docker e docker-compose

## 8. Correção do Docs Viewer

- [x] 8.1 Reconfigurar `MapScalarApiReference` em `Program.cs` para registrar todos os serviços do registry como documentos Scalar (`AddDocument` com `Title` e `Url = /api/services/{id}/spec`)
- [x] 8.2 Remover a rota `/docs/{id}` com HTML customizado e CDN externo
- [x] 8.3 Garantir que `GET /` redireciona para `/scalar/v1`
- [x] 8.4 Atualizar testes de integração para cobrir o novo comportamento do viewer
