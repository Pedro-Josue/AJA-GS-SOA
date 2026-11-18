# Registro de prompts de inteligência artificial

Este arquivo reúne os prompts consolidados usados com inteligência artificial como copiloto para aprender, estruturar e revisar o projeto AJA. A equipe avaliou as sugestões antes de aplicá-las e permaneceu responsável pelas decisões, pela implementação e pelos testes. Os textos foram organizados para facilitar a explicação durante a avaliação e não contêm dados pessoais reais, senhas privadas, tokens ou chaves de produção.

## 1. Delimitação da CitizenTech

**Prompt**

> Atue como orientador de produto e ajude a delimitar uma CitizenTech acadêmica chamada AJA - Acessibilidade, Justiça e Ação. A solução deve apoiar cidadãos no registro e acompanhamento de barreiras de acessibilidade urbana em São Paulo, usando apenas dados simulados. Proponha um problema em uma frase, público beneficiado, jornada principal, indicadores de impacto e relação com os ODS 10 e 11. Mantenha o escopo simples e adequado a uma camada de backend orientada a serviços.

**Finalidade e aplicação no projeto:** o prompt ajudou a consolidar a jornada de cadastro, registro de demanda, apoio, comentário e acompanhamento administrativo. A equipe revisou o escopo para manter somente funcionalidades ligadas ao problema urbano proposto.

## 2. Decomposição dos serviços e do domínio

**Prompt**

> Para o backend da CitizenTech AJA, proponha uma divisão de responsabilidades entre os serviços Auth, Demandas e Participacoes. Descreva os contratos de cada serviço, as entidades do domínio e os limites entre API, Application, Domain e Infrastructure. Considere um monólito modular simples de executar, mas evite dependências diretas entre controllers, banco de dados e regras de negócio.

**Finalidade e aplicação no projeto:** a resposta foi usada para separar autenticação, gestão de demandas e participação comunitária. A equipe manteve contratos explícitos e infraestrutura compartilhada sem transformar o projeto em microsserviços desnecessários.

## 3. Persistência relacional e migrações

**Prompt**

> Analise um modelo relacional para demandas de acessibilidade com usuários, demandas, apoios, comentários e histórico de situação. Sugira chaves primárias e estrangeiras, índices, restrições de unicidade, remoção lógica e comportamento de exclusão. Explique como versionar o esquema com migrações do Entity Framework Core e como provar que as migrações funcionam em um banco SQLite vazio.

**Finalidade e aplicação no projeto:** o prompt apoiou a configuração das relações, do apoio único por usuário e demanda, dos índices de consulta e da cadeia de migrações. A equipe validou a criação e a atualização do banco em testes automatizados.

## 4. Contrato da API REST

**Prompt**

> Proponha um contrato REST versionado em `/api/v1` para cadastro, login, consulta do perfil, criação e consulta de demandas, atualização administrativa, apoio e comentários. Indique método HTTP, rota, autorização e códigos de resposta. Use DTOs em vez de expor entidades e adote Problem Details para erros. Inclua paginação, filtros combináveis e operações idempotentes quando fizer sentido.

**Finalidade e aplicação no projeto:** as sugestões foram comparadas com a jornada funcional e usadas para revisar as rotas, os DTOs, a paginação e os códigos HTTP. A equipe manteve apoio e retirada de apoio idempotentes e padronizou os erros conhecidos.

## 5. Documentação automática dos endpoints

**Prompt**

> Revise a documentação OpenAPI de uma ASP.NET Core Web API. Verifique se todos os endpoints da AJA aparecem no Swagger, se o esquema Bearer JWT está configurado, se parâmetros, corpos, respostas e códigos HTTP são compreensíveis e se um professor consegue executar a jornada principal pelo navegador. Sugira descrições curtas e objetivas, sem inventar operações que não existem.

**Finalidade e aplicação no projeto:** o prompt orientou a revisão do Swagger e das descrições das operações. A equipe confirmou que as operações funcionais estavam presentes no documento OpenAPI e documentou no README como autenticar e testar a API.

## 6. Testes automatizados dos serviços

**Prompt**

> Elabore uma matriz de testes automatizados para o backend orientado a serviços da AJA. Inclua testes do domínio, chamadas HTTP reais, autenticação JWT, autorização por role e autoria, persistência SQLite, filtros, paginação, migrações e OpenAPI. Para cada cenário, explique o risco coberto e o resultado esperado, incluindo casos `401`, `403`, `404` e `409`.

**Finalidade e aplicação no projeto:** os cenários foram usados como referência para testes xUnit unitários e de integração. A equipe validou tanto as regras internas quanto os contratos HTTP, o banco relacional e a documentação automática.

## 7. Revisão da entrega SOA

**Prompt**

> Atue como revisor técnico da entrega de Arquitetura Orientada a Serviços e Web Services da CitizenTech AJA. Verifique se o repositório contém código completo, domínio organizado, conexão com banco, migrações, API REST, testes automatizados, Swagger, instruções de execução, integrantes e evidências da aplicação. Aponte inconsistências entre documentação e código e recomende apenas alterações que possam ser comprovadas no repositório.

**Finalidade e aplicação no projeto:** a revisão final ajudou a alinhar o README ao backend realmente implementado, conferir comandos de execução, documentar os serviços e organizar as evidências. A equipe executou build e testes antes da entrega e manteve apenas dados simulados no repositório.
