# Registro de prompts utilizados

Este arquivo registra, de forma resumida, os usos de inteligência artificial no desenvolvimento e na revisão do AJA. Nenhum prompt contém senha, token, chave privada, dado pessoal real ou raciocínio interno.

## Concepção da solução

- Estruturar uma CitizenTech para registro e acompanhamento comunitário de barreiras de acessibilidade urbana.
- Definir uma jornada simples: cidadão registra demanda, comunidade apoia e comenta, e Admin classifica e atualiza a situação com justificativa.
- Relacionar a proposta aos ODS 10 e 11 e definir indicadores mensuráveis de participação e acompanhamento.

## Backend 

- Projetar uma Web API em C#/.NET organizada nos módulos Auth, Demandas e Participações.
- Separar API, Application, Domain e Infrastructure, usando interfaces e injeção de dependência.
- Aplicar POO com entidades que encapsulam regras de edição, situação, apoio, remoção e moderação.
- Implementar EF Core com SQLite, Identity, LINQ, migrações e persistência relacional.
- Implementar API REST versionada, programação assíncrona, paginação e erros no formato Problem Details.

## Segurança

- Implementar autenticação JWT com validação de assinatura, emissor, audiência, algoritmo e expiração.
- Garantir que cadastro público crie somente User e que operações administrativas exijam a role Admin.
- Validar autoria para edição e remoção de demandas e comentários.
- Criar conta Admin pública apenas no ambiente Development e gerar a chave JWT local em memória.
- Auditar os arquivos candidatos ao Git para impedir o envio de bancos, backups, tokens e chaves privadas.

## Documentação e testes

- Documentar endpoints, modelos e autenticação com Swagger/OpenAPI.
- Criar testes unitários para as regras de domínio.
- Criar testes de integração para HTTP, JWT, autorização, persistência, migrações e OpenAPI.
- Revisar os READMEs para que cada disciplina possua instruções completas e independentes de execução, arquitetura, endpoints, testes, ODS e critérios de avaliação.
