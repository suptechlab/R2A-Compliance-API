CREATE TABLE bsp.TemplateRequirementCheck (
	ReportCode NVARCHAR(32) NOT NULL,
	TemplateGroupCode NVARCHAR(32) NOT NULL,
	TemplateSubCode NVARCHAR(32) NOT NULL,
	UcbBanks BIT NOT NULL DEFAULT 1,
	ThriftBanks BIT NOT NULL DEFAULT 1,
	RuralBanks BIT NOT NULL DEFAULT 1,
	Frequency CHAR(1) NOT NULL DEFAULT 'M',
	RequiresResourcesP1Billion BIT NOT NULL DEFAULT 0,
	RequiresTrustAuthority BIT NOT NULL DEFAULT 0,
	RequiresEmoneyAuthority BIT NOT NULL DEFAULT 0,
	RequiresUcbOrUcbParent BIT NOT NULL DEFAULT 0
);
ALTER TABLE bsp.TemplateRequirementCheck ADD CONSTRAINT PK_TemplateRequirementCheck PRIMARY KEY (ReportCode, TemplateGroupCode, TemplateSubCode);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'EMONEY', '*',  1, 1, 1, 'Q', 0, 0, 1, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'MIFIR', 'MBS', 1, 1, 1, 'M', 0, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'MIFIR', 'MIS', 1, 1, 1, 'Q', 0, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'REE', '*',     1, 1, 0, 'Q', 0, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'CAR', '*',     1, 1, 1, 'Q', 0, 0, 0, 1);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'Agra', '*',    1, 1, 1, 'Q', 0, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'BRIS', '*',    1, 1, 1, 'Q', 0, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'FRPTI', '*',   1, 1, 1, 'Q', 0, 1, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'LCR', '*',     1, 0, 0, 'M', 0, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'MSME', '*',    1, 1, 1, 'Q', 0, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'PBS', '*',     1, 1, 1, 'Q', 1, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'RCPB', '*',    1, 1, 0, 'Q', 0, 0, 0, 1);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'STRS', '*',    1, 1, 0, 'S', 0, 0, 0, 0);
INSERT INTO bsp.TemplateRequirementCheck VALUES('FRP', 'REPO', '*',    1, 1, 0, 'M', 0, 0, 0, 1);