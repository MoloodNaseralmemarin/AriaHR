using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Helpers;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AriaHR.Modules.Organization.Infrastructure.Services;

public class EmployeeIdentityService : IEmployeeIdentityService
{
    private readonly OrganizationDbContext _organizationDbContext;
    private readonly IdentityDbContext _identityDbContext;

    public EmployeeIdentityService(
        OrganizationDbContext organizationDbContext,
        IdentityDbContext identityDbContext)
    {
        _organizationDbContext = organizationDbContext ?? throw new ArgumentNullException(nameof(organizationDbContext));
        _identityDbContext = identityDbContext ?? throw new ArgumentNullException(nameof(identityDbContext));
    }

    public async Task<EmployeeDto> CreateEmployeeWithUserAsync(
        CreateEmployeeRequest request,
        Guid organizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            throw new ArgumentException("نام الزامی است.", nameof(request.FirstName));
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ArgumentException("نام خانوادگی الزامی است.", nameof(request.LastName));
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw new ArgumentException("شماره موبایل الزامی است.", nameof(request.PhoneNumber));
        }

        string normalizedPhone = MobileNumberNormalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            throw new ArgumentException("شماره موبایل نامعتبر است.", nameof(request.PhoneNumber));
        }

        if (string.IsNullOrWhiteSpace(request.PersonnelCode))
        {
            throw new ArgumentException("کد پرسنلی الزامی است.", nameof(request.PersonnelCode));
        }

        if (string.IsNullOrWhiteSpace(request.NationalCode))
        {
            throw new ArgumentException("کد ملی الزامی است.", nameof(request.NationalCode));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.BirthDate >= today)
        {
            throw new ArgumentException("تاریخ تولد باید در گذشته باشد.", nameof(request.BirthDate));
        }

        if (request.HireDate < request.BirthDate)
        {
            throw new ArgumentException("تاریخ استخدام نمی‌تواند قبل از تاریخ تولد باشد.", nameof(request.HireDate));
        }

        // Validate profile image if provided
        byte[]? profileImageData = null;
        string? profileImageContentType = null;
        string? profileImageFileName = null;

        if (request.ProfileImage != null && request.ProfileImage.Length > 0)
        {
            await ProfileImageValidator.ValidateAsync(request.ProfileImage, cancellationToken);
            using var ms = new MemoryStream();
            await request.ProfileImage.CopyToAsync(ms, cancellationToken);
            profileImageData = ms.ToArray();
            profileImageContentType = request.ProfileImage.ContentType;
            profileImageFileName = Path.GetFileName(request.ProfileImage.FileName);
        }

        // Pre-validations before starting transaction
        // 1. Verify organization existence
        var organizationExists = await _organizationDbContext.Organizations
            .AnyAsync(o => o.Id == organizationId && !o.IsDeleted && o.IsActive, cancellationToken);
        if (!organizationExists)
        {
            throw new ArgumentException("سازمان مورد نظر یافت نشد.");
        }

        // 2. Resolve Employee role in Identity
        var employeeRole = await _identityDbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == "Employee", cancellationToken);
        if (employeeRole == null)
        {
            throw new InvalidOperationException("Role 'Employee' does not exist in the database.");
        }

        // 3. Check for existing user with normalized phone number
        var existingUser = await _identityDbContext.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);
        if (existingUser != null)
        {
            throw new ArgumentException("کاربری با این شماره موبایل قبلاً ثبت شده است.");
        }

        // 4. Validate duplicate NationalCode
        string trimmedNationalCode = request.NationalCode.Trim();
        var nationalCodeExists = await _organizationDbContext.Employees
            .AnyAsync(e => e.NationalCode == trimmedNationalCode && !e.IsDeleted, cancellationToken);
        if (nationalCodeExists)
        {
            throw new ArgumentException("کد ملی وارد شده تکراری است.");
        }

        // 5. Validate duplicate PersonnelCode within organization
        string trimmedPersonnelCode = request.PersonnelCode.Trim();
        var personnelCodeExists = await _organizationDbContext.Employees
            .AnyAsync(e => e.OrganizationId == organizationId && e.PersonnelCode == trimmedPersonnelCode && !e.IsDeleted, cancellationToken);
        if (personnelCodeExists)
        {
            throw new ArgumentException("کد پرسنلی در این سازمان تکراری است.");
        }

        bool isRelational = _organizationDbContext.Database.IsRelational() && _identityDbContext.Database.IsRelational();

        IDbContextTransaction? transaction = null;
        if (isRelational)
        {
            await _organizationDbContext.Database.OpenConnectionAsync(cancellationToken);
            var connection = _organizationDbContext.Database.GetDbConnection();
            transaction = await _organizationDbContext.Database.BeginTransactionAsync(cancellationToken);

            _identityDbContext.Database.SetDbConnection(connection);
            _identityDbContext.Database.UseTransaction(transaction.GetDbTransaction());
        }

        try
        {
            var now = DateTime.UtcNow;

            // Step 1: Create User
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PhoneNumber = normalizedPhone,
                Email = request.Email?.Trim(),
                OrganizationId = organizationId,
                IsActive = true,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId
            };

            await _identityDbContext.Users.AddAsync(user, cancellationToken);

            // Step 2: Assign Employee Role
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = employeeRole.Id,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId
            };

            await _identityDbContext.UserRoles.AddAsync(userRole, cancellationToken);

            // Step 3: Create Employee using generated User.Id
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organizationId,
                PersonnelCode = trimmedPersonnelCode,
                NationalCode = trimmedNationalCode,
                BirthDate = request.BirthDate,
                HireDate = request.HireDate,
                Gender = request.Gender?.Trim(),
                ProfileImage = profileImageData,
                ProfileImageContentType = profileImageContentType,
                ProfileImageFileName = profileImageFileName,
                IsActive = true,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId
            };

            await _organizationDbContext.Employees.AddAsync(employee, cancellationToken);

            // Step 4: Save Changes & Commit Transaction
            await _identityDbContext.SaveChangesAsync(cancellationToken);
            await _organizationDbContext.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new EmployeeDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                OrganizationId = employee.OrganizationId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                PersonnelCode = employee.PersonnelCode,
                NationalCode = employee.NationalCode,
                BirthDate = employee.BirthDate,
                HireDate = employee.HireDate,
                Gender = employee.Gender,
                IsActive = employee.IsActive,
                ProfileImageUrl = employee.ProfileImage != null
                    ? $"/api/organizations/employees/{employee.Id}/profile-image"
                    : null,
                CreatedAtUtc = employee.CreatedAtUtc,
                CreatedByUserId = employee.CreatedByUserId
            };
        }
        catch (DbUpdateException ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            if (ex.InnerException?.Message.Contains("IX_Users_PhoneNumber") == true ||
                ex.Message.Contains("IX_Users_PhoneNumber"))
            {
                throw new ArgumentException("کاربری با این شماره موبایل قبلاً ثبت شده است.", nameof(request.PhoneNumber), ex);
            }

            throw;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (isRelational)
            {
                _identityDbContext.Database.UseTransaction(null);
            }

            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه سازمان الزامی است.", nameof(organizationId));
        }

        var employees = await _organizationDbContext.Employees
            .AsNoTracking()
            .Where(e => e.OrganizationId == organizationId && !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (!employees.Any())
        {
            return Enumerable.Empty<EmployeeDto>();
        }

        var userIds = employees.Select(e => e.UserId).Distinct().ToList();

        var users = await _identityDbContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var result = new List<EmployeeDto>();

        foreach (var employee in employees)
        {
            users.TryGetValue(employee.UserId, out var user);

            result.Add(new EmployeeDto
            {
                Id = employee.Id,
                UserId = employee.UserId,
                OrganizationId = employee.OrganizationId,
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty,
                PhoneNumber = user?.PhoneNumber ?? string.Empty,
                Email = user?.Email,
                PersonnelCode = employee.PersonnelCode,
                NationalCode = employee.NationalCode,
                BirthDate = employee.BirthDate,
                HireDate = employee.HireDate,
                Gender = employee.Gender,
                IsActive = employee.IsActive,
                ProfileImageUrl = employee.ProfileImage != null
                    ? $"/api/organizations/employees/{employee.Id}/profile-image"
                    : null,
                CreatedAtUtc = employee.CreatedAtUtc,
                CreatedByUserId = employee.CreatedByUserId
            });
        }

        return result;
    }

    public async Task<EmployeeProfileImageResult?> GetEmployeeProfileImageAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return null;
        }

        var employee = await _organizationDbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted, cancellationToken);

        if (employee == null || employee.ProfileImage == null || employee.ProfileImage.Length == 0)
        {
            return null;
        }

        return new EmployeeProfileImageResult
        {
            EmployeeId = employee.Id,
            OrganizationId = employee.OrganizationId,
            UserId = employee.UserId,
            ImageBytes = employee.ProfileImage,
            ContentType = string.IsNullOrWhiteSpace(employee.ProfileImageContentType)
                ? "image/jpeg"
                : employee.ProfileImageContentType,
            FileName = employee.ProfileImageFileName
        };
    }
}
