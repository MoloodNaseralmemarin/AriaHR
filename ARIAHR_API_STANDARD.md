# ARIAHR API DEVELOPMENT STANDARD

RULE FILE:
ARIAHR_API_STANDARD.md

RULE STATUS:
MANDATORY

PURPOSE:
This file defines the mandatory standard for creating, modifying, reviewing, and fixing ANY API in the AriaHR project.

Whenever I ask you to:
- Create an API
- Build an API
- Add an endpoint
- Implement a backend feature
- Modify an existing API
- Fix an API
- Add a Controller
- Add a UseCase related to an API

you MUST follow this standard automatically.

DO NOT wait for me to remind you about these rules.

==================================================
1. FIRST RULE — INSPECT BEFORE CODING
==================================================

Before writing or modifying any code, inspect the existing project.

You MUST check:

- Existing Controllers
- Existing UseCases
- Existing DTOs
- Existing Authentication configuration
- Existing Authorization configuration
- JWT configuration
- Current User / Claims mechanism
- Existing Exception Handling
- Existing Middleware
- Existing Validation mechanism
- Existing Repository pattern
- Existing DbContexts
- Existing Module structure
- Existing Dependency Injection registrations
- Existing Scalar/OpenAPI configuration
- Existing API response conventions

If an existing implementation or pattern already exists, REUSE it.

DO NOT create a second implementation of something that already exists.

DO NOT invent a new architecture just to implement one API.

==================================================
2. ARCHITECTURE
==================================================

AriaHR uses Modular Monolith architecture.

Every API MUST belong to the correct Business Module.

The standard architecture is:

Module
├── Domain
├── Application
├── Infrastructure
└── API

The standard API flow is:

HTTP Request
    ↓
Authentication
    ↓
Authorization
    ↓
Controller
    ↓
UseCase
    ↓
Application / Domain
    ↓
Repository
    ↓
Module DbContext
    ↓
Database

DO NOT:

- Create a Global DbContext
- Access DbContext directly from Controller
- Put Business Logic inside Controller
- Bypass the Application layer
- Access another module's Infrastructure directly
- Create duplicate infrastructure
- Change Modular Monolith architecture unnecessarily

==================================================
3. AUTHENTICATION — DEFAULT SECURITY RULE
==================================================

Every API MUST be classified as one of:

1. Anonymous/Public
2. Authenticated/Protected

DEFAULT RULE:

All Business APIs are PROTECTED unless there is an explicit reason for them to be public.

Only genuinely public endpoints may use:

[AllowAnonymous]

Examples of potentially anonymous endpoints:

- POST /api/auth/send-otp
- POST /api/auth/verify-otp

Examples that normally MUST be protected:

- Organizations
- Employees
- WorkLocations
- Shifts
- Attendance
- Leaves
- Requests
- Notifications
- Payroll
- Reporting
- User profile
- Organization management
- Center management
- Branch management

Never make an API anonymous simply because the frontend currently does not send a token.

Backend security is mandatory.

==================================================
4. JWT AUTHENTICATION
==================================================

Protected APIs MUST use the existing JWT Authentication system.

The frontend sends:

Authorization: Bearer <JWT>

Do NOT manually validate JWT tokens inside Controllers.

Do NOT manually decode and trust JWT payloads.

ASP.NET Core Authentication/Authorization middleware is responsible for validating the token.

Protected endpoints should use:

[Authorize]

Anonymous endpoints should use:

[AllowAnonymous]

Do NOT create a second authentication mechanism unless explicitly required.

==================================================
5. CURRENT USER / USER ID
==================================================

When an API operates on the currently authenticated user, NEVER trust UserId supplied by the frontend.

Do NOT use:

- UserId from Request Body
- UserId from Query String
- UserId from Route

when the endpoint is intended to operate on the current authenticated user.

User identity MUST come from the authenticated Claims.

Existing pattern:

var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

