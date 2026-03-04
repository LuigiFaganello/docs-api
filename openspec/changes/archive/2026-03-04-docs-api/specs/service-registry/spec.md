## ADDED Requirements

### Requirement: Definição de serviço
O sistema SHALL aceitar a configuração de serviços via arquivo YAML. Cada serviço MUST conter um identificador único (`id`), nome legível (`name`), e URL do spec OpenAPI (`specUrl`). Os campos `description`, `auth` e `headers` são opcionais.

#### Scenario: Serviço mínimo válido
- **WHEN** o arquivo `services.yml` contém um serviço com `id`, `name` e `specUrl`
- **THEN** o sistema SHALL carregar o serviço sem erros

#### Scenario: Serviço com autenticação Basic
- **WHEN** o arquivo contém um serviço com `auth.type: basic` e credenciais `username`/`password`
- **THEN** o sistema SHALL usar essas credenciais ao buscar o spec desse serviço

#### Scenario: Serviço com headers customizados
- **WHEN** o arquivo contém um serviço com `headers` como mapa de chave-valor
- **THEN** o sistema SHALL incluir esses headers em cada requisição de fetch do spec

### Requirement: Carregamento na inicialização
O sistema SHALL carregar e validar o arquivo de configuração durante a inicialização. Se o arquivo não existir ou for inválido, o sistema MUST falhar com mensagem de erro clara antes de aceitar requisições.

#### Scenario: Arquivo ausente
- **WHEN** o arquivo `services.yml` não existe no path configurado
- **THEN** o sistema SHALL encerrar com exit code não-zero e mensagem indicando o arquivo esperado

#### Scenario: YAML inválido
- **WHEN** o arquivo `services.yml` contém YAML malformado
- **THEN** o sistema SHALL encerrar com exit code não-zero e mensagem indicando a linha do erro

#### Scenario: Serviço com `id` duplicado
- **WHEN** dois serviços no arquivo compartilham o mesmo `id`
- **THEN** o sistema SHALL encerrar com mensagem listando o `id` duplicado

### Requirement: Reload de configuração
O sistema SHALL suportar recarregamento da configuração sem reinicialização via sinal SIGHUP ou endpoint dedicado.

#### Scenario: Reload via SIGHUP
- **WHEN** o processo recebe o sinal SIGHUP
- **THEN** o sistema SHALL recarregar o arquivo de configuração e atualizar a lista de serviços sem derrubar conexões ativas

#### Scenario: Reload com config inválida
- **WHEN** o reload é disparado mas o arquivo contém erros
- **THEN** o sistema SHALL manter a configuração anterior e logar o erro, sem interromper o serviço
