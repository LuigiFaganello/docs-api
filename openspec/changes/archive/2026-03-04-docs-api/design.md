## Context

O `docs-api` é um projeto novo em C# .NET 10. O sistema agrega documentação OpenAPI de múltiplos serviços, lendo configurações de um arquivo `.yml`, buscando specs dinamicamente via HTTP, e servindo uma interface de navegação unificada com Scalar. Não há banco de dados — toda persistência é em arquivo.

## Goals / Non-Goals

**Goals:**
- Agregar specs OpenAPI de múltiplos serviços via URL
- Servir interface web com Scalar para navegar entre documentações
- API REST para listar serviços e obter specs
- Suporte a autenticação básica e headers customizados por serviço
- Cache em memória com TTL configurável
- Arquitetura limpa e testável (Clean Architecture)

**Non-Goals:**
- Edição ou modificação dos specs (read-only)
- Persistência em banco de dados
- Multi-tenant ou controle de acesso por usuário
- Sincronização em tempo real
- Suporte a specs fora do padrão OpenAPI/Swagger (AsyncAPI, GraphQL, etc.)

## Decisions

### 1. Runtime: C# .NET 10 com ASP.NET Core

**Escolha**: ASP.NET Core (.NET 10) como plataforma.

**Rationale**: Performance nativa, ecossistema maduro para APIs HTTP, suporte de primeira classe a OpenAPI via `Microsoft.AspNetCore.OpenApi`, e integração direta com `Scalar.AspNetCore`. .NET 10 é a versão atual com suporte LTS.

### 2. Arquitetura: Clean Architecture

**Estrutura de projetos**:

```
DocsApi.sln
├── src/
│   ├── DocsApi.Domain/          # Entidades e interfaces (sem dependências externas)
│   │   ├── Entities/
│   │   │   ├── ServiceDefinition.cs   # Id, Name, SpecUrl, Auth, Headers, Ttl, Insecure
│   │   │   └── CachedSpec.cs          # RawJson, CachedAt, IsStale()
│   │   └── Interfaces/
│   │       ├── IServiceRegistry.cs
│   │       ├── ISpecFetcher.cs
│   │       └── ISpecCache.cs
│   │
│   ├── DocsApi.Application/     # Casos de uso (depende só de Domain)
│   │   ├── UseCases/
│   │   │   ├── GetServiceListUseCase.cs
│   │   │   ├── GetServiceSpecUseCase.cs
│   │   │   ├── RefreshServiceSpecUseCase.cs
│   │   │   └── ClearCacheUseCase.cs
│   │   └── DTOs/
│   │       └── ServiceSummaryDto.cs
│   │
│   ├── DocsApi.Infrastructure/  # Implementações concretas (depende de Domain + Application)
│   │   ├── Registry/
│   │   │   └── YamlServiceRegistry.cs   # Lê services.yml via YamlDotNet
│   │   ├── Fetcher/
│   │   │   └── HttpSpecFetcher.cs        # HttpClient + conversão YAML→JSON
│   │   └── Cache/
│   │       └── InMemorySpecCache.cs      # ConcurrentDictionary + TTL
│   │
│   └── DocsApi.WebApi/          # Entry point: ASP.NET Core + Scalar (depende de Application + Infrastructure)
│       ├── Controllers/
│       │   ├── ServicesController.cs
│       │   └── CacheController.cs
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── DocsApi.Unit/
    └── DocsApi.Integration/
```

**Fluxo de dependências** (Clean Architecture):
```
WebApi ──► Application ──► Domain
  │                           ▲
  └──► Infrastructure ────────┘
```

### 3. Visualização: Scalar multi-documento via `Scalar.AspNetCore`

**Escolha**: `Scalar.AspNetCore` com suporte a múltiplos documentos como interface unificada.

**Rationale**: Scalar suporta nativamente múltiplos documentos no sidebar — exatamente o modelo de "hub de documentações". Não é necessário construir frontend customizado. Cada serviço do registry vira um documento Scalar apontando para `/api/services/{id}/spec`. A navegação entre serviços é feita pelo sidebar nativo do Scalar, com assets embutidos no pacote (sem CDN).

