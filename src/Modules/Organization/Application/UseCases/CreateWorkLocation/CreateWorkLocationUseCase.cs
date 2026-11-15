using System.Security.Cryptography;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Modules.Organization.Application.Options;
using AriaHR.Modules.Organization.Application.Repositories;
using AriaHR.Modules.Organization.Application.Validators;
using AriaHR.Modules.Organization.Domain.Entities;
using AriaHR.Modules.Organization.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AriaHR.Modules.Organization.Application.UseCases.CreateWorkLocation;

public class CreateWorkLocationUseCase : ICreateWorkLocationUseCase
{
    private readonly IWorkLocationRepository _workLocationRepository;
    private readonly QrCodeOptions _options;

    public CreateWorkLocationUseCase(
        IWorkLocationRepository workLocationRepository,
        IOptions<QrCodeOptions>? options = null)
    {
        _workLocationRepository = workLocationRepository ?? throw new ArgumentNullException(nameof(workLocationRepository));
        _options = options?.Value ?? new QrCodeOptions();
    }

    public async Task<WorkLocationDto> ExecuteAsync(
        CreateWorkLocationRequest request,
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

        string name = WorkLocationInputValidator.ValidateName(request.Name);
        WorkLocationInputValidator.ValidateCoordinates(request.Latitude, request.Longitude);
        WorkLocationInputValidator.ValidateRadius(request.AllowedRadiusInMeters, _options);

        if (!Enum.IsDefined(typeof(QrCodeMode), request.QrCodeMode))
        {
            throw new ArgumentException("حالت کد کیوآر معتبر نمی‌باشد.");
        }

        int rotationInterval = WorkLocationInputValidator.ValidateRotationInterval(request.QrRotationIntervalMinutes, _options);

        var organizationExists = await _workLocationRepository.OrganizationExistsAsync(organizationId, cancellationToken);
        if (!organizationExists)
        {
            throw new ArgumentException("سازمان مورد نظر یافت نشد.");
        }

        DateTime nowUtc = DateTime.UtcNow;

        var workLocation = new WorkLocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AllowedRadiusInMeters = request.AllowedRadiusInMeters,
            IsActive = request.IsActive,
            QrCodeMode = request.QrCodeMode,
            QrCodeIdentifier = GenerateIdentifier(),
            QrCodeEnabled = request.IsActive && request.QrCodeEnabled,
            QrRotationIntervalMinutes = rotationInterval,
            QrCodeLastGeneratedAtUtc = request.IsActive && request.QrCodeEnabled ? nowUtc : null,
            CreatedAtUtc = nowUtc,
            CreatedByUserId = createdByUserId == Guid.Empty ? null : createdByUserId
        };

        await _workLocationRepository.AddAsync(workLocation, cancellationToken);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return WorkLocationDtoMapper.Map(workLocation);
    }

    internal static string GenerateIdentifier()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    }
}
