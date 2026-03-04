## ADDED Requirements

### Requirement: Listar serviços registrados
O sistema SHALL expor um endpoint que retorna a lista de todos os serviços registrados com seus metadados públicos (sem credenciais).

#### Scenario: Listagem com serviços cadastrados
- **WHEN** `GET /api/services` é chamado e há serviços configurados
- **THEN** o sistema SHALL retornar array JSON com `id`, `name` e `description` de cada serviço, com status 200

#### Scenario: Listagem sem serviços
- **WHEN** `GET /api/services` é chamado e não há serviços configurados
- **THEN** o sistema SHALL retornar array JSON vazio `[]` com status 200

### Requirement: Obter spec de um serviço
O sistema SHALL expor um endpoint que retorna o spec OpenAPI de um serviço específico, buscando do cache ou da origem.

#### Scenario: Spec encontrada
- **WHEN** `GET /api/services/:id/spec` é chamado para um serviço existente
- **THEN** o sistema SHALL retornar o spec em JSON com status 200 e `Content-Type: application/json`

#### Scenario: Serviço não encontrado
- **WHEN** `GET /api/services/:id/spec` é chamado para um `id` inexistente
- **THEN** o sistema SHALL retornar status 404 com body `{ "error": "Service not found" }`

#### Scenario: Erro ao buscar spec
- **WHEN** `GET /api/services/:id/spec` é chamado mas a origem está indisponível e não há cache
- **THEN** o sistema SHALL retornar status 502 com body `{ "error": "Failed to fetch spec", "service": "<id>" }`

### Requirement: Health check
O sistema SHALL expor um endpoint de health check para monitoramento e readiness probe.

#### Scenario: Servidor saudável
- **WHEN** `GET /health` é chamado e o servidor está operacional
- **THEN** o sistema SHALL retornar status 200 com body `{ "status": "ok" }`

### Requirement: CORS configurável
O sistema SHALL suportar configuração de CORS para permitir que frontends externos consumam a API.

#### Scenario: Requisição cross-origin
- **WHEN** uma requisição com header `Origin` é recebida e CORS está habilitado
- **THEN** o sistema SHALL responder com os headers `Access-Control-Allow-Origin` apropriados conforme configuração

### Requirement: Invalidação de cache via API
O sistema SHALL expor endpoints para invalidação de cache de specs.

#### Scenario: Refresh de serviço específico
- **WHEN** `POST /api/services/:id/refresh` é chamado para um serviço existente
- **THEN** o sistema SHALL invalidar o cache desse serviço e retornar status 200

#### Scenario: Limpeza total de cache
- **WHEN** `POST /api/cache/clear` é chamado
- **THEN** o sistema SHALL invalidar todos os caches e retornar status 200