**Setup correto em `Program.cs`**:
```csharp
var registry = app.Services.GetRequiredService<IServiceRegistry>();

app.MapScalarApiReference(options =>
{
    options.WithTitle("docs-api — Centralização de Documentações");
    foreach (var svc in registry.GetAll())
        options.AddDocument(svc.Name, new ScalarDocument
        {
            Title = svc.Name,
            Url = $"/api/services/{svc.Id}/spec"
        });
});
```

**Comportamento**: `GET /` redireciona para `/scalar/v1`. O sidebar do Scalar lista todos os serviços registrados. Ao selecionar um serviço, o Scalar busca o spec via proxy interno `/api/services/{id}/spec` — sem acesso direto à URL de origem.

**O que NÃO fazer**: rotas `/docs/{id}` com HTML customizado e CDN externo — isso fragmenta a experiência e quebra o requisito de funcionar offline.

### 4. Registro de serviços: arquivo `services.yml`

**Escolha**: Arquivo `services.yml` na raiz (path configurável via `appsettings.json` ou variável de ambiente).

**Rationale**: Simples, versionável no Git, sem necessidade de banco de dados. O `YamlDotNet` é a biblioteca padrão .NET para parsing de YAML.

**Exemplo de `services.yml`**:
```yaml
services:
  - id: orders-api
    name: Orders API
    specUrl: http://orders-service/openapi/v1.json
    ttl: 300

  - id: inventory-api
    name: Inventory API
    specUrl: http://inventory-service/swagger/v1/swagger.json
    auth:
      type: basic
      username: docs
      password: secret
```

### 5. Deploy: Docker com volume para `services.yml`

**Escolha**: Imagem Docker multi-stage (build + runtime) com `services.yml` montado como volume externo.

**Rationale**: Docker é o modelo de deploy padrão para ferramentas internas. O `services.yml` deve ser montado como volume — não copiado para dentro da imagem — para que a lista de serviços possa ser atualizada sem rebuild da imagem.

**Estrutura de arquivos Docker:**
```
Dockerfile          # multi-stage: sdk:10.0 (build) → aspnet:10.0 (runtime)
.dockerignore       # exclui obj/, bin/, tests/, .git/ do build context
docker-compose.yml  # orquestração local com volume e porta configurados
```

**Uso esperado:**
```bash
# Build e execução com docker-compose
docker-compose up --build

# Ou diretamente
docker build -t docs-api .
docker run -p 8080:8080 -v $(pwd)/services.yml:/app/services.yml docs-api
```

**Porta**: `8080` no container, mapeável para qualquer porta no host via `docker-compose.yml`.

**Variáveis de ambiente suportadas no container:**
- `SERVICES_FILE` — path do `services.yml` dentro do container (padrão: `/app/services.yml`)
- `ASPNETCORE_HTTP_PORTS` — porta HTTP (padrão: `8080`)

### 6. Cache: `ConcurrentDictionary` in-memory com TTL

**Escolha**: Cache próprio usando `ConcurrentDictionary<string, CachedSpec>` na camada Infrastructure.

**Rationale**: Thread-safe, zero dependências externas, suficiente para o volume esperado. `IMemoryCache` do ASP.NET Core é uma alternativa aceitável, mas o cache próprio mantém melhor separação entre camadas.

## Risks / Trade-offs

| Risco | Mitigação |
|---|---|
| Serviço externo fora do ar ao buscar spec | Retornar spec stale do cache com header `X-Cache: STALE`; erro 502 apenas se não há cache |
| Spec YAML remota com formato inválido | Validar parsing antes de cachear; retornar 502 com mensagem detalhada |
| `services.yml` inválido na inicialização | Validar com `DataAnnotations` ou `FluentValidation` no startup; falhar com mensagem clara |
| Reload de config concorrente com requisições ativas | Usar `ReaderWriterLockSlim` ou substituição atômica do registry em memória |

## Open Questions

- O Scalar será configurado para exibir as specs dos serviços remotos diretamente, ou sempre através do proxy `/api/services/{id}/spec`? (Recomendação: sempre via proxy, para controle de cache e auth)
- Precisamos de hot-reload do `services.yml` sem reiniciar o processo?
- Há requisito de deploy via Docker ou publicação como imagem?
