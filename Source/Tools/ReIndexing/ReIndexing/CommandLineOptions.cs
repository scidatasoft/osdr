using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace ReIndexing
{
    public class CommandLineOptions
    {
        [Option('u', "users", Required = true, Separator = ',', HelpText = "User Id's to re-index content")]
        public IList<string> Entities { get; set; }

        //[HelpOption]
        //public string GetUsage()
        //{
        //    var help = new HelpText
        //    {
        //        Heading = new HeadingInfo("OSDR Reindexing Application", " v0.12"),
        //        AdditionalNewLineAfterOption = true,
        //        AddDashesToOption = true
        //    };
        //    help.AddPreOptionsLine("Usage: reindexing -u [<uid1, uid2>|<all>]");
        //    help.AddPostOptionsLine("Example: reindexing -u e7097535-2ed4-4f59-b79d-936704e6c00b");
        //    help.AddOptions(this);

        //    return help;
        //}

        [Usage(ApplicationAlias = "Reindexing.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Re-index selected users content", new CommandLineOptions { Entities = new[] { "e7097535-2ed4-4f59-b79d-936704e6c00b, e2397535-2ed4-4f59-b79d-936704e6c45a" } });
                yield return new Example("Re-index all users content", new CommandLineOptions { Entities = new[] { "all" } });
            }
        }
    }
}
