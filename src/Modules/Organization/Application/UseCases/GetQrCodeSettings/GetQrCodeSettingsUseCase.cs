using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.Services;
using AriaHR.Modules.Organization.Domain.Entities;

namespace AriaHR.Modules.Organization.Application.UseCases.GetQrCodeSettings;

public class GetQrCodeSettingsUseCase : IGetQrCodeSettingsUseCase
{
    private readonly IQrCodeRepository _repository;
    private readonly IQrCodeService _qrCodeService;

    public GetQrCodeSettingsUseCase(IQrCodeRepository repository, IQrCodeService qrCodeService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
    }

    public async Task<QrCodeSettingsResponse> ExecuteAsync(
        Guid workLocationId,
        Guid? targetOrganizationId = null,
        CancellationToken cancellationToken = default)
    {
        if (workLocationId == Guid.Empty)
        {
            throw new ArgumentException("شناسه محل کار الزامی است.", nameof(workLocationId));
        }

        WorkLocation? workLocation = await _repository.GetWorkLocationByIdAsync(workLocationId, cancellationToken);
        if (workLocation == null || workLocation.IsDeleted)
        {
            throw new KeyNotFoundException("محل کار مورد نظر یافت نشد.");
        }

        if (targetOrganizationId.HasValue &&
            targetOrganizationId.Value != Guid.Empty &&
            workLocation.OrganizationId != targetOrganizationId.Value)
        {
            throw new UnauthorizedAccessException("امکان دسترسی به تنظیمات کیوآر سازمان دیگر وجود ندارد.");
        }

        QrCodeResponse? currentQrCode = null;

        if (workLocation.QrCodeVerificationEnabled && workLocation.IsActive)
        {
            try
            {
                currentQrCode = await _qrCodeService.GetCurrentAsync(workLocationId, targetOrganizationId, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                currentQrCode = null;
            }
            catch (KeyNotFoundException)
            {
                currentQrCode = null;
            }
        }

        return QrCodeSettingsMapper.Map(workLocation, currentQrCode);
    }
}