if (string.IsNullOrEmpty(userIdClaim) ||
    !Guid.TryParse(userIdClaim, out var userId))
{
    return Unauthorized();
}

If the project already contains:

- ICurrentUserService
- CurrentUserService
- IUserContext
- UserContext
- Equivalent abstraction

then USE the existing abstraction instead of duplicating claim extraction.

There MUST be one consistent Current User mechanism.

==================================================
6. AUTHENTICATION VS AUTHORIZATION
==================================================

Authentication answers:

"Who is the user?"

Authorization answers:

"Is this user allowed to perform this operation?"

A valid JWT does NOT automatically give access to every resource.

Use:

[Authorize]

for authenticated APIs.

Use the existing Role or Permission mechanism when additional authorization is required.

Examples:

[Authorize(Roles = "SystemAdmin")]

or:

[Authorize(Policy = "Organization.Read")]

DO NOT invent a new authorization system if one already exists.

==================================================
7. 401 VS 403
==================================================

This distinction is mandatory.

Return:

401 Unauthorized

when:

- No token exists
- Token is invalid
- Token is expired
- Authentication failed
- User identity cannot be established

Return:

403 Forbidden

when:

- User is authenticated
- But does not have the required Role
- Or does not have the required Permission
- Or is not allowed to perform the requested operation

NEVER confuse 401 and 403.

==================================================
8. OWNERSHIP SECURITY
==================================================

Every protected resource MUST respect ownership.

A user MUST NOT be able to access another user's data simply by changing an ID.

Example:

GET /api/attendance/123

must verify that the authenticated user is authorized to access attendance record 123.

Do NOT rely on:

- Frontend route guards
- Hidden buttons
- Disabled UI
- Frontend role checks

Backend MUST enforce authorization.

==================================================
9. ORGANIZATION / CENTER / BRANCH SCOPE
==================================================

If a resource belongs to:

- Organization
- Center
- Branch
- Department
- Other business scope

the API MUST verify that the authenticated user has access to that scope.

Never trust these values blindly when received from the frontend:

- OrganizationId
- CenterId
- BranchId
- DepartmentId

A user from Organization A MUST NOT be able to access Organization B data by changing an ID.

==================================================
10. CONTROLLER RESPONSIBILITY
==================================================

Controllers MUST remain thin.

Controllers may handle:

- HTTP Request binding
- HTTP-level validation
- Authentication attributes
- Authorization attributes
- Calling UseCases
- Mapping UseCase result to HTTP Response
- Status Codes
- ProblemDetails
- CancellationToken

Controllers MUST NOT contain:

- Business Logic
- Database queries
- EF Core queries
- Direct DbContext access
- Complex calculations
- Business rules
- JWT generation logic
- OTP business logic
- Permission calculation
- Organization access calculation
- Repository implementation

Business Logic belongs in:

- Application
- UseCases
- Domain

==================================================
11. USECASE RULE
==================================================

Business APIs MUST follow the existing UseCase pattern.

Standard:

Controller
    ↓
UseCase
    ↓
Application / Domain
    ↓
Repository
    ↓
DbContext

Do NOT inject DbContext directly into Controller.

Do NOT bypass UseCase.

If the project already has a UseCase for the requested operation, reuse or extend it when appropriate.

==================================================
12. DEPENDENCY INJECTION
==================================================

Use Constructor Injection.

Example:

public AuthController(
    SendOtpUseCase sendOtpUseCase,
    VerifyOtpUseCase verifyOtpUseCase,
    GetCurrentUserUseCase getCurrentUserUseCase,
    IHostEnvironment env)
{
    _sendOtpUseCase = sendOtpUseCase;
    _verifyOtpUseCase = verifyOtpUseCase;
    _getCurrentUserUseCase = getCurrentUserUseCase;
    _env = env;
}

DO NOT:

- Manually instantiate services with new
- Use Service Locator
- Create duplicate service registrations

