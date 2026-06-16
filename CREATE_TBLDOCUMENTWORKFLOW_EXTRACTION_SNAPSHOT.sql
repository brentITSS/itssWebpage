-- One row per extracted field per workflow audit run (for reporting over time).
-- Run date comes from tbldocumentworkflowauditrun (StartedDate / CompletedDate).

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentworkflowextractionsnapshot')
BEGIN
    CREATE TABLE tbldocumentworkflowextractionsnapshot (
        DocumentWorkflowExtractionSnapshotId BIGINT IDENTITY(1,1) PRIMARY KEY,
        DocumentWorkflowAuditRunId BIGINT NOT NULL,
        FieldName NVARCHAR(200) NOT NULL,
        FieldValue NVARCHAR(MAX) NULL,
        Comments NVARCHAR(MAX) NULL,
        CONSTRAINT FK_tbldocumentworkflowextractionsnapshot_tbldocumentworkflowauditrun
            FOREIGN KEY (DocumentWorkflowAuditRunId)
            REFERENCES tbldocumentworkflowauditrun(DocumentWorkflowAuditRunId)
    );

    CREATE INDEX IX_tbldocumentworkflowextractionsnapshot_AuditRunId
        ON tbldocumentworkflowextractionsnapshot (DocumentWorkflowAuditRunId);

    CREATE INDEX IX_tbldocumentworkflowextractionsnapshot_FieldName
        ON tbldocumentworkflowextractionsnapshot (FieldName);
END;
