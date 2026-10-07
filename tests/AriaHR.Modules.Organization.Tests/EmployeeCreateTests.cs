using System.Data.Common;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using AriaHR.Modules.Identity.Domain.Entities;
using AriaHR.Modules.Identity.Infrastructure.Persistence;
using AriaHR.Modules.Organization.API.Controllers;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;
using AriaHR.Modules.Organization.Application.UseCases.GetEmployees;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Infrastructure.Persistence;
using AriaHR.Modules.Organization.Infrastructure.Services;
using AriaHR.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class EmployeeCreateTests
{
    private (OrganizationDbContext orgDb, IdentityDbContext identityDb) GetInMemoryDbContexts()
    {
        string dbName = Guid.NewGuid().ToString();

        var orgOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var orgDb = new OrganizationDbContext(orgOptions);
        var identityDb = new IdentityDbContext(identityOptions);

        return (orgDb, identityDb);
    }

    private (OrganizationDbContext orgDb, IdentityDbContext identityDb, DbConnection connection) GetSqliteDbContexts()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var orgOptions = new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseSqlite(connection)
            .Options;

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;

        var orgDb = new OrganizationDbContext(orgOptions);
        var identityDb = new IdentityDbContext(identityOptions);

        var orgCreator = orgDb.Database.GetService<IRelationalDatabaseCreator>();
        orgCreator.CreateTables();

        var identityCreator = identityDb.Database.GetService<IRelationalDatabaseCreator>();
        identityCreator.CreateTables();

        return (orgDb, identityDb, connection);
    }

    private async Task SeedEmployeeRoleAsync(IdentityDbContext identityDb)
    {
        if (!await identityDb.Roles.AnyAsync(r => r.Name == "Employee"))
        {
            identityDb.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                Name = "Employee",
                Description = "Employee Role",
                CreatedAtUtc = DateTime.UtcNow
            });
            await identityDb.SaveChangesAsync();
        }
    }

    private IFormFile CreateMockFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "ProfileImage", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static readonly byte[] JpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
    private static readonly byte[] PngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
    private static readonly byte[] WebpBytes = new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P' };

    [Fact]
    public async Task ExecuteAsync_WithValidRequestWithoutImage_CreatesUserAndEmployeeSuccessfully()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "09120001122",
            Email = "john.doe@example.com",
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10),
            Gender = "Male",
            ProfileImage = null
        };

        var creatorId = Guid.NewGuid();

        // Act
        var result = await useCase.ExecuteAsync(request, orgId, creatorId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Equal("EMP-001", result.PersonnelCode);
        Assert.Equal("1234567890", result.NationalCode);
        Assert.Equal(new DateOnly(1990, 5, 15), result.BirthDate);
        Assert.Equal(new DateOnly(2022, 1, 10), result.HireDate);
        Assert.Equal("Male", result.Gender);
        Assert.True(result.IsActive);
        Assert.Null(result.ProfileImageUrl);
        Assert.Equal(creatorId, result.CreatedByUserId);

        // Verify Employee saved in Organization database with NULL image fields
        var dbEmployee = await orgDb.Employees.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(dbEmployee);
        Assert.Null(dbEmployee.ProfileImage);
        Assert.Null(dbEmployee.ProfileImageContentType);
        Assert.Null(dbEmployee.ProfileImageFileName);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyProfileImageFile_StoresNullAndReturnsNullProfileImageUrl()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var emptyFile = CreateMockFormFile(Array.Empty<byte>(), "empty.jpg", "image/jpeg");

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Empty",
            PhoneNumber = "09120001199",
            Email = "john.empty@example.com",
            PersonnelCode = "EMP-EMPTY",
            NationalCode = "1234567899",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10),
            Gender = "Male",
            ProfileImage = emptyFile
        };

        var creatorId = Guid.NewGuid();

        // Act
        var result = await useCase.ExecuteAsync(request, orgId, creatorId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ProfileImageUrl);

        var dbEmployee = await orgDb.Employees.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(dbEmployee);
        Assert.Null(dbEmployee.ProfileImage);
        Assert.Null(dbEmployee.ProfileImageContentType);
        Assert.Null(dbEmployee.ProfileImageFileName);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidJpegImage_StoresBinaryDataAndReturnsProfileImageUrl()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var file = CreateMockFormFile(JpegBytes, "avatar.jpeg", "image/jpeg");

        var request = new CreateEmployeeRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "09120001133",
            PersonnelCode = "EMP-JPEG",
            NationalCode = "1234567891",
            BirthDate = new DateOnly(1991, 1, 1),
            HireDate = new DateOnly(2021, 1, 1),
            ProfileImage = file
        };

        // Act
        var result = await identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid());

        // Assert
        Assert.NotNull(result.ProfileImageUrl);
        Assert.Equal($"/api/organizations/employees/{result.Id}/profile-image", result.ProfileImageUrl);

        var dbEmployee = await orgDb.Employees.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(dbEmployee);
        Assert.Equal(JpegBytes, dbEmployee.ProfileImage);
        Assert.Equal("image/jpeg", dbEmployee.ProfileImageContentType);
        Assert.Equal("avatar.jpeg", dbEmployee.ProfileImageFileName);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidPngImage_StoresBinaryDataSuccessfully()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var file = CreateMockFormFile(PngBytes, "avatar.png", "image/png");

        var request = new CreateEmployeeRequest
        {
            FirstName = "Mark",
            LastName = "Png",
            PhoneNumber = "09120001144",
            PersonnelCode = "EMP-PNG",
            NationalCode = "1234567892",
            BirthDate = new DateOnly(1992, 2, 2),
            HireDate = new DateOnly(2022, 2, 2),
            ProfileImage = file
        };

        // Act
        var result = await identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid());

        // Assert
        Assert.NotNull(result.ProfileImageUrl);
        var dbEmployee = await orgDb.Employees.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(dbEmployee);
        Assert.Equal(PngBytes, dbEmployee.ProfileImage);
        Assert.Equal("image/png", dbEmployee.ProfileImageContentType);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidWebPImage_StoresBinaryDataSuccessfully()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var file = CreateMockFormFile(WebpBytes, "avatar.webp", "image/webp");

        var request = new CreateEmployeeRequest
        {
            FirstName = "Sarah",
            LastName = "Webp",
            PhoneNumber = "09120001155",
            PersonnelCode = "EMP-WEBP",
            NationalCode = "1234567893",
            BirthDate = new DateOnly(1993, 3, 3),
            HireDate = new DateOnly(2023, 3, 3),
            ProfileImage = file
        };

        // Act
        var result = await identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid());

        // Assert
        Assert.NotNull(result.ProfileImageUrl);
        var dbEmployee = await orgDb.Employees.FirstOrDefaultAsync(e => e.Id == result.Id);
        Assert.NotNull(dbEmployee);
        Assert.Equal(WebpBytes, dbEmployee.ProfileImage);
        Assert.Equal("image/webp", dbEmployee.ProfileImageContentType);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnsupportedExtension_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var file = CreateMockFormFile(Encoding.UTF8.GetBytes("pdf content"), "doc.pdf", "application/pdf");

        var request = new CreateEmployeeRequest
        {
            FirstName = "Bad",
            LastName = "File",
            PhoneNumber = "09120001166",
            PersonnelCode = "EMP-BAD",
            NationalCode = "1234567894",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            ProfileImage = file
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("فرمت فایل تصویری پشتیبانی نمی‌شود", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFileExceeding2MB_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        byte[] largeBytes = new byte[2 * 1024 * 1024 + 10]; // Exceeds 2MB
        Array.Copy(JpegBytes, largeBytes, JpegBytes.Length);

        var file = CreateMockFormFile(largeBytes, "large.jpg", "image/jpeg");

        var request = new CreateEmployeeRequest
        {
            FirstName = "Large",
            LastName = "File",
            PhoneNumber = "09120001177",
            PersonnelCode = "EMP-LARGE",
            NationalCode = "1234567895",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            ProfileImage = file
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("بیشتر از ۲ مگابایت", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFakeExtensionWrongMagicBytes_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var fakeBytes = Encoding.UTF8.GetBytes("This is plain text pretending to be a JPG");
        var file = CreateMockFormFile(fakeBytes, "fake.jpg", "image/jpeg");

        var request = new CreateEmployeeRequest
        {
            FirstName = "Fake",
            LastName = "Jpg",
            PhoneNumber = "09120001188",
            PersonnelCode = "EMP-FAKE",
            NationalCode = "1234567896",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            ProfileImage = file
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("محتوای فایل ارسالی با فرمت تصویر مطابقت ندارد", ex.Message);
    }

    [Fact]
    public async Task Controller_GetProfileImage_ReturnsFileResultWithCorrectBytesAndContentType()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Hospital",
            Code = "H1",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var file = CreateMockFormFile(JpegBytes, "myavatar.jpg", "image/jpeg");

        var request = new CreateEmployeeRequest
        {
            FirstName = "Profile",
            LastName = "User",
            PhoneNumber = "09125554433",
            PersonnelCode = "P-IMG-1",
            NationalCode = "8877665544",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            ProfileImage = file
        };

        var createdEmp = await identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid());

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", orgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var createUseCase = new CreateEmployeeUseCase(identityService);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);

        var controller = new EmployeesController(createUseCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetProfileImage(createdEmp.Id, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal(JpegBytes, fileResult.FileContents);
        Assert.Equal("myavatar.jpg", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task Controller_GetProfileImage_WhenEmployeeHasNoImage_ReturnsNotFound()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Hospital",
            Code = "H1",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var request = new CreateEmployeeRequest
        {
            FirstName = "NoImg",
            LastName = "User",
            PhoneNumber = "09125554444",
            PersonnelCode = "P-NOIMG",
            NationalCode = "8877665555",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1)
        };

        var createdEmp = await identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid());

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", orgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var createUseCase = new CreateEmployeeUseCase(identityService);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);

        var controller = new EmployeesController(createUseCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetProfileImage(createdEmp.Id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task Controller_GetProfileImage_CenterManagerCrossOrganization_ReturnsForbid()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var targetOrgId = Guid.NewGuid();
        var myManagerOrgId = Guid.NewGuid();

        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = targetOrgId,
            Name = "Target Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var file = CreateMockFormFile(PngBytes, "avatar.png", "image/png");

        var request = new CreateEmployeeRequest
        {
            FirstName = "Target",
            LastName = "Emp",
            PhoneNumber = "09127778899",
            PersonnelCode = "P-TARGET",
            NationalCode = "7766554433",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            ProfileImage = file
        };

        var createdEmp = await identityService.CreateEmployeeWithUserAsync(request, targetOrgId, Guid.NewGuid());

        // CenterManager belonging to myManagerOrgId attempting to access targetOrgId's employee image
        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", myManagerOrgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var createUseCase = new CreateEmployeeUseCase(identityService);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);

        var controller = new EmployeesController(createUseCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        // Act
        var actionResult = await controller.GetProfileImage(createdEmp.Id, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Sqlite_Success_PersistsUserUserRoleAndEmployeeWithMatchingUserId()
    {
        // Arrange
        var (orgDb, identityDb, connection) = GetSqliteDbContexts();
        try
        {
            await SeedEmployeeRoleAsync(identityDb);

            var orgId = Guid.NewGuid();
            orgDb.Organizations.Add(new Domain.Entities.Organization
            {
                Id = orgId,
                Name = "Sqlite Hospital",
                Code = "SQ-01",
                Type = OrganizationType.Clinic,
                IsActive = true
            });
            await orgDb.SaveChangesAsync();

            var identityService = new EmployeeIdentityService(orgDb, identityDb);
            var request = new CreateEmployeeRequest
            {
                FirstName = "Alice",
                LastName = "Smith",
                PhoneNumber = "09123334455",
                Email = "alice@example.com",
                PersonnelCode = "EMP-SQL-1",
                NationalCode = "0011223344",
                BirthDate = new DateOnly(1992, 3, 10),
                HireDate = new DateOnly(2021, 6, 1)
            };

            // Act
            var result = await identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            var createdUser = await identityDb.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == result.UserId);
            var createdUserRole = await identityDb.UserRoles.AsNoTracking().FirstOrDefaultAsync(ur => ur.UserId == result.UserId);
            var createdEmployee = await orgDb.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == result.UserId);

            Assert.NotNull(createdUser);
            Assert.NotNull(createdUserRole);
            Assert.NotNull(createdEmployee);

            Assert.Equal(createdUser.Id, createdUserRole.UserId);
            Assert.Equal(createdUser.Id, createdEmployee.UserId);
            var empRole = await identityDb.Roles.FirstAsync(r => r.Name == "Employee");
            Assert.Equal(empRole.Id, createdUserRole.RoleId);
        }
        finally
        {
            await orgDb.DisposeAsync();
            await identityDb.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Sqlite_FailureScenario_MissingEmployeeRole_RollsBackAll()
    {
        // Arrange
        var (orgDb, identityDb, connection) = GetSqliteDbContexts();
        try
        {
            // Do NOT seed Employee Role

            var orgId = Guid.NewGuid();
            orgDb.Organizations.Add(new Domain.Entities.Organization
            {
                Id = orgId,
                Name = "Sqlite Hospital",
                Code = "SQ-01",
                Type = OrganizationType.Clinic,
                IsActive = true
            });
            await orgDb.SaveChangesAsync();

            var identityService = new EmployeeIdentityService(orgDb, identityDb);
            var request = new CreateEmployeeRequest
            {
                FirstName = "NoRole",
                LastName = "User",
                PhoneNumber = "09129998877",
                PersonnelCode = "EMP-SQL-NOROLE",
                NationalCode = "9988776655",
                BirthDate = new DateOnly(1990, 1, 1),
                HireDate = new DateOnly(2020, 1, 1)
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid()));

            Assert.Contains("Employee", ex.Message);

            // Verify database state: 0 Users, 0 UserRoles, 0 Employees
            Assert.Equal(0, await identityDb.Users.CountAsync());
            Assert.Equal(0, await identityDb.UserRoles.CountAsync());
            Assert.Equal(0, await orgDb.Employees.CountAsync());
        }
        finally
        {
            await orgDb.DisposeAsync();
            await identityDb.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Sqlite_FailureScenario_EmployeeSaveFails_RollsBackUserAndUserRole()
    {
        // Arrange
        var (orgDb, identityDb, connection) = GetSqliteDbContexts();
        try
        {
            await SeedEmployeeRoleAsync(identityDb);

            var orgId = Guid.NewGuid();
            orgDb.Organizations.Add(new Domain.Entities.Organization
            {
                Id = orgId,
                Name = "Sqlite Hospital",
                Code = "SQ-01",
                Type = OrganizationType.Clinic,
                IsActive = true
            });

            // Pre-insert an Employee with NationalCode '1111111111'
            orgDb.Employees.Add(new Employee
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                OrganizationId = orgId,
                PersonnelCode = "EMP-PREV",
                NationalCode = "1111111111",
                BirthDate = new DateOnly(1980, 1, 1),
                HireDate = new DateOnly(2000, 1, 1),
                IsActive = true
            });
            await orgDb.SaveChangesAsync();

            var identityService = new EmployeeIdentityService(orgDb, identityDb);
            var file = CreateMockFormFile(JpegBytes, "fail.jpeg", "image/jpeg");

            var request = new CreateEmployeeRequest
            {
                FirstName = "FailEmp",
                LastName = "Test",
                PhoneNumber = "09121110099",
                PersonnelCode = "EMP-UNIQUE-CODE",
                NationalCode = "1111111111", // Duplicate NationalCode
                BirthDate = new DateOnly(1990, 1, 1),
                HireDate = new DateOnly(2020, 1, 1),
                ProfileImage = file
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                identityService.CreateEmployeeWithUserAsync(request, orgId, Guid.NewGuid()));

            // Verify no user or user role remained for "FailEmp" and no orphaned employee image data
            Assert.False(await identityDb.Users.AnyAsync(u => u.PhoneNumber == "09121110099"));
            Assert.Equal(0, await identityDb.UserRoles.CountAsync());
            Assert.Equal(1, await orgDb.Employees.CountAsync()); // Only pre-existing Employee remains
        }
        finally
        {
            await orgDb.DisposeAsync();
            await identityDb.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingPhoneNumber_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Test Hospital",
            Code = "TH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        identityDb.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            PhoneNumber = "09120001122",
            IsActive = true
        });
        await identityDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "09120001122", // existing phone
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("شماره موبایل", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentOrganization_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "09120001122",
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 5, 15),
            HireDate = new DateOnly(2022, 1, 10)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("سازمان مورد نظر یافت نشد", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithFutureBirthDate_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "09120001122",
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("تاریخ تولد باید در گذشته باشد", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithHireDateBeforeBirthDate_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "09120001122",
            PersonnelCode = "EMP-001",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(2000, 1, 1),
            HireDate = new DateOnly(1999, 1, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Contains("قبل از تاریخ تولد", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateNationalCode_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Hospital",
            Code = "H1",
            Type = OrganizationType.Clinic,
            IsActive = true
        });

        orgDb.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrganizationId = orgId,
            PersonnelCode = "P-100",
            NationalCode = "1234567890",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "Emp2",
            LastName = "User2",
            PhoneNumber = "09120005566",
            PersonnelCode = "P-200",
            NationalCode = "1234567890", // duplicate national code
            BirthDate = new DateOnly(1995, 1, 1),
            HireDate = new DateOnly(2021, 1, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("کد ملی وارد شده تکراری است", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicatePersonnelCodeInSameOrg_ThrowsArgumentException()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var orgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = orgId,
            Name = "Hospital",
            Code = "H1",
            Type = OrganizationType.Clinic,
            IsActive = true
        });

        orgDb.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            OrganizationId = orgId,
            PersonnelCode = "P-100",
            NationalCode = "1111111111",
            BirthDate = new DateOnly(1990, 1, 1),
            HireDate = new DateOnly(2020, 1, 1),
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);

        var request = new CreateEmployeeRequest
        {
            FirstName = "Emp2",
            LastName = "User2",
            PhoneNumber = "09120007788",
            PersonnelCode = "P-100", // duplicate personnel code in same org
            NationalCode = "2222222222",
            BirthDate = new DateOnly(1995, 1, 1),
            HireDate = new DateOnly(2021, 1, 1)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request, orgId, Guid.NewGuid()));
        Assert.Contains("کد پرسنلی در این سازمان تکراری است", ex.Message);
    }

    [Fact]
    public async Task Controller_CreateEmployee_AsCenterManager_DerivesOrgIdFromToken()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var managerOrgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = managerOrgId,
            Name = "Center Manager Hospital",
            Code = "CMH-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager"),
            new Claim("organization_id", managerOrgId.ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(useCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateEmployeeRequest
        {
            FirstName = "Emp",
            LastName = "User",
            PhoneNumber = "09121112233",
            PersonnelCode = "P-888",
            NationalCode = "9876543210",
            BirthDate = new DateOnly(1988, 8, 8),
            HireDate = new DateOnly(2023, 3, 3),
            OrganizationId = Guid.NewGuid() // Client attempt to override OrganizationId
        };

        // Act
        var actionResult = await controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<EmployeeDto>(objectResult.Value);
        Assert.Equal(managerOrgId, dto.OrganizationId); // Verifies OrganizationId was derived from token, not request payload
    }

    [Fact]
    public async Task Controller_CreateEmployee_AsCenterManagerWithoutOrgClaim_ReturnsForbid()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "CenterManager") // No organization_id claim
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(useCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateEmployeeRequest
        {
            FirstName = "Emp",
            LastName = "User",
            PhoneNumber = "09121112233",
            PersonnelCode = "P-999",
            NationalCode = "9999999999",
            BirthDate = new DateOnly(1988, 8, 8),
            HireDate = new DateOnly(2023, 3, 3)
        };

        // Act
        var actionResult = await controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(actionResult);
    }

    [Fact]
    public async Task Controller_CreateEmployee_AsSystemAdmin_UsesRequestBodyOrgId()
    {
        // Arrange
        var (orgDb, identityDb) = GetInMemoryDbContexts();
        await SeedEmployeeRoleAsync(identityDb);

        var targetOrgId = Guid.NewGuid();
        orgDb.Organizations.Add(new Domain.Entities.Organization
        {
            Id = targetOrgId,
            Name = "SysAdmin Target Org",
            Code = "SATO-01",
            Type = OrganizationType.Clinic,
            IsActive = true
        });
        await orgDb.SaveChangesAsync();

        var identityService = new EmployeeIdentityService(orgDb, identityDb);
        var useCase = new CreateEmployeeUseCase(identityService);
        var getEmployeesUseCase = new GetEmployeesUseCase(identityService);

        var currentUserId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(httpContextAccessor);

        var controller = new EmployeesController(useCase, getEmployeesUseCase, identityService, currentUserService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var request = new CreateEmployeeRequest
        {
            FirstName = "SysAdminEmp",
            LastName = "User",
            PhoneNumber = "09122223344",
            PersonnelCode = "P-SYS",
            NationalCode = "5555555555",
            BirthDate = new DateOnly(1985, 5, 5),
            HireDate = new DateOnly(2020, 2, 2),
            OrganizationId = targetOrgId
        };

        // Act
        var actionResult = await controller.CreateEmployee(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var dto = Assert.IsType<EmployeeDto>(objectResult.Value);
        Assert.Equal(targetOrgId, dto.OrganizationId);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute_RestrictedToCenterManagerSystemAdminAndEmployee()
    {
        // Arrange & Act
        var controllerType = typeof(EmployeesController);
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(authorizeAttr);
        Assert.Equal("CenterManager,SystemAdmin,Employee", authorizeAttr.Roles);
    }
}
