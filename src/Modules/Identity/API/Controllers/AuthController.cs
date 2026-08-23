using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.UseCases.ForgotPassword;
using AriaHR.Modules.Identity.Application.UseCases.Login;
using AriaHR.Modules.Identity.Application.UseCases.RefreshToken;
using AriaHR.Modules.Identity.Application.UseCases.Registration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AriaHR.Modules.Identity.API.Controllers;

[ApiController]
[Route("api/auth")]
[Route("api/identity/auth")]
public class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly RefreshTokenUseCase _refreshTokenUseCase;
    private readonly ForgotPasswordUseCase _forgotPasswordUseCase;
    private readonly ResetPasswordUseCase _resetPasswordUseCase;
    private readonly InitiateRegistrationUseCase _initiateRegistrationUseCase;
    private readonly VerifyRegistrationOtpUseCase _verifyRegistrationOtpUseCase;

    public AuthController(
        LoginUseCase loginUseCase,
        RefreshTokenUseCase refreshTokenUseCase,
        ForgotPasswordUseCase forgotPasswordUseCase,
        ResetPasswordUseCase resetPasswordUseCase,
        InitiateRegistrationUseCase initiateRegistrationUseCase,
        VerifyRegistrationOtpUseCase verifyRegistrationOtpUseCase)
    {
        _loginUseCase = loginUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
        _forgotPasswordUseCase = forgotPasswordUseCase;
        _resetPasswordUseCase = resetPasswordUseCase;
        _initiateRegistrationUseCase = initiateRegistrationUseCase;
        _verifyRegistrationOtpUseCase = verifyRegistrationOtpUseCase;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Request body is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var nationalCode = !string.IsNullOrWhiteSpace(request.NationalCode) ? request.NationalCode : request.Username;

        if (string.IsNullOrWhiteSpace(nationalCode) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "NationalCode and Password are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (nationalCode.Length != 10 || !nationalCode.All(char.IsDigit))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "NationalCode must be a 10-digit numeric code.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _loginUseCase.ExecuteAsync(request, clientIp, cancellationToken);

        if (result == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Detail = "Invalid credentials.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _refreshTokenUseCase.ExecuteAsync(request, clientIp, cancellationToken);

        if (result == null)
        {
            return Unauthorized(new { Message = "Invalid or expired refresh token." });
        }

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _forgotPasswordUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(new { Message = "If the phone number exists, a verification code has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _resetPasswordUseCase.ExecuteAsync(request, cancellationToken);

        if (!success)
        {
            return BadRequest(new { Message = "Invalid verification code or password reset request." });
        }

        return Ok(new { Message = "Password has been successfully reset." });
    }

    [HttpPost("register/initiate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InitiateRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateRegistration(
        [FromBody] InitiateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation failed",
                Detail = "MobileNumber is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var result = await _initiateRegistrationUseCase.ExecuteAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Registration initiation failed",
                Detail = "Invalid mobile number.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result);
    }

    [HttpPost("register/verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VerifyRegistrationOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(VerifyRegistrationOtpResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyRegistrationOtp(
        [FromBody] VerifyRegistrationOtpRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new VerifyRegistrationOtpResponse(false, "invalid-input", "Mobile number and verification code are required."));
        }

        var result = await _verifyRegistrationOtpUseCase.ExecuteAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
