# Network Monitoring Dashboard

Vue 3 + Vite frontend, .NET 9 backend, and lightweight Node.js agents for a senior-project network monitoring stack.

## Local Development (manual)

```bash
# install frontend deps
npm install

# run the Vue dev server (http://localhost:5173)
npm run dev
```

Backend (from `backend/`):

```bash
dotnet restore
dotnet run
```

You can log in with **username `fluk` / password `1234`** once the backend is running.

## Full stack with Docker

Everything can run with a single command thanks to the new Docker setup:

```bash
docker compose up --build
```

Services:

| Service    | Port | Description                                                                           |
| ---------- | ---- | ------------------------------------------------------------------------------------- |
| `frontend` | 8080 | Nginx serving the built Vue app (API calls are proxied to the backend under `/api`).  |
| `backend`  | 5001 | ASP.NET Core API connected to PostgreSQL with automatic migrations.                   |
| `db`       | 5432 | PostgreSQL 15 storing users and ping metrics (data persisted in the `pgdata` volume). |
| `agent`    | n/a  | Node.js worker that pings configured targets and posts metrics to the backend.        |

When the stack is up:

- Open http://localhost:8080 to use the dashboard.
- The frontend proxies `/api/*` calls to the backend container, so no extra configuration is needed.

Shut everything down with:

```bash
docker compose down
```

## Configuration Notes

- Frontend API base URL is controlled by `VITE_API_URL`. The Docker build sets it to `/api` so the nginx proxy can forward to the backend. For local dev you can override it in `.env` (e.g. `VITE_API_URL=http://localhost:5162`).
- Backend connection strings can be overridden via `ConnectionStrings__DefaultConnection` (already set in `docker-compose.yml` for the containerized stack).
- `agent/config.json` controls ping targets, HTTP targets, intervals, and the backend URL the agent submits to.

## Useful Commands

```bash
# run tests or tooling as needed
docker compose logs -f backend
docker compose logs -f agent
```

Feel free to extend the compose file with additional services (Grafana, Prometheus, etc.) if you need more telemetry sinks.
