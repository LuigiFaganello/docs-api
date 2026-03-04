## Why

Em arquiteturas de microsserviços e projetos com múltiplos backends, a documentação de API (Swagger/OpenAPI) fica dispersa em repositórios e URLs distintas, dificultando a descoberta, o consumo e a manutenção das APIs. A ideia é centralizar a visualização de toda a documentação em um único lugar, eliminando o atrito de saber onde cada documentação está hospedada.

## What Changes

- Novo projeto `docs-api` que agrega e exibe documentação Swagger/OpenAPI de múltiplos serviços
- Interface web unificada para navegar entre documentações de diferentes serviços
- Registro de serviços via configuração (URL da spec OpenAPI de cada serviço)
- Proxy de specs: busca e cacheia os specs remotos para exibição
- Suporte a múltiplos formatos de spec (JSON e YAML)
- Autenticação opcional por serviço (para specs protegidas)

## Capabilities

### New Capabilities

- `service-registry`: Cadastro e gerenciamento de serviços e suas URLs de spec OpenAPI/Swagger. Suporta configuração via arquivo e via API REST.
- `spec-fetcher`: Busca, valida e cacheia specs OpenAPI remotas. Lida com formatos JSON e YAML, autenticação básica e headers customizados.
- `docs-viewer`: Interface web (Swagger UI / Redoc) que exibe a documentação de um serviço selecionado, com navegação entre serviços registrados.
- `api-gateway`: API REST que expõe os endpoints do sistema: listar serviços, obter spec de um serviço, e gerenciar o registro.

### Modified Capabilities

<!-- Nenhuma — projeto novo sem specs existentes -->

## Impact

- **Novo projeto**: aplicação standalone (Node.js/TypeScript)
- **Dependências externas**: Swagger UI ou Redoc para renderização; biblioteca de parsing de YAML/JSON para specs
- **Infraestrutura**: requer acesso de rede aos serviços registrados para buscar as specs
- **Consumidores**: desenvolvedores frontend, backend e QA que precisam consultar APIs
