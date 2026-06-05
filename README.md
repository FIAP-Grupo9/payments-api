> **Nota:** Esta e a copia independente do microsservico **PaymentsAPI** gerada por split-repos.ps1 a partir do monorepo TechChallenge-Fiap-FCG. Shared/ foi embedded em ./shared/. Build local: docker build -t fcg-payments-api . (da raiz deste repo).

---

# PaymentsAPI â€” FCG MicrosserviÃ§o de Pagamentos

Processa (simula) o pagamento de uma compra de jogo. **Consume** `OrderPlacedEvent` (publicado pelo CatalogAPI) e **publica** `PaymentProcessedEvent` com o resultado (Approved/Rejected).

---

## Responsabilidades

- Persistir uma **Order** (ledger de pagamento) com o estado do processo (Pending â†’ Approved | Rejected)
- Simular o gateway de pagamento via `PaymentSimulator` (determinÃ­stico: amounts > R$ 10.000 sÃ£o rejeitados)
- Publicar `PaymentProcessedEvent` com o resultado

## NÃ£o Ã© responsabilidade

- Atualizar biblioteca do usuÃ¡rio (isso Ã© CatalogAPI)
- Enviar e-mails (isso Ã© NotificationsAPI)

---

## Endpoints

| MÃ©todo | Rota | Auth | DescriÃ§Ã£o |
|---|---|---|---|
| GET | `/api/orders/{id}` | autenticado | Consulta status de uma ordem (debug/observabilidade) |
| GET | `/health` | pÃºblico | Health check |

PaymentsAPI Ã© majoritariamente **consumer-driven** â€” nÃ£o tem rotas REST de negÃ³cio.

## Eventos consumidos

| Evento | Origem | AÃ§Ã£o |
|---|---|---|
| `OrderPlacedEvent` | CatalogAPI | Insere Order(Pending), simula, atualiza estado, publica `PaymentProcessedEvent` |

## Eventos publicados

| Evento | Quando | Payload |
|---|---|---|
| `PaymentProcessedEvent` | ApÃ³s processar uma `Order` | `MessageId, OccurredAt, OrderId, UserId, UserEmail, GameId, GameName, Amount, Status, ProcessedAt, FailureReason?, PromocaoId?` |

Consumidores conhecidos: **CatalogAPI** (materializa biblioteca se Approved), **NotificationsAPI** (envia e-mail de confirmaÃ§Ã£o ou falha).

### IdempotÃªncia

1. **`processed_messages`** dedup por `MessageId` (camada 1)
2. **`Order.Id = OrderPlacedEvent.OrderId`** â€” PK previne ordens duplicadas; se mesmo OrderId chegar 2x, o segundo Ã© tratado idempotentemente

---

## VariÃ¡veis de ambiente

| VariÃ¡vel | Default | DescriÃ§Ã£o |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Database=payments_db;...` | Postgres do serviÃ§o |
| `JwtSettings__Secret` / `Issuer` / `Audience` | dev placeholders / `FCG.UsersAPI` / `FCG.Services` | Para validar tokens no endpoint de debug |
| `RabbitMq__*` | localhost/5672/guest/guest | Broker |

---

## Rodando localmente

```powershell
docker compose -f ..\Orchestration\docker-compose.yml up -d postgres rabbitmq
dotnet run --project src/FCG.Payments.Api
# NÃ£o tem endpoint para iniciar pagamento â€” dispare via CatalogAPI (POST /api/biblioteca)
```

Para inspecionar uma ordem apÃ³s o fluxo:

```powershell
$token = ...  # token JWT do UsersAPI
Invoke-RestMethod -Uri "http://localhost:5003/api/orders/$orderId" -Headers @{ Authorization = "Bearer $token" }
```

---

## DecisÃµes arquiteturais

- **SimulaÃ§Ã£o determinÃ­stica** (`amount > 10000 â†’ rejected`): garante reprodutibilidade para demo e testes. Em produÃ§Ã£o, integraÃ§Ã£o com gateway real (Stripe, MercadoPago etc.).
- **`Order.Id = OrderId do evento`**: correlaÃ§Ã£o direta com CatalogAPI sem necessidade de campo separado de `external_id`.
- **Sem retry customizado** alÃ©m do default do MassTransit (3 tentativas com backoff). Mensagens com falha persistente vÃ£o para a fila de dead-letter automÃ¡tica do RabbitMQ.
- **EF Core `EnsureCreated()`** para simplicidade do demo.

