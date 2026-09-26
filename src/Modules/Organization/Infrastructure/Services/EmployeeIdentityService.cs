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
            throw new ArgumentException("شناسه سازمان الزامی است.");
        }

        if (string.IsNullOrWhiteSpace(request.PersonnelCode))
        {
            throw new ArgumentException("کد پرسنلی الزامی است.");
        }

        if (string.IsNullOrWhiteSpace(request.NationalCode))
        {
            throw new ArgumentException("کد ملی الزامی است.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.BirthDate >= today)
        {
            throw new ArgumentException("تاریخ تولد باید در گذشته باشد.");
        }

        if (request.HireDate < request.BirthDate)
        {
            throw new ArgumentException("تاریخ استخدام نمی‌تواند قبل از تاریخ تولد باشد.");
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
            // 1. Verify Organization exists
            var organizationExists = await _organizationDbContext.Organizations
                .AsNoTracking()
                .AnyAsync(o => !o.IsDeleted && o.IsActive && o.Id == organizationId, cancellationToken);

            if (!organizationExists)
            {
                throw new ArgumentException("سازمان مورد نظر یافت نشد.");
            }

            // 2. Check duplicate Employee rules
            var nationalCodeExists = await _organizationDbContext.Employees
                .AsNoTracking()
                .AnyAsync(e => !e.IsDeleted && e.NationalCode == request.NationalCode.Trim(), cancellationToken);

            if (nationalCodeExists)
            {
                throw new ArgumentException("کد ملی وارد شده تکراری است.");
            }

            var personnelCodeExists = await _organizationDbContext.Employees
                .AsNoTracking()
                .AnyAsync(e => !e.IsDeleted && e.OrganizationId == organizationId && e.PersonnelCode == request.PersonnelCode.Trim(), cancellationToken);

            if (personnelCodeExists)
            {
                throw new ArgumentException("کد پرسنلی در این سازمان تکراری است.");
            }

            // 3. Resolve Employee role from Identity
            var employeeRole = await _identityDbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "Employee", cancellationToken);

            if (employeeRole == null)
            {
                throw new InvalidOperationException("نقش 'Employee' در پایگاه داده یافت نشد.");
            }

            User? user = null;
            var now = DateTime.UtcNow;

            // 4. User resolution/creation in Identity
            if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
            {
                user = await _identityDbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == request.UserId.Value, cancellationToken);

                if (user == null || user.IsDeleted || !user.IsActive)
                {
                    throw new ArgumentException("کاربر مورد نظر یافت نشد.");
                }

                if (user.OrganizationId.HasValue && user.OrganizationId.Value != Guid.Empty && user.OrganizationId.Value != organizationId)
                {
                    throw new ArgumentException("کاربر به سازمان دیگری تعلق دارد.");
                }

                if (!user.OrganizationId.HasValue)
                {
                    user.OrganizationId = organizationId;
                }
            }
            else
            {
                string? normalizedPhone = null;
                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    normalizedPhone = MobileNumberNormalizer.Normalize(request.PhoneNumber);
                }

                if (!string.IsNullOrWhiteSpace(normalizedPhone))
                {
                    user = await _identityDbContext.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone, cancellationToken);
                }

                if (user != null)
                {
                    if (user.OrganizationId.HasValue && user.OrganizationId.Value != Guid.Empty && user.OrganizationId.Value != organizationId)
                    {
                        throw new ArgumentException("کاربر به سازمان دیگری تعلق دارد.");
                    }

                    if (!user.OrganizationId.HasValue)
                    {
                        user.OrganizationId = organizationId;
                    }

                    if (!string.IsNullOrWhiteSpace(request.FirstName))
                    {
                        user.FirstName = request.FirstName.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(request.LastName))
                    {
                        user.LastName = request.LastName.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(request.Email))
                    {
                        user.Email = request.Email.Trim();
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(request.FirstName) ||
                        string.IsNullOrWhiteSpace(request.LastName) ||
                        string.IsNullOrWhiteSpace(request.PhoneNumber))
                    {
                        throw new ArgumentException("اطلاعات کاربر (نام، نام خانوادگی و شماره موبایل) الزامی است.");
                    }

                    if (string.IsNullOrWhiteSpace(normalizedPhone))
                    {
                        normalizedPhone = MobileNumberNormalizer.Normalize(request.PhoneNumber);
                    }

                    if (string.IsNullOrWhiteSpace(normalizedPhone))
                    {
                        throw new ArgumentException("شماره موبایل وارد شده معتبر نمی‌باشد.");
                    }

                    user = new User
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
                }
            }

            // Assign Employee role to User if not present
            bool hasEmployeeRole = await _identityDbContext.UserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == employeeRole.Id, cancellationToken);

            if (!hasEmployeeRole)
            {
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = employeeRole.Id,
                    CreatedAtUtc = now,
                    CreatedByUserId = createdByUserId
                };

                await _identityDbContext.UserRoles.AddAsync(userRole, cancellationToken);
            }

            await _identityDbContext.SaveChangesAsync(cancellationToken);

            // 5. Verify User is not already linked to an Employee
            var userAlreadyLinked = await _organizationDbContext.Employees
                .AsNoTracking()
                .AnyAsync(e => !e.IsDeleted && e.IsActive && e.UserId == user.Id, cancellationToken);

            if (userAlreadyLinked)
            {
                throw new ArgumentException("این کاربر قبلاً به عنوان کارمند ثبت شده است.");
            }

            // 6. Create Employee in Organization DbContext
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organizationId,
                PersonnelCode = request.PersonnelCode.Trim(),
                NationalCode = request.NationalCode.Trim(),
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
                throw new ArgumentException("کاربری با این شماره موبایل قبلاً ثبت شده است.");
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
