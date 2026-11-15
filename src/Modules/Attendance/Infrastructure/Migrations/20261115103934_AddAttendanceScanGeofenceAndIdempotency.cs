using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AriaHR.Modules.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceScanGeofenceAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[AttendanceRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [AttendanceRecords] (
        [Id] uniqueidentifier NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [CheckInTime] datetime2 NOT NULL,
        [CheckOutTime] datetime2 NULL,
        [CheckInLatitude] float NULL,
        [CheckInLongitude] float NULL,
        [CheckOutLatitude] float NULL,
        [CheckOutLongitude] float NULL,
        [CheckInAccuracy] float NULL,
        [CheckOutAccuracy] float NULL,
        [CheckInDistanceInMeters] float NULL,
        [CheckOutDistanceInMeters] float NULL,
        [CheckInLocationTimestampUtc] datetime2 NULL,
        [CheckOutLocationTimestampUtc] datetime2 NULL,
        [CheckInIdempotencyKey] nvarchar(100) NULL,
        [CheckOutIdempotencyKey] nvarchar(100) NULL,
        [WorkLocationId] uniqueidentifier NULL,
        [QRCodeId] uniqueidentifier NULL,
        [Method] nvarchar(50) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [IsWithinGeofence] bit NOT NULL,
        [DeviceInfo] nvarchar(500) NULL,
        [IPAddress] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [ApprovedBy] uniqueidentifier NULL,
        [ApprovedAt] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAtUtc] datetime2 NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id])
    );
END;

IF COL_LENGTH('AttendanceRecords', 'CheckInDistanceInMeters') IS NULL
    ALTER TABLE [AttendanceRecords] ADD [CheckInDistanceInMeters] float NULL;

IF COL_LENGTH('AttendanceRecords', 'CheckOutDistanceInMeters') IS NULL
    ALTER TABLE [AttendanceRecords] ADD [CheckOutDistanceInMeters] float NULL;

IF COL_LENGTH('AttendanceRecords', 'CheckInLocationTimestampUtc') IS NULL
    ALTER TABLE [AttendanceRecords] ADD [CheckInLocationTimestampUtc] datetime2 NULL;

IF COL_LENGTH('AttendanceRecords', 'CheckOutLocationTimestampUtc') IS NULL
    ALTER TABLE [AttendanceRecords] ADD [CheckOutLocationTimestampUtc] datetime2 NULL;

IF COL_LENGTH('AttendanceRecords', 'CheckInIdempotencyKey') IS NULL
    ALTER TABLE [AttendanceRecords] ADD [CheckInIdempotencyKey] nvarchar(100) NULL;

IF COL_LENGTH('AttendanceRecords', 'CheckOutIdempotencyKey') IS NULL
    ALTER TABLE [AttendanceRecords] ADD [CheckOutIdempotencyKey] nvarchar(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_OrganizationId_EmployeeId_CheckInTime' AND object_id = OBJECT_ID('AttendanceRecords'))
    CREATE INDEX [IX_AttendanceRecords_OrganizationId_EmployeeId_CheckInTime] ON [AttendanceRecords] ([OrganizationId], [EmployeeId], [CheckInTime]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_EmployeeId_CheckOutTime' AND object_id = OBJECT_ID('AttendanceRecords'))
    CREATE INDEX [IX_AttendanceRecords_EmployeeId_CheckOutTime] ON [AttendanceRecords] ([EmployeeId], [CheckOutTime]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_EmployeeId_CheckInIdempotencyKey' AND object_id = OBJECT_ID('AttendanceRecords'))
    CREATE UNIQUE INDEX [IX_AttendanceRecords_EmployeeId_CheckInIdempotencyKey] ON [AttendanceRecords] ([EmployeeId], [CheckInIdempotencyKey]) WHERE [CheckInIdempotencyKey] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_EmployeeId_CheckOutIdempotencyKey' AND object_id = OBJECT_ID('AttendanceRecords'))
    CREATE UNIQUE INDEX [IX_AttendanceRecords_EmployeeId_CheckOutIdempotencyKey] ON [AttendanceRecords] ([EmployeeId], [CheckOutIdempotencyKey]) WHERE [CheckOutIdempotencyKey] IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_EmployeeId_CheckInIdempotencyKey' AND object_id = OBJECT_ID('AttendanceRecords'))
    DROP INDEX [IX_AttendanceRecords_EmployeeId_CheckInIdempotencyKey] ON [AttendanceRecords];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AttendanceRecords_EmployeeId_CheckOutIdempotencyKey' AND object_id = OBJECT_ID('AttendanceRecords'))
    DROP INDEX [IX_AttendanceRecords_EmployeeId_CheckOutIdempotencyKey] ON [AttendanceRecords];

IF COL_LENGTH('AttendanceRecords', 'CheckInDistanceInMeters') IS NOT NULL
    ALTER TABLE [AttendanceRecords] DROP COLUMN [CheckInDistanceInMeters];

IF COL_LENGTH('AttendanceRecords', 'CheckOutDistanceInMeters') IS NOT NULL
    ALTER TABLE [AttendanceRecords] DROP COLUMN [CheckOutDistanceInMeters];

IF COL_LENGTH('AttendanceRecords', 'CheckInLocationTimestampUtc') IS NOT NULL
    ALTER TABLE [AttendanceRecords] DROP COLUMN [CheckInLocationTimestampUtc];

IF COL_LENGTH('AttendanceRecords', 'CheckOutLocationTimestampUtc') IS NOT NULL
    ALTER TABLE [AttendanceRecords] DROP COLUMN [CheckOutLocationTimestampUtc];

IF COL_LENGTH('AttendanceRecords', 'CheckInIdempotencyKey') IS NOT NULL
    ALTER TABLE [AttendanceRecords] DROP COLUMN [CheckInIdempotencyKey];

IF COL_LENGTH('AttendanceRecords', 'CheckOutIdempotencyKey') IS NOT NULL
    ALTER TABLE [AttendanceRecords] DROP COLUMN [CheckOutIdempotencyKey];
");
        }
    }
}
