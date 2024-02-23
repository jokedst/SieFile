using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Text;

/// <summary>
/// Represents a SIE file (v 1-4). 
/// </summary>
/// <remarks>
/// Might be split into a reader and writer later to make the code cleaner.
/// </remarks>
public class SieFile
{
    public bool AlreadyImportedFlag {  get; set; }
    public SieFileType FileType { get; set; }
    public string Program { get; set; }
    public string ProgramVersion { get; private set; }
    public string Contact { get; set; }
    public string AdressLine1 { get; set; }
    public string AdressLine2 { get; set; }
    public string Phone { get; set; }
    public string CompanySNI { get; set; }
    public string CompanyName { get; set; }
    public DateOnly Generated {  get; set; }
    public string GeneratedBy { get; set; }

    public Dictionary<string, Dimension> Dimensions { get; set; } = [];
    public Dictionary<string, Account> Accounts { get; set; } = [];
    // For now just a list. Could be a dictionary by year, account and dimensions.
    public List<Balance> Balances { get; set; } = [];
    public List<ObjectAmount> PeriodChanges { get; set; } = [];
    public List<string> Notes { get; set; } = [];
    public Dictionary<string, (string StartDate, string EndDate)> Years { get; set; } = [];

    public IList<string> Errors => _errors;
    public IList<string> Warnings => _warnings;

