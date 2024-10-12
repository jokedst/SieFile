﻿using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>
/// Represents a SIE file (v 1-4). 
/// </summary>
public class SieFile
{
    internal SieFile() { }

    public SieFile(SieFileType fileType, string program, string programVersion, string companyName, DateOnly currentYearStart, DateOnly? currentYearEnd = null)
    {
        Generated = DateOnly.FromDateTime(DateTime.Now);
        Program = program;
        ProgramVersion = programVersion;
        FileType = fileType;
        CompanyName = companyName;
        currentYearEnd ??= currentYearStart.AddYears(1).AddDays(-1);
        Years["0"] = (currentYearStart.ToString("yyyyMMdd"), currentYearEnd.Value.ToString("yyyyMMdd"));
    }

    public bool AlreadyImportedFlag { get; set; }
    public SieFileType FileType { get; set; }
    public string Program { get; set; }
    public string ProgramVersion { get; set; }
    public string Contact { get; set; }
    public string AdressLine1 { get; set; }
    public string AdressLine2 { get; set; }
    public string Phone { get; set; }
    public string CompanySNI { get; set; }
    public string CompanyName { get; set; }
    public DateOnly Generated { get; set; }
    public string GeneratedBy { get; set; }

    public Dictionary<string, Dimension> Dimensions { get; set; } = [];
    public Dictionary<string, Account> Accounts { get; set; } = [];
    public List<PeriodSummary> PeriodSummeries { get; set; } = [];
    public List<string> Notes { get; set; } = [];
    public Dictionary<string, (string StartDate, string EndDate)> Years { get; set; } = [];
    public List<Verification> Verifications { get; set; } = [];

    /// <summary>
    /// Financial reporting program's internal code for the company which is exported.
    /// </summary>
    public string InternalCompanyId { get; set; }
    /// <summary>
    /// Type of company as defined by Bolagsverket. Valid values are AB,E,HB,KB,EK,KHF,BRF,BF,SF,I,S,FL,BAB,MB,SB,BFL,FAB,OFB,SE,SCE,TSF,X
    /// </summary>
    public string CompanyType { get; set; }
    /// <summary>
    /// Base accounting plan, e.g "BAS95" (default), "EUBAS97" etc.
    /// </summary>
    public string BaseAccountPlan { get; set; }
    /// <summary>
    /// End date for period balances (file type 2 & 3)
    /// </summary>
    public string BalanceEndDate { get; set; }
    public string OrganisationNumber { get; set; }
    public string OrganisationInternalNumber { get; set; }
    public string OrganisationInternalNumber2 { get; set; }
    public string TaxationYear { get; set; }
    public string Currency { get; set; }

    /// <summary>
    /// If you build a SIE file from scratch, some fields can be calculated and set automatically. 
    /// </summary>
    public void ValidateAndFill()
    {
        if (BalanceEndDate == null && (  FileType == SieFileType.Type2 || FileType == SieFileType.Type3))
        {
            BalanceEndDate = Years["0"].EndDate;
        }

        //TODO: Ensure all accounts used are in this.Accounts
        //TODO: Ensure all account types are valid
        //TODO: Ensure all years referenced in balances are int this.Years
        //Type 1 and 2 can not have DIM

        if (FileType == SieFileType.Type4I)
        {
            // 4E can not have balance or period changes
            
            PeriodSummeries.Clear();
        }
    }
}

public class Dimension
{
    public string Name { get; set; }
    public Dictionary<string, string> Values { get; } = [];
    public string ParentDimension { get; set; }
}

public class Account
{
    public string Name { get; set; }
    public char Type {  get; set; }
    public string SRU { get; set; }
    public string Unit { get; set; }
}

/// <summary>
/// Represents the balance or result of an account or object at a given time.
/// </summary>
public class PeriodSummary
{
    public PeriodSummary(string yearIndex, string period, AmountType type, string account, Dictionary<string, string> dimensions, decimal amount, decimal? quantity)
    {
        YearIndex = yearIndex;
        Period = period;
        Type = type;
        Account = account;
        Dimensions = dimensions;
        Amount = amount;
        Quantity = quantity;
    }

    /// <summary> Year, as defined by a #RAR row (typically 0 for current year) </summary>
    public string YearIndex { get; set; }
    /// <summary>
    /// (optional) If this a change, this speicifes the month on the format yyyyMM.
    /// </summary>
    public string Period { get; set; }
    /// <summary>
    /// Type of amount.
    /// Changes must have period; balances must NOT have periods.
    /// </summary>
    public AmountType Type { get; set; }
    public string Account { get; set; }
    /// <summary>
    /// (optional) Specifies dimension values for this amount. If null this is an account amount.
    /// </summary>
    public Dictionary<string, string> Dimensions { get; set; }
    public Decimal Amount { get; set; }
    public Decimal? Quantity { get; set; }
}

public enum AmountType
{
    /// <summary>
    /// Incoming balance at the start of year. Only balance accounts (T and S).
    /// </summary>
    IncomingBalance,
    OutgoingBalance,
    ObjectIncomingBalance,
    ObjectOutgoingBalance,
    Result,
    PeriodChange,
    PeriodBudgetChange
}

[Flags]
public enum SieFileType
{
    Type1=1,
    Type2=2,
    Type3=4,
    Type4I=8,
    Type4E=16
}

/// <summary>
/// Represents an entry in the ledger, with a set of balancing rows
/// </summary>
public class Verification
{
    public Verification(string series, string voucherNumber, DateOnly date, string text, DateOnly? registrationDate, string user)
    {
        Series = series;
        VoucherNumber = voucherNumber;
        Date = date;
        Text = text;
        RegistrationDate = registrationDate;
        User = user;
    }

    public string Series {  get; set; }
    public string VoucherNumber { get; set; }
    public DateOnly Date { get; set; }
    public string Text { get; set; }
    public DateOnly? RegistrationDate { get; set; }
    public string User { get; set; }
    public List<VerificationRow> Rows { get; set; } = [];
    /// <summary> If the verificate has been modified, this will contain a copy of all added rows, with info about who did the change. This can be ignored, it's only history. </summary>
    public List<VerificationRow> AddedRows { get; set; } = [];
    /// <summary> If the verificate has been modified, this will contain the original rows before change. This can be ignored, it's only history. </summary>
    public List<VerificationRow> RemovedRows { get; set; } = [];
}

public class VerificationRow
{
    public VerificationRow(string account, Dictionary<string, string> dimensions, decimal amount, DateOnly? transactionDate, string text, decimal? quantity, string user, int rowNumber)
    {
        Account = account;
        Dimensions = dimensions;
        Amount = amount;
        TransactionDate = transactionDate;
        Text = text;
        Quantity = quantity;
        User = user;
        RowNumber = rowNumber;
    }

    public string Account { get; }
    public Dictionary<string, string> Dimensions { get; }
    public decimal Amount { get; }
    public DateOnly? TransactionDate { get; }
    public string Text { get; }
    public decimal? Quantity { get; }
    public string User { get; }
    /// <summary> Used to keep track of where added and removed rows belong. </summary>
    public int RowNumber { get; set; }
}