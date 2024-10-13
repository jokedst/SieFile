using System.Text;
using System.Text.RegularExpressions;

namespace SieFileTests;

public class SieFileReaderFacts
{
    private readonly SieFileReader reader = new();

    [Test]
    public void CanReadSimpleFile()
    {
        var sieData = @"#FLAGGA 0 
#MUU 123"
        .To437Stream();

        var sie = reader.Read(sieData, "muu.si");

        Assert.That(reader.Errors, Is.Empty);
        Assert.That(reader.Warnings, Is.Empty);
        Assert.That(!sie.AlreadyImportedFlag);
    }

    [Test]
    public void CanReadEmptyName()
    {
        var sieData = @"#FLAGGA 0 
#KONTO 123 """""
        .To437Stream();

        var sie = reader.Read(sieData, "muu.si");

        Assert.That(reader.Errors, Is.Empty);
        Assert.That(reader.Warnings, Is.Empty);
        Assert.That(!sie.AlreadyImportedFlag);
        Assert.That(sie.Accounts, Has.Count.EqualTo(1));
    }

    [Test]
    public void ErrorOnUnclosedVER()
    {
        var sie = reader.Read(@"#FLAGGA 0 
#VER A 1 20210105 Kaffebröd 20210310
{
   #TRANS 1910 {} -195.00
   #TRANS 2641 {} 20.88".To437Stream(), "muu.si");

        Assert.Multiple(() =>
        {
            Assert.That(reader.Errors, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(reader.Errors[0], Is.EqualTo("Post #VER was not closed with a '}' (row 5)"));
            Assert.That(reader.Warnings, Is.Empty);
        });
    }

    [Test]
    public void CanReadFullFileType3()
    {
        var sieData = @"#FLAGGA 0 
#FORMAT PC8
#SIETYP 3
#PROGRAM ""Super bokföring \""2000\"" med coola grejjer"" 3.14
#GEN 20230822 
#FNAMN ""Övningsbolaget AB""
#FNR ""exort873123-3123-12""
#ORGNR 555555-5555
#ADRESS ""Gun Hellsweek"" ""Box 1"" ""123 45 STORSTAD"" ""012-34 56 78""
#RAR 0 20210101 20211231
#RAR -1 20200101 20201231
#TAXAR 2022 
#VALUTA SEK
#KPTYP EUBAS97
#KONTO 1221 Inventarier
#KTYP 1221 T
#SRU 1221 7215
#KONTO 2081 Aktiekapital
#KTYP 2081 S
#SRU 2081 7301
#KONTO 3041 ""Försäljn tjänst 25% sv""
#KTYP 3041 I
#SRU 3041 7410
#KONTO 4010 ""Inköp material och varor""
#KTYP 4010 K
#SRU 4010 7512
#DIM 1 Resultatenhet
#OBJEKT 1 Nord ""Kontor Nord""
#OBJEKT 1 Syd ""Kontor Syd""
#IB 0 1221 421457.53
#UB 0 1221 518057.53 123.2
#IB 0 2081 -200000.00
#UB 0 2081 -200000.00
#RES 0 3041 -1690380.20
#RES 0 4010 2391104.75
#IB -1 1221 789418.53
#UB -1 1221 421457.53
#IB -1 2081 -200000.00
#UB -1 2081 -200000.00
#RES -1 3041 -1616182.70
#RES -1 4010 2300437.22

#OIB 0 1221 {1 ""Nord""} 23780.78
#OIB 0 1221 {1 ""Syd"" 7 JonasX} 555 12.2
#OIB 0 2081 {7 ""KalleB""} 1000.01 6
#OUB 0 2081 {7 ""KalleB""} 999 1.3

"
.To437Stream();

        var sie = reader.Read(sieData, "foo.si");
        Assert.Multiple(() =>
        {
            Assert.That(reader.Errors, Is.Empty);
            Assert.That(reader.Warnings, Is.Empty);
            Assert.That(sie.AlreadyImportedFlag, Is.EqualTo(false));
            Assert.That(sie.PeriodSummeries.Any(x => x.Type== AmountType.ObjectIncomingBalance && x.Dimensions?.ContainsKey("7") == true && x.Dimensions["7"] == "KalleB" && x.Quantity==6 && x.Amount == 1000.01m));
            Assert.That(sie.PeriodSummeries.Any(x => x.Type == AmountType.ObjectOutgoingBalance && x.Dimensions?.ContainsKey("7") == true && x.Dimensions["7"] == "KalleB" && x.Quantity == 1.3m && x.Amount == 999));

            Assert.That(sie.PeriodSummeries.Any(x => x.Type == AmountType.IncomingBalance && x.Account == "1221" && x.Amount == 421457.53m && x.Quantity == null));
            Assert.That(sie.PeriodSummeries.Any(x => x.Type == AmountType.OutgoingBalance && x.Account == "1221" && x.Amount == 518057.53m && x.Quantity == 123.2m));
            Assert.That(sie.PeriodSummeries.Any(x => x.Type == AmountType.Result && x.Account == "3041" && x.Amount == -1690380.20m && x.YearIndex == "0"));
            Assert.That(sie.PeriodSummeries.Any(x => x.Type == AmountType.Result && x.Account == "4010" && x.Amount == 2300437.22m && x.YearIndex == "-1"));
        });
    }

    [Test]
    public void CanReadFullFileType4E()
    {
        var sieData = @"#FLAGGA 0 
#FORMAT PC8
#SIETYP 4
#PROSA  \""muu\""			123	 hej""citat  sträng4 sträng5
#PROGRAM ""Super bokföring \""2000\"" med coola grejjer""   3.14
#GEN 20230822 
#FNAMN ""Övningsbolaget AB""
#FNR ""exort873123-3123-12""
#ORGNR 555555-5555
#ADRESS ""Gun Hellsweek"" ""Box 1"" ""123 45 STORSTAD"" ""012-34 56 78""
#RAR 0 20210101 20211231
#RAR -1 20200101 20201231
#TAXAR 2022 
#VALUTA SEK
#KPTYP EUBAS97
#KONTO 1221 Inventarier
#KTYP 1221 T
#SRU 1221 7215
#KONTO 2081 Aktiekapital
#KTYP 2081 S
#SRU 2081 7301
#KONTO 3041 ""Försäljn tjänst 25% sv""
#KTYP 3041 I
#SRU 3041 7410
#KONTO 4010 ""Inköp material och varor""
#KTYP 4010 K
#SRU 4010 7512
#DIM 1 Resultatenhet
#OBJEKT 1 Nord ""Kontor Nord""
#OBJEKT 1 Syd ""Kontor Syd""
#IB 0 1221 421457.53
#UB 0 1221 518057.53 123.2
#IB 0 2081 -200000.00
#UB 0 2081 -200000.00
#RES 0 3041 -1690380.20
#RES 0 4010 2391104.75
#IB -1 1221 789418.53
#UB -1 1221 421457.53
#IB -1 2081 -200000.00
#UB -1 2081 -200000.00
#RES -1 3041 -1616182.70
#RES -1 4010 2300437.22

#OIB 0 1221 {1 ""Nord""} 23780.78

#VER A 1 20210105 Kaffebröd 20210310
{
   #TRANS 1221 {} -195.00
   #TRANS 2641 {} 20.88
   #TRANS 7690 {} 174.12
}
"
.To437Stream();

        var sie = reader.Read(sieData, "foo.se");
        Assert.Multiple(() =>
        {
            Assert.That(reader.Errors, Is.Empty);
            Assert.That(reader.Warnings, Is.Empty);
            Assert.That(sie.AlreadyImportedFlag, Is.EqualTo(false));
        });
    }

    [Test]
    public void CanReadTestFiles()
    {
        int errors = 0, warnings = 0;
        var allErrors = new List<string>();
        foreach (var filename in Directory.EnumerateFiles(@"..\..\..\sie_test_files"))
        {
            using var stream = File.OpenRead(filename);
            var sie = reader.Read(stream, filename);

            errors += reader.Errors.Count;
            warnings += reader.Warnings.Count;
            allErrors.AddRange(reader.Errors.Select(x => Path.GetFileName(filename) + ": " + x));
            foreach (var error in reader.Errors)
            {
                Console.WriteLine("ERROR:" + Path.GetFileName(filename) + ": " + error);
            }
            foreach (var warning in reader.Warnings)
            {
                Console.WriteLine("WARN:" + Path.GetFileName(filename) + ": " + warning);
            }
        }

        Assert.Multiple(() =>
        {
            // The test files contain 13 errors
            Assert.That(errors, Is.EqualTo(13));
            Assert.That(allErrors, Does.Contain("BL0001_typ4I.SI: Post '#RAR' is missing parameter 2 (row 7)"));
            Assert.That(allErrors, Does.Contain("BL0001_typ4I.SI: Post '#RAR' is missing parameter 3 (row 7)"));
            Assert.That(allErrors, Does.Contain("SIE-fil från Visma Enskild Firma 2010.se: Post '#SRU' does not have 2 parameters (row 76)"));
            Assert.That(allErrors, Does.Contain("SIE-fil från Visma Enskild Firma 2010.se: Post '#SRU' does not have 2 parameters (row 79)"));
            Assert.That(allErrors, Does.Contain("SIE-fil från Visma Enskild Firma 2010.se: Post '#SRU' does not have 2 parameters (row 82)"));
            Assert.That(allErrors, Does.Contain("SIE-fil från Visma Enskild Firma 2010.se: Post '#SRU' does not have 2 parameters (row 85)"));
            Assert.That(allErrors, Does.Contain("Sie4.se: Post '#KTYP' does not have 2 parameters (row 593)"));
            Assert.That(allErrors, Does.Contain("Sie4.si: Post '#ORGNR' is missing parameter 1 (row 9)"));
            Assert.That(allErrors, Does.Contain("SIE_exempelfil.se: Post '#ORGNR' is missing parameter 1 (row 8)"));
            Assert.That(allErrors, Does.Contain("testWrite.se: Post #VER sum of rows is not zero (row 1372)"));
            Assert.That(allErrors, Does.Contain("testWrite1.se: Post #VER sum of rows is not zero (row 1372)"));
            Assert.That(allErrors, Does.Contain("transaktioner_ovnbolag-bad-balance.se: Post #VER sum of rows is not zero (row 3910)"));
            Assert.That(allErrors, Does.Contain("XE_SIE_4_20151125095119.SE: Post #VER sum of rows is not zero (row 1360)"));
        });
    }

    [Test]
    public void CanReadThenWriteTestFiles()
    {
        var allErrors = new List<string>();
        var writer = new SieFileWriter();
        Directory.CreateDirectory("testfiles_output");
        foreach (var filename in Directory.EnumerateFiles(@"..\..\..\sie_test_files"))
        {
            using var stream = File.OpenRead(filename);
            var sie = reader.Read(stream, filename);

            using (var fs = File.OpenWrite(Path.Combine("testfiles_output", Path.GetFileName(filename))))
            {
                writer.Write(fs, sie);
            }

            var wstream = new MemoryStream();
            writer.Write(wstream, sie);

            var content = wstream.GetFileContent();

            var originalContent = File.ReadAllLines(filename, Encoding.GetEncoding(437));

            // Compare.. something?
           // Console.WriteLine(originalContent.Length + " vs " + content.Length);

            var createdLines = Regex.Split(content, @"\r?\n|\r");

            var created = createdLines.ToHashSet();
            var original = originalContent.ToHashSet();

            var union= created.Intersect(original); 

            Console.WriteLine("==== file: " + Path.GetFileName(filename) + " ====");
            Console.WriteLine("Original file: " + originalContent.Length + " rows");
            Console.WriteLine("Generated file: " + createdLines.Length + " rows");
            Console.WriteLine("Common rows: " + union.Count() + " rows");

            // Normalize the original lines and try again
            var normalizedRows = originalContent.Select(Normalize).ToList();
            var normalized = new HashSet<string>(normalizedRows);
            union= created.Intersect(normalized); 
            Console.WriteLine("Common rows (normalized): " + union.Count() + " rows");
            File.WriteAllLines(Path.Combine("testfiles_output", Path.GetFileName(filename)+"x.txt"), normalizedRows);
        }
    }

    [Test]
    public void TestTest()
    {
        var n=Normalize("#RES	0	7220	    461999.89");
        Assert.That(Normalize("#RES	0	7220	    461999.89"), Is.EqualTo("#RES 0 7220 461999.89"));
        
         n=Normalize("#RES {\"a\"   123 } 12");
        Assert.That(Normalize("#RES {\"a\"   123 } 12"), Is.EqualTo("#RES {a 123} 12"));
    }

    private string Normalize(string data)
    {
        // var parts = data.SplitSieLine();
        // var escaped = parts.Select(Escape).ToArray();
        // var joined = string.Join(' ', escaped);
        // return joined;

        return string.Join(' ', data.SplitSieLine().Select(Escape));

    }

    private string Escape(string data)
    {
        if (string.IsNullOrEmpty(data)) return "\"\"";
        if(data.StartsWith('{') && data.EndsWith('}')) return '{' + Normalize(data[1..^1]) + '}';
        if (data.Contains(' ')) return "\"" + data.Replace("\"", "\\\"") + "\"";
        return data;
    }
}
