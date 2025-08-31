# MicroBlogging Platform

A lightweight **microblogging platform** built with **.NET 8**, following **Clean Architecture** principles and using **CQRS + MediatR**.  
The system allows users to register, log in, and interact with a personalized **newsfeed** that aggregates posts (with images) from other users.

---

## 🏗 Architecture

The project follows a layered **Clean Architecture** structure

---

## ⚙️ Tech Stack

- **Backend Framework:** .NET 8 (ASP.NET Core MVC)  
- **Data Access:** Entity Framework Core (PostgreSQL)  
- **Architecture:** Clean Architecture + CQRS (MediatR)  
- **Authentication:** Cookie-based auth with custom Login & Register flow  
- **UI:** Razor Views + Bootstrap 5 (for styling and modals)  

---

## 🛠 Getting Started

### 1 Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://hub.docker.com/_/postgres)
- [EF Core Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

### 2 Setup the database
Update your connection string in appsettings.json (inside Web/ project) if needed.
Run EF Core migrations:
`dotnet ef database update -s Presentation/MicroBlogging.Web -p Infrastructure/MicroBlogging.Persistence`

### 3 Run the app
`dotnet run -p Presentation/MicroBlogging.Web`
