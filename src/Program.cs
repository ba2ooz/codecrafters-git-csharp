using codecrafters_git.Commands;

Dictionary<string, Func<ICommand>> CommandsMap = new ()
{
    ["init"] = () => new InitCommand(),
    ["cat-file"] = () => new CatFileCommand(),
    ["hash-object"] = () => new HashObjectCommand(),
    ["ls-tree"] = () => new ListTreeCommand(),
    ["write-tree"] = () => new WriteTreeCommand(),
}; 

if (args.Length < 1)
{
    Console.WriteLine("Please provide a command.");
    return;
}

var commandName = args[0];
if (!CommandsMap.TryGetValue(commandName,  out var command))
    throw new ArgumentException($"Unknown command {commandName}");

command().Run(args);