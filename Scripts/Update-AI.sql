BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011072735_AddAIConversationScope'
)
BEGIN
    DROP INDEX [IX_AIConversations_UserId] ON [AIConversations];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011072735_AddAIConversationScope'
)
BEGIN
    ALTER TABLE [AIConversations] ADD [Scope] nvarchar(20) NOT NULL DEFAULT N'Travel';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011072735_AddAIConversationScope'
)
BEGIN
    CREATE INDEX [IX_AIConversations_UserId_Scope_UpdatedAt] ON [AIConversations] ([UserId], [Scope], [UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011072735_AddAIConversationScope'
)
BEGIN
    UPDATE c SET c.[Scope] = 'Admin'
    FROM [AIConversations] c
    INNER JOIN [Users] u ON u.[Id] = c.[UserId]
    WHERE u.[Role] = 'Admin';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261011072735_AddAIConversationScope'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261011072735_AddAIConversationScope', N'10.0.6');
END;

COMMIT;
GO

