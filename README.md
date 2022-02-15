# Wordle Helper

## Introduction

This is a small utility that suggests potential words to use in wordle based upon:

- The <span style="color:green">GREEN</span> letters you know are in the correct place in the word.
- The <span style="color:yellow">YELLOW</span> letters you know are in the word but not their position.
- The <span style="color:grey">GREY</span> letters you know are not in the word at all.

## Command line options

| Option      | Description |
| ----------- | ----------- |
| ```-w --word``` | 5 characters representing the characters in the word or wildcards of a ```space``` or ```?``` e.g. ```"M?TAL"```       |
| ```-k --known-letters``` (optional) | A string of known letters in the word e.g. ```ABF```|
| ```-e --excluded-letters``` (optional) | A string of letters known not to be in the word e.g. ```QUZ```|
| ```-d --dictionary-file```| Full path to the dictionary/word list file to be used to locate potential words e.g ```"./etc/dictionary-en.txt"```|


## The Code

### Getting The Code

You can get the code by cloning this repository with:

```
git clone https://github.com/IanG/wordle-helper.git
```

### Building The Application

From the base directory build the application with:

```
dotnet restore ./WordleHelper
dotnet build ./WordleHelper
```

### Running The Application

A dictionary file is provided in the ```etc``` directory which can be used with the ```--dictionary-file``` command line parameter.


#### Discover Command Line Parameters
To see the available command line parameters run:
```
dotnet run --project WordleHelper -- --help
```
which will report back:

```
Usage: WordleHelper [options]
Options:
  -w|--word              The Word to get help with
  -k|--known-letters     Letters known to be in the word but not their position
  -e|--excluded-letters  Letters known NOT to be in the word.
  -d|--dictionary-file   Path to the dictionary file containing words to check against
  -?|-h|--help           Show help information.
```

#### Finding Words

Locate words by providing the appropriate values for command line parameters e.g.

```
dotnet run --project WordleHelper -- --word "M????" --known-letters "AL" --excluded-letters="CDFGHIKOPQRSUVWXYZ" --dictionary-file "./etc/dictionary-en.txt" 
```

Will give the following output:

```
WordleHelper 1.0.0.0

            Word: M _ _ _ _ 
   Known Letters: A L 
Excluded Letters: C D F G H I K O P Q R S U V W X Y Z 

 Dictionary File: ./etc/dictionary-en.txt

 Potential Words: 13

MABEL MABLE MALAM MALAN MALEE MALET MALTA MANAL MELAM MELAN 
MELBA MELLA METAL 
```

## Tooling Used

This solution has been engineered using Microsoft .NET 5.0 using the following freely available tooling:

- A Bash shell.
- [Microsoft .NET Core SDK](https://www.microsoft.com/net/download)
- Nuget Package(s)
  - [McMaster.Extensions.CommandLineUtils](https://www.nuget.org/packages/McMaster.Extensions.CommandLineUtils/)
- [Microsoft Visual Studio Code](https://code.visualstudio.com/) enhanced with the following plugins:
    - C# for Visual Studio Code (Omnisharp)
    - GitLens
    - Markdown Preview Enhanced (for creating this file)

