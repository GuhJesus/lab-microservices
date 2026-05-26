# Lab Microservices (.NET + Docker)

Laboratorio para estudar uma arquitetura simples de microsservicos com `.NET 8`, `Docker`, `RabbitMQ`, `Redis`, `SQL Server`, `YARP` e `OpenTelemetry`.

## Uso como base versionada

Este repositorio esta preparado para ser a base de novos laboratorios ou projetos derivados.

- Repositorio base: mantenha esta estrutura limpa e estavel.
- Variacoes pequenas: crie `branches`.
- Variacoes por cliente ou produto: crie um novo repositorio a partir desta base.
- Publicacao em nuvem: suba a base para um remoto privado no `GitHub`, `GitLab` ou `Azure DevOps`.

Guia rapido de versionamento e template: [docs/runbooks/repository-template.md](C:/Aplicativo.NET/Projetos/lab-microservices/docs/runbooks/repository-template.md)

## O que existe neste lab

### Projetos da solution

- `src/orders-api`
  API principal do laboratorio. Recebe pedidos, grava no `SQL Server`, alimenta cache no `Redis` e publica o evento `OrderCreated` no `RabbitMQ`.

- `src/gateway-api`
  Reverse proxy com `YARP`. Expone uma entrada unica para encaminhar chamadas HTTP para a `orders-api`.

- `src/notification-worker`
  Worker em background que consome a fila `orders.created` no `RabbitMQ` e registra o processamento do evento.

### Infraestrutura de apoio

- `rabbitmq`
  Broker para a mensageria entre a API de pedidos e o worker.

- `redis`
  Cache de leitura para consultas de pedido.

- `sqlserver`
  Banco relacional onde os pedidos sao persistidos.

### Pastas de apoio

- `compose/`
  Arquivos do `docker compose`.

- `docker/`
  Dockerfiles dos projetos `.NET`.

- `scripts/`
  Scripts utilitarios, inclusive verificacao de pre-requisitos.

- `docs/`
  Runbooks e materiais complementares.

- `tests/`
  Espaco reservado para testes automatizados do laboratorio.

## Pre-requisitos

- `Docker Desktop` em execucao
- `.NET SDK 8` ou superior
- `Visual Studio 2022` com workload de `.NET`/ASP.NET, caso voce queira debugar pela IDE

## Verificacao rapida

```powershell
docker --version
dotnet --version
./scripts/init/verify-prereqs.ps1
```

## Configuracao por ambiente

Este projeto foi organizado para ser reutilizado sem editar codigo-fonte a cada nova variacao.

- Variaveis de ambiente: use `.env` baseado em [.env.example](C:/Aplicativo.NET/Projetos/lab-microservices/.env.example)
- Docker Compose: configure portas e credenciais pelo `.env`
- `appsettings*.json`: mantenha defaults seguros e use override por ambiente
- Segredos reais: nao commite no repositorio; use variaveis de ambiente, secret manager ou o cofre da sua plataforma

## Como subir tudo com Docker Compose

Se a ideia for rodar o lab inteiro em containers, incluindo os tres projetos `.NET`, use:

```powershell
docker compose -f compose/docker-compose.yml -f compose/docker-compose.override.yml up --build
```

Endpoints esperados nesse modo:

- Gateway health: `http://localhost:8080/health`
- Orders health: `http://localhost:8081/health`
- RabbitMQ management: `http://localhost:15672`

Criacao de pedido via gateway:

```http
POST http://localhost:8080/orders/orders
Content-Type: application/json

{
  "customerName": "Maria",
  "total": 149.90
}
```

Consulta do pedido criado:

```http
GET http://localhost:8080/orders/orders/1
```

Para encerrar:

```powershell
docker compose -f compose/docker-compose.yml -f compose/docker-compose.override.yml down
```

## Como subir para debugar no Visual Studio

Se voce quer colocar breakpoint e acompanhar o fluxo dentro dos projetos `.NET`, nao suba a stack completa. Nesse caso, o melhor caminho e subir apenas a infraestrutura e executar os projetos pela solution.

### 1. Suba somente a infraestrutura

```powershell
docker compose -f compose/docker-compose.yml up -d rabbitmq redis sqlserver
```

Isso deixa ativos:

- `RabbitMQ` em `localhost:5672`
- `Redis` em `localhost:6379`
- `SQL Server` em `localhost:1433`
- painel do `RabbitMQ` em `http://localhost:15672`

As configuracoes locais da `orders-api` ja apontam para esses enderecos em `localhost`.

### 2. Abra a solution

Abra [lab-microservices.sln](C:/Aplicativo.NET/Projetos/lab-microservices/lab-microservices.sln).

### 3. Configure projetos de inicializacao

No Visual Studio:

1. Clique com o botao direito na solution.
2. Abra `Set Startup Projects...`.
3. Escolha `Multiple startup projects`.
4. Marque `Start` para:
   - `orders-api`
   - `notification-worker`
5. Marque `Start` para `gateway-api` apenas se voce quiser debugar o proxy tambem.

