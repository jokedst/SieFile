using System.Globalization;
using System.Text;

/// <summary>
/// Writes a SIE file (type 1-4).
/// </summary>
public class SieFileWriter
{
    /// <summary>
    /// Write given SIE file to a stream
    /// </summary>
    /// <param name="stream">Where to write. This will be closed when done.</param>
    /// <param name="sie">SIE file to write.</param>
    /// <param name="includeHistory">If true, VER row history will be written (RTRANS and BTRANS rows) if available.</param>
    public void Write(Stream stream, SieFile sie, bool includeHistory = false)
    {
        // Ensure codepage 437 is loaded
        if (!Encoding.GetEncodings().Any(x => x.CodePage == 437))
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        var encoding = Encoding.GetEncoding(437);
        sie.ValidateAndFill();

        using StreamWriter sw = new StreamWriter(stream, encoding);

        sw.WriteLine("#FLAGGA " + (sie.AlreadyImportedFlag ? '1' : '0'));
        sw.WriteLine("#PROGRAM " + Escape(sie.Program) + " " + Escape(sie.ProgramVersion));
        sw.WriteLine("#FORMAT PC8");
        Row(sw, "#GEN", sie.Generated.ToString("yyyyMMdd"), sie.GeneratedBy);
        sw.WriteLine("#SIETYP " + sie.FileType.ToSieString());
        foreach (var prosa in sie.Notes) sw.WriteLine("#PROSA " + Escape(prosa));
        OptionalRow(sw, "#FTYP", sie.CompanyType);
        OptionalRow(sw, "#FNR", sie.InternalCompanyId);
        OptionalRow(sw, "#ORGNR", sie.OrganisationNumber, sie.OrganisationInternalNumber, sie.OrganisationInternalNumber2);
        OptionalRow(sw, "#BKOD", sie.CompanySNI);
        OptionalRow(sw, "#ADRESS", sie.Contact, sie.AdressLine1, sie.AdressLine2, sie.Phone);
        sw.WriteLine("#FNAMN " + Escape(sie.CompanyName));
        foreach (var year in sie.Years)
            sw.WriteLine("#RAR " + Escape(year.Key) + " " + Escape(year.Value.StartDate) + " " + Escape(year.Value.EndDate));
        OptionalRow(sw, "#TAXAR", sie.TaxationYear);
        
        if(sie.FileType == SieFileType.Type2|| sie.FileType == SieFileType.Type3|| sie.FileType == SieFileType.Type4E)
            OptionalRow(sw, "#OMFATTN", sie.BalanceEndDate);
        OptionalRow(sw, "#KPTYP", sie.BaseAccountPlan);
        OptionalRow(sw, "#VALUTA", sie.Currency);

        foreach(var account in sie.Accounts)
        {
            sw.WriteLine("#KONTO " + Escape(account.Key) + " " + Escape(account.Value.Name));
            if(account.Value.Type != default)
                sw.WriteLine("#KTYP " + Escape(account.Key) + " " + account.Value.Type);
            if (account.Value.SRU != null)
                sw.WriteLine("#SRU " + Escape(account.Key) + " " + Escape(account.Value.SRU));
            if (account.Value.Unit != null)
                sw.WriteLine("#ENHET " + Escape(account.Key) + " " + Escape(account.Value.Unit));
        }

        foreach (var dimension in sie.Dimensions)
        {
            if (dimension.Value.ParentDimension != null)
                sw.WriteLine("#UNDERDIM " + Escape(dimension.Key) + " " + Escape(dimension.Value.Name) + " " + Escape(dimension.Value.ParentDimension));
            else
                sw.WriteLine("#DIM " + Escape(dimension.Key) + " " + Escape(dimension.Value.Name));
            
            foreach(var dimValue in dimension.Value.Values)
            {
                sw.WriteLine("#OBJEKT " + Escape(dimension.Key) + " " + Escape(dimValue.Key) + " " + Escape(dimValue.Value));
            }
        }

        foreach (var change in sie.PeriodSummeries)
        {
            switch (change.Type)
            {
                case AmountType.IncomingBalance:
                case AmountType.OutgoingBalance:
                    Row(sw, change.Type.ToRowType(), change.YearIndex, change.Account, change.Amount.ToString("F", CultureInfo.InvariantCulture), change.Quantity?.ToString("F", CultureInfo.InvariantCulture));
                    break;
                case AmountType.ObjectIncomingBalance:
                case AmountType.ObjectOutgoingBalance:
                    Row(sw, change.Type.ToRowType(), change.YearIndex, change.Account, FormatDictionary(change.Dimensions), change.Amount.ToString("F", CultureInfo.InvariantCulture), change.Quantity?.ToString("F", CultureInfo.InvariantCulture));
                    break;
                case AmountType.Result:
                    sw.WriteLine("#RES " + Escape(change.YearIndex) + " " + Escape(change.Account) + " " + Decimal(change.Amount) + Optional(change.Quantity));
                    break;
                case AmountType.PeriodChange:
                    sw.WriteLine("#PSALDO " + Escape(change.YearIndex) + " " + Escape(change.Period) + " " + Escape(change.Account) + " " + FormatDictionary(change.Dimensions) + " " + Decimal(change.Amount) + Optional(change.Quantity));
                    break;
                case AmountType.PeriodBudgetChange:
                    sw.WriteLine("#PBUDGET " + Escape(change.YearIndex) + " " + Escape(change.Period) + " " + Escape(change.Account) + " " + FormatDictionary(change.Dimensions) + " " + Decimal(change.Amount) + Optional(change.Quantity));
                    break;
            }
        }

        foreach (var ver in sie.Verifications)
        {
            Row(sw, "#VER", ver.Series, ver.VoucherNumber, ver.Date.ToString("yyyyMMdd"), ver.Text, ver.RegistrationDate?.ToString("yyyyMMdd"), ver.User);
            sw.WriteLine("{");
            foreach(var verLine in ver.Rows)
            {
                if (includeHistory)
                {
                    var btrans = ver.RemovedRows.Find(r => r.RowNumber == verLine.RowNumber);
                    if (btrans != null) Row(sw, "   #BTRANS", btrans.Account, FormatDictionary(btrans.Dimensions), Decimal(btrans.Amount), btrans.TransactionDate?.ToString("yyyyMMdd"), btrans.Text, btrans.Quantity?.ToString("F", CultureInfo.InvariantCulture), btrans.User);
                    var rtrans = ver.AddedRows.Find(r => r.RowNumber == verLine.RowNumber);
                    if (rtrans != null) Row(sw, "   #RTRANS", rtrans.Account, FormatDictionary(rtrans.Dimensions), Decimal(rtrans.Amount), rtrans.TransactionDate?.ToString("yyyyMMdd"), rtrans.Text, rtrans.Quantity?.ToString("F", CultureInfo.InvariantCulture), rtrans.User);
                }
                Row(sw, "   #TRANS", verLine.Account, FormatDictionary(verLine.Dimensions), Decimal(verLine.Amount), verLine.TransactionDate?.ToString("yyyyMMdd"), verLine.Text, verLine.Quantity?.ToString("F", CultureInfo.InvariantCulture), verLine.User);
            }
            sw.WriteLine("}");
        }
    }

