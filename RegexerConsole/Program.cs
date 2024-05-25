Console.WriteLine("Hello, World!");


await using var fileStream = File.OpenRead("../../../../RegexerConsole/Input.tsx");
{
    using var streamReader = new StreamReader(fileStream);
    var input = await streamReader.ReadToEndAsync();
    await using var fileStream2 = File.OpenRead("../../../../RegexerConsole/Pattern.txt");
    using var streamReader2 = new StreamReader(fileStream2);
    var pattern = await streamReader2.ReadToEndAsync();
    await using var fileStream3 = File.OpenRead("../../../../RegexerConsole/Replace.txt");
    using var streamReader3 = new StreamReader(fileStream3);
    var replace = await streamReader3.ReadToEndAsync();
    var result = new Regexer.Regexer().AutoRegex(input, pattern, replace);
    Console.WriteLine(result);
    return;
    await using var fileStream4 = File.OpenWrite("../../../../RegexerConsole/Output.cs");
    await using var streamWriter = new StreamWriter(fileStream4);
    await streamWriter.WriteLineAsync(result);
}