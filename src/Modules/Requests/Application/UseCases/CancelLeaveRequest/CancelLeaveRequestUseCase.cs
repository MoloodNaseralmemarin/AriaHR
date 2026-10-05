using AriaHR.Modules.Requests.Application.Repositories;
using AriaHR.Shared.Services;
using AriaHR.Modules.Requests.Domain.Enums;

namespace AriaHR.Modules.Requests.Application.UseCases.CancelLeaveRequest;

public class CancelLeaveRequestUseCase : ICancelLeaveRequestUseCase
{
    private readonly ILeaveRequestRepository _repository;
    private readonly IEmployeeLookupService _employeeLookupService;

    public CancelLeaveRequestUseCase(
        ILeaveRequestRepository repository,
        IEmployeeLookupService employeeLookupService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _employeeLookupService = employeeLookupService ?? throw new ArgumentNullException(nameof(employeeLookupService));
    }

    public async Task ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("شناسه درخواست الزامی است.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("شناسه کاربر الزامی است.", nameof(userId));
        }

        var employeeDetails = await _employeeLookupService.GetEmployeeDetailsByUserIdAsync(userId, cancellationToken);
        if (!employeeDetails.HasValue)
        {
            throw new UnauthorizedAccessException("اطلاعات پرسنلی کاربر یافت نشد.");
        }

        var (employeeId, _) = employeeDetails.Value;

        var request = await _repository.GetByIdAsync(id, cancellationToken);
        if (request == null)
        {
            throw new KeyNotFoundException("درخواست مورد نظر یافت نشد.");
        }

        if (request.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException("شما مجاز به لغو این درخواست نمی‌باشید.");
        }

        if (request.Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("تنها درخواست‌های در انتظار قابل لغو می‌باشند.");
        }

        var now = DateTime.UtcNow;
        request.Status = RequestStatus.Cancelled;
        request.UpdatedAtUtc = now;
        request.UpdatedByUserId = userId;

        await _repository.UpdateAsync(request, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
