============================================================
ARIAHR — MASTER ARCHITECTURE INITIALIZATION PROMPT
============================================================
PROJECT INFO
PROJECT: AriaHR

TASK: Initialize and establish the complete Modular Monolith architectural foundation for the AriaHR backend.

ARCHITECTURE: Modular Monolith

TARGET FRAMEWORK: .NET 10

BUSINESS MODULES:

Identity
Organization
Scheduling
Attendance
Requests
Notification
Reporting
MAIN HOST:

AriaHR.API

SHARED PROJECT:

AriaHR.Shared

============================================================
PROJECT CONTEXT (الزامی)
============================================================
This task establishes the initial architectural foundation of AriaHR.

The repository rules are authoritative.

Before making any change, inspect and follow all existing project rules.

Read completely:

00-JULES-GIT-RULES.md
01-ARIAHR-RULES.md
02-MODULE-ARCHITECTURE-RULES.md
ENTITY_RULES.md
If additional project rule files exist in the repository and apply to this task:

Read them.
Follow them.
Do not override them with assumptions.
Repository rules always have priority over this prompt.

Do NOT invent architecture where the repository already defines one.

Do NOT replace existing valid architecture unnecessarily.

============================================================
CONTEXT VALIDATION (الزامی)
============================================================
Before implementation:

Inspect the complete repository structure.
Inspect the Git status.
Inspect the current Git branch.
Inspect the existing solution.
Inspect existing .csproj files.
Inspect existing modules.
Inspect existing API host.
Inspect existing Shared project.
Inspect BaseEntity.
Inspect existing project references.
Inspect existing solution-folder organization.
Inspect existing NuGet packages.
Inspect existing configuration files.
Inspect existing architecture documentation.
Do NOT ask the human questions for decisions that are already defined by:

repository rules
existing code
existing architecture
this prompt
Use:

Repository → Rules → Existing Code → This Prompt

as the decision hierarchy.

If an exact decision already exists in the repository, follow it automatically.

============================================================
REUSE FIRST POLICY (الزامی)
============================================================
For EVERY required component:

FIRST: Search for an existing implementation.

IF IT EXISTS:

Inspect it.
Reuse it.
Preserve valid existing work.
Do NOT recreate it.
Do NOT duplicate it.
Do NOT overwrite unrelated work.
IF IT DOES NOT EXIST:

Create it according to this prompt and repository rules.
This applies to:

AriaHR.sln
AriaHR.API
AriaHR.Shared
BaseEntity
Identity module
Organization module
Scheduling module
Attendance module
Requests module
Notification module
Reporting module
Domain projects
Application projects
Infrastructure projects
API projects
Solution folders
The rule is:

EXISTS → REUSE

DOES NOT EXIST → CREATE

Do NOT ask whether to create something that this prompt already requires.

============================================================
بازبینی قبل از نوشتن — RULE 28
============================================================
Before writing or modifying files, internally determine:

required project structure
exact project names
exact physical paths
exact namespaces
dependency direction
solution-folder structure
existing files to preserve
files that must be created
files that must remain untouched
files that must not be created
Do not ask clarification questions for decisions already established.

Only stop if there is a genuine contradiction between authoritative repository rules that cannot be resolved safely.

============================================================
SOLUTION
============================================================
The repository MUST contain:

AriaHR.sln

at repository root.

If AriaHR.sln exists:

Inspect it.
Reuse it.
Preserve existing valid projects.
Do NOT recreate it.
If AriaHR.sln does NOT exist:

Create it at repository root.

The solution must contain:

AriaHR.API
AriaHR.Shared
all required module projects
============================================================
MAIN API HOST
============================================================
Required project:

AriaHR.API

Physical path:

src/AriaHR.API/

Project file:

src/AriaHR.API/AriaHR.API.csproj

Target:

.NET 10

IF THE PROJECT EXISTS:

Inspect it.
Reuse it.
Preserve valid configuration.
Do NOT recreate it.
IF THE PROJECT DOES NOT EXIST:

