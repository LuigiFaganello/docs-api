# docs-api

Centraliza a visualização de documentação Swagger/OpenAPI de múltiplos serviços em um único lugar.

## Como funciona

Configure as URLs dos seus serviços no arquivo `services.yml`. O `docs-api` busca os specs via HTTP, faz cache em memória e serve a documentação através do [Scalar](https://scalar.com) — uma interface unificada com todos os serviços no sidebar.

## Pré-requisitos

- .NET 10 SDK (para desenvolvimento)
- Docker (para execução em container)

## Configuração

### 1. `services.yml`

Crie um arquivo `services.yml` na raiz do projeto (ou no diretório de execução):

```yaml
services:
  - id: orders-api
    name: Orders API
    description: Serviço de pedidos
    specUrl: http://orders-service/openapi/v1.json
    ttl: 300                    # cache em segundos (padrão: 300)

  - id: inventory-api
    name: Inventory API
    specUrl: http://inventory-service/swagger/v1/swagger.json
    auth:
      type: basic
      username: docs-reader
      password: changeme

  - id: legacy-service
    name: Legacy Service
    specUrl: https://legacy-service/api-docs
    insecure: true              # ignora certificado TLS inválido
    headers:
      X-Internal-Token: my-token
```

**Campos por serviço:**

| Campo | Obrigatório | Descrição |
|---|---|---|
| `id` | Sim | Identificador único (kebab-case recomendado) |
| `name` | Sim | Nome legível exibido na interface |
| `specUrl` | Sim | URL do JSON/YAML do spec OpenAPI |
| `description` | Não | Descrição curta exibida na listagem |
| `ttl` | Não | Tempo de cache em segundos (padrão: 300) |
| `auth.type` | Não | Tipo de autenticação: `basic` |
| `auth.username` | Não | Usuário para Basic Auth |
| `auth.password` | Não | Senha para Basic Auth |
| `insecure` | Não | `true` para ignorar certificado TLS (dev only) |
| `headers` | Não | Headers customizados como mapa chave-valor |

### 2. `appsettings.json` (opcional)

```json
{
  "ServicesFile": "services.yml",
  "FetchTimeoutSeconds": 10,
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  }
}
```

### 3. Variáveis de ambiente

| Variável | Descrição |
|---|---|
| `SERVICES_FILE` | Path do `services.yml` (sobrepõe `appsettings.json`) |
| `ASPNETCORE_HTTP_PORTS` | Porta HTTP (padrão: 5000 em dev, 8080 no Docker) |

## Execução local

```bash
dotnet run --project src/DocsApi.WebApi
```

Acesse `http://localhost:5000` — o Scalar abrirá com todos os serviços configurados no sidebar.

## Docker

### docker-compose (recomendado)

```bash
# Subir (build + run)
docker-compose up --build

# Em background
docker-compose up -d --build
```

Acesse `http://localhost:8080`.

O `services.yml` da raiz do projeto é montado automaticamente como volume — edite o arquivo e reinicie o container para aplicar mudanças.

### Docker direto

```bash
# Build
docker build -t docs-api .

# Run montando seu services.yml
docker run -p 8080:8080 -v $(pwd)/services.yml:/app/services.yml docs-api
```

## Endpoints

| Endpoint | Descrição |
|---|---|
| `GET /` | Redireciona para `/docs` |
| `GET /docs` | Interface Scalar com todos os serviços no sidebar |
| `GET /api/services` | Lista todos os serviços registrados (JSON) |
| `GET /api/services/{id}/spec` | Retorna o spec OpenAPI do serviço (JSON) |
| `POST /api/services/{id}/refresh` | Invalida o cache do serviço |
| `POST /api/cache/clear` | Invalida todo o cache |
| `GET /health` | Health check |

O header `X-Cache` nas respostas de `/spec` indica: `HIT`, `MISS` ou `STALE`.

## Testes

```bash
dotnet test
```
