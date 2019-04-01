

CREATE TABLE dbo.Division (
	Id INT IDENTITY(1,1) NOT NULL,
	Code nvarchar(32) NOT NULL,
	Title nvarchar(255) NOT NULL
);
ALTER TABLE dbo.Division ADD CONSTRAINT PK_Division PRIMARY KEY (Id);
INSERT INTO dbo.Division(Code, Title) VALUES('SDC', 'Statistical Data Center');

CREATE TABLE dbo.RecurrenceType (
	Code NCHAR(1) NOT NULL,
	Title NVARCHAR(128)
);
ALTER TABLE dbo.RecurrenceType ADD CONSTRAINT PK_RecurrenceType PRIMARY KEY (Code);
INSERT INTO dbo.RecurrenceType(Code, Title) VALUES('D', 'Daily');
INSERT INTO dbo.RecurrenceType(Code, Title) VALUES('W', 'Weekly');
INSERT INTO dbo.RecurrenceType(Code, Title) VALUES('M', 'Monthly');
INSERT INTO dbo.RecurrenceType(Code, Title) VALUES('Q', 'Quarterly');
INSERT INTO dbo.RecurrenceType(Code, Title) VALUES('S', 'Semesterly');
INSERT INTO dbo.RecurrenceType(Code, Title) VALUES('Y', 'Yearly');
INSERT INTO dbo.RecurrenceType(Code, Title) VALUES('X', 'Any time');

CREATE TABLE dbo.Report (
	Id INT IDENTITY(1,1) NOT NULL,
	Code NVARCHAR(20) NOT NULL,
	Name NVARCHAR(256) NOT NULL,
	RecurrenceType NCHAR(1) NOT NULL,
	NumberOfMonthsAfter SMALLINT NOT NULL DEFAULT 12,
	NumberOfDaysAfter SMALLINT NOT NULL DEFAULT 0,
	ProcessingType NVARCHAR(16) NOT NULL DEFAULT 'INTERNAL',
	PresentPeriodAllowed BIT NOT NULL DEFAULT 0
);

ALTER TABLE dbo.Report ADD CONSTRAINT PK_Report PRIMARY KEY (Id);
ALTER TABLE dbo.Report ADD CONSTRAINT UQ_Report_Code UNIQUE (Code);
ALTER TABLE dbo.Report ADD CONSTRAINT FK_RecurrenceType_RecurrenceType FOREIGN KEY (RecurrenceType) REFERENCES dbo.RecurrenceType(Code);


CREATE TABLE dbo.ReportVersion (
	Id INT IDENTITY(1,1) NOT NULL,
	ReportId INT NOT NULL,
	ReportingPeriodFrom DATE NOT NULL,
	ReportingPeriodTo DATE,
	VersionDate DATETIME2 NOT NULL,
	Version NVARCHAR(4) NOT NULL,
	Active BIT NOT NULL DEFAULT 1, 
	JsonDefinition NVARCHAR(1024) NOT NULL,
	XsdDefinition NVARCHAR(1024) NOT NULL,
	XsdNamespace NVARCHAR(1024) NOT NULL
);
ALTER TABLE dbo.ReportVersion ADD CONSTRAINT PK_ReportVersion PRIMARY KEY (Id);
ALTER TABLE dbo.ReportVersion ADD CONSTRAINT UQ_ReportVersion_XsdNamespace UNIQUE (XsdNamespace);
ALTER TABLE dbo.ReportVersion ADD CONSTRAINT UQ_ReportVersion_ReportIdVersion UNIQUE (ReportId, Version);
ALTER TABLE dbo.ReportVersion ADD CONSTRAINT FK_ReportVersion_ReportId FOREIGN KEY (ReportId) REFERENCES dbo.Report(Id);

CREATE TABLE dbo.ReportToDivision(
	Id INT IDENTITY(1,1) NOT NULL,
	ReportId INT NOT NULL,
	DivisionId INT NOT NULL
);

ALTER TABLE dbo.ReportToDivision ADD CONSTRAINT PK_ReportToDivision PRIMARY KEY (Id);
ALTER TABLE dbo.ReportToDivision ADD CONSTRAINT UQ_ReportToDivision_ReportIdDivisionId UNIQUE (ReportId, DivisionId);
ALTER TABLE dbo.ReportToDivision ADD CONSTRAINT FK_ReportToDivision_ReportId FOREIGN KEY (ReportId) REFERENCES dbo.Report(Id);
ALTER TABLE dbo.ReportToDivision ADD CONSTRAINT FK_ReportToDivision_DivisionId FOREIGN KEY (DivisionId) REFERENCES dbo.Division(Id);

--DROP TABLE dbo.SubmittedReport
CREATE TABLE dbo.SubmittedReport (
	Id INT IDENTITY(1,1) NOT NULL,
	ReportId INT NULL,
	ReportVersionId INT NULL,
	ReportingPeriod NVARCHAR(64),
	UndertakingId INT NULL,
	UserId INT NOT NULL DEFAULT 0,
	SubmissionTime DATETIME2(0) NOT NULL,
	ProcessingStartTime DATETIME2 NOT NULL,
	ProcessingEndTime DATETIME2 NOT NULL,
	SubmittedReportStatus INT NOT NULL DEFAULT 0,
	ReportStatusId INT NOT NULL,
	XmlLocation NVARCHAR(1024) NOT NULL,
	PdfProcessReportLocation NVARCHAR(1024) NOT NULL,
	XmlProcessReportLocation NVARCHAR(1024) NOT NULL,
	IsViewable bit NOT NULL
);
ALTER TABLE dbo.SubmittedReport ADD CONSTRAINT PK_SubmittedReport PRIMARY KEY (Id);
ALTER TABLE dbo.SubmittedReport ADD CONSTRAINT FK_SubmittedReport_ReportId FOREIGN KEY (ReportId) REFERENCES dbo.Report(Id);
ALTER TABLE dbo.SubmittedReport ADD CONSTRAINT FK_SubmittedReport_ReportVersionId FOREIGN KEY (ReportVersionId) REFERENCES dbo.ReportVersion(Id);
ALTER TABLE dbo.SubmittedReport ADD CONSTRAINT FK_SubmittedReport_UndertakingId FOREIGN KEY (UndertakingId) REFERENCES bsp.Bank(Id);
ALTER TABLE dbo.SubmittedReport ADD CONSTRAINT FK_SubmittedReport_ReportStatus FOREIGN KEY (ReportStatusId) REFERENCES dbo.ReportStatus(Id);

INSERT INTO bsp.Bank(Code, Title, BankTypeId, HasResourcesP1Billion, HasTrustAuthority, HasEMoneyAuthority, ParentBankId) VALUES('123456', 'BPI', 1, 1, 1, 1, null);



