CREATE TABLE [dbo].[ClientImportBatch]
(
    [BatchId]           UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [Environment]       NVARCHAR(20)     NOT NULL,
    [Status]            NVARCHAR(20)     NOT NULL,
    [StartedByUserId]   NVARCHAR(50)     NULL,
    [StartedByUserName] NVARCHAR(200)    NULL,
    [StartedAt]         DATETIME2        NOT NULL,
    [CompletedAt]       DATETIME2        NULL,
    [TotalRows]         INT              NOT NULL DEFAULT 0,
    [SucceededRows]     INT              NOT NULL DEFAULT 0,
    [FailedRows]        INT              NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX [UX_ClientImportBatch_RunningPerEnvironment]
    ON [dbo].[ClientImportBatch] ([Environment])
    WHERE [Status] = 'Running';

CREATE TABLE [dbo].[ClientImportBatchRow]
(
    [Id]              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [BatchId]         UNIQUEIDENTIFIER     NOT NULL,
    [RowNumber]       INT                  NOT NULL,
    [ClientCode]      NVARCHAR(10)         NOT NULL,
    [ClientName]      NVARCHAR(200)        NOT NULL,
    [Status]          NVARCHAR(20)         NOT NULL,
    [StepsCompleted]  NVARCHAR(500)        NULL,
    [ErrorMessage]    NVARCHAR(1000)       NULL,
    [ResultClientId]  UNIQUEIDENTIFIER     NULL,
    CONSTRAINT [FK_ClientImportBatchRow_ClientImportBatch]
        FOREIGN KEY ([BatchId]) REFERENCES [dbo].[ClientImportBatch]([BatchId])
);

CREATE INDEX [IX_ClientImportBatchRow_BatchId] ON [dbo].[ClientImportBatchRow] ([BatchId]);
CREATE INDEX [IX_ClientImportBatchRow_ClientCode] ON [dbo].[ClientImportBatchRow] ([ClientCode]);