    private void OptionalRow(StreamWriter sw, string sieKeyword, string value, params string[] optionalParameters)
    {
        if (value == null) return;
        sw.Write(sieKeyword);
        sw.Write(' ');
        sw.Write(Escape(value));
        foreach (var parameter in optionalParameters)
        {
            if (parameter == null) continue;
            sw.Write(' ');
            sw.Write(Escape(parameter));
        }
        sw.WriteLine();
    }

    private void Row(StreamWriter sw, string sieKeyword, params object[] optionalParameters)
    {
        sw.Write(sieKeyword);

        var lastParamWithValue = (optionalParameters?.Length ?? 0) - 1;
        while (lastParamWithValue >= 0 && optionalParameters[lastParamWithValue] == null)
            lastParamWithValue--;

        for (int i = 0; i <= lastParamWithValue; i++)
        {
            sw.Write(' ');
            if(optionalParameters[i] is string s)
                sw.Write(Escape(s));
            else 
                sw.Write(optionalParameters[i]);
        }
        sw.WriteLine();
    }

    private string Escape(string data, bool andPrefix = false)
    {
        if (string.IsNullOrEmpty(data)) return (andPrefix ? " " : "") + "\"\"";
        if (data.Contains(' ')) return (andPrefix ? " " : "") + "\"" + data.Replace("\"", "\\\"") + "\"";
        return (andPrefix ? " " : "") + data;
    }

    private string Optional(decimal? data)
    {
        if (data == null) return "";
        return " " + data.Value.ToString("F", CultureInfo.InvariantCulture);
    }

    private string Decimal(decimal data)
    {
        return data.ToString("F", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a dictionary into a SIE string, e.g. "{A NameA B NameB}"
    /// </summary>
    /// <param name="dict"></param>
    /// <returns>Returns a Lazy string, just so the other code doesn't escape it.</returns>
    private Lazy<string> FormatDictionary(Dictionary<string, string> dict)
    {
        if (dict == null) return new Lazy<string>( "{}");
        var sb = new StringBuilder();
        sb.Append("{");
        var first = true;
        foreach (var kvp in dict)
        {
            if (first) first = false; else sb.Append(" ");
            sb.Append(Escape(kvp.Key));
            sb.Append(' ');
            sb.Append(Escape(kvp.Value));
        }
        sb.Append("}");
        return new Lazy<string>(sb.ToString());
    }
}
