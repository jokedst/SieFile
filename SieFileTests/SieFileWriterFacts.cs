namespace SieFileTests;

public class SieFileWriterFacts
{
    private readonly SieFileWriter writer = new();

    [Test]
    public void CanWriteMinimalFile()
    {
        var sie = new SieFile(SieFileType.Type1, "My Program", "3.11", "My company", new DateOnly(2020, 01, 01))
        {
            Generated = new DateOnly(2000, 9, 21)
        };

        var stream = new MemoryStream();
        writer.Write(stream, sie);

        var content = stream.GetFileContent();
        Assert.That(content,
            Is.EqualTo((string?)"""
            #FLAGGA 0
            #PROGRAM "My Program" 3.11
            #FORMAT PC8
            #GEN 20000921
            #SIETYP 1
            #FNAMN "My company"
            #RAR 0 20200101 20201231

            """));
    }

    [Test]
    public void CanWriteSmallFile()
    {
        var sie = new SieFile(SieFileType.Type3, "My Program", "3.11", "Övningsbolaget AB", new DateOnly(2011, 01, 01))
        {
            Contact = "Box 1",
            AdressLine1 = "123 45",
            AdressLine2 = "STORSTAD",
            Phone = "012-34 56 78",
            Generated = new DateOnly(2010, 10, 18),
            OrganisationNumber = "555555-5555",
            BaseAccountPlan = "BAS2010",
            Currency = "SEK"

        };

        sie.Accounts["1010"] = new Account { Name = "Balanserade utgifter", Type = 'T', SRU = "7201" };
        sie.Accounts["1018"] = new Account { Name = "Ack nedskrivningar balanserade utg", Type = 'T', SRU = "7201" };
        sie.Accounts["1019"] = new Account { Name = "Ack avskrivningar balanserade utg", Type = 'T', SRU = "7201" };

        var stream = new MemoryStream();
        writer.Write(stream, sie);

        var content = stream.GetFileContent();
        Assert.That(content,
            Is.EqualTo((string?)"""
            #FLAGGA 0
            #PROGRAM "My Program" 3.11
            #FORMAT PC8
            #GEN 20101018
            #SIETYP 3
            #ORGNR 555555-5555
            #ADRESS "Box 1" "123 45" STORSTAD "012-34 56 78"
            #FNAMN "Övningsbolaget AB"
            #RAR 0 20110101 20111231
            #OMFATTN 20111231
            #KPTYP BAS2010
            #VALUTA SEK
            #KONTO 1010 "Balanserade utgifter"
            #KTYP 1010 T
            #SRU 1010 7201
            #KONTO 1018 "Ack nedskrivningar balanserade utg"
            #KTYP 1018 T
            #SRU 1018 7201
            #KONTO 1019 "Ack avskrivningar balanserade utg"
            #KTYP 1019 T
            #SRU 1019 7201

            """));
    }