### 4. Execute em modo Debug

Portas locais dos projetos:

- `orders-api`: `http://localhost:5214`
- `gateway-api`: `http://localhost:5148`
- `notification-worker`: sem endpoint HTTP, roda em background

### 5. Entenda o detalhe do gateway local

Hoje o arquivo [src/gateway-api/yarp.json](C:/Aplicativo.NET/Projetos/lab-microservices/src/gateway-api/yarp.json) encaminha para:

```json
"Address": "http://orders-api:8080/"
```

Esse destino funciona quando a `orders-api` esta em container, porque `orders-api` e o nome do servico no `docker compose`.

Se voce rodar o `gateway-api` localmente no Visual Studio, ele nao vai encontrar `http://orders-api:8080/` por padrao. Para debugar localmente, voce tem duas opcoes:

- Opcao 1: testar a `orders-api` diretamente em `http://localhost:5214`
- Opcao 2: editar temporariamente o [src/gateway-api/yarp.json](C:/Aplicativo.NET/Projetos/lab-microservices/src/gateway-api/yarp.json) para apontar para `http://localhost:5214/`

Exemplo de destino local:

```json
"Address": "http://localhost:5214/"
```

Quando terminar o debug do proxy, volte o arquivo para `http://orders-api:8080/` se quiser continuar usando a stack completa em containers.

## Como criar um pedido e acompanhar o caminho dele

### Fluxo resumido

1. O cliente envia `POST /orders` para a `orders-api`, ou `POST /orders/orders` passando pelo `gateway-api`.
2. A `orders-api` normaliza os dados do pedido.
3. O pedido e persistido no `SQL Server`.
4. O pedido e gravado em cache no `Redis`.
5. A `orders-api` publica um evento `OrderCreated` no `RabbitMQ`.
6. O `notification-worker` consome a fila `orders.created`.
7. Consultas futuras ao pedido podem vir do cache antes de ir ao banco.

### Caminhos HTTP

Se voce estiver usando a `orders-api` diretamente no Visual Studio:

- Criar pedido: `POST http://localhost:5214/orders`
- Consultar pedido: `GET http://localhost:5214/orders/{id}`
- Health check: `GET http://localhost:5214/health`

Se voce estiver usando o `gateway-api`:

- Criar pedido: `POST http://localhost:8080/orders/orders`
- Consultar pedido: `GET http://localhost:8080/orders/orders/{id}`
- Health check do gateway: `GET http://localhost:8080/health`

### Exemplo de criacao de pedido

```http
POST http://localhost:5214/orders
Content-Type: application/json

{
  "customerName": "Maria",
  "total": 149.90
}
```

Resposta esperada:

```json
{
  "id": 1,
  "code": "ORD-...",
  "customerName": "Maria",
  "total": 149.90,
  "status": "Created",
  "eventPublished": true
}
```

### O que observar durante o debug

- Na `orders-api`, o `POST /orders` grava no banco, popula cache e publica no broker.
- No `notification-worker`, o breakpoint principal fica no consumo da fila `orders.created`.
- Na consulta `GET /orders/{id}`, a resposta informa a origem dos dados no campo `source`:
  - `redis` quando veio do cache
  - `sqlserver` quando veio do banco

### Ordem recomendada para debugar

1. Suba `rabbitmq`, `redis` e `sqlserver` com Docker.
2. Rode `orders-api` e `notification-worker` no Visual Studio.
3. Envie um `POST /orders`.
4. Verifique o breakpoint da publicacao na `orders-api`.
5. Verifique o breakpoint do consumo no `notification-worker`.
6. Execute um `GET /orders/{id}` duas vezes para observar banco na primeira leitura e cache nas proximas.

## Build da solution

```powershell
dotnet build lab-microservices.sln
```

## Estrategia recomendada para variacoes

1. Evolua a base no branch principal.
2. Marque pontos estaveis com tags, por exemplo `v1.0-base`.
3. Para uma customizacao pequena, abra um branch como `feature/webhook-consumer`.
4. Para um novo projeto, crie um repositorio derivado desta base.
5. Mantenha um changelog simples ou releases para saber o que cada variacao herdou.

## Publicacao inicial no remoto

Exemplo com `GitHub`:

```powershell
git add .
git commit -m "chore: bootstrap base microservices lab"
git branch -M main
git remote add origin https://github.com/<seu-usuario>/lab-microservices.git
git push -u origin main
```

Depois, se quiser usar como molde:

- `GitHub`: habilite `Settings > General > Template repository`
- `GitLab`: crie um novo projeto a partir do repositório exportado ou espelhado
- `Azure DevOps`: mantenha um repositório base e gere novos repositórios por import

## Referencias uteis

- Runbook de validacao manual: [docs/runbooks/local-validation.md](C:/Aplicativo.NET/Projetos/lab-microservices/docs/runbooks/local-validation.md)
- Guia de versionamento/template: [docs/runbooks/repository-template.md](C:/Aplicativo.NET/Projetos/lab-microservices/docs/runbooks/repository-template.md)
