using AriaHR.Modules.Identity.Application.Common;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Domain.Enums;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AriaHR.Modules.Organization.Infrastructure.Services;

public class OrganizationManagerIdentityService : IOrganizationManagerIdentityService
{
    private readonly OrganizationDbContext _organizationDbContext;
    private readonly IdentityDbContext _identityDbContext;

    public OrganizationManagerIdentityService(
        OrganizationDbContext organizationDbContext,
        IdentityDbContext identityDbContext)
    {
        _organizationDbContext = organizationDbContext ?? throw new ArgumentNullException(nameof(organizationDbContext));
        _identityDbContext = identityDbContext ?? throw new ArgumentNullException(nameof(identityDbContext));
    }

    public async Task<OrganizationDto> CreateOrganizationWithManagerAsync(
        CreateOrganizationRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("نام سازمان الزامی است.", nameof(request.Name));
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("کد سازمان الزامی است.", nameof(request.Code));
        }

        if (!Enum.IsDefined(typeof(OrganizationType), request.Type))
        {
            throw new ArgumentException("نوع سازمان نامعتبر است.", nameof(request.Type));
        }

        if (string.IsNullOrWhiteSpace(request.ManagerFirstName))
        {
            throw new ArgumentException("نام مدیر الزامی است.", nameof(request.ManagerFirstName));
        }

        if (string.IsNullOrWhiteSpace(request.ManagerLastName))
        {
            throw new ArgumentException("نام خانوادگی مدیر الزامی است.", nameof(request.ManagerLastName));
        }

        if (string.IsNullOrWhiteSpace(request.ManagerMobile))
        {
            throw new ArgumentException("شماره موبایل مدیر الزامی است.", nameof(request.ManagerMobile));
        }

        string normalizedMobile = MobileNumberNormalizer.Normalize(request.ManagerMobile);
        if (string.IsNullOrWhiteSpace(normalizedMobile))
        {
            throw new ArgumentException("شماره موبایل مدیر نامعتبر است.", nameof(request.ManagerMobile));
        }

        string? normalizedEmail = null;
        if (!string.IsNullOrWhiteSpace(request.ManagerEmail))
        {
            var trimmedEmail = request.ManagerEmail.Trim();
            var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            int atIdx = trimmedEmail.IndexOf('@');
            if (!emailAttr.IsValid(trimmedEmail) || atIdx <= 0 || atIdx == trimmedEmail.Length - 1 || !trimmedEmail.Substring(atIdx + 1).Contains('.'))
            {
                throw new ArgumentException("فرمت ایمیل مدیر نامعتبر است.", nameof(request.ManagerEmail));
            }
            normalizedEmail = trimmedEmail;
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
            // 1. Resolve CenterManager role
            var centerManagerRole = await _identityDbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "CenterManager", cancellationToken);

            if (centerManagerRole == null)
            {
                throw new InvalidOperationException("Role 'CenterManager' does not exist in the database.");
            }

            // 2. Find existing user with manager mobile and validate Case C before creating Organization
            var existingUser = await _identityDbContext.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedMobile, cancellationToken);

            var orgId = Guid.NewGuid();

            if (existingUser != null && existingUser.OrganizationId.HasValue && existingUser.OrganizationId.Value != orgId)
            {
                throw new ArgumentException("کاربری با این شماره موبایل قبلاً به سازمان دیگری اختصاص یافته است.", nameof(request.ManagerMobile));
            }

            // 3. Create Organization
            var organization = new Domain.Entities.Organization
            {
                Id = orgId,
                Name = request.Name.Trim(),
                Code = request.Code.Trim(),
                OrganizationType = request.Type,
                NationalIdentifier = request.NationalIdentifier?.Trim(),
                Phone = request.Phone?.Trim(),
                Address = request.Address?.Trim(),
                ManagerFirstName = request.ManagerFirstName.Trim(),
                ManagerLastName = request.ManagerLastName.Trim(),
                ManagerMobile = normalizedMobile,
                ManagerEmail = normalizedEmail,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            await _organizationDbContext.Organizations.AddAsync(organization, cancellationToken);
            await _organizationDbContext.SaveChangesAsync(cancellationToken);

            var now = DateTime.UtcNow;

            if (existingUser != null)
            {

                // Case A & B: User has no organization or belongs to this organization
                if (!existingUser.OrganizationId.HasValue)
                {
                    existingUser.OrganizationId = organization.Id;
                }

                if (!string.IsNullOrWhiteSpace(request.ManagerFirstName))
                {
                    existingUser.FirstName = request.ManagerFirstName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.ManagerLastName))
                {
                    existingUser.LastName = request.ManagerLastName.Trim();
                }

                if (normalizedEmail != null)
                {
                    existingUser.Email = normalizedEmail;
                }

                existingUser.IsActive = true;

                // Ensure CenterManager role is assigned
                bool hasRole = await _identityDbContext.UserRoles
                    .AnyAsync(ur => ur.UserId == existingUser.Id && ur.RoleId == centerManagerRole.Id, cancellationToken);

                if (!hasRole)
                {
                    var userRole = new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = existingUser.Id,
                        RoleId = centerManagerRole.Id,
                        CreatedAtUtc = now,
                        CreatedByUserId = createdByUserId
                    };

                    await _identityDbContext.UserRoles.AddAsync(userRole, cancellationToken);
                }
            }
            else
            {
                // Create new User using Manager information
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = request.ManagerFirstName.Trim(),
                    LastName = request.ManagerLastName.Trim(),
                    PhoneNumber = normalizedMobile,
                    Email = normalizedEmail,
                    OrganizationId = organization.Id,
                    IsActive = true,
                    CreatedAtUtc = now,
                    CreatedByUserId = createdByUserId
                };

                await _identityDbContext.Users.AddAsync(user, cancellationToken);

                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = centerManagerRole.Id,
                    CreatedAtUtc = now,
                    CreatedByUserId = createdByUserId
                };

                await _identityDbContext.UserRoles.AddAsync(userRole, cancellationToken);
            }

            await _identityDbContext.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new OrganizationDto
            {
                Id = organization.Id,
                Name = organization.Name,
                Code = organization.Code,
                OrganizationType = organization.OrganizationType,
                NationalIdentifier = organization.NationalIdentifier,
                Phone = organization.Phone,
                Address = organization.Address,
                ManagerFirstName = organization.ManagerFirstName,
                ManagerLastName = organization.ManagerLastName,
                ManagerMobile = organization.ManagerMobile,
                ManagerEmail = organization.ManagerEmail,
                IsActive = organization.IsActive,
                CreatedAtUtc = organization.CreatedAtUtc,
                CreatedByUserId = organization.CreatedByUserId
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
                throw new ArgumentException("کاربری با این شماره موبایل قبلاً ثبت شده است.", nameof(request.ManagerMobile), ex);
            }

            if (ex.InnerException?.Message.Contains("IX_Users_Email") == true ||
                ex.Message.Contains("IX_Users_Email"))
            {
                throw new ArgumentException("این ایمیل قبلاً ثبت شده است.", nameof(request.ManagerEmail), ex);
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
