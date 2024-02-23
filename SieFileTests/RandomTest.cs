namespace SieFileTests;

[TestFixture]
public class RandomTest
{
    [Test]
    public void TestStuff()
    {
        Assert.That(DateOnly.TryParseExact("20230406", "yyyyMMdd", out var generated), Is.EqualTo(true));
        Assert.That(generated, Is.EqualTo(new DateOnly(2023,4,6)));
    }

    [Test]
    public void CanParseSieLines()
    {
        var parts = "#TYPE  param1 \t  \"param 2\"".SplitOutsideQuotes(new[] { ' ', '\t' }, false, true, false);
        Assert.That(parts, Has.Length.EqualTo(3));
        Assert.That(parts, Is.EqualTo(new[] { "#TYPE", "param1", "param 2" }));

        parts = "#PROSA  \\\"muu\\\"\t\t\t123\t hej\"citat  sträng4 sträng5".SplitOutsideQuotes(new[] { ' ', '\t' }, false, true, false);
        Assert.That(parts, Is.EqualTo(new[] { "#PROSA", "\"muu\"", "123", "hej\"citat", "sträng4", "sträng5" }));
    }

    [Test]
    public void CanParseSieLinesWithEmptyFields()
    {
        var parts = "#TYPE \"\"   \"\" fourth".SplitOutsideQuotes(new[] { ' ', '\t' }, false, true, false);
        Assert.That(parts, Has.Length.EqualTo(4));
        Assert.That(parts, Is.EqualTo(new[] { "#TYPE", "", "", "fourth" }));
    }

    [Test]
    public void CanParseSieLinesWithBrackets()
    {
        var parts = "#TYPE {param1 muu } hello".SplitOutsideQuotes(new[] { ' ', '\t' }, false, true, false);
        Assert.That(parts, Has.Length.EqualTo(3));
        Assert.That(parts, Is.EqualTo(new[] { "#TYPE", "{param1 muu }", "hello" }));


        parts = "#TY{PE \"{ in quotes \" {param1 \"muu muu\" } hello".SplitOutsideQuotes(new[] { ' ', '\t' }, false, true, false);
        Assert.That(parts, Is.EqualTo(new[] { "#TY{PE", "{ in quotes ", "{param1 \"muu muu\" }", "hello" }));
    }
}