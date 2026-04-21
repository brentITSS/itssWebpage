IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbldocumentclassificationlabel')
AND NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE Name = N'ClassificationDescription'
      AND Object_ID = Object_ID(N'tbldocumentclassificationlabel')
)
BEGIN
    ALTER TABLE tbldocumentclassificationlabel
    ADD ClassificationDescription NVARCHAR(2000) NULL;
END;
