# TravelSystem — Backend (ASP.NET Web API)

Sistema de organização de viagens com roteirização, IA, controlo financeiro e reservas.

---

## Arquitetura

```
TravelSystem/
├── src/
│   ├── TravelSystem.API/             # Camada de Apresentação
│   │   ├── Controllers/              # AuthController, ItinerariesController, etc.
│   │   ├── Middleware/               # ExceptionHandlerMiddleware, CurrentUserService
│   │   └── Extensions/              # ServiceExtensions (DI setup)
│   │
│   ├── TravelSystem.Application/     # Camada de Negócio
│   │   ├── DTOs/                    # Request/Response records
│   │   ├── Interfaces/              # IAuthService, IItineraryService, etc.
│   │   ├── Services/                # AuthService, ItineraryService, AiAssistantService...
│   │   ├── Validators/              # FluentValidation validators
│   │   └── Mappings/                # AutoMapper profiles
│   │
│   ├── TravelSystem.Domain/          # Camada de Domínio
│   │   ├── Entities/                # User, Itinerary, Hotel, Flight, Booking...
│   │   ├── Enums/                   # ItineraryStatus, BookingStatus...
│   │   └── Interfaces/              # IRepository, IUnitOfWork
│   │
│   └── TravelSystem.Infrastructure/ # Camada de Infraestrutura
│       ├── Data/
│       │   ├── AppDbContext.cs       # EF Core DbContext com configurações
│       │   ├── DbSeeder.cs           # Seed inicial (admin + roles)
│       │   ├── Repositories/        # Implementações dos repositórios
│       │   └── Migrations/          # InitialSchema.sql
│       └── Services/
│           ├── EmailService.cs       # MailKit / SMTP
│           ├── HotelService.cs       # Pesquisa e reservas de hotéis
│           ├── FlightService.cs      # Pesquisa e alertas de voos
│           └── FlightAlertBackgroundService.cs  # Job a cada hora
```

---

## Pré-requisitos

| Ferramenta       | Versão mínima |
|------------------|---------------|
| .NET SDK         | 8.0           |
| MySQL            | 8.0           |
| Visual Studio    | 2022 / VS Code|

---

## Configuração Inicial

### 1. Clonar e restaurar dependências

```bash
git clone <repo-url>
cd TravelSystem
dotnet restore
```

### 2. Configurar base de dados

Criar a base de dados no MySQL:
```sql
CREATE DATABASE travel_system_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 3. Configurar `appsettings.Development.json`

Editar `src/TravelSystem.API/appsettings.json` com os seus dados:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Port=3306;Database=travel_system_dev;User=root;Password=SUA_SENHA;"
  },
  "Jwt": {
    "Secret": "UMA_CHAVE_SECRETA_COM_PELO_MENOS_32_CARACTERES"
  },
  "AI": {
    "ApiKey": "SUA_CHAVE_ANTHROPIC"
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Username": "seu-email@gmail.com",
    "Password": "sua-app-password"
  }
}
```

### 4. Aplicar migrações (EF Core)

```bash
cd src/TravelSystem.API
dotnet ef migrations add InitialCreate --project ../TravelSystem.Infrastructure
dotnet ef database update
```

> Alternativa: executar manualmente o script `InitialSchema.sql` no MySQL Workbench.

### 5. Executar a API

```bash
dotnet run --project src/TravelSystem.API
```

A API estará disponível em:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`
- **Swagger UI**: `https://localhost:5001/swagger`

---

## Utilizador Admin Padrão (Seed)

| Campo  | Valor                  |
|--------|------------------------|
| Email  | admin@travelsystem.ao  |
| Senha  | Admin@123456           |
| Role   | Admin                  |

> Altere a senha após o primeiro login!

---

## Endpoints da API

### Autenticação (`/api/auth`)

| Método | Rota                    | Auth | Descrição                  |
|--------|-------------------------|------|----------------------------|
| POST   | `/register`             | ❌   | Registar novo utilizador   |
| POST   | `/login`                | ❌   | Autenticar                 |
| POST   | `/logout`               | ✅   | Terminar sessão            |
| POST   | `/refresh`              | ❌   | Renovar token JWT          |
| POST   | `/forgot-password`      | ❌   | Solicitar reset de senha   |
| POST   | `/reset-password`       | ❌   | Redefinir senha com token  |
| POST   | `/change-password`      | ✅   | Alterar senha autenticado  |
| GET    | `/profile`              | ✅   | Obter perfil próprio       |
| PUT    | `/profile`              | ✅   | Atualizar perfil           |

