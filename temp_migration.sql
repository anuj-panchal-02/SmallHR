BEGIN TRANSACTION;
ALTER TABLE [Tenants] ADD [AdminEmail] nvarchar(max) NULL;

ALTER TABLE [Tenants] ADD [AdminFirstName] nvarchar(max) NULL;

ALTER TABLE [Tenants] ADD [AdminLastName] nvarchar(max) NULL;

ALTER TABLE [Tenants] ADD [FailureReason] nvarchar(max) NULL;

ALTER TABLE [Tenants] ADD [IdempotencyToken] nvarchar(max) NULL;

ALTER TABLE [Tenants] ADD [ProvisionedAt] datetime2 NULL;

ALTER TABLE [Tenants] ADD [Status] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251102070636_AddProvisioningFieldsToTenant', N'9.0.10');

COMMIT;
GO

