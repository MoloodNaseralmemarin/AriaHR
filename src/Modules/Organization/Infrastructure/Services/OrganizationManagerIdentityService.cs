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
            throw new ArgumentException("Organization Name is required.", nameof(request.Name));
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Organization Code is required.", nameof(request.Code));
        }

        if (!Enum.IsDefined(typeof(OrganizationType), request.Type))
        {
            throw new ArgumentException("Invalid Organization Type.", nameof(request.Type));
        }

        if (string.IsNullOrWhiteSpace(request.ManagerFirstName))
        {
            throw new ArgumentException("Manager First Name is required.", nameof(request.ManagerFirstName));
        }

        if (string.IsNullOrWhiteSpace(request.ManagerLastName))
        {
            throw new ArgumentException("Manager Last Name is required.", nameof(request.ManagerLastName));
        }

        if (string.IsNullOrWhiteSpace(request.ManagerMobile))
        {
            throw new ArgumentException("Manager Mobile is required.", nameof(request.ManagerMobile));
        }

        string normalizedMobile = MobileNumberNormalizer.Normalize(request.ManagerMobile);
        if (string.IsNullOrWhiteSpace(normalizedMobile))
        {
            throw new ArgumentException("Manager Mobile is invalid.", nameof(request.ManagerMobile));
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
            // 1. Check whether a user with the same manager mobile already exists
            bool existingUserWithMobile = await _identityDbContext.Users
                .AnyAsync(u => u.PhoneNumber == normalizedMobile, cancellationToken);

            if (existingUserWithMobile)
            {
                throw new ArgumentException("A user with this mobile number already exists.", nameof(request.ManagerMobile));
            }

            // 2. Resolve CenterManager role
            var centerManagerRole = await _identityDbContext.Roles
                .FirstOrDefaultAsync(r => r.Name == "CenterManager", cancellationToken);

            if (centerManagerRole == null)
            {
                throw new InvalidOperationException("Role 'CenterManager' does not exist in the database.");
            }

            // 3. Create Organization
            var organization = new Domain.Entities.Organization
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Code = request.Code.Trim(),
                Type = request.Type,
                NationalIdentifier = request.NationalIdentifier?.Trim(),
                Phone = request.Phone?.Trim(),
                Address = request.Address?.Trim(),
                ManagerFirstName = request.ManagerFirstName.Trim(),
                ManagerLastName = request.ManagerLastName.Trim(),
                ManagerMobile = normalizedMobile,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            await _organizationDbContext.Organizations.AddAsync(organization, cancellationToken);
            await _organizationDbContext.SaveChangesAsync(cancellationToken);

            // 4. Create User using Manager information
            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.ManagerFirstName.Trim(),
                LastName = request.ManagerLastName.Trim(),
                PhoneNumber = normalizedMobile,
                Email = request.ManagerEmail,
                IsActive = true,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId
            };

            await _identityDbContext.Users.AddAsync(user, cancellationToken);

            // 5. Create UserRole with CenterManager role
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = centerManagerRole.Id,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId
            };

            await _identityDbContext.UserRoles.AddAsync(userRole, cancellationToken);

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
                Type = organization.Type,
                NationalIdentifier = organization.NationalIdentifier,
                Phone = organization.Phone,
                Address = organization.Address,
                ManagerFirstName = organization.ManagerFirstName,
                ManagerLastName = organization.ManagerLastName,
                ManagerMobile = organization.ManagerMobile,
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
                throw new ArgumentException("A user with this mobile number already exists.", nameof(request.ManagerMobile), ex);
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