### Roteiros (`/api/itineraries`)

| Método | Rota                            | Descrição                     |
|--------|---------------------------------|-------------------------------|
| GET    | `/`                             | Listar meus roteiros          |
| GET    | `/{id}`                         | Detalhes + paragens + despesas|
| POST   | `/`                             | Criar roteiro                 |
| PUT    | `/{id}`                         | Atualizar roteiro             |
| DELETE | `/{id}`                         | Eliminar roteiro              |
| POST   | `/{id}/stops`                   | Adicionar paragem             |
| DELETE | `/{id}/stops/{stopId}`          | Remover paragem               |
| PATCH  | `/{id}/stops/{stopId}/reorder`  | Reordenar paragem             |

### Hotéis (`/api/hotels`)

| Método | Rota                         | Descrição              |
|--------|------------------------------|------------------------|
| GET    | `/search`                    | Pesquisar hotéis       |
| GET    | `/{id}`                      | Detalhes do hotel      |
| POST   | `/bookings`                  | Fazer reserva          |
| GET    | `/bookings`                  | Minhas reservas        |
| GET    | `/bookings/{bookingId}`      | Detalhe da reserva     |
| DELETE | `/bookings/{bookingId}`      | Cancelar reserva       |

### Voos (`/api/flights`)

| Método | Rota                         | Descrição              |
|--------|------------------------------|------------------------|
| GET    | `/search`                    | Pesquisar voos         |
| POST   | `/bookings`                  | Reservar voo           |
| GET    | `/alerts`                    | Meus alertas de preço  |
| POST   | `/alerts`                    | Criar alerta           |
| DELETE | `/alerts/{alertId}`          | Eliminar alerta        |
| PATCH  | `/alerts/{alertId}/toggle`   | Ativar/desativar alerta|

### Assistente IA (`/api/ai`)

| Método | Rota                   | Descrição                       |
|--------|------------------------|---------------------------------|
| POST   | `/chat`                | Enviar mensagem ao assistente   |
| POST   | `/suggest`             | Sugestão de roteiro por IA      |
| GET    | `/chat/{itineraryId}`  | Histórico de chat do roteiro    |
| DELETE | `/chat/{itineraryId}`  | Limpar histórico                |

### Relatórios (`/api/reports`)

| Método | Rota                       | Descrição                      |
|--------|----------------------------|--------------------------------|
| GET    | `/summary/{itineraryId}`   | Resumo financeiro (JSON)       |
| POST   | `/pdf`                     | Download relatório PDF         |
| POST   | `/csv`                     | Download despesas CSV          |

### Admin (`/api/admin`) — Role: Admin

| Método | Rota                          | Descrição               |
|--------|-------------------------------|-------------------------|
| GET    | `/users`                      | Listar todos os users   |
| PATCH  | `/users/{userId}/deactivate`  | Desativar utilizador    |

---

## Roles e Permissões

| Role             | Funcionalidades                          |
|------------------|------------------------------------------|
| Traveler         | CRUD roteiros, reservas, alertas, IA     |
| PremiumTraveler  | Tudo do Traveler + prioridade no suporte |
| Admin            | Gestão de utilizadores + tudo acima      |

---

## Tecnologias Usadas

| Camada         | Tecnologia                              |
|----------------|-----------------------------------------|
| Framework      | ASP.NET Web API (.NET 8)               |
| ORM            | Entity Framework Core 8 (Pomelo MySQL) |
| Base de Dados  | MySQL 8.0                              |
| Autenticação   | ASP.NET Identity + JWT Bearer          |
| Hash de Senhas | BCrypt.Net                             |
| Validação      | FluentValidation                       |
| Mapeamento     | AutoMapper                             |
| Email          | MailKit                                |
| IA             | Claude API (Anthropic)                 |
| PDF            | iTextSharp                             |
| CSV            | CsvHelper                              |
| Logging        | Serilog                                |
| Docs           | Swagger / Swashbuckle                  |

---

## Próximos Passos (Frontend Angular)

O backend expõe uma API REST completa. O frontend Angular deverá:

1. Implementar interceptor HTTP para enviar o `Bearer <token>` em cada pedido
2. Implementar refresh automático do token ao receber `401`
3. Usar `i18n` com `@ngx-translate` para suporte a PT/EN
4. Integrar Google Maps JavaScript API para visualização das atrações
5. Implementar `dark mode` via classe CSS no `<html>` + variáveis CSS
