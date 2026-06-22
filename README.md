# Task Tracker REST API

A backend-only REST API for task tracking built with ASP.NET Core 8.0, Entity Framework Core, and SQLite. 

This API implements JWT-based authentication so each user can register, log in, and securely manage their own tasks.

## Features
- **JWT Authentication**: User registration and login. Secure endpoints using JWT Bearer tokens.
- **User Separation**: Users only see and manage their own tasks.
- **CRUD Operations**: Complete CRUD capabilities for tasks.
- **SQLite Database**: Persistent file-based storage using SQLite.
- **Advanced Filtering**: Filter tasks by `completed` status and `priority` level.
- **Pagination**: Supports paginating task results using `page` and `pageSize` query parameters.
- **Validation**: Enforced rules for required titles, optional descriptions, and valid due dates (today or in the future).
- **Swagger UI**: Integrated API documentation with support for inputting JWT Bearer Tokens.

---

## How to Run

### Prerequisites
- .NET 8.0 SDK installed.

### 1. Database Setup
Apply the Entity Framework migrations to create the SQLite database:
```bash
dotnet ef database update
```
*(This will generate the local `tasks.db` database file.)*

### 2. Run the Application
Start the API server locally:
```bash
dotnet run --launch-profile http
```
The server will start and listen at **`http://localhost:5147`**.

### 3. Access Swagger UI (Optional)
You can access the Swagger interactive documentation in your browser:
* URL: [http://localhost:5147/swagger/index.html](http://localhost:5147/swagger/index.html)
* *Note: To test secure task endpoints in Swagger, click the **Authorize** button and paste your JWT token in the format: `Bearer {your_token}`.*

---

## Example API Requests

### 1. Register a New Account (POST)
```bash
curl -X POST http://localhost:5147/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "alice",
    "password": "password123"
  }'
```

### 2. Log In & Retrieve JWT Token (POST)
Authenticate credentials and obtain a bearer token for future requests.
```bash
curl -X POST http://localhost:5147/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "alice",
    "password": "password123"
  }'
```
*Expected Response:*
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpX... (truncated)"
}
```

---

### Secure Task Endpoints
*Include the header `"Authorization: Bearer <your_token>"` on all task endpoints below.*

### 3. Create a Task (POST)
Creates a new task. The `dueDate` must be today or in the future.
```bash
curl -X POST http://localhost:5147/tasks \
  -H "Authorization: Bearer <your_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Buy groceries",
    "description": "Milk, eggs, bread",
    "dueDate": "2026-07-01",
    "priority": "Medium"
  }'
```

### 4. Get All Tasks with Filtering & Pagination (GET)
Returns the tasks belonging only to the authenticated user.
```bash
# Retrieve tasks with filters
curl -H "Authorization: Bearer <your_token>" "http://localhost:5147/tasks?completed=false&priority=Medium"

# Retrieve tasks with pagination (page 1, size 10)
curl -H "Authorization: Bearer <your_token>" "http://localhost:5147/tasks?page=1&pageSize=10"
```

### 5. Update a Task (PUT)
Updates the details of an existing task owned by the authenticated user.
```bash
curl -X PUT http://localhost:5147/tasks/1 \
  -H "Authorization: Bearer <your_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Buy groceries",
    "description": "Milk, eggs, bread",
    "isCompleted": true,
    "dueDate": "2026-07-01",
    "priority": "Medium"
  }'
```

---

## Confirming Persistence
The application uses SQLite as its database engine. When tasks are created, updated, or deleted, the changes are stored in the local file `tasks.db`. 

If you stop the application (`Ctrl+C`) and start it again using `dotnet run`, all previously created tasks and registered users will remain intact.