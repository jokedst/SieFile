using System.Globalization;
using System.Text;

/// <summary>
/// Read a SIE file (type 1-4).
/// </summary>
/// <remarks>
/// This class is NOT thread safe.
/// </remarks>
public class SieFileReader 
{ 
    public IList<string> Errors => _errors;
    public IList<string> Warnings => _warnings;
    private List<string> _errors = [];
    private List<string> _warnings = [];

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
    public SieFile Read(Stream stream, string filename)
    {
        var sie = new SieFile();
        _rowNumber = 0; _errors = []; _warnings = [];
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

            _parts = line.SplitSieLine();
            var rowType = _parts[0];
            foundTypes.Add(rowType);

            switch (rowType)
            {
                case "#FLAGGA":
                    if (Assert(_parts.Length >= 2, "#FLAGGA is missing parameters") && 
                        Assert(_parts[1] == "0" || _parts[1] == "1", $"#FLAGGA has invalid value '{_parts[1]}'"))
                        sie.AlreadyImportedFlag = _parts[1] == "1";
                    break;
                case "#PROGRAM": sie.Program = Required(1); sie.ProgramVersion = Required(2); break;
                case "#FORMAT": WarnIf(Optional(1) != "PC8","Only format PCB is allowed"); break;
                case "#GEN": 
                    if (!AssertParameters(1)) break;
                    if (Assert(DateOnly.TryParseExact(_parts[1], "yyyyMMdd", out var generated), "Could not parse #GEN date"))
                        sie.Generated = generated;
                    sie.GeneratedBy = Optional(2);
                    break;
                case "#SIETYP":
                    switch (Required(1))
                    {
                        case "1": sie.FileType = SieFileType.Type1; break;
                        case "2": sie.FileType = SieFileType.Type2; break;
                        case "3": sie.FileType = SieFileType.Type3; break;
                        case "4":
                            sie.FileType = filename.EndsWith(".se", StringComparison.InvariantCultureIgnoreCase) ? SieFileType.Type4E : SieFileType.Type4I; 
                            break;
                        default: _errors.Add($"Post '#SIETYP' has invalid value (row {_rowNumber})"); break;
                    }
                    break;
                case "#PROSA": sie.Notes.Add(Required(1)); break;
                case "#FTYP": sie.CompanyType = Required(1); break;
                case "#FNR": sie.InternalCompanyId = Required(1); break;
                case "#ORGNR": sie.OrganisationNumber = Required(1); sie.OrganisationInternalNumber = Optional(2); sie.OrganisationInternalNumber2 = Optional(3); break;
                case "#BKOD": sie.CompanySNI = Optional(1); break;
                case "#ADRESS": sie.Contact = Optional(1); sie.AdressLine1 = Optional(2); sie.AdressLine2 = Optional(3); sie.Phone = Optional(4); break;
                case "#FNAMN": sie.CompanyName = Required(1); break;
                case "#RAR": sie.Years[YearIndex(1)] = (Required(2), Required(3)); break;
                case "#TAXAR": sie.TaxationYear = Year(1); break;
                case "#OMFATTN": sie.BalanceEndDate = Required(1); break;
                case "#KPTYP": sie.BaseAccountPlan = Required(1); break;
                case "#VALUTA": sie.Currency = Required(1); break;
                case "#KONTO":
                    if (AssertParameters(2))
                    {
                        if (sie.Accounts.TryGetValue(_parts[1], out var account))
                            account.Name = _parts[2];
                        else
                            sie.Accounts.Add(_parts[1], new Account { Name = _parts[2] });
                    }
                    break;
                case "#DIM":
                    if (AssertParameters(2))
                    {
                        if (sie.Dimensions.TryGetValue(_parts[1], out var dimension))
                            dimension.Name = _parts[2];
                        else
                            sie.Dimensions.Add(_parts[1], new Dimension { Name = _parts[2] });
                    }
                    break;
                case "#UNDERDIM":
                    if (AssertParameters(3))
                    {
                        if (!sie.Dimensions.TryGetValue(_parts[1], out var dimension))
                            sie.Dimensions[_parts[1]] = dimension = new Dimension();
                        dimension.Name = _parts[2];
                        dimension.ParentDimension = _parts[3];
                    }
                    break;
                case "#KTYP":
                    if (!AssertParameters(2) ||
                        WarnIf(!sie.Accounts.ContainsKey(_parts[1]), $"Account {_parts[1]} is not declared") ||
                        !Assert(new[] { "T", "S", "K", "I" }.Contains(_parts[2]), $"Account type {_parts[2]} is unknown"))
                        break;
                    sie.Accounts[_parts[1]].Type = _parts[2][0];
                    break;
                case "#ENHET": 
                    if (!AssertParameters(2) || WarnIf(!sie.Accounts.ContainsKey(_parts[1]), $"Account {_parts[1]} is not declared")) 
                        break;
                    sie.Accounts[_parts[1]].Unit = _parts[2];
                    break;
                case "#SRU":
                    if (AssertParameters(2))
                    {
                        if (!sie.Accounts.TryGetValue(_parts[1], out var account))
                            sie.Accounts[_parts[1]] = account = new Account();
                        account.SRU = _parts[2];
                    }
                    break;
                case "#OBJEKT":
                    if (AssertParameters(3))
                    {
                        // This dimension might not be defined in the file, it might come from the reserved dimensions in the SIE format, 1-19
                        if (!sie.Dimensions.TryGetValue(_parts[1], out var dimension))
                        {
                            // TODO: Warn if the dimension isn't 1-19 (?)
                            sie.Dimensions[_parts[1]] = dimension = new Dimension();
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
                        sie.Balances.Add(new Balance { YearIndex = _parts[1], Account = _parts[2], Amount = balance, Quantity = quantityIB, IncomingBalance = true });
                    }
                    break;
                case "#UB":
                    if (AssertParameters(3))
                    {
                        decimal? quantityUB = null;
                        Assert(decimal.TryParse(_parts[3], CultureInfo.InvariantCulture, out var balance), "Could not parse #UB balance");
                        if (_parts.Length > 4 && Assert(decimal.TryParse(_parts[4], CultureInfo.InvariantCulture, out var quantity), "Could not parse #UB quantity"))
                            quantityUB = quantity;
                        sie.Balances.Add(new Balance { YearIndex = _parts[1], Account = _parts[2], Amount = balance, Quantity = quantityUB, IncomingBalance = false });
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
                        var dimParts = dimString.SplitSieLine();
                        Assert(dimParts.Length % 2 == 0, $"{rowType} dimensions invalid");
                        var dimensions = new Dictionary<string, string>();
                        for (var i = 0; i < dimParts.Length / 2; i++)
                            dimensions[dimParts[i * 2]] = dimParts[i * 2 + 1];

                        sie.Balances.Add(new Balance { YearIndex = _parts[1], Account = _parts[2], Amount = balance, Quantity = quantityUB, Dimensions = dimensions, IncomingBalance = rowType == "#OIB" });
                    }
                    break;
                case "#RES": // Not entierly sure, but it seems this is the same as "UB" but for accounts of type K or I (expense or revenue)
                    if (AssertParameters(3))
                    {
                        sie.Balances.Add(new Balance { YearIndex = YearIndex(1), Account = _parts[2], Amount = Decimal(3), Quantity = _parts.Length > 4 ? Decimal(4) : null });
                    }
                    break; 
                case "#PSALDO": 
                case "#PBUDGET":
                    if (AssertParameters(5))
                    {
                        sie.PeriodChanges.Add(new ObjectAmount(YearIndex(1), Period(2), AmountTypeForRow(), _parts[3], ParseDictionary(4), Decimal(5), _parts.Length > 6 ? Decimal(6) : null));
                    }
                    break;
                case "#VER":
                    AssertParameters(3);
                    var entry = new Verification(Optional(1), Optional(2), ParseDate(3) ?? DateOnly.MinValue, Optional(4), ParseDate(5, false), Optional(6));
                    sie.Verifications.Add(entry);
                    var firstRow = true;
                    var verRowNumber = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        _rowNumber++;
                        if (string.IsNullOrWhiteSpace(line)) continue; // Empty lines are allowed and should be ignored.
                        line = line.TrimStart();
                        if (firstRow)
                        {
                            if (!Assert(line.Trim() == "{", "Post #VER was not followed by '{'")) break;
                            firstRow = false;
                            continue;
                        }
                        if (line.Trim() == "}") break;
                        if (line[0] != '#')
                        {
                            _warnings.Add($"Row {_rowNumber} does not start with a '#'");
                            continue;
                        }

                        _parts = line.SplitSieLine();
                        rowType = _parts[0];
                        foundTypes.Add(rowType);
                        switch (rowType)
                        {
                            case "#TRANS": if (AssertParameters(3))
                                {
                                    entry.Rows.Add(new VerificationRow(_parts[1], ParseDictionary(2), Decimal(3), ParseDate(4, false), Optional(5), _parts.Length > 6 ? Decimal(6) : null, Optional(7), verRowNumber++));
                                }
                                break;
                            // RTRANS and BTRANS is keeping track of history if a verificate has been changed. 
                            case "#RTRANS":
                                if (AssertParameters(3))
                                {
                                    entry.AddedRows.Add(new VerificationRow(_parts[1], ParseDictionary(2), Decimal(3), ParseDate(4, false), Optional(5), _parts.Length > 6 ? Decimal(6) : null, Optional(7), verRowNumber));
                                }
                                break;
                            case "#BTRANS":
                                if (AssertParameters(3))
                                {
                                    entry.RemovedRows.Add(new VerificationRow(_parts[1], ParseDictionary(2), Decimal(3), ParseDate(4, false), Optional(5), _parts.Length > 6 ? Decimal(6) : null, Optional(7), verRowNumber));                                    
                                }
                                break;
                            default: break; // Unknown key words should be ignored.
                        }
                    }
                    Assert(line?.Trim() == "}", "Post #VER was not closed with a '}'");
                    Assert(entry.Rows.Sum(r => r.Amount) == 0, "Post #VER sum of rows is not zero");
                    break;
                case "#KSUMMA": // This is for calculating and verifying a CRC32 checksum. Not seen in the wild. Might implement later.
                    break;
                default: break; // Unknown key words should be ignored.
            }
        }
        return sie;
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
    /// Ensures parameter exists, and is a valid date.
    /// </summary>
    private DateOnly? ParseDate(int index, bool required = true)
    {
        if (required && Required(index) == null) return null;

        if (_parts?.Length <= index || _parts[index] == null)
            return null;

        if (!DateOnly.TryParseExact(_parts[index], "yyyyMMdd", out var result))
        { 
            if(required)
                _errors.Add($"Post '{_parts[0]}' parameter {index} ('{_parts[index]}') is not a valid date. (row {_rowNumber})");
            return null;
        }
        return result;
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
        var dimParts = _parts[index][1..^1].SplitSieLine();
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
