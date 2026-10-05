namespace AriaHR.Modules.Requests.Application.UseCases.CancelLeaveRequest;

public interface ICancelLeaveRequestUseCase
{
    Task ExecuteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