    public string InternalCompanyId { get;  set; }
    /// <summary>
    /// Type of company as defined by Bolagsverket. Valid values are AB,E,HB,KB,EK,KHF,BRF,BF,SF,I,S,FL,BAB,MB,SB,BFL,FAB,OFB,SE,SCE,TSF,X
    /// </summary>
    public string CompanyType { get; private set; }
    /// <summary>
    /// Base accounting plan, e.g "BAS95" (default), "EUBAS97" etc.
    /// </summary>
    public string BaseAccountPlan { get; private set; }
    /// <summary>
    /// End date for period balances (file type 2 & 3)
    /// </summary>
    public string BalanceEndDate { get; private set; }
    public string OrganisationNumber { get; private set; }
    public string OrganisationInternalNumber { get; private set; }
    public string OrganisationInternalNumber2 { get; private set; }
    public string TaxationYear { get; private set; }
    public string Currency { get; private set; }

    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];
    // Reading-specific: Kinda hackish - to not have to send these parameters to all read functions
    private string[] _parts;
    private int _rowNumber;



    /// <summary>
    /// Reads a SIE file of type 1-4
    /// </summary>
    /// <param name="stream">Stream to read from. Must be in encoding PC8, aka codepage 427.</param>
    /// <param name="filename">
    /// The only difference between a type 4 import (4I) and export (4E) file seems to be the file extension, so this is needed to know what file type it is.
    /// Only used for type 4 files, and only the file extension is evaluated.
    /// </param>
    public void Read(Stream stream, string filename)
    {
        _errors.Clear();
        _warnings.Clear();
        Dimensions.Clear();
        Accounts.Clear();
        _rowNumber = 0;
        //TODO: Clear the rest :P
        var foundTypes = new HashSet<string>();

        // Ensure codepage 437 is loaded
        if (!Encoding.GetEncodings().Any(x => x.CodePage == 437))
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        var encoding = Encoding.GetEncoding(437);

        using StreamReader sr = new StreamReader(stream, encoding);
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            _rowNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue; // Empty lines are allowed and should be ignored.
            line = line.TrimStart();
            if (line[0] != '#')
            {
                _warnings.Add($"Row {_rowNumber} does not start with a '#'");
                continue;
            }

            _parts = line.SplitOutsideQuotes();
            var rowType = _parts[0];
            foundTypes.Add(rowType);

            switch (rowType)
            {
                case "#FLAGGA":
                    if (Assert(_parts.Length >= 2, "#FLAGGA is missing parameters") && 
                        Assert(_parts[1] == "0" || _parts[1] == "1", $"#FLAGGA has invalid value '{_parts[1]}'"))
                        this.AlreadyImportedFlag = _parts[1] == "1";
                    break;
                case "#PROGRAM": this.Program = Required(1); this.ProgramVersion = Required(2); break;
                case "#FORMAT": WarnIf(Optional(1) != "PC8","Only format PCB is allowed"); break;
                case "#GEN": 
                    if (!AssertParameters(1)) break;
                    if (Assert(DateOnly.TryParseExact(_parts[1], "yyyyMMdd", out var generated), "Could not parse #GEN date"))
                        this.Generated = generated;
                    this.GeneratedBy = Optional(2);
                    break;
                case "#SIETYP":
                    switch (Required(1))
                    {
                        case "1": this.FileType = SieFileType.Type1; break;
                        case "2": this.FileType = SieFileType.Type2; break;
                        case "3": this.FileType = SieFileType.Type3; break;
                        case "4": this.FileType = filename.EndsWith(".se", StringComparison.InvariantCultureIgnoreCase) ? SieFileType.Type4E : SieFileType.Type4I; 
                            break;
                        default: _errors.Add($"Post '#SIETYP' has invalid value (row {_rowNumber})"); break;
                    }
                    break;
                case "#PROSA": this.Notes.Add(Required(1)); break;
                case "#FTYP": this.CompanyType = Required(1); break;
                case "#FNR": this.InternalCompanyId = Required(1); break;
                case "#ORGNR": this.OrganisationNumber = Required(1); this.OrganisationInternalNumber = Optional(2); this.OrganisationInternalNumber2 = Optional(3); break;
                case "#BKOD": this.CompanySNI = Optional(1); break;
                case "#ADRESS": this.Contact = Optional(1); this.AdressLine1 = Optional(2); this.AdressLine2 = Optional(3); this.Phone = Optional(4); break;
                case "#FNAMN": this.CompanyName = Required(1); break;
                case "#RAR": this.Years[YearIndex(1)] = (Required(2), Required(3)); break;
                case "#TAXAR": this.TaxationYear = Year(1); break;
                case "#OMFATTN": this.BalanceEndDate = Required(1); break;
                case "#KPTYP": this.BaseAccountPlan = Required(1); break;
                case "#VALUTA": this.Currency = Required(1); break;
                case "#KONTO":
                    if (AssertParameters(2))
                    {
                        if (this.Accounts.TryGetValue(_parts[1], out var account))
                            account.Name = _parts[2];
                        else
                            this.Accounts.Add(_parts[1], new Account { Name = _parts[2] });
                    }
                    break;
                case "#DIM":
                    if (AssertParameters(2))
                    {
                        if (this.Dimensions.TryGetValue(_parts[1], out var dimension))
                            dimension.Name = _parts[2];
                        else
                            this.Dimensions.Add(_parts[1], new Dimension { Name = _parts[2] });
                    }
                    break;
                case "#UNDERDIM":
                    if (AssertParameters(3))
                    {
                        if (!this.Dimensions.TryGetValue(_parts[1], out var dimension))
                            this.Dimensions[_parts[1]] = dimension = new Dimension();
                        dimension.Name = _parts[2];
                        dimension.ParentDimension = _parts[3];
                    }
                    break;
                case "#KTYP":
                    if (!AssertParameters(2) || 
                        WarnIf(!this.Accounts.ContainsKey(_parts[1]), $"Account {_parts[1]} is not declared") || 
                        WarnIf(!new[] { "T", "S", "K", "I" }.Contains(_parts[2]),$"Account type {_parts[2]} is unknown"))
                        break;
                    this.Accounts[_parts[1]].Type = _parts[2][0];
                    break;
                case "#ENHET": 
                    if (!AssertParameters(2) || WarnIf(!this.Accounts.ContainsKey(_parts[1]), $"Account {_parts[1]} is not declared")) 
                        break;
                    this.Accounts[_parts[1]].Unit = _parts[2];
                    break;
                case "#SRU":
                    if (AssertParameters(2))
                    {
                        if (!this.Accounts.TryGetValue(_parts[1], out var account))
                            this.Accounts[_parts[1]] = account = new Account();
                        account.SRU = _parts[2];
                    }
                    break;
                case "#OBJEKT":
                    if (AssertParameters(3))
                    {
                        // This dimension might not be defined in the file, it might come from the reserved dimensions in the SIE format, 1-19
                        if (!this.Dimensions.TryGetValue(_parts[1], out var dimension))
                        {
                            // TODO: Warn if the dimension isn't 1-19 (?)
                            this.Dimensions[_parts[1]] = dimension = new Dimension();
                        }
                        dimension.Values[_parts[2]] = _parts[3];
                    }
                    break;
                case "#IB": 
                    if (AssertParameters(3))
                    {
                        decimal? quantityIB = null;
                        Assert(decimal.TryParse(_parts[3], CultureInfo.InvariantCulture, out var balance), "Could not parse #IB balance");
                        if (_parts.Length > 4 && Assert(decimal.TryParse(_parts[4], CultureInfo.InvariantCulture, out var quantity), "Could not parse #IB quantity"))
                            quantityIB = quantity;
                        this.Balances.Add(new Balance { YearIndex = _parts[1], Account = _parts[2], Amount = balance, Quantity = quantityIB, IncomingBalance = true });
                    }
                    break;
                case "#UB":
                    if (AssertParameters(3))
                    {
                        decimal? quantityUB = null;
                        Assert(decimal.TryParse(_parts[3], CultureInfo.InvariantCulture, out var balance), "Could not parse #UB balance");
                        if (_parts.Length > 4 && Assert(decimal.TryParse(_parts[4], CultureInfo.InvariantCulture, out var quantity), "Could not parse #UB quantity"))
                            quantityUB = quantity;
                        this.Balances.Add(new Balance { YearIndex = _parts[1], Account = _parts[2], Amount = balance, Quantity = quantityUB, IncomingBalance = true });
                    }
                    break;
                case "#OIB":
                case "#OUB":
                    if (AssertParameters(4))
                    {
                        decimal? quantityUB = null;
                        Assert(decimal.TryParse(_parts[4], CultureInfo.InvariantCulture, out var balance), $"Could not parse {rowType} balance");
                        if (_parts.Length > 5 && Assert(decimal.TryParse(_parts[5], CultureInfo.InvariantCulture, out var quantity), $"Could not parse {rowType} quantity"))
                            quantityUB = quantity;
                        
                        if(!Assert(_parts[3].StartsWith('{') && _parts[3].EndsWith('}'), $"{rowType} dimensions invalid")) break;
                        var dimString = _parts[3].Substring(1, _parts[3].Length - 2);
                        var dimParts = dimString.SplitOutsideQuotes();
                        Assert(dimParts.Length % 2 == 0, $"{rowType} dimensions invalid");
                        var dimensions = new Dictionary<string, string>();
                        for (var i = 0; i < dimParts.Length / 2; i++)
                            dimensions[dimParts[i * 2]] = dimParts[i * 2 + 1];

                        this.Balances.Add(new Balance { YearIndex = _parts[1], Account = _parts[2], Amount = balance, Quantity = quantityUB, Dimensions = dimensions, IncomingBalance = rowType == "#OIB" });
                    }
                    break;
                case "#RES": // Not entierly sure, but it seems this is the same as "UB" but for accounts of type K or I (expense or revenue)
                    if (AssertParameters(3))
                    {
                        this.Balances.Add(new Balance { YearIndex = YearIndex(1), Account = _parts[2], Amount = Decimal(3), Quantity = _parts.Length > 4 ? Decimal(4) : null });
                    }
                    break; 
                case "#PSALDO": 
                case "#PBUDGET":
                    if (AssertParameters(5))
                    {
                        this.PeriodChanges.Add(new ObjectAmount(YearIndex(1), Period(2), AmountTypeForRow(), _parts[3], ParseDictionary(4), Decimal(5), _parts.Length > 6 ? Decimal(6) : null));
                    }
                    break;
                case "#VER":
                    while ((line = sr.ReadLine()) != null)
                    {
                        _rowNumber++;
                        if (line.Trim() == "}")
                            break;
                    }
                    Assert(line?.Trim() == "}", "File ended without closing #VER post");                  
                    break;
                case "#TRANS":
                case "#RTRANS":
                case "#BTRANS":
                case "#KSUMMA":
                default: break; // Unknown key words should be ignored.
            }
        }
    }   

    /// <summary> Get an optional part of line. No warning if the part is missing. </summary>
    private string Optional(int index)
    {
        if (_parts?.Length > index)
            return _parts[index];
        return null;
    }

    /// <summary>
    /// Get an required part of the current line. If missing an error is logged.
    /// </summary>
    private string Required(int index)
    {
        if(_parts?.Length > index) 
            return _parts[index];
        _errors.Add($"Post '{_parts[0]}' is missing parameter {index} (row {_rowNumber})");
        return null;
    }

    private bool WarnIf(bool statement, string warning)
    {
        if(statement)
        {
            _warnings.Add(warning + $" (row {_rowNumber})");
        }
        return statement;
    }

    private bool Assert(bool statement, string errorText)
    {
        if (!statement)
            _errors.Add(errorText + $" (row {_rowNumber})");
        return statement;
    }

    /// <summary>
    /// Ensures parameter exists, and is a year index.
    /// </summary>
    private string YearIndex(int index)
    {
        if (_parts?.Length <= index)
        {
            _errors.Add($"Post '{_parts[0]}' is missing parameter {index}. (row {_rowNumber})");
            return null;
        }
        if (!int.TryParse(_parts[index], out var year) || Math.Abs(year) > 100)
            _errors.Add($"Post '{_parts[0]}' parameter {index} ('{_parts[index]}') is not a valid year index. (row {_rowNumber})");
        return _parts[index];
    }

    /// <summary>
    /// Ensures parameter exists, and is a valid year.
    /// </summary>
    private string Year(int index)
    {
        if (Required(index) == null) return null;
        if (_parts[index].Length != 4 || !DateOnly.TryParseExact(_parts[index], "yyyy", out _))
            _errors.Add($"Post '{_parts[0]}' parameter {index} ('{_parts[index]}') is not a valid year. (row {_rowNumber})");
        return _parts[index];
    }

    /// <summary>
    /// Ensures parameter exists, and is a valid period.
    /// </summary>
    private string Period(int index)
    {
        if (Required(index) == null) return null;
        //if (_parts?.Length <= index)
        //{
        //    _errors.Add($"Post '{_parts[0]}' is missing parameter {index}. (row {_rowNumber})");
        //    return null;
        //}
        if (_parts[index].Length != 6 || !DateOnly.TryParseExact(_parts[index], "yyyyMM", out _))
            _errors.Add($"Post '{_parts[0]}' parameter {index} ('{_parts[index]}') is not a valid period. (row {_rowNumber})");
        return _parts[index];
    }

    /// <summary>
    /// Ensures parameter exists, and is a valid decimal number.
    /// </summary>
    private decimal Decimal(int index)
    {
        if (_parts?.Length <= index)
        {
            _errors.Add($"Post '{_parts[0]}' is missing parameter {index}. (row {_rowNumber})");
            return 0;
        }
        if (!decimal.TryParse(_parts[index], CultureInfo.InvariantCulture, out var amount))
        {
            _errors.Add($"Post '{_parts[0]}' parameter {index} ('{_parts[index]}') is not a valid number. (row {_rowNumber})");
            return 0;
        }
        return amount;
    }

    private AmountType AmountTypeForRow()
    {
        return _parts[0] switch
        {
            "#IB" or "#OIB" => AmountType.IncomingBalance,
            "#UB" or "#OUB" => AmountType.OutgoingBalance,
            "#PSALDO" => AmountType.PeriodChange,
            "#PBUDGET" => AmountType.PeriodBudgetChange,
            _ => throw new InvalidOperationException($"Post '{_parts[0]}' has no associated amount type. (row {_rowNumber})")
        };
    }

    /// <summary>
    /// Parses a SIE dimension string ("{key value key2 value2}") into a dictionary
    /// </summary>
    private Dictionary<string, string> ParseDictionary(int index)
    {
        if (Required(index) == null || 
            !Assert(_parts[index].StartsWith('{') && _parts[index].EndsWith('}'), $"Post '{_parts[0]}' dimensions invalid")) return null;
        var dimParts = _parts[index][1..^1].SplitOutsideQuotes();
        Assert(dimParts.Length % 2 == 0, $"Post '{_parts[0]}' dimensions invalid");
        var dimensions = new Dictionary<string, string>();
        for (var i = 0; i < dimParts.Length / 2; i++)
            dimensions[dimParts[i * 2]] = dimParts[i * 2 + 1];
        return dimensions;
    }

    /// <summary>
    /// Asserts current row has at least given number of parameters.
    /// ("#X 1 2" is considered 2 parameters)
    /// </summary>
    private bool AssertParameters(int parameterCount)
    {
        if (_parts.Length <= parameterCount)
        {
            _errors.Add($"Post '{_parts[0]}' does not have {parameterCount} parameters (row {_rowNumber})");
            return false;
        }
        return true;
    }

    public void Write(Stream outputStream, int sieType, string program)
    {        
        // Ensure codepage 437 is loaded
        if (!Encoding.GetEncodings().Any(x => x.CodePage == 437))        
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        using StreamWriter sr = new StreamWriter(outputStream, Encoding.GetEncoding(437));

        sr.WriteLine("#FLAGGA 0");
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
/// Represents the balance of an account or object at a given time.
/// </summary>
public class Balance
{
    /// <summary> Year, as defined by a #RAR row (typically 0 for current year) </summary>
    public string YearIndex { get; set; }
    /// <summary>
    /// If true this represents the start of the period, if false the end.
    /// These might overlap, e.g. incoming for year 0 and outgoing for year 1 should be the same value (if both exist)
    /// </summary>
    public bool IncomingBalance {  get; set; }
    public string Account {  get; set; }
    /// <summary>
    /// (optional) Specifies dimension values for this balance. If null this is an account balance.
    /// </summary>
    public Dictionary <string, string> Dimensions { get; set; }
    public Decimal Amount { get; set; }
    public Decimal? Quantity { get; set; }
}

/// <summary>
/// Represents the balance of an account or object at a given time.
/// </summary>
public class ObjectAmount
{
    public ObjectAmount(string yearIndex, string period, AmountType type, string account, Dictionary<string, string> dimensions, decimal amount, decimal? quantity)
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
    IncomingBalance,
    OutgoingBalance,
    PeriodChange,
    PeriodBudgetChange
}

public enum SieFileType
{
    Type1,
    Type2,
    Type3,
    Type4I,
    Type4E
}