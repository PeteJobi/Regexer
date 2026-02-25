using System.Diagnostics;
using Newtonsoft.Json;
using Regexer;

namespace RegexerTests
{
    public class UnitTests
    {
        private const string TEST_DATA_FOLDER = "TestData";
        private Regexer.Regexer regexer;
        public UnitTests()
        {
            regexer = new Regexer.Regexer();
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task Test(string testFolder, string input, string pattern, string replace, string output, RegexerMatchPair[]? matches)
        {
            //regexer.EnableFasterML(true);
            var result = await regexer.AutoRegex(input, pattern, replace);
            //var x = JsonConvert.SerializeObject(result.Matches);
            Assert.NotNull(result);
            Assert.Equal(output, result.Output);

            //if(matches == null) return;
            //for (var i = 0; i < matches.Length; i++)
            //{
            //    Assert.NotNull(result.Matches);
            //    Assert.Equal(matches[i], result.Matches![i]);
            //}
        }

        static IEnumerable<object[]> TestData()
        {
            string? specificTest = null; //Set to the name of test folder to run specific test e.g "MLBugfix"
            var testFolders = specificTest == null
                ? Directory.EnumerateDirectories(TEST_DATA_FOLDER)
                : new[] { $"{TEST_DATA_FOLDER}/{specificTest}" };

            foreach (var testFolder in testFolders)
            {
                var input = File.ReadAllText($"{testFolder}/Input.txt");
                var pattern = File.ReadAllText($"{testFolder}/Pattern.txt");
                var replace = File.ReadAllText($"{testFolder}/Replace.txt");
                var output = File.ReadAllText($"{testFolder}/Output.txt");
                RegexerMatchPair[]? matches = null;
                var matchesPath = $"{testFolder}/Matches.txt";
                if (File.Exists(matchesPath))
                {
                    var matchesJson = File.ReadAllText(matchesPath);
                    matches = JsonConvert.DeserializeObject<RegexerMatchPair[]>(matchesJson);
                }
                yield return new object[] { Path.GetFileName(testFolder), input, pattern, replace, output, matches };
            }
        }
    }
}