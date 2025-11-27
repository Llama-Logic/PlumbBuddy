namespace PlumbBuddy.Terminal;

class Program
{
    static void Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var rootCommand = new RootCommand("PlumbBuddy Command Line Interface");

        var directory = new Option<DirectoryInfo>("--directory")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The directory in which to perform a batch operation",
        };
        rootCommand.Options.Add(directory);

        var scaffolding = new Option<FileInfo>("--scaffolding")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The scaffolding file which serves as the basis for an operation",
        };
        rootCommand.Options.Add(scaffolding);

        rootCommand.Subcommands.Add(new Command("update-manifest", "Update the manifests for the mod files indicated by the specified scaffolding -OR- all the manifested mod files that can be found via a recursive traversal of the specified directory"));

        rootCommand.SetAction(parseResult =>
        {
        });

        rootCommand.Parse(args).Invoke();
    }
}
