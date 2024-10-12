using System.Globalization;
using System.Text;

public class SieFileWriter
{
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
        sw.WriteLine("#GEN " + sie.Generated.ToString("yyyyMMdd") + Optional(sie.GeneratedBy));
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

        foreach(var balance in sie.Balances)
        {
            if (balance.Dimensions == null)
            {
                sw.WriteLine("#"+(balance.IncomingBalance?"I":"U")+"B " + Escape(balance.YearIndex) + " " + Escape(balance.Account) + " " + balance.Amount.ToString("F", CultureInfo.InvariantCulture) + Optional(balance.Quantity));
            }
            else
            {
                sw.WriteLine("#O" + (balance.IncomingBalance ? "I" : "U") + "B " + Escape(balance.YearIndex) + " " + Escape(balance.Account) + " " +FormatDictionary(balance.Dimensions)+ " " + balance.Amount.ToString("F", CultureInfo.InvariantCulture) + Optional(balance.Quantity));
            }
        }

        foreach (var change in sie.PeriodChanges)
        {
            switch (change.Type)
            {
                case AmountType.IncomingBalance:
                    break;
                case AmountType.OutgoingBalance:
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

        foreach(var ver in sie.Verifications)
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

    private void Row(StreamWriter sw, string sieKeyword, params string[] optionalParameters)
    {
        sw.Write(sieKeyword);

        var lastParamWithValue = (optionalParameters?.Length ?? 0) - 1;
        while (lastParamWithValue >= 0 && optionalParameters[lastParamWithValue] == null)
            lastParamWithValue--;

        for (int i = 0; i <= lastParamWithValue; i++)
        {
            sw.Write(' ');
            sw.Write(Escape(optionalParameters[i]));
        }
        sw.WriteLine();
    }

    private string Escape(string data, bool andPrefix = false)
    {
        if (string.IsNullOrEmpty(data)) return (andPrefix ? " " : "") + "\"\"";
        if (data.Contains(' ')) return (andPrefix ? " " : "") + "\"" + data.Replace("\"", "\\\"") + "\"";
        return (andPrefix ? " " : "") + data;
    }

    /// <summary>
    /// Adds a parameter if it has value, including a prefix space
    /// </summary>
    private string Optional(string data, bool andPrefix = true)
    {
        if (data == null) return "";
        return (andPrefix ? " " : "") + Escape(data);
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

    private string FormatDictionary(Dictionary<string, string> dict)
    {
        if (dict == null) return "{}";
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
        return sb.ToString();
    }
}
