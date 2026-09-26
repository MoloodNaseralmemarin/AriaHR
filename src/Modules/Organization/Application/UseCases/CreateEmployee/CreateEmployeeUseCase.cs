using AriaHR.Modules.Identity.Application.Repositories;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateEmployee;

public class CreateEmployeeUseCase : ICreateEmployeeUseCase
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;

    public CreateEmployeeUseCase(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<EmployeeDto> ExecuteAsync(
        CreateEmployeeRequest request,
        Guid organizationId,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException("شناسه کاربر الزامی است.");
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

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || user.IsDeleted || !user.IsActive)
        {
            throw new ArgumentException("کاربر مورد نظر یافت نشد.");
        }

        if (user.OrganizationId.HasValue && user.OrganizationId.Value != Guid.Empty && user.OrganizationId.Value != organizationId)
        {
            throw new ArgumentException("کاربر به سازمان دیگری تعلق دارد.");
        }

        var organizationExists = await _employeeRepository.OrganizationExistsAsync(organizationId, cancellationToken);
        if (!organizationExists)
        {
            throw new ArgumentException("سازمان مورد نظر یافت نشد.");
        }

        var userAlreadyLinked = await _employeeRepository.ExistsByUserIdAsync(request.UserId, cancellationToken);
        if (userAlreadyLinked)
        {
            throw new ArgumentException("این کاربر قبلاً به عنوان کارمند ثبت شده است.");
        }

        var nationalCodeExists = await _employeeRepository.ExistsByNationalCodeAsync(request.NationalCode.Trim(), cancellationToken);
        if (nationalCodeExists)
        {
            throw new ArgumentException("کد ملی وارد شده تکراری است.");
        }

        var personnelCodeExists = await _employeeRepository.ExistsByPersonnelCodeAsync(request.PersonnelCode.Trim(), organizationId, cancellationToken);
        if (personnelCodeExists)
        {
            throw new ArgumentException("کد پرسنلی در این سازمان تکراری است.");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            OrganizationId = organizationId,
            PersonnelCode = request.PersonnelCode.Trim(),
            NationalCode = request.NationalCode.Trim(),
            BirthDate = request.BirthDate,
            HireDate = request.HireDate,
            Gender = request.Gender?.Trim(),
            ProfileImagePath = request.ProfileImagePath?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

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
}
