using codecrafters_git.Abstractions;
using codecrafters_git.Commands;
using codecrafters_git.Infrastructure;

var repoFactory = new RepoFactory();

Dictionary<string, Func<ICommand>> CommandsMap = new ()
{
    ["init"] = () => new InitCommand(repoFactory.Create()),
    ["cat-file"] = () => new CatFileCommand(repoFactory.Create()),
    ["hash-object"] = () => new HashObjectCommand(repoFactory.Create()),
    ["ls-tree"] = () => new ListTreeCommand(repoFactory.Create()),
    ["write-tree"] = () => new WriteTreeCommand(repoFactory.Create()),
    ["commit-tree"] = () => new WriteCommitCommand(repoFactory.Create()),
    ["clone"] = () => new CloneCommand(repoFactory),
}; 

if (args.Length < 1)
   throw new ArgumentException("No command has been provided.");

var commandName = args[0];
if (!CommandsMap.TryGetValue(commandName,  out var command))
    throw new ArgumentException($"Unknown command {commandName}");

command().Run(args);