    [Test]
    public void CanWriteSie3FileWithAllFeatures()
    {
        var sie = new SieFile(SieFileType.Type3, "My Program", "3.11", "Övningsbolaget AB", new DateOnly(2011, 01, 01))
        {
            Contact = "Box 1",
            AdressLine1 = "123 45",
            AdressLine2 = "STORSTAD",
            Phone = "012-34 56 78",
            Generated = new DateOnly(2010, 10, 18),
            OrganisationNumber = "555555-5555",
            BaseAccountPlan = "BAS2010",
            Currency = "SEK"

        };

        sie.Accounts["1010"] = new Account { Name = "Balanserade utgifter", Type = 'T', SRU = "7201" };
        sie.Accounts["1018"] = new Account { Name = "Ack nedskrivningar balanserade utg", Type = 'T', SRU = "7201" };
        sie.Accounts["1019"] = new Account { Name = "Ack avskrivningar balanserade utg", Type = 'T', SRU = "7201", Unit="ST" };

        sie.Dimensions["1"] = new Dimension { Name = "First dimension" };
        sie.Dimensions["2"] = new Dimension { Name = "Sub dimension", ParentDimension = "1" };
        sie.Dimensions["2"].Values["0001"] = "Dim 2 value 0001";

        //sie.PeriodChanges.Add(new ObjectAmount { IncomingBalance = true, YearIndex = "0", Account = "1010", Amount = 12.3m });
        sie.PeriodSummeries.Add(new PeriodSummary("0", null, AmountType.IncomingBalance, "1010", null, 12.3m, null));
        sie.PeriodSummeries.Add(new PeriodSummary("0", null, AmountType.OutgoingBalance, "1010", null, 5, 1.2m));
        // sie.Balances.Add(new Balance { IncomingBalance = false, YearIndex = "0", Account = "1010", Amount = 5, Quantity=1.2m });
        sie.PeriodSummeries.Add(new PeriodSummary("0", null, AmountType.ObjectIncomingBalance, "1010", new() { ["1"] = "ABC", ["2"] = "0001" }, 11.1m, null));
        //sie.Balances.Add(new Balance
        //{
        //    IncomingBalance = true,
        //    YearIndex = "0",
        //    Account = "1010",
        //    Amount = 11.1m,
        //    Dimensions = new Dictionary<string, string> { ["1"] = "ABC", ["2"]="0001" }
        //});
        sie.PeriodSummeries.Add(new PeriodSummary("0", null, AmountType.ObjectOutgoingBalance, "1010", new() { { "1", "ABC" } }, 22.2m, null));
        //sie.Balances.Add(new Balance
        //{
        //    IncomingBalance = false,
        //    YearIndex = "0",
        //    Account = "1010",
        //    Amount = 22.2m,
        //    Dimensions = new() { { "1", "ABC" } }
        //});

        var stream = new MemoryStream();
        writer.Write(stream, sie);

        var content = stream.GetFileContent();
        Assert.That(content,
            Is.EqualTo((string?)"""
            #FLAGGA 0
            #PROGRAM "My Program" 3.11
            #FORMAT PC8
            #GEN 20101018
            #SIETYP 3
            #ORGNR 555555-5555
            #ADRESS "Box 1" "123 45" STORSTAD "012-34 56 78"
            #FNAMN "Övningsbolaget AB"
            #RAR 0 20110101 20111231
            #OMFATTN 20111231
            #KPTYP BAS2010
            #VALUTA SEK
            #KONTO 1010 "Balanserade utgifter"
            #KTYP 1010 T
            #SRU 1010 7201
            #KONTO 1018 "Ack nedskrivningar balanserade utg"
            #KTYP 1018 T
            #SRU 1018 7201
            #KONTO 1019 "Ack avskrivningar balanserade utg"
            #KTYP 1019 T
            #SRU 1019 7201
            #ENHET 1019 ST
            #DIM 1 "First dimension"
            #UNDERDIM 2 "Sub dimension" 1
            #OBJEKT 2 0001 "Dim 2 value 0001"
            #IB 0 1010 12.30
            #UB 0 1010 5.00 1.20
            #OIB 0 1010 {1 ABC 2 0001} 11.10
            #OUB 0 1010 {1 ABC} 22.20

            """));
    }

    [Test]
    public void CanWriteVerificates()
    {
        var sie = new SieFile(SieFileType.Type4I, "My Program", "3.11", "My company", new DateOnly(2020, 01, 01))
        {
            Generated = new DateOnly(2000, 9, 21)
        };

        sie.Verifications.Add(new Verification("A", "1", new DateOnly(2023, 10, 20), "ver text", new DateOnly(2023, 10, 30), "Joe")
        {
            Rows = [new VerificationRow("1000",null,12,null,null,null,null,1),
            new VerificationRow("2000",null,-12,null,null,null,null,2)]
        });

        var stream = new MemoryStream();
        writer.Write(stream, sie);

        var content = stream.GetFileContent();
        Assert.That(content,
            Is.EqualTo((string?)"""
            #FLAGGA 0
            #PROGRAM "My Program" 3.11
            #FORMAT PC8
            #GEN 20000921
            #SIETYP 4
            #FNAMN "My company"
            #RAR 0 20200101 20201231
            #VER A 1 20231020 "ver text" 20231030 Joe
            {
               #TRANS 1000 {} 12.00
               #TRANS 2000 {} -12.00
            }

            """));
    }
}
