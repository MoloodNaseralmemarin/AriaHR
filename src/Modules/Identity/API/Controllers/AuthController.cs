using System.Security.Claims;
using AriaHR.Modules.Identity.Application.DTOs;
using AriaHR.Modules.Identity.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace AriaHR.Modules.Identity.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SendOtpUseCase _sendOtpUseCase;
    private readonly VerifyOtpUseCase _verifyOtpUseCase;
    private readonly GetCurrentUserUseCase _getCurrentUserUseCase;
    private readonly IHostEnvironment _environment;

    public AuthController(
        SendOtpUseCase sendOtpUseCase,
        VerifyOtpUseCase verifyOtpUseCase,
        GetCurrentUserUseCase getCurrentUserUseCase,
        IHostEnvironment environment)
    {
        _sendOtpUseCase = sendOtpUseCase;
        _verifyOtpUseCase = verifyOtpUseCase;
        _getCurrentUserUseCase = getCurrentUserUseCase;
        _environment = environment;
    }

    [HttpPost("send-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ورودی نامعتبر",
                Detail = "شماره تلفن الزامی است."
            });
        }

        var result = await _sendOtpUseCase.ExecuteAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطا در ارسال کد تایید",
                Detail = result.ErrorMessage
            });
        }

        if (_environment.IsDevelopment())
        {
            return Ok(new
            {
                message = "کد تایید با موفقیت ارسال شد",
                otpCode = result.OtpCode
            });
        }

        return Ok(new { message = "کد تایید با موفقیت ارسال شد" });
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VerifyOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ورودی نامعتبر",
                Detail = "شماره تلفن و کد تایید الزامی است."
            });
        }

        var result = await _verifyOtpUseCase.ExecuteAsync(request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "اعتبارسنجی ناموفق",
                Detail = result.ErrorMessage
            });
        }

        return Ok(result.Response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var currentUser = await _getCurrentUserUseCase.ExecuteAsync(userId, cancellationToken);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        return Ok(currentUser);
    }
}
