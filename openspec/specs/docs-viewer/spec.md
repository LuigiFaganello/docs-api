## Requirements

### Requirement: Interface unificada via Scalar multi-documento
O sistema SHALL configurar o Scalar como interface de navegação unificada, registrando todos os serviços do registry como documentos do Scalar no startup. O Scalar SHALL ser a única interface de visualização — não há frontend customizado. A navegação entre serviços é feita pelo sidebar nativo do Scalar.

#### Scenario: Acesso à raiz
- **WHEN** o usuário acessa `GET /`
- **THEN** o sistema SHALL redirecionar para a interface Scalar com todos os serviços disponíveis no sidebar

#### Scenario: Serviços exibidos no sidebar
- **WHEN** o usuário abre a interface Scalar
- **THEN** o sidebar SHALL listar todos os serviços registrados no `services.yml`, cada um como um documento selecionável

#### Scenario: Seleção de serviço
- **WHEN** o usuário seleciona um serviço no sidebar do Scalar
- **THEN** o Scalar SHALL renderizar a documentação completa desse serviço, buscando o spec via `/api/services/{id}/spec`

#### Scenario: Assets offline
- **WHEN** o servidor não tem acesso à internet
- **THEN** o sistema SHALL servir a interface Scalar normalmente a partir dos assets do pacote `Scalar.AspNetCore`, sem dependência de CDN

### Requirement: Documentos registrados dinamicamente no startup
O sistema SHALL registrar os documentos no Scalar a partir do `IServiceRegistry` durante o startup da aplicação. Cada `ServiceDefinition` no registry SHALL corresponder a um documento Scalar com `Title = service.Name` e `Url = /api/services/{id}/spec`.

#### Scenario: Novo serviço adicionado ao services.yml
- **WHEN** um novo serviço é adicionado ao `services.yml` e a aplicação é reiniciada
- **THEN** o serviço SHALL aparecer automaticamente no sidebar do Scalar sem alteração de código

#### Scenario: Sem serviços configurados
- **WHEN** o `services.yml` não contém nenhum serviço
- **THEN** o sistema SHALL falhar no startup com mensagem indicando que ao menos um serviço é necessário