Create a standard ASP.NET Core Web API project targeting .NET 10.

The main host is the Composition Root.

At this architecture-foundation stage:

DO NOT create:

business Controllers
business Endpoints
business DTOs
business Services
business Repositories
business Use Cases
business Entities
DbContext
migrations
seed data
business logic
Do NOT add speculative module registrations.

Do NOT create unnecessary module references.

Follow the existing AriaHR host architecture.

============================================================
OPENAPI / SCALAR
============================================================
If the existing project rules require OpenAPI and Scalar:

Use the existing project convention.

Do NOT add Swagger/Swashbuckle if the repository standard explicitly uses:

Microsoft.AspNetCore.OpenApi
Scalar.AspNetCore
If the host is newly created and repository rules specify Scalar:

Configure the host accordingly.

Do NOT add unnecessary API packages.

============================================================
SHARED PROJECT
============================================================
Required project:

AriaHR.Shared

Physical path:

src/Shared/AriaHR.Shared/

Project:

src/Shared/AriaHR.Shared/AriaHR.Shared.csproj

Target:

.NET 10

IF EXISTS:

Inspect.
Reuse.
Preserve.
IF NOT EXISTS:

Create a standard .NET 10 Class Library.

Shared MUST remain minimal.

Do NOT use Shared as a dumping ground.

Do NOT create arbitrary helper classes.

Do NOT create business-specific logic.

Do NOT create:

Controllers
DTOs
Services
Repositories
Use Cases
DbContext
migrations
business module logic
============================================================
BASEENTITY
============================================================
Before creating BaseEntity:

Read:

ENTITY_RULES.md

Inspect the existing BaseEntity.

IF BaseEntity EXISTS:

Reuse it.
Do NOT create a duplicate.
Do NOT modify it unless explicitly required by ENTITY_RULES.md.
IF BaseEntity DOES NOT EXIST:

Create BaseEntity according to ENTITY_RULES.md.

BaseEntity MUST NOT contain business-specific properties.

Do NOT invent BaseEntity properties.

Do NOT duplicate BaseEntity properties inside business Entities.

All Entity implementations must follow ENTITY_RULES.md.

============================================================
MODULE ARCHITECTURE
============================================================
Create or reuse the following modules:

Identity
Organization
Scheduling
Attendance
Requests
Notification
Reporting
Each module MUST have exactly these architectural layers:

Domain
Application
Infrastructure
API
Each project targets:

.NET 10

============================================================
MODULE PROJECT NAMING
============================================================
Use the established AriaHR naming convention:

AriaHR.Modules.[ModuleName].[LayerName]

Examples:

AriaHR.Modules.Identity.Domain AriaHR.Modules.Identity.Application AriaHR.Modules.Identity.Infrastructure AriaHR.Modules.Identity.API

AriaHR.Modules.Organization.Domain AriaHR.Modules.Organization.Application AriaHR.Modules.Organization.Infrastructure AriaHR.Modules.Organization.API

AriaHR.Modules.Scheduling.Domain AriaHR.Modules.Scheduling.Application AriaHR.Modules.Scheduling.Infrastructure AriaHR.Modules.Scheduling.API

AriaHR.Modules.Attendance.Domain AriaHR.Modules.Attendance.Application AriaHR.Modules.Attendance.Infrastructure AriaHR.Modules.Attendance.API

AriaHR.Modules.Requests.Domain AriaHR.Modules.Requests.Application AriaHR.Modules.Requests.Infrastructure AriaHR.Modules.Requests.API

AriaHR.Modules.Notification.Domain AriaHR.Modules.Notification.Application AriaHR.Modules.Notification.Infrastructure AriaHR.Modules.Notification.API

AriaHR.Modules.Reporting.Domain AriaHR.Modules.Reporting.Application AriaHR.Modules.Reporting.Infrastructure AriaHR.Modules.Reporting.API

============================================================
PHYSICAL MODULE STRUCTURE
============================================================
The required structure is:

src/ │ ├── AriaHR.API/ │ ├── Shared/ │ └── AriaHR.Shared/ │ └── Modules/ │ ├── Identity/ │ ├── Domain/ │ ├── Application/ │ ├── Infrastructure/ │ └── API/ │ ├── Organization/ │ ├── Domain/ │ ├── Application/ │ ├── Infrastructure/ │ └── API/ │ ├── Scheduling/ │ ├── Domain/ │ ├── Application/ │ ├── Infrastructure/ │ └── API/ │ ├── Attendance/ │ ├── Domain/ │ ├── Application/ │ ├── Infrastructure/ │ └── API/ │ ├── Requests/ │ ├── Domain/ │ ├── Application/ │ ├── Infrastructure/ │ └── API/ │ ├── Notification/ │ ├── Domain/ │ ├── Application/ │ ├── Infrastructure/ │ └── API/ │ └── Reporting/ ├── Domain/ ├── Application/ ├── Infrastructure/ └── API/

============================================================
IDENTITY MODULE
============================================================
Create or reuse:

AriaHR.Modules.Identity.Domain AriaHR.Modules.Identity.Application AriaHR.Modules.Identity.Infrastructure AriaHR.Modules.Identity.API

Physical path:

src/Modules/Identity/

Architecture only.

Do NOT create business Entities during module initialization unless the task explicitly requests Entities.

Do NOT create:

User Entity
Role Entity
UserRole Entity
authentication implementation
JWT implementation
authorization implementation
password service
login endpoint
registration endpoint
unless explicitly requested by a later task.

Identity is an independent module.

============================================================
ORGANIZATION MODULE
============================================================
Create or reuse:

AriaHR.Modules.Organization.Domain AriaHR.Modules.Organization.Application AriaHR.Modules.Organization.Infrastructure AriaHR.Modules.Organization.API

Physical path:

src/Modules/Organization/

Architecture only.

Do NOT create:

Department Entity
Position Entity
WorkLocation Entity
Employee Entity
unless explicitly requested by a later Entity task.

Do NOT implement business functionality.

============================================================
SCHEDULING MODULE
============================================================
Create or reuse:

AriaHR.Modules.Scheduling.Domain AriaHR.Modules.Scheduling.Application AriaHR.Modules.Scheduling.Infrastructure AriaHR.Modules.Scheduling.API

Physical path:

src/Modules/Scheduling/

Architecture only.

Do NOT create:

Shift Entity
ShiftAssignment Entity
ShiftSwapRequest Entity
unless explicitly requested later.

Do NOT implement scheduling business logic.

============================================================
ATTENDANCE MODULE
============================================================
Create or reuse:

AriaHR.Modules.Attendance.Domain AriaHR.Modules.Attendance.Application AriaHR.Modules.Attendance.Infrastructure AriaHR.Modules.Attendance.API

Physical path:

src/Modules/Attendance/

Architecture only.

Do NOT create:

AttendanceRecord Entity
GPS logic
QR logic
Geofencing logic
Check-in logic
Check-out logic
unless explicitly requested later.

============================================================
REQUESTS MODULE
============================================================
Create or reuse:

AriaHR.Modules.Requests.Domain AriaHR.Modules.Requests.Application AriaHR.Modules.Requests.Infrastructure AriaHR.Modules.Requests.API

Physical path:

src/Modules/Requests/

Architecture only.

Do NOT create:

LeaveRequest Entity
MissionRequest Entity
LeaveType Entity
LeaveBalance Entity
approval workflow
unless explicitly requested later.

============================================================
NOTIFICATION MODULE
============================================================
Create or reuse:

AriaHR.Modules.Notification.Domain AriaHR.Modules.Notification.Application AriaHR.Modules.Notification.Infrastructure AriaHR.Modules.Notification.API

Physical path:

src/Modules/Notification/

Architecture only.

Do NOT create:

Notification Entity
UserDevice Entity
Firebase integration
Push notification service
Email notification service
SMS service
unless explicitly requested later.

============================================================
REPORTING MODULE
============================================================
Create or reuse:

AriaHR.Modules.Reporting.Domain AriaHR.Modules.Reporting.Application AriaHR.Modules.Reporting.Infrastructure AriaHR.Modules.Reporting.API

Physical path:

src/Modules/Reporting/

Architecture only.

Do NOT create:

Report Entity
report DTOs
reporting endpoints
analytics logic
dashboards
queries
unless explicitly requested later.

============================================================
DOMAIN PROJECT RULES
============================================================
Every Domain project:

targets .NET 10
is a Class Library
contains only domain-level code
must remain clean
must not reference Infrastructure
must not reference API
must not reference other business modules
Do NOT create placeholder business code.

Delete:

Class1.cs

or equivalent default template files when they are not required.

Do NOT create fake placeholder classes just to keep folders non-empty.

============================================================
APPLICATION PROJECT RULES
============================================================
Every Application project:

targets .NET 10
references its own Domain project
does not reference Infrastructure
does not reference API
does not reference other business modules
Do NOT create:

DTOs
Use Cases
CQRS
MediatR
handlers
services
unless explicitly required by a later task.

============================================================
INFRASTRUCTURE PROJECT RULES
============================================================
Every Infrastructure project:

targets .NET 10
follows the repository dependency convention
references its own Application project
may reference its own Domain project if required by the established architecture
Do NOT reference other business modules.

Do NOT create:

DbContext
migrations
repositories
EF configurations
database tables
seed data
external integrations
unless explicitly requested by a later task.

Do NOT add NuGet packages unless repository rules require them.

============================================================
API PROJECT RULES
============================================================
Every module API project:

targets .NET 10
follows the existing AriaHR API project convention
references its own Application project where required
Do NOT create:

Controllers
endpoints
DTOs
authentication
authorization
business logic
during architecture initialization.

Delete default template files such as:

Class1.cs

when not required.

============================================================
DEPENDENCY DIRECTION
============================================================
Use the following dependency direction:

Domain ↑ Application ↑ Infrastructure

API ↓ Application

More explicitly:

Application → Domain

Infrastructure → Application

Infrastructure → Domain

API → Application

Do NOT create reverse dependencies.

Do NOT create:

Domain → Application

Domain → Infrastructure

Domain → API

Application → Infrastructure

Application → API

Infrastructure → API

============================================================
CROSS-MODULE DEPENDENCY RULE
============================================================
Business modules MUST remain independent.

Do NOT directly reference:

Identity Organization Scheduling Attendance Requests Notification Reporting

from another business module.

Do NOT create cross-module:

Entity references
navigation properties
foreign keys
repositories
services
direct project references
unless explicitly defined by a later architectural decision.

Do NOT create shared abstractions simply to connect modules.

============================================================
ARIAHR.SHARED DEPENDENCY
============================================================
Do NOT automatically reference:

AriaHR.Shared

from every module.

Only reference AriaHR.Shared when:

ENTITY_RULES.md requires it,
existing repository architecture requires it,
or a specific implementation task explicitly requires it.
Do NOT create unnecessary coupling to Shared.

============================================================
MAIN HOST DEPENDENCY
============================================================
Do NOT automatically modify:

src/AriaHR.API/

Do NOT automatically add references to all module APIs.

Do NOT automatically register all modules.

The main host must remain untouched unless the existing repository architecture explicitly requires host integration at this stage.

If existing valid host references exist:

preserve them
do not remove them without explicit instruction
============================================================
DEFAULT FILE CLEANUP
============================================================
After creating Class Library projects:

Delete default files such as:

Class1.cs

when they are not required.

Do NOT replace them with:

EmptyClass.cs
Placeholder.cs
Dummy.cs
Temp.cs
Architectural folders should be clean.

Only required files should remain.

============================================================
NUGET RULES
============================================================
Do NOT add unnecessary NuGet packages.

Do NOT add:

MediatR
Swashbuckle
AutoMapper
FluentValidation
Serilog
Hangfire
Quartz
MassTransit
RabbitMQ
Redis packages
JWT packages
EF Core packages
unless explicitly required by the existing repository rules or the current task.