If a new service is required, verify that its DI registration exists.

==================================================
13. CANCELLATION TOKEN
==================================================

All asynchronous API operations MUST accept CancellationToken.

Example:

public async Task<IActionResult> GetSomething(
    CancellationToken cancellationToken)

The token MUST be passed to the UseCase:

await _useCase.ExecuteAsync(request, cancellationToken);

The token MUST continue through async operations where supported.

Do NOT accept CancellationToken and then ignore it.

==================================================
14. REQUEST VALIDATION
==================================================

Every API request MUST be validated.

Check:

- Null request
- Required fields
- Empty strings
- Invalid formats
- Invalid IDs
- Invalid ranges
- Invalid combinations
- Other request-level constraints

Example:

if (request == null ||
    string.IsNullOrWhiteSpace(request.PhoneNumber))
{
    return BadRequest(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Invalid input",
        Detail = "Phone number is required."
    });
}

Complex Business Validation MUST NOT be placed inside Controller.

Business Validation belongs in Application/Domain.

==================================================
15. DTO RULE
==================================================

Domain Entities MUST NOT be returned directly from API endpoints.

WRONG:

return Ok(employeeEntity);

CORRECT:

return Ok(employeeResponse);

Use:

Request DTO
Response DTO

Do NOT expose:

- Password
- Password Hash
- OTP Hash
- JWT Secret
- Refresh Token unless explicitly required
- Internal infrastructure data
- Database internals
- Sensitive security information

==================================================
16. ERROR HANDLING
==================================================

API errors MUST follow a consistent structure.

Use ProblemDetails for expected HTTP errors.

Example:

return BadRequest(new ProblemDetails
{
    Status = StatusCodes.Status400BadRequest,
    Title = "Invalid input",
    Detail = "..."
});

Avoid inconsistent responses such as:

return BadRequest("error");

unless the existing project-wide convention explicitly requires it.

Unexpected exceptions MUST be handled by the existing Global Exception Handler / Middleware.

DO NOT add repetitive generic try/catch blocks to every Controller.

==================================================
17. HTTP STATUS CODES
==================================================

Use correct HTTP Status Codes.

200 OK
Successful operation with response data.

201 Created
Resource successfully created when appropriate.

204 No Content
Successful operation with no response body.

400 Bad Request
Invalid request or expected validation/business validation failure.

401 Unauthorized
Authentication missing or invalid.

403 Forbidden
Authenticated but not authorized.

404 Not Found
Requested resource does not exist or cannot be accessed according to project policy.

409 Conflict
Duplicate resource or business state conflict.

500 Internal Server Error
Unexpected server error.

Never use Status Codes randomly.

==================================================
18. PRODUCES RESPONSE TYPE
==================================================

Important APIs MUST document their response types.

Example:

[ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]

Only document Status Codes that the endpoint can actually return.

==================================================
19. SCALAR — API DOCUMENTATION STANDARD
==================================================

AriaHR uses SCALAR for API documentation and API testing.

Scalar is the standard API documentation UI of this project.

DO NOT introduce Swagger UI.

DO NOT replace Scalar with Swagger UI.

DO NOT create a second API documentation interface.

OpenAPI may still be used as the specification source.

Architecture:

OpenAPI Specification
        ↓
Scalar
        ↓
API Documentation / Testing

Every new API MUST appear correctly in Scalar.

==================================================
20. SCALAR VERIFICATION
==================================================

After implementing an API, verify that Scalar correctly shows:

- HTTP Method
- Route
- Request DTO
- Request Body
- Response Type
- HTTP Status Codes
- ProblemDetails
- Authentication Requirement
- Bearer Authentication for protected APIs
- Authorization information when supported by the existing configuration

Do NOT modify Scalar configuration unnecessarily.

==================================================
21. ROUTING
==================================================

Use consistent RESTful routing.

Existing Auth convention:

