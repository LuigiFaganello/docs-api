## Requirements

### Requirement: Busca de spec remota por URL
O sistema SHALL buscar o spec OpenAPI de um serviço exclusivamente via HTTP/HTTPS a partir da `specUrl` configurada. O sistema MUST NOT armazenar specs em disco — specs são obtidas da URL de origem e mantidas apenas em cache de memória. O sistema MUST suportar specs nos formatos JSON e YAML, convertendo YAML para JSON antes de retornar ao cliente.

#### Scenario: Spec JSON remota
- **WHEN** a `specUrl` retorna um documento JSON válido com OpenAPI 2.x ou 3.x
- **THEN** o sistema SHALL retornar o spec como JSON

#### Scenario: Spec YAML remota
- **WHEN** a `specUrl` retorna um documento YAML válido com OpenAPI 2.x ou 3.x
- **THEN** o sistema SHALL converter para JSON e retornar ao cliente

#### Scenario: Serviço indisponível
- **WHEN** a `specUrl` não responde dentro do timeout configurado
- **THEN** o sistema SHALL retornar erro 502 com mensagem indicando o serviço afetado

### Requirement: Autenticação Basic Auth na busca
Quando um serviço tiver credenciais de Basic Auth configuradas, o sistema SHALL incluir o header `Authorization: Basic <base64>` em todas as requisições HTTP feitas para a `specUrl` desse serviço. Credenciais MUST NOT ser expostas em logs, respostas de API ou qualquer saída do sistema.

#### Scenario: Fetch com Basic Auth
- **WHEN** o serviço tem `auth.type: basic` com `username` e `password` configurados
- **THEN** o sistema SHALL enviar o header `Authorization: Basic <credenciais>` na requisição para a `specUrl`

#### Scenario: URL protegida sem credenciais configuradas
- **WHEN** a `specUrl` retorna HTTP 401 e o serviço não tem `auth` configurado
- **THEN** o sistema SHALL retornar erro 502 com mensagem indicando falha de autenticação na origem

#### Scenario: Credenciais inválidas
- **WHEN** a `specUrl` retorna HTTP 401 mesmo com `auth` configurado
- **THEN** o sistema SHALL retornar erro 502 com mensagem indicando credenciais rejeitadas pela origem, sem expor as credenciais na resposta

### Requirement: Cache com TTL
O sistema SHALL armazenar em cache o spec de cada serviço após a primeira busca bem-sucedida. O TTL padrão SHALL ser de 5 minutos, configurável globalmente ou por serviço no `services.yaml`.

#### Scenario: Cache hit
- **WHEN** um spec é solicitado e existe no cache dentro do TTL
- **THEN** o sistema SHALL retornar o spec em cache sem realizar nova requisição HTTP

#### Scenario: Cache expirado
- **WHEN** o TTL do spec em cache foi excedido
- **THEN** o sistema SHALL buscar novamente a spec remota e atualizar o cache

#### Scenario: Falha na atualização com cache stale
- **WHEN** o cache expirou e a requisição de atualização falha
- **THEN** o sistema SHALL retornar o spec em cache (stale) com header `X-Cache: STALE` e logar o erro

### Requirement: Invalidação manual de cache
O sistema SHALL prover mecanismo para invalidar o cache de um serviço específico ou de todos os serviços.

#### Scenario: Invalidação de serviço específico
- **WHEN** o endpoint `POST /api/services/:id/refresh` é chamado
- **THEN** o sistema SHALL remover o cache do serviço e buscar nova spec na próxima requisição

#### Scenario: Invalidação global
- **WHEN** o endpoint `POST /api/cache/clear` é chamado
- **THEN** o sistema SHALL remover todos os itens em cache

### Requirement: Suporte a TLS não-verificado
O sistema SHALL suportar a flag `insecure: true` por serviço para desabilitar verificação de certificado TLS, destinada a ambientes de desenvolvimento com certificados autoassinados.

#### Scenario: Serviço com certificado autoassinado
- **WHEN** um serviço tem `insecure: true` e a URL usa HTTPS com certificado autoassinado
- **THEN** o sistema SHALL buscar o spec sem verificar o certificado