Do NOT install packages merely because they may be useful in the future.

============================================================
ENTITY RULE
============================================================
This initialization task does NOT create business Entities.

Business Entities will be implemented later through separate Entity tasks.

When an Entity is eventually created:

read ENTITY_RULES.md
inspect BaseEntity
inherit from BaseEntity if required
use exact property types defined by the rules
do not duplicate BaseEntity properties
create the Entity only in its own module Domain project
create its EF configuration only in its own module Infrastructure project
Do NOT create Entity configurations during this architecture initialization task.

============================================================
SOLUTION FOLDERS
============================================================
Organize AriaHR.sln using the existing repository convention.

Expected structure:

src │ ├── AriaHR.API │ ├── Shared │ └── AriaHR.Shared │ └── Modules ├── Identity │ ├── Domain │ ├── Application │ ├── Infrastructure │ └── API │ ├── Organization │ ├── Domain │ ├── Application │ ├── Infrastructure │ └── API │ ├── Scheduling │ ├── Domain │ ├── Application │ ├── Infrastructure │ └── API │ ├── Attendance │ ├── Domain │ ├── Application │ ├── Infrastructure │ └── API │ ├── Requests │ ├── Domain │ ├── Application │ ├── Infrastructure │ └── API │ ├── Notification │ ├── Domain │ ├── Application │ ├── Infrastructure │ └── API │ └── Reporting ├── Domain ├── Application ├── Infrastructure └── API

If the existing solution organization differs:

REUSE THE EXISTING CONVENTION.

============================================================
STEP-BY-STEP EXECUTION
============================================================
STEP 1 — Repository Inspection
Inspect:

Git
solution
projects
modules
Shared
BaseEntity
rules
package references
Do not modify anything yet.

STEP 2 — Solution
Reuse existing:

AriaHR.sln

or create it if missing.

STEP 3 — Shared
Reuse existing:

AriaHR.Shared

or create it if missing.

Reuse/create BaseEntity according to ENTITY_RULES.md.

STEP 4 — Main API
Reuse existing:

AriaHR.API

or create it if missing.

Do not implement business functionality.

STEP 5 — Identity
Create or reuse:

Domain Application Infrastructure API

STEP 6 — Organization
Create or reuse:

Domain Application Infrastructure API

STEP 7 — Scheduling
Create or reuse:

Domain Application Infrastructure API

STEP 8 — Attendance
Create or reuse:

Domain Application Infrastructure API

STEP 9 — Requests
Create or reuse:

Domain Application Infrastructure API

STEP 10 — Notification
Create or reuse:

Domain Application Infrastructure API

STEP 11 — Reporting
Create or reuse:

Domain Application Infrastructure API

STEP 12 — Solution Registration
Add all missing projects to:

AriaHR.sln

Organize solution folders according to the existing convention.

STEP 13 — Cleanup
Delete unnecessary default files such as:

Class1.cs

Do not create replacement placeholder files.

STEP 14 — Dependency Validation
Verify:

Application → Domain

Infrastructure → Application

Infrastructure → Domain

API → Application

Verify no reverse dependency exists.

Verify no unintended cross-module dependencies exist.

============================================================
OUT OF SCOPE
============================================================
Do NOT create:

business Entities
DTOs
Use Cases
Controllers
API endpoints
repositories
services
DbContext
EF configurations
migrations
database tables
seed data
authentication
authorization
JWT
password hashing
GPS
QR
geofencing
attendance logic
leave workflow
mission workflow
shift logic
notification providers
reporting logic
dashboard logic
payroll
insurance
CQRS
MediatR
event bus
microservices
Angular
UI
new business features
============================================================
BUILD VALIDATION
============================================================
After implementation:

Run:

dotnet restore

Then:

dotnet build

The solution must build successfully.

If the build fails because of an existing unrelated issue:

Do NOT modify unrelated code.
Identify the problem.
Report it as pre-existing.
============================================================
FINAL VALIDATION (MANDATORY)
============================================================
Before finishing verify:

AriaHR.sln exists.