POST /api/auth/send-otp
POST /api/auth/verify-otp
GET  /api/auth/me

Follow the same project convention.

Avoid inconsistent routes such as:

/api/getEmployees
/api/employee-list
/api/GetAllEmployee

Prefer consistent resource-oriented routes.

==================================================
22. HTTP VERBS
==================================================

Use HTTP methods correctly.

GET
Read data.

POST
Create a resource or execute a command/action.

PUT
Full replacement/update.

PATCH
Partial update, only if supported by existing project conventions.

DELETE
Delete resource.

Do not use POST for every operation without a valid reason.

==================================================
23. ASYNC CODE
==================================================

All I/O operations MUST be asynchronous.

Do NOT use:

.Result

.Wait()

Do not block async execution.

==================================================
24. DATABASE ACCESS
==================================================

Controllers MUST NOT access DbContext directly.

WRONG:

_context.Employees.Where(...)

CORRECT:

Controller
    ↓
UseCase
    ↓
Repository / Application Service
    ↓
Module DbContext

Respect the existing per-module DbContext architecture.

Never create a Global DbContext.

==================================================
25. SECURITY REVIEW
==================================================

Before marking any API as complete, verify:

- Authentication
- Authorization
- Ownership
- Organization scope
- Center/Branch scope when applicable
- Input validation
- ID manipulation protection
- Sensitive information protection
- Token handling
- Error information exposure
- Logging security

Frontend authorization MUST NEVER be considered sufficient.

Backend is the final security boundary.

==================================================
26. OTP SECURITY
==================================================

OTP may ONLY be returned in API responses in Development/Test environments if the existing authentication implementation explicitly supports it.

Existing pattern:

if (_env.IsDevelopment())
{
    return Ok(new
    {
        message = "OTP sent successfully",
        otpCode = result.OtpCode
    });
}

Production MUST NOT return OTP codes.

Never log:

- OTP
- JWT
- Password
- Password Hash
- Secret
- Refresh Token

==================================================
27. AUTH API REFERENCE
==================================================

The existing AuthController is the reference implementation for authentication APIs.

Expected endpoints:

POST /api/auth/send-otp
POST /api/auth/verify-otp
GET  /api/auth/me

send-otp:

[AllowAnonymous]

verify-otp:

[AllowAnonymous]

me:

[Authorize]

Authentication flow:

Frontend
    ↓
POST /api/auth/send-otp
    ↓
OTP
    ↓
POST /api/auth/verify-otp
    ↓
JWT Token
    ↓
Frontend sends:
Authorization: Bearer <token>
    ↓
Protected API
    ↓
JWT Authentication
    ↓
Claims
    ↓
Authorization
    ↓
Controller
    ↓
UseCase

All new protected APIs MUST be compatible with this authentication flow.

==================================================
28. NO DUPLICATE INFRASTRUCTURE
==================================================

Before creating any of the following:

- CurrentUser Service
- Authorization Service
- Response Wrapper
- Exception Handler
- Validation Helper
- Authentication Helper
- Security Helper

FIRST search the project.

If an equivalent exists:

REUSE IT.

Do not create a duplicate.

==================================================
29. NO UNNECESSARY CHANGES
==================================================

When implementing an API, modify only what is required.

DO NOT:

- Upgrade .NET
- Upgrade Angular
- Change framework versions
- Change authentication architecture
- Change database architecture
- Change Modular Monolith architecture
- Rename unrelated modules
- Rewrite working infrastructure
- Refactor unrelated code

Avoid scope creep.

==================================================
30. BUILD VERIFICATION
==================================================

After implementation:

1. Build the affected project/module.
2. Fix compilation errors caused by your changes.
3. Verify DI registration.
4. Verify route registration.
5. Verify authentication configuration.
6. Verify authorization configuration.
7. Verify Scalar/OpenAPI generation.
8. Check for obvious runtime issues.

