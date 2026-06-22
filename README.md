# Task Tracker REST API

A backend-only REST API for task tracking built with ASP.NET Core 8.0, Entity Framework Core, and SQLite.

## Features
- **CRUD Operations**: Complete CRUD capabilities for managing tasks.
- **SQLite Database**: Persistent file-based storage using SQLite.
- **Advanced Filtering**: Filter tasks by `completed` status and `priority` level.
- **Validation**: Enforced rules for required titles, optional descriptions, and valid due dates (today or in the future).
- **Swagger UI**: Integrated API documentation and testing interface.

---

## How to Run

### Prerequisites
- .NET 8.0 SDK installed.

### 1. Database Setup
Apply the Entity Framework migrations to create the SQLite database:
```bash
dotnet ef database update
```
*(This will generate the local `tasks.db` file in the project directory.)*

### 2. Run the Application
Start the API server locally:
```bash
dotnet run --launch-profile http
```
The server will start and listen at **`http://localhost:5147`**.

### 3. Access Swagger UI (Optional)
When running in `Development` environment, you can access the Swagger interactive documentation in your browser:
* URL: [http://localhost:5147/swagger/index.html](http://localhost:5147/swagger/index.html)

---

## Example API Requests

### 1. Create a Task (POST)
Creates a new task. The `dueDate` must be today or in the future.
```bash
curl -X POST http://localhost:5147/tasks \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Buy groceries",
    "description": "Milk, eggs, bread",
    "dueDate": "2026-07-01",
    "priority": "Medium"
  }'
```

### 2. Get All Tasks with Filtering & Pagination (GET)
Returns all tasks. Supports optional filtering by `completed` and `priority` query parameters, as well as pagination using `page` and `pageSize`.
```bash
# Retrieve tasks with filters
curl "http://localhost:5147/tasks?completed=false&priority=Medium"

# Retrieve tasks with pagination (page 1, size 10)
curl "http://localhost:5147/tasks?page=1&pageSize=10"
```

### 3. Update a Task (PUT)
Updates the details of an existing task.
```bash
curl -X PUT http://localhost:5147/tasks/1 \
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

If you stop the application (`Ctrl+C`) and start it again using `dotnet run`, all previously created tasks will remain intact.