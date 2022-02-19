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
            ShowPotentialWords(Word, KnownLetters, ExcudedLetters, DictionaryFile);
        }

        private static void ShowPotentialWords(string word, string knownLetters, string excudedLetters, string dictionaryFile)
        {
            const int WORDS_PER_LINE = 10;

            WordleHelper wordleHelper = new WordleHelper(word, knownLetters, excudedLetters, dictionaryFile);

            List<string> potentialWords = wordleHelper.GetPotentialWords();

            Console.WriteLine($"\n Potential Words: {potentialWords.Count}\n");

            char[] wordChars = word.ToUpper().ToCharArray();
            int wordsOnLine = 0;

            foreach (string potentialWord in potentialWords)
            {
                if (wordsOnLine >= WORDS_PER_LINE)
                {
                    wordsOnLine = 0;
                    Console.WriteLine();
                }

                char[] potentialWordChars = potentialWord.ToUpper().ToCharArray();

                for (int i = 0; i < potentialWordChars.Length; i++)
                {
                    if (potentialWord[i] == wordChars[i])
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (!String.IsNullOrEmpty(knownLetters) && knownLetters.ToUpper().Contains(potentialWordChars[i]))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else
                    {
                        Console.ResetColor();
                    }

                    Console.Write(potentialWordChars[i]);
                }

                Console.Write(" ");
                wordsOnLine++;
            }

            Console.ResetColor();
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
                Console.ForegroundColor = ConsoleColor.DarkGray; 
            
                excludedLetters.ToUpper().OrderBy(c => c).ToList().ForEach(c => Console.Write($"{c} "));

                Console.ResetColor();
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
            
            Console.Write("            Word: ");
            foreach (char letter in word)
            {
                if (wildcards.Contains(letter))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("_ ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{Char.ToUpper(letter)} ");                    
                }
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void ShowKnownLetters(string knownLetters)
        {
            Console.Write("   Known Letters: ");
            if (!String.IsNullOrEmpty(knownLetters))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                knownLetters.ToUpper().OrderBy(c => c).ToList().ForEach(c => Console.Write($"{c} "));
                
                Console.ResetColor();
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
                    this.knownLetters = knownLetters.ToUpper().OrderBy(c => c).ToArray();
                }

                if (!String.IsNullOrEmpty(excludedLetters))
                {
                    this.excludedLetters = excludedLetters.ToUpper().OrderBy(c => c).ToArray();
                }

                this.dictionaryFile = dictionaryFile;
            }

            public List<string> GetPotentialWords()
            {
                List<string> potentialWords = new List<string>();

                Regex regex = GenerateRegEx();

                foreach (string potentialWord in File.ReadLines(dictionaryFile))
                {
                    if (regex.Match(potentialWord).Success && ContainsKnownLetters(potentialWord))
                    {
                        potentialWords.Add(potentialWord.ToUpper());
                    }
                }

                return potentialWords;
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
                foreach (char letter in word)
                {
                    if (wildcards.Contains(letter))
                    {
                        if (excludedLetters != null && excludedLetters.Length > 0)
                        {
                            sb.Append($"(?![{new String(excludedLetters)}])");
                        }
                        sb.Append("[a-z]{1}");
                    }
                    else
                    {
                        sb.Append($"{letter}{{1}}");
                    }
                }
                sb.Append("$");

                return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
            }
        }
    }
}