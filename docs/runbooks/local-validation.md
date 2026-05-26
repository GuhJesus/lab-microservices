# Local Validation Runbook

## Objetivo

Validar localmente o fluxo ponta a ponta do laboratorio:
- health dos servicos HTTP;
- criacao e consulta de pedidos;
- persistencia no SQL Server;
- cache no Redis;
- publicacao e consumo de evento no RabbitMQ;
- observabilidade basica com OpenTelemetry.

## Pre-requisitos

- Docker Desktop em execucao.
- Portas locais `8080`, `8081`, `1433`, `15672`, `5672` e `6379` livres.

## Subir a stack

```powershell
docker compose -f compose/docker-compose.yml -f compose/docker-compose.override.yml up --build -d
docker compose -f compose/docker-compose.yml -f compose/docker-compose.override.yml ps
```

Esperado:
- `gateway-api`, `orders-api`, `notification-worker`, `rabbitmq`, `redis` e `sqlserver` em execucao.
- `gateway-api`, `orders-api`, `rabbitmq`, `redis` e `sqlserver` com health status saudavel.

## Healthchecks HTTP

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:8080/health | Select-Object -ExpandProperty Content
Invoke-WebRequest -UseBasicParsing http://localhost:8081/health | Select-Object -ExpandProperty Content
```

Esperado:
- `{"status":"ok","service":"gateway-api"}`
- `{"status":"ok","service":"orders-api"}`

## Criar pedido via gateway

```powershell
$body = '{"customerName":"Alice","total":149.90}'
Invoke-WebRequest -UseBasicParsing -Method Post -Uri http://localhost:8080/orders/orders -ContentType 'application/json' -Body $body |
  Select-Object -ExpandProperty Content
```

Exemplo validado:

```json
{"id":1,"code":"ORD-9AE679E8","customerName":"Alice","total":149.90,"status":"Created","eventPublished":true}
```

## Consultar pedido via gateway

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:8080/orders/orders/1 | Select-Object -ExpandProperty Content
```

Esperado:
- payload do pedido com `source` indicando a origem atual da leitura.
- na validacao mais recente, o `POST` ja populou o cache e a leitura respondeu com `source: "redis"`.

## Confirmar persistencia no SQL Server

```powershell
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "Your_strong_password123" -Q "SELECT TOP 5 Id, Code, CustomerName, Total, Status FROM OrdersDb.dbo.Orders;"
```

Esperado:
- linha do pedido criado presente na tabela `Orders`.

## Confirmar cache no Redis

```powershell
docker exec redis redis-cli TYPE orders-api:orders:1
docker exec redis redis-cli HGETALL orders-api:orders:1
```

Esperado:
- tipo `hash`;
- campo `data` contendo o JSON serializado do pedido.

## Confirmar consumo do evento

```powershell
docker logs notification-worker --tail 100
```

Esperado:
- log `Evento recebido: ...`;
- activity `orders.created consume` no output de OpenTelemetry.

## Evidencia mais recente

Na validacao manual registrada em `2026-05-15`:
- `gateway-api` e `orders-api` responderam corretamente.
- o pedido `Id=1` e `Code=ORD-9AE679E8` foi criado com sucesso.
- o registro apareceu no `SQL Server`.
- a chave `orders-api:orders:1` apareceu no `Redis`.
- o `notification-worker` consumiu o evento `OrderCreated`.

## Observacao operacional

O healthcheck HTTP do Compose usa `GET` real com `wget -q -O -`, porque `wget --spider` envia `HEAD` e faria o endpoint `/health` retornar `405`, marcando o container como `unhealthy` sem problema real da aplicacao.
