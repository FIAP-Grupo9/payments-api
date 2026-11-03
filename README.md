> **Nota:** Esta e a copia independente do microsservico **PaymentsAPI** gerada por split-repos.ps1 a partir do monorepo TechChallenge-Fiap-FCG. Shared/ foi embedded em ./shared/. Build local: docker build -t fcg-payments-api . (da raiz deste repo).

---

# PaymentsAPI — FCG Microsserviço de Pagamentos

Processa (simula) o pagamento de uma compra de jogo. **Consume** `OrderPlacedEvent` (publicado pelo CatalogAPI) e **publica** `PaymentProcessedEvent` com o resultado (Approved/Rejected).

---

## Responsabilidades

- Persistir uma **Order** (ledger de pagamento) com o estado do processo (Pending → Approved | Rejected)
- Simular o gateway de pagamento via `PaymentSimulator` (determinístico: amounts > R$ 10.000 são rejeitados)
- Publicar `PaymentProcessedEvent` com o resultado

## Não é responsabilidade

- Atualizar biblioteca do usuário (isso é CatalogAPI)
- Enviar e-mails (isso é NotificationsAPI)

---

## Endpoints

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/api/orders/{id}` | autenticado | Consulta status de uma ordem (debug/observabilidade) |
| GET | `/health` | público | Health check |
| GET | `/metrics` | interno | Métricas no formato Prometheus |

---

## Observabilidade (Fase 3)

O serviço publica métricas em `/metrics`, coletadas pelo Prometheus e exibidas no painel do Grafana: tempo de resposta, volume de requisições e taxa de erros. O endereço é interno ao cluster.

Este serviço permanece **interno**: não é roteado pelo gateway, já que a compra é iniciada pela CatalogAPI e o pagamento acontece por mensageria.

PaymentsAPI é majoritariamente **consumer-driven** — não tem rotas REST de negócio.

## Eventos consumidos

| Evento | Origem | Ação |
|---|---|---|
| `OrderPlacedEvent` | CatalogAPI | Insere Order(Pending), simula, atualiza estado, publica `PaymentProcessedEvent` |

## Eventos publicados

| Evento | Quando | Payload |
|---|---|---|
| `PaymentProcessedEvent` | Após processar uma `Order` | `MessageId, OccurredAt, OrderId, UserId, UserEmail, GameId, GameName, Amount, Status, ProcessedAt, FailureReason?, PromocaoId?` |

Consumidores conhecidos: **CatalogAPI** (materializa biblioteca se Approved), **NotificationsAPI** (envia e-mail de confirmação ou falha).

### Idempotência

1. **`processed_messages`** dedup por `MessageId` (camada 1)
2. **`Order.Id = OrderPlacedEvent.OrderId`** — PK previne ordens duplicadas; se mesmo OrderId chegar 2x, o segundo é tratado idempotentemente

---

## Variáveis de ambiente

| Variável | Default | Descrição |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=localhost;Database=payments_db;...` | Postgres do serviço |
| `JwtSettings__Secret` / `Issuer` / `Audience` | dev placeholders / `FCG.UsersAPI` / `FCG.Services` | Para validar tokens no endpoint de debug |
| `RabbitMq__*` | localhost/5672/guest/guest | Broker |

---

## Rodando localmente

```powershell
docker compose -f ..\Orchestration\docker-compose.yml up -d postgres rabbitmq
dotnet run --project src/FCG.Payments.Api
# Não tem endpoint para iniciar pagamento — dispare via CatalogAPI (POST /api/biblioteca)
```

Para inspecionar uma ordem após o fluxo:

```powershell
$token = ...  # token JWT do UsersAPI
Invoke-RestMethod -Uri "http://localhost:5003/api/orders/$orderId" -Headers @{ Authorization = "Bearer $token" }
```

---

## Testes

Projeto `tests/FCG.Payments.Api.Tests` (xUnit + Moq):

```powershell
dotnet test tests/FCG.Payments.Api.Tests/FCG.Payments.Api.Tests.csproj
```

## CI/CD

GitHub Actions (`.github/workflows/ci.yml`):

- **CI** — em qualquer pull request (não importa a branch de base) e em todo push na `master`: `dotnet restore` + `build` + os testes acima.
- **CD** — só em push na `master`: build e publicação da imagem Docker em `ghcr.io/fiap-grupo9/payments-api` (tags `latest` e `<sha>`), usando o token automático do Actions.

Não há deploy automático: o único ambiente hoje é o Kubernetes/docker-compose local (ver `orchestration`), fora do alcance do GitHub Actions. A imagem fica publicada no registry, pronta para um deploy manual.

## Decisões arquiteturais

- **Simulação determinística** (`amount > 10000 → rejected`): garante reprodutibilidade para demo e testes. Em produção, integração com gateway real (Stripe, MercadoPago etc.).
- **`Order.Id = OrderId do evento`**: correlação direta com CatalogAPI sem necessidade de campo separado de `external_id`.
- **Sem retry customizado** além do default do MassTransit (3 tentativas com backoff). Mensagens com falha persistente vão para a fila de dead-letter automática do RabbitMQ.
- **EF Core `EnsureCreated()`** para simplicidade do demo.

