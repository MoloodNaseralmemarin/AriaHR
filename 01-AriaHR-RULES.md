
# AriaHR Development Rules

Version: 1.0  
Project: AriaHR  
Architecture: Modular Monolith  
Technology: .NET 10 / ASP.NET Core / SQL Server / Entity Framework Core / Angular

---

# 1. Purpose

This document defines the permanent development standards and architectural rules of the AriaHR project.

Every developer, AI assistant, automation tool, and contributor MUST follow these rules.

The goal:

- Maintain clean architecture.
- Keep modules independent.
- Prevent unnecessary complexity.
- Avoid architectural degradation.
- Build a scalable healthcare workforce management platform.

If any requirement is unclear:

STOP and ask for clarification.

Do not guess.

Do not invent.

---

# 2. Technology Stack

Backend:

- ASP.NET Core Web API
- .NET 10
- C#
- Entity Framework Core
- SQL Server

Frontend:

- Angular

Database:

- SQL Server

ORM:

- Entity Framework Core

---

# 3. Architecture Standard

AriaHR MUST follow:

## Modular Monolith Architecture

The project MUST NOT be converted into:

- Microservices
- Distributed services
- Multiple independent applications

The system is one application with isolated business modules.

---

# 4. Business Modules

Current planned modules:
