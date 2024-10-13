using System.Globalization;
using System.Text;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        var sieReader = new SieFileReader();

        Console.WriteLine("Testing SIE files");

        var sie = sieReader.Read(@"C:\Users\joawen\Source\Repos\SieFile\SieFileTests\sie_test_files\Norstedts Revision SIE 1.se");

        TestFile(@"C:\Users\joawen\Source\Repos\SieFile\SieFileTests\sie_test_files\Norstedts Bokslut SIE 1.se");


        sieReader.Read(File.OpenRead(@"SIE4 Exempelfil.SE"), ".SE");
     //   var jsonizer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Dictionary<string, string>));
        var json = JsonSerializer.Serialize(new Dictionary<string,string>{["key1"]="value1", ["key\"'withCrap"]="value\"'withcrap"});


        var n = TestFile(@"D:\Downloads\sitest20240225_031358.si");
        VerToSql(n, "8A06CE3D-1E43-44BF-820B-90415BED2A1D", "1.100.1");

        TestFile(@"C:\Users\joawen\Source\Repos\SieFile\SieFileTests\sie_test_files\Sie1.se");

        GenNodes();

        var allEnc = Encoding.GetEncodings();
        var enc = Encoding.GetEncoding(437);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var lines = File.ReadAllLines(@"SIE4 Exempelfil.SE", enc);


        //var writer = new SieFileWriter();
        var stream = new MemoryStream();
        using (StreamWriter sw = new StreamWriter(stream, enc))
        {
            Row(sw, "#MUU", "one", null, "two", null);
            Row(sw, "#UNO");
            Row(sw, "#UNO", null);
            Row(sw, "#UNO", null, null);
        };
        stream.TryGetBuffer(out var buffer);
        var linex = Encoding.GetEncoding(437).GetString(buffer);
        Console.WriteLine(linex);

        foreach (var line in lines)
        {
            var parts = line.Split(' ', '\t');
            var rowType = parts[0];
        }

        void VerToSql(SieFile sie, string nodeId = "1E614737-72E3-4AD8-B077-12D7CE70E52C", string path = "1.7.3.2")
        {
            var js = new JsonSerializerOptions{IncludeFields=true, Encoder =  System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull};
            using var sw = new StreamWriter("create_vers.sql");
            for (int i = 0; i < sie.Verifications.Count; i++)
            {                
                //if(i%1000==0)sw.WriteLine("INSERT INTO acLedgerEntry ([Id],[ParentBranchId],[ParentPathLocator],[AdditionalInformation],[Series],[VoucherNumber],[LedgerDate],[CreateDate]) VALUES"); else sw.Write(',');
                sw.WriteLine("INSERT INTO acLedgerEntry ([Id],[ParentBranchId],[ParentPathLocator],[AdditionalInformation],[Series],[VoucherNumber],[LedgerDate],[CreateDate]) VALUES");
                var ver = sie.Verifications[i];
                var id = Guid.NewGuid();
                sw.Write($"('{id}','{nodeId}','{path}','");
                //var meta = new VerMetaData(){Title=ver.Text, Rows=ver.Rows.Select(r=>(r.Account, r.Amount, r.Dimensions)).ToList()};
                var meta = new VerMetaData(){Title=ver.Text, Rows=ver.Rows.Select(r=>new VerMetaData.Row(r.Account, r.Amount, r.Dimensions)).ToList()};
                string v = JsonSerializer.Serialize(meta, js);
                sw.Write(v.Replace("'","''"));
                //TODO: write json
                //sw.Write("{\"Title\":\""+ver.Text.Replace("'","''").Replace("\"","")+"\"}");
                sw.WriteLine($"','{ver.Series}','{ver.VoucherNumber}','{ver.Date.ToString("yyyyMMdd")}',GETDATE())");

                if(ver.Rows?.Any()!=true)continue;
                sw.WriteLine("INSERT INTO acLedgerRow ([Id],[ParentBranchId],[ParentPathLocator],[LedgerEntryId],[Amount],[LedgerDate],[Dimensions],[DimensionAccount],[AdditionalInformation]) VALUES");
                for (int j = 0; j < ver.Rows.Count; j++)
                {
                    if(j>0)sw.Write(',');
                    var row = ver.Rows[j];
                    var dims = new Dictionary<string, string>(row.Dimensions){["Account"] = row.Account};
                    var json = JsonSerializer.Serialize(dims, js).Replace("'","''");
                    sw.WriteLine($"('{Guid.NewGuid()}','{nodeId}','{path}','{id}',{row.Amount.ToString("F", CultureInfo.InvariantCulture)},'{ver.Date:yyyyMMdd}','{json}','{row.Account}','')");
                }
            }
        }

        void GenNodes()
        {
            // Generate a bunch of nodes
            var root = new Node { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), path = "1" };
            var nodes = new List<Node>();
            // put 100 nodes at root
            for (int i = 0; i < 100; i++)
            {
                nodes.Add(new Node(root));
            }

            // place a bunch of nodes all over
            var r = new Random();
            for (int i = 0; i < 9900; i++)
            {
                nodes.Add(new Node(nodes[r.Next(0,nodes.Count)]));
            }

            using var sw = new StreamWriter("create_nodes.sql");
            for (int i = 0; i < nodes.Count; i++)
            {
                if(i%1000==0)sw.WriteLine("INSERT INTO CompanyBranch ([Id],[ParentBranchId],[ParentPathLocator],[ShortId],[Name],[PathLocator]) VALUES");
                else sw.Write(',');
                Node node = nodes[i];
                sw.WriteLine($"('{node.Id}','{node.Parent.Id}','{node.Parent.path}',{node.ShortId},'{node.name}','{node.path}')");
            }
        }

        SieFile TestFile(string filename)
        {
            var sie = sieReader.Read(File.OpenRead(filename), filename);
            foreach (var error in sieReader.Errors)
            {
                Console.WriteLine("ERROR:" + Path.GetFileName(filename) + ": " + error);
            }
            foreach (var warning in sieReader.Warnings)
            {
                Console.WriteLine("WARN:" + Path.GetFileName(filename) + ": " + warning);
            }

            Console.WriteLine($"Read file '{Path.GetFileName(filename)}'");
            Console.WriteLine($"File had {sie.Accounts.Count} accounts");
            Console.WriteLine("File had {0} verifications", sie.Verifications.Count);
            return sie;
        }


        void Row(StreamWriter sw, string sieKeyword, params string[] optionalParameters)
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
        string Escape(string data, bool andPrefix = false)
        {
            if (string.IsNullOrEmpty(data)) return (andPrefix ? " " : "") + "\"\"";
            if (data.Contains(' ')) return (andPrefix ? " " : "") + "\"" + data.Replace("\"", "\\\"") + "\"";
            return (andPrefix ? " " : "") + data;
        }
    }
}

class VerMetaData
{
    public string Title {get;set;}
    //public List<(string Account, decimal Amount, Dictionary<string,string> Dimensions)> Rows {get;set;} = [];
    public List<Row> Rows {get;set;} = [];

    public struct Row
    {
        public Row(string account, decimal amount, Dictionary<string, string> dimensions)
        {
            this.Account = account;
            this.Amount = amount;
            if(dimensions.Any())
                this.Dimensions = dimensions;
        }

        public string Account {get;set;}
        public decimal Amount {get;set;}
        public Dictionary<string, string> Dimensions {get;set;}
    }
}

class Node
{
    public Guid Id = Guid.NewGuid();
    public string name;
    public string path;
    public Node Parent;
    public int ShortId;

    public List<Node> Children = [];

    public Node(){
        name=Path.GetRandomFileName();
    }
    public Node(Node parent){
        name=Path.GetRandomFileName();
        Parent = parent;
        ShortId = parent.Children.Count+1;
        path = parent.path + "." + ShortId;
        parent.Children.Add(this);
    }
};