NEVER report an API as complete if the affected code does not compile.

==================================================
31. FINAL SECURITY REVIEW
==================================================

Before declaring DONE, answer:

1. Is the endpoint public or protected?
2. If protected, does it have [Authorize] or the project's equivalent?
3. Is Role/Permission enforced when required?
4. Is UserId taken from authenticated Claims or the existing Current User abstraction?
5. Can a user manipulate an ID to access another user's data?
6. Is Organization/Center/Branch scope enforced?
7. Are all inputs validated?
8. Are sensitive fields protected?
9. Are 401 and 403 used correctly?
10. Are unexpected exceptions handled globally?
11. Is CancellationToken passed through?
12. Does the API appear correctly in Scalar?

If any answer is NO, the API is NOT complete.

==================================================
32. FINAL API CHECKLIST
==================================================

Before declaring an API complete:

[ ] Correct Module
[ ] Correct Controller
[ ] Correct Route
[ ] Correct HTTP Verb
[ ] Authentication reviewed
[ ] [Authorize] added when required
[ ] [AllowAnonymous] used only when justified
[ ] Role/Permission reviewed
[ ] Current User handling reviewed
[ ] Ownership reviewed
[ ] Organization/Center/Branch scope reviewed
[ ] Request DTO created/reused
[ ] Response DTO created/reused
[ ] Validation implemented
[ ] Business validation is outside Controller
[ ] UseCase implemented/reused
[ ] Repository pattern respected
[ ] DbContext architecture respected
[ ] CancellationToken passed
[ ] ProblemDetails used for expected errors
[ ] Correct HTTP Status Codes
[ ] ProducesResponseType added where appropriate
[ ] Sensitive data protected
[ ] Logging does not expose secrets
[ ] Scalar/OpenAPI documentation works
[ ] Bearer Authentication works for protected APIs
[ ] DI registration verified
[ ] Build succeeds
[ ] No unrelated architecture changes

==================================================
33. REQUIRED FINAL REPORT
==================================================

After implementing an API, report:

API:
<HTTP METHOD> <ROUTE>

Authentication:
Anonymous / JWT Required

Authorization:
None / Role / Permission

Current User:
Required / Not Required

Organization Scope:
Required / Not Required

Center/Branch Scope:
Required / Not Required

UseCase:
<UseCase name>

Request DTO:
<DTO name>

Response DTO:
<DTO name>

Validation:
<Summary>

Status Codes:
<List>

Scalar:
Verified / Not Verified

Build:
Passed / Failed

Security Review:
Passed / Issues Found

Potential Issues:
<List or None>

==================================================
34. GOLDEN RULE
==================================================

DO NOT JUST MAKE THE API WORK.

Every API must be:

Functional
+
Secure
+
Authenticated when required
+
Authorized
+
Validated
+
Consistent
+
Architecturally Correct
+
Documented in Scalar
+
Buildable
+
Maintainable

If an API technically works but violates this standard:

IT IS NOT COMPLETE.

==================================================
35. MANDATORY AI AGENT BEHAVIOR
==================================================

Whenever the user asks for an API, the coding agent MUST automatically:

1. Inspect the existing architecture.
2. Inspect the existing AuthController/Auth flow.
3. Inspect JWT configuration.
4. Determine whether the endpoint must be Anonymous or Protected.
5. Determine required Role/Permission.
6. Check Current User implementation.
7. Check Ownership and Organization scope.
8. Reuse existing patterns.
9. Implement DTOs.
10. Implement/reuse UseCase.
11. Implement Controller.
12. Add validation.
13. Add correct authentication/authorization.
14. Add correct response types.
15. Use ProblemDetails for expected errors.
16. Verify Scalar.
17. Build the affected project.
18. Perform the security review.
19. Report the final result.

Do NOT wait for the user to explicitly request these steps.

==================================================
END OF ARIAHR API DEVELOPMENT STANDARD
==================================================