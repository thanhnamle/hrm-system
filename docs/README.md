# HR Management System — Documentation

| Document | Contents |
|---|---|
| [plan.md](plan.md) | Scope, 10-day schedule, cut order, final checklist |
| [architecture.md](architecture.md) | System architecture, Clean Architecture layers, repo layout, infrastructure, frontend structure |
| [application-layer.md](application-layer.md) | CQRS + MediatR structure, pipeline, use cases, API endpoints |
| [security.md](security.md) | Authentication design, threats & mitigations, error mapping |
| [rbac.md](rbac.md) | Roles, permission matrix, 3-layer access control, implementation |

## Diagrams (draw.io)

Open with [draw.io desktop](https://www.drawio.com/), [app.diagrams.net](https://app.diagrams.net/), or the VS Code extension *Draw.io Integration* (`hediet.vscode-drawio`).

| File | Diagram |
|---|---|
| [diagrams/system-architecture.drawio](diagrams/system-architecture.drawio) | **Detailed** (components inside every container, ports, request path ①–⑥, cross-cutting concerns) · **Overview** · **Local development** |
| [diagrams/clean-architecture.drawio](diagrams/clean-architecture.drawio) | Backend layers, dependency rule, request flow through MediatR |
| [diagrams/erd.drawio](diagrams/erd.drawio) | Database schema (entities, columns, relationships) |
| [diagrams/rbac-layers.drawio](diagrams/rbac-layers.drawio) | The three access-control layers |
| [diagrams/sequence-login.drawio](diagrams/sequence-login.drawio) | Login flow |
| [diagrams/sequence-token-refresh.drawio](diagrams/sequence-token-refresh.drawio) | Silent token refresh with rotation & reuse detection |
| [diagrams/sequence-approve-leave.drawio](diagrams/sequence-approve-leave.drawio) | Manager approves a leave request |

## Tech stack

- **Backend:** C# / ASP.NET Core (.NET 10), Clean Architecture, CQRS, MediatR, FluentValidation, EF Core
- **Database:** PostgreSQL 17
- **Frontend:** Angular (standalone components + signals), TypeScript, Tailwind CSS v4
- **Infrastructure:** Docker Compose, Nginx reverse proxy
