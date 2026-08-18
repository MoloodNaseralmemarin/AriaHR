# AriaHR — Module Architecture Rules

**Version:** 1.0
**Architecture:** Modular Monolith

These rules define the required architecture and dependency boundaries for every business module in AriaHR.

Before implementing, modifying, or extending any business module, Jules MUST read and follow this document.

If an architectural requirement is unclear:

**STOP and ask for clarification.**

Do not guess or invent architectural decisions.

---

# 1. Module Structure

Every business module MUST be independently organized using the following structure:

```text
Module/
├── Domain/
├── Application/
├── Infrastructure/
└── API/
```

Each layer MUST be a separate .NET project when the module is implemented.

The target framework MUST follow the project's approved technology version.

Do NOT create additional architectural layers or projects unless explicitly required by the task or existing architecture.

---

# 2. Domain Layer

The Domain layer contains the core business model and domain rules.

It MAY contain:

* Entities
* Aggregates
* Value Objects
* Domain Rules
* Domain Services
* Domain Events
* Domain Interfaces

The Domain layer MUST NOT contain:

* Database code
* Entity Framework Core configuration
* DbContext
* DbSet
* Repositories implementations
* API code
* HTTP code
* Controllers
* DTOs
* Infrastructure implementations

The Domain layer MUST remain independent from Application, Infrastructure, and API.

---

# 3. Application Layer

The Application layer contains application-level behavior and contracts.

It MAY contain:

* Use Cases
* Application Services
* DTOs
* Commands
* Queries
* Handlers
* Application Interfaces
* Application Contracts

The Application layer MUST NOT contain:

* EF Core implementations
* DbContext
* Database-specific code
* Repository implementations
* Controllers
* HTTP-specific implementations
* Infrastructure implementations

The Application layer MAY depend on Domain.

The Application layer MUST NOT depend on Infrastructure.

---

# 4. Infrastructure Layer

The Infrastructure layer contains technical implementations.

It MAY contain:

* EF Core configuration
* DbContext
* DbSets
* Repository implementations
* Database access
* External service integrations
* Infrastructure implementations of Application or Domain interfaces

Infrastructure MAY reference:

* Domain
* Application

Infrastructure MUST NOT be referenced by Domain.

Infrastructure MUST NOT be referenced by Application.

---

# 5. API Layer

The API layer represents the external HTTP boundary of the module.

It MAY contain:

* Controllers
* HTTP endpoints
* Request/response configuration
* API-specific configuration

API MAY reference:

* Application

API MUST NOT directly depend on Infrastructure unless explicitly required by the application's composition root or approved architecture.

API MUST NOT bypass Application to perform business operations directly against Domain entities.

API MUST NOT contain business logic.

---

# 6. Dependency Direction

The standard dependency direction is:

```text
API
 ↓
Application
 ↓
Domain
```

Infrastructure is positioned outside the core dependency flow:

```text
Infrastructure
 ↓
Application
 ↓
Domain
```

Infrastructure MAY also reference Domain directly when required.

The following dependencies are FORBIDDEN:

```text
Domain → Application
Domain → Infrastructure
Domain → API

Application → Infrastructure
Application → API

Domain → API

Infrastructure → API
```

Do NOT introduce circular dependencies.

Keep the dependency graph minimal.

---

# 7. Module Isolation

Every business module MUST remain isolated from other business modules.

A module MUST NOT directly access another module's:

* Entities
* Aggregates
* DbContext
* DbSets
* Repositories
* Infrastructure
* Internal services
* Internal application implementations

For example:

Employee MUST NOT directly reference or access:

* Attendance entities
* Shift entities
* ShiftAssignment entities
* Leave entities
* Notification entities
* Dashboard entities

The same isolation rule applies in the opposite direction.

No business module may bypass another module's architectural boundary.

---

# 8. Cross-Module Communication

Cross-module communication is NOT allowed by default.

If modules need to communicate, the communication mechanism MUST be explicitly defined by the approved architecture or explicitly requested by the task.

Possible mechanisms may include:

* Explicit contracts
* Public application contracts
* Approved integration events
* Other explicitly approved mechanisms

Do NOT invent or introduce a cross-module communication mechanism during a module architecture foundation task.

Do NOT create cross-module references merely because they may be useful in the future.

---

# 9. Shared Project

`AriaHR.Shared` is reserved for genuinely cross-cutting functionality.

Shared MUST NOT contain:

* Business entities belonging to a module
* Module-specific business logic
* Module-specific services
* Module-specific repositories
* Module-specific DTOs
* Module-specific database configuration

Do NOT move code into Shared merely to bypass module boundaries.

Do NOT create generic abstractions solely for the purpose of increasing reuse.

Existing shared foundation components may be used when permitted by the architecture.

---

# 10. Module Ownership

Each module owns its own business model.

A module MUST own:

* Its domain entities
* Its domain rules
* Its application behavior
* Its infrastructure implementation
* Its database-related implementation

One module MUST NOT directly manage another module's domain objects or persistence.

Do NOT duplicate another module's entities.

Do NOT create copies of another module's domain model to bypass dependency rules.

---

# 11. Module Creation

When creating a new module:

* Create only the required module layers.
* Follow the standard module structure.
* Establish only the permitted project references.
* Keep the dependency graph minimal.
* Do not create business functionality unless explicitly requested.
* Do not create cross-module references.
* Do not introduce unnecessary abstractions.

The module must be architecturally valid even if its business functionality has not yet been implemented.

---

# 12. Architectural Foundation Tasks

If a task explicitly requests only the architectural foundation of a module:

DO NOT automatically create:

* Entities
* Aggregates
* Value Objects
* Domain Services
* Domain Events
* DTOs
* Use Cases
* Commands
* Queries
* Handlers
* Controllers
* Endpoints
* Repositories
* DbContext
* DbSets
* Entity configurations
* Database tables
* Migrations
* External integrations

Create only the projects, namespaces, and permitted references required to establish the module boundary.

---

# 13. Project References

Every project reference MUST have an architectural reason.

Before adding a project reference, verify that it is permitted by the dependency rules.

Forbidden cross-module project references MUST NOT be added.

Do NOT add a reference simply because another module already contains functionality that may be useful.

---

# 14. Architectural Validation

After creating or modifying a module, verify:

1. The module contains the required layers.
2. Each layer has the correct project references.
3. No forbidden dependency exists.
4. No circular dependency exists.
5. No cross-module reference exists without explicit approval.
6. The module does not access another module's internal implementation.
7. Shared is not being used to bypass module isolation.
8. The dependency graph remains minimal.

The module is considered architecturally valid only when all of the above conditions are satisfied.

---

# 15. Final Principle

The purpose of module architecture is to maintain:

* Strong module boundaries
* Clear ownership
* Low coupling
* High cohesion
* Independent business domains
* Maintainable dependencies
* Scalable architecture

Prefer the simplest valid architecture.

Do not introduce dependencies, abstractions, communication mechanisms, or architectural patterns without a clear requirement.
