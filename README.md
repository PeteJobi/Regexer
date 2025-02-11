# Regexer (Multi-line Find-And-Replace)

This is a simple tool for finding and replacing text without having to deal with complex Regex patterns. Only supports Windows 10 and 11 (not tested on other versions of Windows).

![image](https://github.com/PeteJobi/Regexer/assets/45200292/b0e99cd9-d9d5-4221-9365-bd8a6d76e3a5)
![image](https://github.com/PeteJobi/Regexer/assets/45200292/cc00be95-c994-4691-8824-846299145697)

## How to build
You need to have at least .NET 6 runtime installed to build the software. Download the latest runtime [here](https://dotnet.microsoft.com/en-us/download). If you're not sure which one to download, try [.NET 6.0 Version 6.0.16](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.408-windows-x64-installer)

In the project folder, run the below
```
dotnet publish -p:PublishSingleFile=true -r win-x64 -c Release --self-contained false
```
When that completes, go to `\bin\Release\net<version>-windows\win-x64\publish` and you'll find the **RegexerUI.exe**. Run it to use the software.

## Run without building
You can also just download the release builds if you don't wish to build manually.

If you wish to run the software without installing the required .NET runtime, download the self-contained release.

## How to use
The text to search in should be entered in the **Input** textbox.<br />
The structure of the text to search for is entered in the **Pattern** textbox. Use _**[[double braces]]**_ to capture text for replacement, equivalent to Regex groups. The text within the double braces becomes the name of the captured group. This text must be made up of word characters i.e it **_should not contain symbols_** besides underscore (_).<br />
The text that should replace captures is entered into the **Replace** textbox. You can use the names entered in the Pattern textbox here, and rewrite text surrounding it to change structure of each match in the input.<br />
The **Output** textbox shows the result of the replacement.

You can import text files into the program with the **Select input** button and generated output can also be saved to a file with the **Save output** button. If you want to, for example, perform more operations on generated output, you can hit the **Copy output to input** button to copy the generated output to the input box. This is also useful for recursively running the same pattern on your input.

If you use the same patterns and replacements often, you can save each pattern-replace pair as a template so you don't have to type them in each time you open the program. Click the **Save template** button and enter the name of the template in the dialog that pops up or select an existing template to overwrite. Templates are saved as text files in a folder called _RegexerTemplates_ located in the same directory as the executable. In the program, you can delete templates with the **Delete template** button.

## Syntax
- **[[foo]]**: This can capture a character, word, line or depending on what surrounds it. Translates to **([^\r\n]+?)**.
  
  Example (characters)
  ```
  //Input
  madam
  
  //Pattern
  [[foo]]
  
  //Replace
  [[foo]],
  
  //Output
  m,a,d,a,m,
  ```
  Example (words)
  ```
  //Input
  (madam)
  
  //Pattern
  ([[foo]])
  
  //Replace
  {[[foo]]} 
  
  //Output
  {madam}
  ```
  Example (single lines)
  ```
  //Input
  <tag>
    <content>Hello</content>
  </tag>
  
  //Pattern
  <tag>
    [[foo]]
  </tag>
  
  //Replace
  <new-tag>
    [[foo]]
  </new-tag>
  
  //Output
  <new-tag>
    <content>Hello</content>
  </new-tag>
  ```
- **[[foo|\<quantifier\>]]**: Two quantifiers are available to use with captures: Optional (**o**) and greedy (**g**). The optional quantifier, used as **[[foo|o]]**, means the capture may or may not appear in the match. Translates to **([^\r\n]+?)?**. The greedy quantitfier, used as **[[foo|g]]**, will capture the most it can on the line it appears in. Translates to **([^\r\n]+)**. Both can be used together.

  Example (greedy)
  ```
  //Input
  madam

  //Pattern
  [[foo|g]]

  //Replace
  [[foo]],

  //Output
  madam,
  ```
  
  Example (optional)
  ```
  //Input
  "madam."
  "tree. //huge and leafy"
  "mud."
  "well. //deep and wide"

  //Pattern
  "[[word]].[[comment|o]]"

  //Replace
  ([[word]]):[[comment]]

  //Output
  (madam):
  (tree): //huge and leafy
  (mud):
  (well): //deep and wide
  ```
- **[[foo|\<restriction\>]]**: You can restrict your capture to word characters (**w**), digits (**d**) or whitespace (**s**). Translates to **(\w+?)**, **(\d+?)** and **([^\S\r\n]+?)** respectively. Each can be used with quantifiers e.g **[[foo|so]]** which translates to **([^\S\r\n]+?)?**. You can only use one restriction in a capture.
  
  Example
  ```
  //Input
  Peter, 12.
  Drew!, 25.
  Judas, 30.
  Macon, .
  Linda, thirty.
  Bossa, 9.

  //Pattern
  [[name|w]], [[age|do]].

  //Replace
  Name: [[name]], Age: [[age]]

  //Output
  Name: Peter, Age: 12
  Drew!, 25.
  Name: Judas, Age: 30
  Name: Macon, Age: 
  Linda, thirty.
  Name: Bossa, Age: 9
  ```
- **[[foo|l]]**: Use this to include new-line characters in the capture. In other words, make the match span multiple lines. Can be used with restriction and quantifiers. **[[foo|l]]** translates to **([\S\s]+?)** and **[[foo|wl]]** translates to **([\w\r\n]+?)**.

  Example
  ```
  //Input
  names: {
  "Pete"
  "Abigail"
  "Tolani"
  }
  
  ages: {
  45
  18
  23
  }

  //Pattern
  [[key]]: {[[nums|dl]]}

  //Replace
  [[key]]:[[nums]]

  //Output
  names: {
  "Pete"
  "Abigail"
  "Tolani"
  }
  
  ages:
  45
  18
  23
  ```
- **[[foo|ml]]**: This captures one or more lines. Translates to **([^\r\n]*?)(\r\n(([^\S\r\n]\*)[^\r\n]\*?)?)\*?**. The difference between this and the _include new-lines option ("l")_ is that each line captured using this can be modified in the replacement using prefixes or suffixes. Cannot be used with restrictions or quantifiers.
  
  Example
  ```
  //Input
  <tag>
    <content>Hello</content>
    <content>Hi</content>
  </tag>

  //Pattern
  <[[bar]]>
    [[foo|ml]]
  </[[lou]]>

  //Replace
  <new-[[bar]]>
    <p>[[foo]]</p>
  </new-[[bar]]>

  //Output
  <new-tag>
    <p><content>Hello</content></p>
    <p><content>Hi</content></p>
  </new-tag>
  ```
- **[[foo|u|phrase-or-line-to-capture]]**: Use this to capture phrases (space-separated) or lines (new-line-separated) that can be optional and appear in any order. You may also omit the name (i.e **[[u|phrase-or-line-to-match]]**) and it will be used for matching and not captured. In the pattern, each phrase/line capture should appear on separate lines and should be adjacent to one another to represent a group.

  Example (phrases)
  ```
  //Input
  <input id="name" type="text" disabled class="name"/>
  <input type="button" id="submit" class="big"
      max-length="5" disabled/>

  //Pattern
  <input
  [[id|u|id="[[_id|w]]"]]
  [[class|u|class="[[_class|w]]"]]
  [[type|u|type="[[_type|w]]"]]
  [[length|u|max-length="[[_length|w]]"]]
  [[u|disabled]]
  />

  //Replace
  <input [[id]] [[class]] [[type]] [[length]]/>

  //Output
  <input id="name" class="name" type="text"/>
  <input id="submit" class="big" type="button" max-length="5"/>
  ```

  Example (lines)
  ```
  //Input
  saveUser(
    name: Peter,
    age: 12,
    gender: M,
    isEditing: false,
    status: alive
  )
  
  saveUser(
    age: 9,
    gender: F,
    name: Bossa,
    isEditing: true,
    status: alive
  )
  
  saveUser(
    name: Judas,
    age: 30,
    isEditing: false,
    status: alive
  )
  
  saveUser(
    name: Paula,
    isEditing: true,
    status: alive
  )

  //Pattern
  saveUser(
    [[name|u|name: [[userName]],]]
    [[age|u|age: [[userAge]],]]
    [[u|gender: M,]]
    [[u|gender: F,]]
    [[editing|u|isEditing: true,]]
    [[adding|u|isEditing: false,]]
    status: alive
  )

  //Replace
  [[adding|addUser{]][[editing|editUser{]]
     [[name|User's name is [[userName]]]]
     [[age|User's age is [[userAge]]]]
     [[userName]] is [[userAge]] years old
  }

  //Output
  addUser{
     User's name is Peter
     User's age is 12
     Peter is 12 years old
  }
  
  editUser{
     User's name is Bossa
     User's age is 9
     Bossa is 9 years old
  }
  
  addUser{
     User's name is Judas
     User's age is 30
     Judas is 30 years old
  }
  
  editUser{
     User's name is Paula
     Paula is  years old
  }
  ```
- **[[foo{regex}]]**: Use this to specify a custom regex pattern. You may also omit the name (i.e **[[{regex}]]**) and it will be used for matching and not captured.

  Example
  ```
  //Input
  Ade has 50 apples.
  Tolu had ten chairs,
  Bose had 70 oranges?
  Tayo1 has 30 bowls.
  Shola had 15 caps!

  //Pattern
  [[name{[a-zA-Z]+?}]] [[{(has|had)}]] [[{\d+}]] [[items{\w+}]][[{[.,?!]}]]

  //Replace
  -Name: [[name]]
  -Items: [[items]]

  //Output
  -Name: Ade
  -Items: apples
  Tolu had ten chairs,
  -Name: Bose
  -Items: oranges
  Tayo1 has 30 bowls.
  -Name: Shola
  -Items: caps
  ```
