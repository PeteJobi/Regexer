# Regexer (Multi-line Find-And-Replace)

This is a tool for finding and replacing text without having to deal with complex Regex patterns. Only supports Windows 10 and 11 (not tested on other versions of Windows).

<img width="1270" height="717" alt="image" src="https://github.com/user-attachments/assets/941fc830-ce1a-4f56-902f-8203d1cdc94f" />
<img width="3439" height="1391" alt="image" src="https://github.com/user-attachments/assets/a21a8c6a-ffcc-4754-bff4-91c47b944a0e" />


## How to build
You need to have at least .NET 6 runtime installed to build the software. Download the latest runtime [here](https://dotnet.microsoft.com/en-us/download). If you're not sure which one to download, try [.NET 6.0 Version 6.0.16](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.408-windows-x64-installer)

In the project folder, run the below
```
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false
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
    <p>[[foo|ml]]</p>
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
## Replacement transformations:
You can apply a few transformations to your match replacements, as described below:
- **Duplications**: You can repeat a match multiple times in the output text with the replacement syntax **[[foo|d:(number/expression):separator]]**. The number represents the amount of duplications for the match. You can specify an expression instead of a number, and in this expression, you can use _**i**_, which represents the position of the match i.e if the match is the second one, **_i_** is 2. The only characters allowed in the expression are digits, +, -, /, * and % (modulo). Spaces are not allowed. The separator, which is optional, is the text that should appear between the duplications. If the separator isn't specified, the duplications are adjacent to each other (separated by nothing). To put each duplication in a separate line, use **_ml_** as the separator.
  
  Example (basic)
  ```
  //Input
  Tasty!

  //Pattern
  [[foo|g]]

  //Replace
  [[foo|d:5]]

  //Output
  Tasty!Tasty!Tasty!Tasty!Tasty!
  ```
  
  Example (separator)
  ```
  //Input
  Tasty!

  //Pattern
  [[foo|g]]

  //Replace
  [[foo|d:5:...]]

  //Output
  Tasty!...Tasty!...Tasty!...Tasty!...Tasty!
  ```

  Example (expression)
  ```
  //Input
  The soup was delicious!
  The porridge was sweet!
  The pap was tasty!

  //Pattern
  The [[food]] was [[adjective]]!

  //Replace
  The [[food]] was [[adjective|d:i]]!

  //Output
  The soup was delicious!
  The porridge was sweetsweet!
  The pap was tastytastytasty!
  ```
  
- **Capitalizations**: You can change the capitalization of a match to any of 3 supported types namely Upper case (**u**), Lower case (**l**) and Sentence case (**s**). The syntax is **[[foo|c:(u/l/s)]]**.
  
  Example
  ```
  //Input
  peter went to school.
  jackson went to class.

  //Pattern
  [[name]] went to [[place]].

  //Replace
  [[name|c:s]] went to [[place|c:u]].

  //Output
  Peter went to SCHOOL.
  Jackson went to CLASS.
  ```
  
- **Evaluations**: You can replace a number match with an evaluation involving the number itself and/or the position of the match. The syntax is **[[foo|e:expression]]**. The expression works the same as in **Duplications**, except that in addition to **i** for using the match position, you also have **m** for using the match itself. This transform can only be used for matches restricted to digits i.e **[[foo|d]]**. An example of an expression is **_(m * 2) + i_**. In this example, if the match is 4, and its position is 2, the match is replaced with 10.

  Example
  ```
  //Input
  1. Peter is 7 years old.
  1. Tolani is 23 years old.
  1. Adeolu is 12 years old.

  //Pattern
  [[sn|d]]. [[name]] is [[age|d]] years old.

  //Replace
  [[sn|e:i]]. [[name]] will be [[age|e:m+5]] years old in 5 years.

  //Output
  1. Peter will be 12 years old in 5 years.
  2. Tolani will be 28 years old in 5 years.
  3. Adeolu will be 17 years old in 5 years.
  ```

- **Conditional appearances**: For optional matches (**[[foo|o]]**) and optional phrase/line capture (**[[foo|u|phrase-or-line-to-capture]]**), you can add text to the match depending on whether they are present. The syntax is [[foo|o:conditional_text]].

  Example
  ```
  //Input
  "madam."
  "tree. //huge and leafy"
  "mud."
  "well. //deep and wide"

  //Pattern
  "[[word]].[[comment|o]]"

  //Replace
  ([[comment|o:The ]][[word]][[comment|o: line had comments]])

  //Output
  (madam)
  (The tree line had comments)
  (mud)
  (The well line had comments)
  ```