AriaHR.API exists.

AriaHR.Shared exists.

BaseEntity exists according to ENTITY_RULES.md.

Identity module exists.

Organization module exists.

Scheduling module exists.

Attendance module exists.

Requests module exists.

Notification module exists.

Reporting module exists.

Every module contains:

Domain Application Infrastructure API

All projects target .NET 10.

Project names follow:

AriaHR.Modules.[Module].[Layer]

Physical paths follow:

src/Modules/[Module]/[Layer]

Solution folders follow repository convention.

No duplicate projects exist.

No unnecessary Class1.cs files remain.

No business Entities were created.

No DTOs were created.

No Controllers were created.

No DbContext was created.

No migrations were created.

No database tables were created.

No cross-module dependencies were introduced.

No unnecessary NuGet packages were added.

Existing valid work was preserved.

Unrelated modules were not modified.

AriaHR.API was not unnecessarily modified.

dotnet restore succeeds.

dotnet build succeeds.

Final Git diff contains only intended changes.

============================================================
GIT RULES
============================================================
Follow:

00-JULES-GIT-RULES.md

EXACTLY.

The Git rules are authoritative.

Before implementation inspect the current branch.

If the environment automatically creates a generated Jules branch instead of the required branch:

STOP.

Do NOT silently continue.

Do NOT rename or ignore the generated branch unless the Git rules explicitly allow it.

Report the branch mismatch.

If the required branch can be safely created according to the Git rules:

Use the exact branch required by the current task.

Do NOT invent branch names.

Do NOT append random suffixes.

Do NOT create a Pull Request unless the repository rules explicitly require one.

Commit ONLY intended changes.

Use the commit message defined by:

00-JULES-GIT-RULES.md

Do NOT invent a different commit message if the rules define one.

Push ONLY to the corresponding remote branch.

============================================================
TASK COMPLETION
============================================================
The task is complete only when:

architecture exists
solution is valid
project references are valid
default files are cleaned
restore succeeds
build succeeds
Git changes are reviewed
required commit succeeds
required push succeeds
============================================================
REPORT
============================================================
At the end report exactly:

Branch: Commit: Push Status:

Solution: AriaHR.sln

Main Host: AriaHR.API

Shared: AriaHR.Shared

Modules:

Identity
Organization
Scheduling
Attendance
Requests
Notification
Reporting
Projects Created: [List only newly created projects]

Projects Reused: [List existing projects reused]

Project References: [List actual project references]

Solution Structure: [Describe final solution folders]

Entities Created: None

DTOs Created: None

Controllers Created: None

DbContext Created: None

Migrations Created: None

NuGet Packages Added: None unless explicitly required

Files Changed: [Complete changed-file list]

Build Status: [Success/Failure]

Potential Issues: [List only real issues]

============================================================
IMPORTANT FINAL RULE
============================================================
DO NOT ASK CLARIFICATION QUESTIONS FOR DECISIONS ALREADY DEFINED ABOVE.

Do NOT ask:

"Should I create AriaHR.sln?"

The answer is:

YES, if missing. REUSE, if existing.

Do NOT ask:

"Should I create AriaHR.API?"

The answer is:

YES, if missing. REUSE, if existing.

Do NOT ask:

"Should I create AriaHR.Shared?"

The answer is:

YES, if missing. REUSE, if existing.

Do NOT ask:

"Should I create the seven modules?"

The answer is:

YES.

Do NOT ask:

"Should I create four projects per module?"

The answer is:

YES.

Do NOT ask:

"Should I delete Class1.cs?"

The answer is:

YES, if it is unnecessary.

Do NOT ask questions that can be answered by:

repository rules
existing architecture
existing code
this prompt
Only stop for a genuine unresolved contradiction.

FINAL WORKFLOW:

INSPECT → READ RULES → REUSE EXISTING → CREATE MISSING → CLEAN TEMPLATE FILES → VALIDATE REFERENCES → UPDATE SOLUTION → RESTORE → BUILD → REVIEW DIFF → COMMIT → PUSH → REPORT