-- ACMS configuration and audit database (SQL Server).
--
-- Run once against an empty database, e.g.:
--   sqlcmd -S <server> -d ACMS -E -i 001_create_schema.sql
--
-- Generated from Acms.Infrastructure.Data.AcmsDbContext. If the model changes,
-- add a new numbered script instead of editing this one.
--
-- ACMS stores only its own data here (AWACS server list and audit trail).
-- It never connects to the AWACS database.

CREATE TABLE [AuditEntries] (
    [Id] bigint NOT NULL IDENTITY,
    [TimestampUtc] datetime2 NOT NULL,
    [UserName] nvarchar(256) NOT NULL,
    [Action] nvarchar(32) NOT NULL,
    [Outcome] nvarchar(16) NOT NULL,
    [ServerId] int NULL,
    [ServerName] nvarchar(100) NULL,
    [WsId] nvarchar(64) NULL,
    [RequestedJson] nvarchar(max) NULL,
    [BeforeJson] nvarchar(max) NULL,
    [AfterJson] nvarchar(max) NULL,
    [Message] nvarchar(2000) NULL,
    [CorrelationId] nvarchar(32) NOT NULL,
    CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AwacsServers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [BaseUrl] nvarchar(400) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [CreatedBy] nvarchar(256) NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [UpdatedBy] nvarchar(256) NULL,
    CONSTRAINT [PK_AwacsServers] PRIMARY KEY ([Id])
);
GO


CREATE INDEX [IX_AuditEntries_ServerId_WsId] ON [AuditEntries] ([ServerId], [WsId]);
GO


CREATE INDEX [IX_AuditEntries_TimestampUtc] ON [AuditEntries] ([TimestampUtc]);
GO


CREATE INDEX [IX_AuditEntries_UserName] ON [AuditEntries] ([UserName]);
GO


CREATE UNIQUE INDEX [IX_AwacsServers_Name] ON [AwacsServers] ([Name]);
GO





-- Permissions for the IIS application pool / service account.
-- Replace COMPANY\svc-acms with the real account. The audit trail is append-only:
-- the application can insert and read audit rows but never change or delete them.
--
-- CREATE LOGIN [COMPANY\svc-acms] FROM WINDOWS;
-- CREATE USER [COMPANY\svc-acms] FOR LOGIN [COMPANY\svc-acms];
-- GRANT SELECT, INSERT, UPDATE ON [AwacsServers] TO [COMPANY\svc-acms];
-- GRANT SELECT, INSERT ON [AuditEntries] TO [COMPANY\svc-acms];
-- DENY UPDATE, DELETE ON [AuditEntries] TO [COMPANY\svc-acms];
-- GO
