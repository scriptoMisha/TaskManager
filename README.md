# Task Manager

[![CI](https://github.com/scriptoMisha/TaskManager/actions/workflows/ci.yml/badge.svg)](https://github.com/scriptoMisha/TaskManager/actions/workflows/ci.yml)

REST API для личных списков задач и небольшой веб-клиент к нему. Пользователь регистрируется, входит по email и паролю, получает JWT и работает только со своими задачами.

## Стек

- **Backend:** .NET 10, ASP.NET Core (контроллеры), EF Core 10 + PostgreSQL (Npgsql), JWT Bearer, BCrypt, Swagger (Swashbuckle)
- **Frontend:** React 19, TypeScript, Vite
- **Тесты:** xUnit, `WebApplicationFactory`, Testcontainers (настоящий PostgreSQL в Docker)
- **Инфраструктура:** Docker (multi-stage), docker-compose, nginx, GitHub Actions

## API

| Метод | Путь | Доступ | Ответ |
|---|---|---|---|
| POST | `/api/auth/register` | открыт | 201 / 400 / 409 (email занят) |
| POST | `/api/auth/login` | открыт | 200 + токен / 401 |
| GET | `/api/tasks?status=&sort=&page=&pageSize=` | JWT | 200, страница своих задач |
| GET | `/api/tasks/{id}` | JWT | 200 / 404 |
| POST | `/api/tasks` | JWT | 201 + `Location` |
| PUT | `/api/tasks/{id}` | JWT | 204 / 404 |
| PATCH | `/api/tasks/{id}/done` | JWT | 204 / 404 |
| DELETE | `/api/tasks/{id}` | JWT | 204 / 404 |

Параметры списка:

- `status`: `active` или `done`, без параметра возвращаются все задачи;
- `sort`: `newest` (по умолчанию), `oldest` или `title`;
- `page` (с 1) и `pageSize` (1–100, по умолчанию 20).

Ответ списка: `{ items, page, pageSize, totalCount }`.

Ошибки возвращаются в формате [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`). Запрос к чужой задаче даёт 404.

## Запуск в Docker

Нужен только Docker.

1. Создать `.env` из примера:

   ```bash
   cp .env.example .env
   ```

2. Вписать в `.env` случайный `JWT_KEY` длиной не меньше 32 символов. Сгенерировать его можно так:

   ```bash
   openssl rand -base64 48
   ```

   В PowerShell:

   ```powershell
   [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
   ```

3. Поднять всё:

   ```bash
   docker compose up --build -d
   ```

| Сервис | Адрес |
|---|---|
| Веб-клиент | http://localhost:3000 |
| Swagger | http://localhost:8080/swagger |
| PostgreSQL | `localhost:5433` |

Миграции применяются автоматически при старте API. Порты и пароль БД меняются в `.env`.

Остановить:

```bash
docker compose down
```

Флаг `-v` дополнительно удалит данные БД.

### Как получить токен в Swagger

1. Выполнить `POST /api/auth/register`, затем `POST /api/auth/login`.
2. Скопировать `accessToken` из ответа.
3. Нажать **Authorize** и вставить токен без слова `Bearer`.



## Тесты

```bash
dotnet test
```

Должен быть запущен Docker: тесты сами поднимают отдельный контейнер PostgreSQL и удаляют его после прогона.

- **Интеграционные тесты** гоняют настоящие HTTP-запросы через всё приложение: регистрацию и вход, CRUD задач, фильтры и пагинацию, коды ответов, изоляцию задач между пользователями.
- **Unit-тесты** проверяют выдачу и валидацию JWT.



## CI

GitHub Actions запускается на каждый push и pull request и выполняет три задачи:

1. **backend:** `restore`, `build` и `test` для .NET-решения;
2. **frontend:** `npm ci`, `lint` и `build`;
3. **docker:** если первые две задачи прошли, собирает образы API и веб-клиента.
