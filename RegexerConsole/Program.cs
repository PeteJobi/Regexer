Console.WriteLine("Hello, World!");

await using var fileStream = File.OpenRead(Path.Combine(Environment.CurrentDirectory, "Input.txt"));
{
    using var streamReader = new StreamReader(fileStream);
    var input = await streamReader.ReadToEndAsync();
    await using var fileStream2 = File.OpenRead(Path.Combine(Environment.CurrentDirectory, "Pattern.txt"));
    using var streamReader2 = new StreamReader(fileStream2);
    var pattern = await streamReader2.ReadToEndAsync();
    await using var fileStream3 = File.OpenRead(Path.Combine(Environment.CurrentDirectory, "Replace.txt"));
    using var streamReader3 = new StreamReader(fileStream3);
    var replace = await streamReader3.ReadToEndAsync();
    try
    {
        var result = await new Regexer.Regexer().AutoRegex(input, pattern, replace);
        Console.WriteLine(result);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
    //await using var fileStream4 = File.OpenWrite(Path.Combine(Environment.CurrentDirectory, "Output.txt"));
    //await using var streamWriter = new StreamWriter(fileStream4);
    //await streamWriter.WriteLineAsync(result);
}