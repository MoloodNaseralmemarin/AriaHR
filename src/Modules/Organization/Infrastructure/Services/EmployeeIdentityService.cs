using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Application.DTOs;
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

            var now = DateTime.UtcNow;

            // 6. Create User in Identity
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

            // 7. Assign Employee Role
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = employeeRole.Id,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId
            };

            await _identityDbContext.UserRoles.AddAsync(userRole, cancellationToken);

            await _identityDbContext.SaveChangesAsync(cancellationToken);

            // 8. Create Employee linked to User
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
                ProfileImagePath = request.ProfileImagePath?.Trim(),
                IsActive = true,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId
            };

            await _organizationDbContext.Employees.AddAsync(employee, cancellationToken);
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
                PersonnelCode = employee.PersonnelCode,
                NationalCode = employee.NationalCode,
                BirthDate = employee.BirthDate,
                HireDate = employee.HireDate,
                Gender = employee.Gender,
                IsActive = employee.IsActive,
                ProfileImagePath = employee.ProfileImagePath,
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
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
