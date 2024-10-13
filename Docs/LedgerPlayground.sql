
/****** Object:  Table [dbo].[acAccountingDetails]    Script Date: 2024-10-13 18:25:20 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[acLedgerEntry](
	[Id] [uniqueidentifier] NOT NULL,
	[ParentBranchId] [uniqueidentifier] NOT NULL,
	[ParentPathLocator] [nvarchar](255) NOT NULL,
	[AdditionalInformation] [nvarchar](max) NULL,
	
	[Series] [nvarchar](50) NULL,
	[VoucherNumber] [bigint] NOT NULL,

	[LedgerDate] [Date] NOT NULL,
	[CreateDate] [datetime2] NOT NULL,
	[Version] [int] NOT NULL,

	/*[AccountedObjectId] [uniqueidentifier] NOT NULL,
	[Currency] [varchar](3) NOT NULL,
	[Status] [int] NOT NULL,
	[AccountingType] [int] NOT NULL,
	[ItemType] [int] NOT NULL,
	[PeriodGroups] [nvarchar](max) NULL,*/
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


ALTER TABLE [dbo].[acLedgerEntry] ADD  DEFAULT ((1)) FOR [Version]
GO

ALTER TABLE [dbo].acLedgerEntry  WITH CHECK ADD  CONSTRAINT [FK_acLedgerEntry_CompanyBranch] FOREIGN KEY([ParentBranchId])
REFERENCES [dbo].[CompanyBranch] ([Id])
GO

ALTER TABLE [dbo].acLedgerEntry CHECK CONSTRAINT [FK_acLedgerEntry_CompanyBranch]
GO


-------------------------
DROP TABLE [acLedgerRow] 

