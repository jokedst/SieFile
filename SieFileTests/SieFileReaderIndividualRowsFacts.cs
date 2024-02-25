namespace SieFileTests;

public class SieFileReaderIndividualRowsFacts
{
    [Test]
    public void CanReadValuta()
    {
        var sie = new SieFile();
        sie.Read(@"#VALUTA NOK".To437Stream(), "muu.si");
        Assert.That(sie.Currency, Is.EqualTo("NOK"));

        sie.Read(@"#VALUTA".To437Stream(), "muu.si");
        Assert.That(sie.Errors.Contains("Post '#VALUTA' is missing parameter 1 (row 1)"), Is.True);
    }

    [Test]
    public void CanReadSieTyp()
    {
        var sie = new SieFile();
        sie.Read(@"#SIETYP".To437Stream(), "muu.si");
        Assert.That(sie.Errors.Contains("Post '#SIETYP' is missing parameter 1 (row 1)"), Is.True);

        sie.Read(@"#SIETYP 1".To437Stream(), "muu.si");
        Assert.That(sie.FileType, Is.EqualTo(SieFileType.Type1));
        sie.Read(@"#SIETYP 2".To437Stream(), "muu.si");
        Assert.That(sie.FileType, Is.EqualTo(SieFileType.Type2));
        sie.Read(@"#SIETYP 3".To437Stream(), "muu.si");
        Assert.That(sie.FileType, Is.EqualTo(SieFileType.Type3));
        sie.Read(@"#SIETYP 4".To437Stream(), "muu.si");
        Assert.That(sie.FileType, Is.EqualTo(SieFileType.Type4I));
        sie.Read(@"#SIETYP 4".To437Stream(), "muu.se");
        Assert.That(sie.FileType, Is.EqualTo(SieFileType.Type4E));
    }

    [Test]
    public void CanReadVer()
    {
        var sie = new SieFile();
        sie.Read(@"#VER".To437Stream(), "muu.si");
        Assert.That(sie.Errors.Contains("Post '#VER' does not have 3 parameters (row 1)"), Is.True);

        sie.Read(@"#VER """" """" 20081216 ""Porto""".To437Stream(), "muu.si");
        Assert.That(sie.Verifications, Has.Count.EqualTo(1));
        Assert.That(sie.Verifications[0].Text, Is.EqualTo("Porto"));
        Assert.That(sie.Verifications[0].Date, Is.EqualTo(new DateOnly(2008, 12, 16)));


        sie.Read(@"#VER """" """" 20081216 ""Porto""
#SOMETHING ELSE".To437Stream(), "muu.si");
        Assert.That(sie.Errors.Contains("Post #VER was not followed by '{' (row 2)"), Is.True);
    }

    [Test]
    public void CanReadVER()
    {
        var sie = new SieFile();
        sie.Read(@"#VER A 252 20220603 ""Namn"" 20220920
{
#TRANS 1933 {} -10337 """" """" 0
#TRANS 2393 {} 7637 """" """" 0
#BTRANS 8400 {} 2700 """" """" 0 ""Namn 2""
#RTRANS 8410 {} 2700 """" """" 0 ""Namn 2""
#TRANS 8410 {} 2700 """" """" 0
}".To437Stream(), "muu.si");

        Assert.That(sie.Verifications, Has.Count.EqualTo(1));
        Assert.That(sie.Verifications[0].Rows, Has.Count.EqualTo(3));
        Assert.That(sie.Verifications[0].AddedRows, Has.Count.EqualTo(1));
        Assert.That(sie.Verifications[0].RemovedRows, Has.Count.EqualTo(1));
        Assert.That(sie.Errors.Count(x => x.StartsWith("Post #VER sum of rows is not zero")), Is.EqualTo(0));
    }
}
