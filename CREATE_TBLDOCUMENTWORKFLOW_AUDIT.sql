-- Document workflow execution audit tables (SQL Server)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentworkflowauditrun')
BEGIN
    CREATE TABLE tbldocumentworkflowauditrun (
        DocumentWorkflowAuditRunId BIGINT IDENTITY(1,1) PRIMARY KEY,
        MessageId NVARCHAR(1024) NOT NULL,
        MailboxUser NVARCHAR(320) NULL,
        Subject NVARCHAR(500) NULL,
        ClassificationLabel NVARCHAR(120) NULL,
        ClassificationScore FLOAT NULL,
        DocumentWorkflowRuleId INT NULL,
        WorkflowName NVARCHAR(200) NULL,
        Status NVARCHAR(40) NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        SummarisationText NVARCHAR(MAX) NULL,
        ExtractionJson NVARCHAR(MAX) NULL,
        StartedDate DATETIME2 NOT NULL CONSTRAINT DF_tbldocumentworkflowauditrun_StartedDate DEFAULT (SYSUTCDATETIME()),
        CompletedDate DATETIME2 NULL
    );

    CREATE INDEX IX_tbldocumentworkflowauditrun_MessageId
        ON tbldocumentworkflowauditrun (MessageId, StartedDate DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentworkflowauditstep')
BEGIN
    CREATE TABLE tbldocumentworkflowauditstep (
        DocumentWorkflowAuditStepId BIGINT IDENTITY(1,1) PRIMARY KEY,
        DocumentWorkflowAuditRunId BIGINT NOT NULL,
        StepOrder INT NOT NULL,
        StepType NVARCHAR(80) NOT NULL,
        Status NVARCHAR(40) NOT NULL,
        Details NVARCHAR(MAX) NULL,
        StartedDate DATETIME2 NOT NULL CONSTRAINT DF_tbldocumentworkflowauditstep_StartedDate DEFAULT (SYSUTCDATETIME()),
        CompletedDate DATETIME2 NULL,
        CONSTRAINT FK_tbldocumentworkflowauditstep_tbldocumentworkflowauditrun
            FOREIGN KEY (DocumentWorkflowAuditRunId)
            REFERENCES tbldocumentworkflowauditrun(DocumentWorkflowAuditRunId)
    );

    CREATE INDEX IX_tbldocumentworkflowauditstep_RunId_Order
        ON tbldocumentworkflowauditstep (DocumentWorkflowAuditRunId, StepOrder);
END;
GO
