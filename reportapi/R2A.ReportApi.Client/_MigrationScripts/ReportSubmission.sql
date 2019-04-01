if not exists (select * from sys.tables t join sys.schemas s on (t.schema_id = s.schema_id) where s.name = 'dbo' and t.name = 'ReportStatus') 
begin
	CREATE TABLE dbo.ReportStatus
	(Token UNIQUEIDENTIFIER NOT NULL,
	 Id INT IDENTITY(1,1) NOT NULL,
	 ReportCode nvarchar(20) NOT NULL,
	 BankCode nvarchar(20) NOT NULL,
	 PeriodInfo nvarchar(64) NOT NULL,
	 SubmissionStatus char(2) NOT NULL,
	 SubmissionStatusMessage nvarchar(max) NULL,
	 DataProcessingStatus char(3) NOT NULL DEFAULT 'DP0',
	 DataProcessingStatusMessage nvarchar(max) NULL,
	 StatusFilePath nvarchar(256) NULL,
	 PdfReportFilePath nvarchar(256) NULL,
	 TimeSubmitted datetime2(0) NOT NULL DEFAULT CURRENT_TIMESTAMP
	 )

	ALTER TABLE dbo.ReportStatus
	ADD CONSTRAINT reportsApi_ReportStatus_Token
	PRIMARY KEY NONCLUSTERED (Token)

	CREATE UNIQUE CLUSTERED INDEX CIX_ReportStatus ON dbo.ReportStatus(Id)
end