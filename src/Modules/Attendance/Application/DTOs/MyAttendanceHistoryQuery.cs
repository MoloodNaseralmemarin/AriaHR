using System;

namespace AriaHR.Modules.Attendance.Application.DTOs;

/// <summary>
/// Query parameters for retrieving the authenticated employee's attendance history with pagination.
/// </summary>
public record MyAttendanceHistoryQuery
{
    /// <summary>
    /// Page number (1-based). Default is 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Default is 20, maximum is 100.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Optional start date for filtering attendance records (inclusive).
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Optional end date for filtering attendance records (exclusive).
    /// </summary>
    public DateOnly? EndDate { get; set; }
}

/// <summary>
/// Paged result for attendance history.
/// </summary>
public record PagedResult<T>
{
    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// The items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = new List<T>();

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Current page number.
    ///