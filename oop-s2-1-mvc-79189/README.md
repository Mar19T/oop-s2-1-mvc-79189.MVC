# VGC College — Multi-Branch Student & Course Management System

A web application built with ASP.NET Core MVC to manage student registration,
attendance tracking, and academic progress across three college branches.

## Tech Stack
- ASP.NET Core MVC (.NET 9)
- Entity Framework Core + MySQL (Pomelo)
- ASP.NET Core Identity (authentication + RBAC)
- Serilog (structured logging)
- xUnit (unit tests)
- GitHub Actions (CI)

## How to Run Locally

### Prerequisites
- .NET 9 SDK
- MySQL Server running locally

### Setup

1. Clone the repository:
   git clone https://github.com/JohnRowleyDorsetCollege/oop-s2-1-mvc-79189
   cd oop-s2-1-mvc-79189.MVC

2. Update the connection string in appsettings.json:
   "DefaultConnection": "Server=localhost;Database=VgcCollege;User=root;Password=YOUR_PASSWORD;"

3. Run migrations:
   cd oop-s2-1-mvc-79189
   dotnet ef database update

4. Run the app:
   dotnet run

5. Open browser at https://localhost:7124

## Seeded Demo Accounts

| Role          | Email               | Password    |
|---------------|---------------------|-------------|
| Administrator | admin@vgc.ie        | Admin123!   |
| Faculty       | faculty1@vgc.ie     | Faculty123! |
| Faculty       | faculty2@vgc.ie     | Faculty123! |
| Faculty       | faculty3@vgc.ie     | Faculty123! |
| Student       | student1@vgc.ie     | Student123! |
| Student       | student2@vgc.ie     | Student123! |
| Student       | student3@vgc.ie     | Student123! |
| Student       | student4@vgc.ie     | Student123! |
| Student       | student5@vgc.ie     | Student123! |
| Student       | student6@vgc.ie     | Student123! |

## How to Run Tests

   cd tests/VgcCollege.Tests
   dotnet test

## Design Decisions

- Domain models live in a separate Entities.Domain project for clean separation
- Bogus library used for realistic seed data generation
- Role-based access enforced server-side on every controller action
- Students cannot view provisional exam results until Admin releases them
- Faculty only sees students enrolled in their assigned courses
- Serilog logs to both console and rolling daily file in /logs folder
- Global exception middleware catches unhandled errors and shows friendly page