CREATE TABLE [dbo].[acLedgerRow](
	[Id] [uniqueidentifier] NOT NULL,
	[ParentBranchId] [uniqueidentifier] NOT NULL,
	[ParentPathLocator] [nvarchar](255) NOT NULL,
	[LedgerEntryId] [uniqueidentifier] NOT NULL,
	[Amount] [decimal](19, 4) NOT NULL,
	[LedgerDate] [Date] NOT NULL,
	[Dimensions] [nvarchar](max) NOT NULL,
	[DimensionAccount] [nvarchar](50) NULL,
	[Version] [int] NOT NULL,
	[AdditionalInformation] [nvarchar](max) NULL,
	
	PRIMARY KEY CLUSTERED ([Id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[acLedgerRow] ADD  DEFAULT ((1)) FOR [Version]
GO

ALTER TABLE [dbo].[acLedgerRow]  WITH CHECK ADD CONSTRAINT [FK_LedgerRow_LedgerEntry] FOREIGN KEY([LedgerEntryId])
REFERENCES [dbo].[acLedgerEntry] ([Id])
GO

ALTER TABLE [dbo].[acLedgerRow] CHECK CONSTRAINT [FK_LedgerRow_LedgerEntry]
GO

---------------------

INSERT INTO acLedgerEntry ([Id],[ParentBranchId],[ParentPathLocator],[AdditionalInformation],[Series],[VoucherNumber],[LedgerDate],[CreateDate]) VALUES
(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2'
,<AdditionalInformation, nvarchar(max),>
,<Series, nvarchar(50),>
,<VoucherNumber, bigint,>
,<LedgerDate, date,>
,GETDATE())
GO

INSERT INTO acLedgerEntry ([Id],[ParentBranchId],[ParentPathLocator],[AdditionalInformation],[Series],[VoucherNumber],[LedgerDate],[CreateDate]) VALUES
(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2','','A','1','20220103',GETDATE())
,(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2','','A','2','20220103',GETDATE())
,(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2','','A','3','20220103',GETDATE())
,(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2','','A','4','20220103',GETDATE())
,(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2','','A','5','20220103',GETDATE())
,(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2','','A','6','20220103',GETDATE())
,(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2','','A','7','20220103',GETDATE())
,(dbo.NewCombId(NEWID()),'1E614737-72E3-4AD8-B077-12D7CE70E52C','1.7.3.2','','A','8','20220103',GETDATE())

USE [MicroCoz]
GO

INSERT INTO acLedgerRow ([Id],[ParentBranchId],[ParentPathLocator],[LedgerEntryId],[Amount],[LedgerDate],[Dimensions],[DimensionAccount],[AdditionalInformation]) VALUES
(<Id, uniqueidentifier,>
,<ParentBranchId, uniqueidentifier,>
,<ParentPathLocator, nvarchar(255),>
,<LedgerEntryId, uniqueidentifier,>
,<Amount, decimal(19,4),>
,<LedgerDate, date,>
,<Dimensions, nvarchar(max),>
,<DimensionAccount, nvarchar(50),>
,<AdditionalInformation, nvarchar(max),>)
GO



/*
Id										ParentBranchId			ParentPathLocator	ShortId	PartyId	Name	PathLocator	Version
11111111-1111-1111-1111-111111111111	NULL										1	NULL	Root	1	1
A1D32F31-2580-E459-A603-36976E33F109	11111111-1111-1111-1111-111111111111	1	2	NULL	External Systems	1.2	1
55DFBBDC-B502-CAE0-F6AC-247B35D54B7C	A1D32F31-2580-E459-A603-36976E33F109	1.2	3	NULL	Inbox Clientid 9122	1.2.3	1
7EDB0EAB-9954-54C7-1D33-D626CB1FA1EE	A1D32F31-2580-E459-A603-36976E33F109	1.2	4	NULL	Inbox Clientid 9954	1.2.4	1
17B0C500-01CA-01CA-A738-B764DCF8D3F4	A1D32F31-2580-E459-A603-36976E33F109	1.2	5	NULL	Inbox Clientid 13	1.2.5	1
498AF079-0565-0565-8EA7-0F060444BCDF	A1D32F31-2580-E459-A603-36976E33F109	1.2	6	NULL	Inbox Clientid 999	1.2.6	1
C2C2C2C2-0046-C21F-981D-96E4F6BF358A	A1D32F31-2580-E459-A603-36976E33F109	1.2	7	NULL	Cosmoz 2 RCS	1.2.7	1
C20000C2-0047-DE16-B93E-EBC7BEB45AD7	A1D32F31-2580-E459-A603-36976E33F109	1.2	8	NULL	Cosmoz 2 RCN	1.2.8	1
D72819BD-918E-447B-AFF5-AE5D4C21C3D8	11111111-1111-1111-1111-111111111111	1	4	NULL	Self sign-up companies	1.4	1
*/
INSERT INTO CompanyBranch ([Id],[ParentBranchId],[ParentPathLocator],[ShortId],[Name],[PathLocator]) VALUES
('11111111-1111-1111-1111-111111111111',null,'',1,'Root','1'),
(dbo.NewCombId(NEWID()),'11111111-1111-1111-1111-111111111111','1',1,'Client root','1.1')

select * from CompanyBranch order by PathLocator

select * from acLedgerEntry -- truncate table acLedgerEntry;truncate table acLedgerRow
select * from acLedgerRow


select * from acLedgerRow where DimensionAccount like '3%' order by abs(amount) desc
select * from acLedgerRow r join acLedgerEntry e on e.Id=r.LedgerEntryId where DimensionAccount like '3%' order by abs(amount) desc
select min(DimensionAccount) acc, count(*) n, sum(amount) totAmt, sum(abs(amount)) totAbs, sum(case when amount>0 then amount else 0 end) debit, sum(case when amount<0 then amount else 0 end) kredit from acLedgerRow where DimensionAccount like '1%'
select min(DimensionAccount) acc, count(*) n, sum(amount) totAmt, sum(abs(amount)) totAbs, sum(case when amount>0 then amount else 0 end) debit, sum(case when amount<0 then amount else 0 end) kredit from acLedgerRow where DimensionAccount like '2%'
select min(DimensionAccount) acc, count(*) n, sum(amount) totAmt, sum(abs(amount)) totAbs, sum(case when amount>0 then amount else 0 end) debit, sum(case when amount<0 then amount else 0 end) kredit from acLedgerRow where DimensionAccount like '3%'
select min(DimensionAccount) acc, count(*) n, sum(amount) totAmt, sum(abs(amount)) totAbs, sum(case when amount>0 then amount else 0 end) debit, sum(case when amount<0 then amount else 0 end) kredit from acLedgerRow where DimensionAccount >= '4000'

select * from acLedgerRow r join acLedgerEntry e on e.Id=r.LedgerEntryId where DimensionAccount like '3%' order by (amount) desc

