using System.Text.Json;
using AriaHR.Modules.Organization.Application.DTOs;
using AriaHR.Shared.Converters;
using Xunit;

namespace AriaHR.Modules.Organization.Tests;

public class UtcDateTimeJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public UtcDateTimeJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new UtcDateTimeJsonConverter());
    }

    [Fact]
    public void Serialize_DateTimeWithUnspecifiedKind_AppendsZSuffix()
    {
        // Arrange
        var dateTime = new DateTime(2026, 8, 25, 20, 21, 44, DateTimeKind.Unspecified);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        Assert.Equal("\"2026-08-25T20:21:44Z\"", json);
    }

    [Fact]
    public void Serialize_DateTimeWithUtcKind_AppendsZSuffix()
    {
        // Arrange
        var dateTime = new DateTime(2026, 8, 25, 20, 21, 44, 371, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        Assert.Equal("\"2026-08-25T20:21:44.371Z\"", json);
    }

    [Fact]
    public void Serialize_RecentActivityDto_ContainsUtcZFormattedDate()
    {
        // Arrange
        var dto = new RecentActivityDto
        {
            Type = "OrganizationCreated",
            Title = "مرکز جدید ثبت شد",
            Description = "مرکز تصویر برداری دکتر فاطمه سالمی",
            CreatedAtUtc = new DateTime(2026, 8, 25, 20, 21, 44, DateTimeKind.Unspecified)
        };

        // Act
        var json = JsonSerializer.Serialize(dto, _options);

        // Assert
        Assert.Contains("\"CreatedAtUtc\":\"2026-08-25T20:21:44Z\"", json);
    }

    [Fact]
    public void Deserialize_UtcStringWithZ_ParsesToUtcDateTime()
    {
        // Arrange
        var json = "\"2026-08-25T20:21:44.3713409Z\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(2026, result.Year);
        Assert.Equal(8, result.Month);
        Assert.Equal(25, result.Day);
        Assert.Equal(20, result.Hour);
        Assert.Equal(21, result.Minute);
    }
}
