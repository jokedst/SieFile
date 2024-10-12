global using NUnit.Framework;
using System.Text;

[SetUpFixture]
public class GlobalFixture
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
