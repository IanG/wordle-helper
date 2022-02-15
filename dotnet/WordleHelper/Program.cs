using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using McMaster.Extensions.CommandLineUtils;

namespace iandgratton.WordleHelper
{
    class Program
    {
        const string WORD_FORMAT_ERROR = "Word must be 5 characters of either A-Z, a-z, ?, or ' '";
        const string KNOWN_LETTERS_FORMAT_ERROR = "Known letters must be a unique list of A-Z or a-z characters";
        const string EXCLUDED_LETTERS_FORMAT_ERROR = "Excluded letters must be a unique list of A-Z or a-z characters";

        [Option("-w|--word", "The Word to get help with", CommandOptionType.SingleValue)]
        [Required]
        [StringLength(5, MinimumLength=5, ErrorMessage=WORD_FORMAT_ERROR)]
        [RegularExpression(@"^(?i)[a-z\s\?]{5}$", ErrorMessage=WORD_FORMAT_ERROR)]
        public string Word { get; }

        [Option("-k|--known-letters", "Letters known to be in the word but not their position", CommandOptionType.SingleValue)]
        [StringLength(5, MinimumLength=1, ErrorMessage=KNOWN_LETTERS_FORMAT_ERROR)]
        [RegularExpression(@"^(?i)(?:([a-z])(?!.*\1))*$", ErrorMessage=KNOWN_LETTERS_FORMAT_ERROR)]
        public string KnownLetters { get; }

        [Option("-e|--excluded-letters", "Letters known NOT to be in the word.", CommandOptionType.SingleValue)]
        [StringLength(26, MinimumLength=1, ErrorMessage=EXCLUDED_LETTERS_FORMAT_ERROR)]
        [RegularExpression(@"^(?i)(?:([a-z])(?!.*\1))*$", ErrorMessage=EXCLUDED_LETTERS_FORMAT_ERROR)]
        public string ExcudedLetters { get; }

        [Option("-d|--dictionary-file", "Path to the dictionary file containing words to check against", CommandOptionType.SingleValue)]
        [Required]
        [FileExists]
        public string DictionaryFile { get; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        private void OnExecute()
        {
            ShowVersion();
            ShowWord(Word);
            ShowKnownLetters(KnownLetters);
            ShowExcludedLetters(ExcudedLetters);
            ShowDictionaryFile(DictionaryFile);

            WordleHelper wordleHelper = new WordleHelper(Word, KnownLetters, ExcudedLetters, DictionaryFile);

            List<string> potentialWords = wordleHelper.GetPotentialWords();

            ShowPotentialWords(potentialWords, Word, KnownLetters);
        }

        private static void ShowPotentialWords(List<string> potentialWords, string Word, string KnownLetters)
        {
            const int WORDS_PER_LINE = 10;

            Console.WriteLine($"\n Potential Words: {potentialWords.Count}\n");

            int wordsOnLine = 0;

            ConsoleColor startingColour = Console.ForegroundColor;
            foreach (string word in potentialWords)
            {
                if (wordsOnLine >= WORDS_PER_LINE)
                {
                    wordsOnLine = 0;
                    Console.WriteLine();
                }

                foreach (char c in word)
                {
                    if (Word.Contains(c))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (!String.IsNullOrEmpty(KnownLetters) && KnownLetters.Contains(c))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else
                    {
                        Console.ForegroundColor = startingColour;
                    }

                    Console.Write(c);
                }
                
                Console.Write(" ");
                wordsOnLine++;
            }

            Console.Write("\n\n");
        }

        private static void ShowDictionaryFile(string dictionaryFile)
        {
            Console.WriteLine($"\n Dictionary File: {dictionaryFile}");
        }
        private static void ShowExcludedLetters(string excludedLetters)
        {
            Console.Write("Excluded Letters: ");

            if (!String.IsNullOrEmpty(excludedLetters))
            {
                ConsoleColor startingColour = Console.ForegroundColor;
        
                Console.ForegroundColor = ConsoleColor.DarkGray; 
            
                foreach (char letter in excludedLetters.OrderBy(c => c))
                {
                    Console.Write($"{letter} ");
                }

                Console.ForegroundColor = startingColour;
            }
            else
            {
                Console.Write("None");
            }

            Console.WriteLine();
        }
        private static void ShowWord(string word)
        {
            List<char> wildcards = new List<char>() { ' ', '?' };
            
            ConsoleColor startingColour = Console.ForegroundColor;

            Console.Write("            Word: ");
            foreach (char c in word)
            {
                if (wildcards.Contains(c))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("_ ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{Char.ToUpper(c)} ");                    
                }
            }
            Console.WriteLine();

            Console.ForegroundColor = startingColour;
        }

        private static void ShowKnownLetters(string knownLetters)
        {
            Console.Write("   Known Letters: ");
            if (!String.IsNullOrEmpty(knownLetters))
            {
                ConsoleColor startingColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;

                foreach (char letter in knownLetters.OrderBy(c => c))
                {
                    Console.Write($"{letter} ");
                }

                Console.ForegroundColor = startingColour;
            }
            else
            {
                Console.Write("None");
            }
            Console.WriteLine();
        }
        private static void ShowVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            Console.WriteLine($"{assembly.GetName().Name} {assembly.GetName().Version} {fileVersionInfo.LegalCopyright} {fileVersionInfo.CompanyName}\n");
        }

        class WordleHelper
        {
            private readonly List<char> wildcards = new List<char>() { ' ', '?' };
            private string word;
            private char[] knownLetters;
            private char[] excludedLetters;
            private string dictionaryFile;

            public WordleHelper(string word, string knownLetters, string excludedLetters, string dictionaryFile)
            {
                this.word = word;

                if (!String.IsNullOrEmpty(knownLetters))
                {
                    this.knownLetters = knownLetters.OrderBy(c => c).ToArray();
                }

                if (!String.IsNullOrEmpty(excludedLetters))
                {
                    this.excludedLetters = excludedLetters.OrderBy(c => c).ToArray();
                }

                this.dictionaryFile = dictionaryFile;
            }

            public List<string> GetPotentialWords()
            {
                List<string> words = new List<string>();

                Regex regex = GenerateRegEx();

                foreach (string word in File.ReadLines(dictionaryFile))
                {
                    if (regex.Match(word).Success && ContainsKnownLetters(word))
                    {
                        words.Add(word.ToUpper());
                    }
                }
                
                return words;
            }

            private bool ContainsKnownLetters(string word)
            {
                if (knownLetters == null) return true;
                return knownLetters.All(word.ToUpper().Contains);
            }

            private Regex GenerateRegEx()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("^");
                foreach (char c in word)
                {
                    if (wildcards.Contains(c))
                    {
                        if (excludedLetters != null)
                        {
                            sb.Append($"(?![{new String(excludedLetters)}])[a-z]{{1}}");
                        }
                        else
                        {
                            sb.Append("[a-z]{1}");
                        }
                    }
                    else
                    {
                        sb.Append($"{c}{{1}}");
                    }
                }
                sb.Append("$");

                return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
            }
        }
    }
}
