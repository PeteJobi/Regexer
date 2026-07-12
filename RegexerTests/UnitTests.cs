using Newtonsoft.Json;
using Regexer;
using System.Diagnostics;

namespace RegexerTests
{
    public class UnitTests
    {
        private const string TEST_DATA_FOLDER = "TestData";
        private const string TEST_DATA_FOLDER_V2 = "TestDataV2";
        private readonly Regexer.Regexer regexer = new();
        private readonly RegexerV2.Regexer regexerV2 = new();

        [Theory]
        [MemberData(nameof(TestData), false)]
        public async Task Test(string testFolder, string input, string find, string replace, string output, RegexerMatchPair[]? matches)
        {
            //regexer.EnableFasterML(true);
            var result = await regexer.AutoRegex(input, find, replace);
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

        [Theory]
        [MemberData(nameof(TestData), true)]
        public async Task TestV2(string testFolder, string input, string find, string replace, string output, RegexerMatchPair[]? matches)
        {
            var result = await regexerV2.AutoRegex(input, find, replace, CancellationToken.None);
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

        static IEnumerable<object[]> TestData(bool v2)
        {
            string? specificTest = null; //Set to the name of test folder to run specific test e.g "MLBugfix"
            var folder = v2 ? TEST_DATA_FOLDER_V2 : TEST_DATA_FOLDER;
            var testFolders = specificTest == null
                ? Directory.EnumerateDirectories(folder)
                : [$"{folder}/{specificTest}"];

            foreach (var testFolder in testFolders)
            {
                var input = File.ReadAllText($"{testFolder}/Input.txt");
                var find = File.ReadAllText($"{testFolder}/Pattern.txt");
                var replace = File.ReadAllText($"{testFolder}/Replace.txt");
                var output = File.ReadAllText($"{testFolder}/Output.txt");
                RegexerMatchPair[]? matches = null;
                var matchesPath = $"{testFolder}/Matches.txt";
                if (File.Exists(matchesPath))
                {
                    var matchesJson = File.ReadAllText(matchesPath);
                    matches = JsonConvert.DeserializeObject<RegexerMatchPair[]>(matchesJson);
                }
                yield return [Path.GetFileName(testFolder), input, find, replace, output, matches];
            }
        }
    }
}