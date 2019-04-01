CREATE TABLE dbo.ReportValidation (
	Id INT IDENTITY(1,1) NOT NULL,
	ReportVersionId INT NOT NULL,
	Code NVARCHAR(32) NOT NULL,
	Description NVARCHAR(1024) NOT NULL,
	AdditionalDescription NVARCHAR(256) NOT NULL,
	Operator NVARCHAR(8) NOT NULL,
	Severity INT NOT NULL DEFAULT 1,
	LeftFormula NVARCHAR(MAX) NOT NULL,
	RightFormula NVARCHAR(MAX) NOT NULL,
	UserFriendlyFormula NVARCHAR(MAX) NULL,
	Tolerance DECIMAL(17,4) NULL,
	RequiredTemplates NVARCHAR(256) NULL
);
ALTER TABLE dbo.ReportValidation ADD CONSTRAINT PK_ReportValidation PRIMARY KEY (Id);
ALTER TABLE dbo.ReportValidation ADD CONSTRAINT FK_ReportValidation_ReportVersionId FOREIGN KEY (ReportVersionId) REFERENCES dbo.ReportVersion(Id);
ALTER TABLE dbo.ReportValidation ADD CONSTRAINT UQ_ReportValidation_ReportVersionIdCode UNIQUE (ReportVersionId, Code);