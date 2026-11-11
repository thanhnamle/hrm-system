# hrm-client

Angular front end of the HR Management System. See the [root README](../README.md) for the whole project.

| | |
|---|---|
| Framework | Angular 22 (standalone components, signals, zoneless, OnPush by default) |
| Styling | Tailwind CSS v4; design tokens in `src/styles.css` (`@theme`) |
| Tests | Vitest (`npm test`) |
| Lint / format | ESLint with angular-eslint, Prettier |

## Run it

Requires Node.js 22.22+, 24.15+ or 26+, and the API running on `http://localhost:5183` (see the root README).

```bash
npm install
npm start
```

Open http://localhost:4200. Requests to `/api/*` and `/health` are forwarded to the API by `proxy.conf.json`, so the browser only ever talks to one origin (the same as behind Nginx in production).

| Script | What it does |
|---|---|
| `npm start` | Dev server with the API proxy and live reload |
| `npm run build` | Production build into `dist/hrm-client` |
| `npm test` | Unit tests (Vitest) |
| `npm run lint` | ESLint, including the layering rule below |
| `npm run format` / `format:check` | Prettier |

## Structure

```
src/app/
├── app.config.ts · app.routes.ts   providers and top-level routes (every page lazy-loaded)
├── core/        singletons: auth (permissions, AuthService, interceptors, guards), http, models (API DTOs)
├── shared/      reusable UI, directives and pipes; knows nothing about HR features
├── layout/      shell, sidebar, top bar, menu items
└── features/    one folder per area, each with its own <feature>.routes.ts
```

Rules:

1. `features` may import `core` and `shared`; `core`, `shared` and `layout` never import `features` (enforced by `npm run lint`).
2. Features don't import each other (a convention, not a lint rule).
3. Each feature calls the API through its own `<feature>.api.ts`; components never use `HttpClient` directly.
4. `core/models` mirrors the API DTOs; when a DTO changes, update its file there.
5. Nothing secret goes in `src/environments` or anywhere else in the client: the whole bundle is public.
