using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace TextAnalysisTool
{

    //Mārtiņš Būmanis 151RDB442
    public class Program
    {
        static Dictionary<string, List<string>> termsMap = new Dictionary<string, List<string>>();
        static List<List<string>> termsList = new List<List<string>>();
        static List<string> replacedTermsList = new List<string>();
        static string firstFilePath;
        static string queryFilePath;
        static string queryListFilePath;
        static string resultFilePath;

        //static string firstFilePath = @"C:\Users\Martins\source\repos\TextAnalysisTool\sample.txt";
        //static string queryFilePath = @"C:\Users\Martins\source\repos\TextAnalysisTool\regulquery2.txt";
        ///static string queryListFilePath = @"C:\Users\Martins\source\repos\TextAnalysisTool\aizquery.txt";
        //static string resultFilePath = @"C:\Users\Martins\source\repos\TextAnalysisTool\results.txt";


        /// <summary>
        /// Main method
        /// </summary>
        public static void Main()
        {
            // lai mazaak vietu aiznem mainaa, ieliku atseviski visu failu nosaukumu vadisanu
            // https://www.dotnetperls.com/inverted-index
            GetFilePaths();

            //Datu lasīšana no failiem
            FillDictionaryFromFile(); //vardina no sample
            FillTermsListFromFile(); //regularie vaicajumi
            FillReplaceTermsListFromFile(); //aizstajej vaicajumi

            foreach (List<string> query in termsList) //query ir iedotie query, kas var saturēt vairākus terminus, viens cikls vienam query
            {

                //Get postings, 1. uzdevums
                List<string> postingsGet = GetPostings(query);
                postingsGet.Sort();

                //Getquery and or 2.uzd
                List<string> postingsAnd = QueryAnd(query);
                postingsAnd.Sort();

                List<string> postingsOr = QueryOr(query);
                postingsOr.Sort();


                //TF-IDF uzdevums queryand
                List<KeyValuePair<string, double>> notSortedAnd = new List<KeyValuePair<string, double>>();
                foreach (string doc in QueryAnd(query))
                {
                    double docScore = GetDocScore(query, doc);
                    notSortedAnd.Add(new KeyValuePair<string, double>(doc, docScore));
                }
                List<KeyValuePair<string, double>> sortedAnd = notSortedAnd.OrderByDescending(x => x.Value).ToList();

                //TF-IDF uzdevums queryor
                List<KeyValuePair<string, double>> notSortedOr = new List<KeyValuePair<string, double>>(); //tukšs lists
                foreach (string doc in QueryOr(query))
                {
                    double docScore = GetDocScore(query, doc); //tf-idf for katram dokumentam
                    notSortedOr.Add(new KeyValuePair<string, double>(doc, docScore));
                }
                List<KeyValuePair<string, double>> sortedOr = notSortedOr.OrderByDescending(x => x.Value).ToList();


                // ieraksta failaa
                using (TextWriter tw = File.AppendText(resultFilePath))
                {
                    WritePostings(tw, query, postingsGet);
                    WriteAnd(tw, query, postingsAnd);
                    WriteTFIDF(tw, query, sortedAnd);
                    WriteOr(tw, query, postingsOr);
                    WriteTFIDF(tw, query, sortedOr);
                }
            }
            // int myint = 1;
            foreach (string term in replacedTermsList)
            {


                List<KeyValuePair<string, List<string>>> bigramPostings = GetBigramIndex(term);

                using (TextWriter tw = File.AppendText(resultFilePath))
                {
                    WriteBigram(tw, term, bigramPostings);

                };


                //myint += 1;


            }
        }

        /* Te ir bi-gram, viņš sadala tā kā vajag, pa diviem burtiem*/
        static List<KeyValuePair<string, List<string>>> GetBigramIndex(string term)
        {


            List<string> postings = new List<string>();
            List<string> spliced = new List<string>();


            //https://www.codeproject.com/Articles/12098/Term-frequency-Inverse-document-frequency-implemen
            int gramLength = 2;
            List<string> grams = new List<string>();

            int length = term.Length;
            //sadala vārdu pa daļām 

            for (int i = 1; i <= gramLength - 1; i++)
            {
                string gram = term.Substring(0, (i) - (0));
                if (gram.Contains("*"))
                {

                    // gram = gram.Replace("*", "");
                };
                int gramStringLength = gram.Length;
                if (gramStringLength != 0 && grams.IndexOf(gram) == -1)
                    grams.Add(gram);

            }

            for (int i = 0; i < (length - gramLength) + 1; i++)
            {
                string gram = term.Substring(i, (i + gramLength) - (i));
                if (gram.Contains("*"))
                {

                    //gram = gram.Replace("*", "");
                };
                int gramStringLength = gram.Length;
                if (gramStringLength != 0 && grams.IndexOf(gram) == -1)
                    grams.Add(gram);
            }

            for (int i = (length - gramLength) + 1; i < length; i++)
            {
                string gram = term.Substring(i, (length) - (i));
                if (gram.Contains("*"))
                {

                    //gram = gram.Replace("*", "");
                };
                int gramStringLength = gram.Length;
                if (gramStringLength != 0 && grams.IndexOf(gram) == -1)
                    grams.Add(gram);
            }
            // beidz sadalīt pa daļām
            List<string> bigramPostings = new List<string>();




            List<List<string>> listOfPostings = new List<List<string>>();



            List<KeyValuePair<string, List<string>>> keys = termsMap.ToList();


            //šeit sākam meklēšanu
            string gramer;
            foreach (string gram in grams)
            {
                gramer = gram;
                /*
                if (gram.Contains("*"))
                {

                     gramer = gram.Replace("*", "");
                };
                */
                if (gram == grams.Last())
                {
                    if (gram.Contains("*"))
                    {
                        if (gram.EndsWith("*"))
                        {

                            gramer = gram.Replace("*", "");
                            keys = keys.Where(x => x.Key.EndsWith(gramer)).ToList();
                        }
                        if (!gram.EndsWith("*"))
                        {
                            gramer = gram.Replace("*", "");
                            keys = keys.Where(x => x.Key.EndsWith(gramer)).ToList();

                        }

                    }
                    if (!gram.Contains("*"))
                    {

                        keys = keys.Where(x => x.Key.EndsWith(gramer)).ToList();
                    }



                }
                if (gram == grams.First())
                {

                    if (gram.Contains("*"))
                    {
                        if (!gram.StartsWith("*"))
                        {
                            gramer = gram.Replace("*", "");
                            keys = keys.Where(x => x.Key.StartsWith(gramer)).ToList();
                        }

                        if (gram.StartsWith("*"))
                        {
                            if (gram == "*")
                            {

                                //do nothing if first character is *
                            }
                            else
                            {

                                gramer = gram.Replace("*", "");
                                keys = keys.Where(x => x.Key.StartsWith(gramer)).ToList();
                            }
                        }
                    }

                    if (!gram.Contains("*"))
                    {

                        keys = keys.Where(x => x.Key.StartsWith(gramer)).ToList();

                    }


                }
                else
                {


                    if (!gram.Contains("*"))
                    {
                        //gramer = gram.Replace("*", "");
                        keys = keys.Where(x => x.Key.Contains(gramer)).ToList();
                    }

                    if (gram.Contains("*"))
                    {
                        gramer = gram.Replace("*", "");
                        keys = keys.Where(x => x.Key.Contains(gramer)).ToList();
                    }

                    /*
                    if (!gram.Contains("*"))
                    {

                        keys = keys.Where(x => x.Key.Contains(gramer)).ToList();

                    }
                    */
                }

                // keys = keys.Where(x => x.Key.Contains(gramer)).ToList();

                // keys = keys; //debug purposes

            }


            return keys;


        }



       

        /// <summary>
        /// Fills Dictionary termsMap with content from given file
        /// </summary>
        /// <param name="filePath"></param>
        static void FillDictionaryFromFile()
        {
            // https://www.dotnetperls.com/inverted-index
            // Map each term to a list of pages.
            //termsMap = new Dictionary<string, List<string>>();
            foreach (string line in File.ReadLines(firstFilePath))
            {
                string title = GetTitleFromPage(line);
                string[] terms = GetTermsFromPage(line);
                // Loop over terms.
                foreach (string term in terms)
                {
                    // Create list of titles for each term.
                    if (termsMap.TryGetValue(term, out List<string> valueList))
                    {
                        // A list already exists.
                        // ... Add the title if it is not already in the list.
                        if (!valueList.Contains(title))
                        {
                            valueList.Add(title);
                        }
                    }
                    else
                    {
                        // Add new list for this title.
                        var tempList = new List<string>() { title };
                        termsMap[term] = tempList;
                    }
                }
            }
        }

        /// <summary>
        /// Gets title from page(each line) for creating dictionary
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static string GetTitleFromPage(string data)
        {
            // Parse the title.
            var parts = data.Split("\t"); //atrod id
            return parts[0];
        }

        /// <summary>
        /// Gets terms from page(each line) for creating dictionary
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static string[] GetTermsFromPage(string data)
        {
            // Parse the terms.
            var parts = data.Split("\t");

            // case insensitive
            // var secondPart = parts[1].ToLower();

            // case sensitive
            var secondPart = parts[1];
            return secondPart.Split(' ');
        }

        /// <summary>
        /// Fills List of Lists with terms with content from given file. Each line in file is a list of terms, which is then added to termsList
        /// </summary>
        /// <param name="filePath"></param>
        static void FillTermsListFromFile()
        {
            foreach (string line in File.ReadLines(queryFilePath))
            {
                termsList.Add(line.Split(' ').ToList()); //term to list, spereator is a space
            }
        }

        /// <summary>
        /// Aizstajej vaicajumi
        /// </summary>
        static void FillReplaceTermsListFromFile()
        {
            foreach (string line in File.ReadLines(queryListFilePath))
            {
                replacedTermsList.Add(line); //term to list, spereator is a space
            }
        }

        /*
        static List<string> Get(string search)
        {
            // Get all matching pages from a term.
            termsMap.TryGetValue(search, out List<string> matches);
            return matches;
        }
        */

        /// <summary>
        /// Gets all postings for each term
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        static List<string> GetPostings(List<string> terms)
        {
            List<string> postings = new List<string>();
            foreach (string term in terms)
            {
                if (termsMap.TryGetValue(term, out List<string> matches))
                {
                    postings.Add(string.Join(" ", matches));
                }
            }
            return postings;
        }

        /// <summary>
        /// Gets postings which match for all terms given
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        static List<string> QueryAnd(List<string> terms)
        {
            List<List<string>> listOfPostings = new List<List<string>>(); //katram termam var but dazadi postingi tapec list listaa

            foreach (string term in terms)
            {
                if (termsMap.TryGetValue(term, out List<string> matches)) //katram termam mekle values, kas var but match matches ir doc nr
                {
                    listOfPostings.Add(matches);
                }
            }

            List<string> queryAndResult = new List<string>();
            // https://stackoverflow.com/questions/1674742/intersection-of-multiple-lists-with-ienumerable-intersect
            if (listOfPostings.Any()) //ja nav empty
            {
                queryAndResult = listOfPostings //panem to kas ir visos(and) intersect 
                    .Skip(1)
                    .Aggregate(
                    new HashSet<string>(listOfPostings.First()),
                    (h, e) => { h.IntersectWith(e); return h; }).ToList(); //uztaisa jaunu list tikai tur kur visi ir
            }
            return queryAndResult;
        }

        static List<string> QueryOr(List<string> terms)
        {
            List<List<string>> listOfPostings = new List<List<string>>();

            foreach (string term in terms)
            {
                if (termsMap.TryGetValue(term, out List<string> matches))
                {
                    listOfPostings.Add(matches);
                }
            }

            List<string> queryOrResult = new List<string>();
            // https://stackoverflow.com/questions/1674742/intersection-of-multiple-lists-with-ienumerable-intersect tikai ar unionwith
            if (listOfPostings.Any())
            {
                queryOrResult = listOfPostings //atrod ka jebkur ir unionwith
                .Skip(1)
                .Aggregate(
                new HashSet<string>(listOfPostings.First()),
                (h, e) => { h.UnionWith(e); return h; }).ToList(); //savieno visus viena
            }
            return queryOrResult;
        }

        /// <summary>
        /// Formulas for TF-IDF
        /// </summary>
        /// <param name="term"></param>
        /// <param name="docTitle"></param>
        /// <returns></returns>
        static double TFIDFscore(string term, string docTitle)
        {
            double tf_score;
            double idf_score;
            double tfidf_result;
            List<string> words = new List<string>();
            foreach (string line in File.ReadLines(firstFilePath)) //lasam dokumentu pa liniju
            {
                if (GetTitleFromPage(line).Equals(docTitle)) //mekle vai title ir linija ar doctitle ko vajag
                {
                    words = GetTermsFromPage(line).ToList(); //ja ir tad ieliek list
                    break;
                }
            }
            int docCountContainsTerm;
            int termCountInDoc = words.Where(x => x.Equals(term)).Count(); //termina t skaits dokumenta
            int totalTermCountInDoc = words.Count(); //kopejais termina skaits dokumenta
            tf_score = (double)termCountInDoc / totalTermCountInDoc; //dota formula

            List<string> matches = new List<string>();
            int totalDocCount = File.ReadLines(firstFilePath).Count(); //kopejais dokumentu(liniju) skaits


            if (termsMap.TryGetValue(term, out matches)) //dokumentu skaits kas satur termninu t
            {
                docCountContainsTerm = matches.Count();
            }
            else
            { 
                docCountContainsTerm = 0; 
            }

            //formulas


            idf_score = (double)totalDocCount / docCountContainsTerm;

            tfidf_result = tf_score * idf_score;

            return tfidf_result;
        }

        /// <summary>
        /// Get TF-IDF score
        /// </summary>
        /// <param name="query"></param>
        /// <param name="docTitle"></param>
        /// <returns></returns>
        static double GetDocScore(List<string> query, string docTitle)
        {
            double score = 0;
            foreach (string t in query)
            {
                score += TFIDFscore(t, docTitle);
            }
            return score;
        }

        static void PrintAllDictionary(Dictionary<string, List<string>> termsMap)
        {
            foreach (KeyValuePair<string, List<string>> entry in termsMap)
            {
                string matchingPages = string.Join(" ", entry.Value);
                Console.WriteLine($"{entry.Key} = {matchingPages}");
            }
        }

        static void WildCardQuery(string wildcard)
        {
            // 4. solis
        }



        /// <summary>
        /// Reads all file paths
        /// </summary>
        static void GetFilePaths()
        {
            // Dokumenta fails
            Console.WriteLine("Martins Bumanis 151RDB442");
            Console.WriteLine("Ievadiet faila nosaukumu(piemeram sample.txt) kas satur dokumentu id un teikumus: ");
            firstFilePath = Directory.GetCurrentDirectory() + @"\" + Console.ReadLine();

            while (!File.Exists(firstFilePath))
            {
                Console.WriteLine("Fails neeksiste, ievadiet pareizu path: ");
                firstFilePath = Directory.GetCurrentDirectory() + @"\" + Console.ReadLine();
            }

            // Vaicajumu fails

            Console.WriteLine("Ievadiet faila nosaukumu(piemeram vaicajumi.txt), kas satur vaicajumus: ");
            queryFilePath = Directory.GetCurrentDirectory() + @"\" + Console.ReadLine();
            while (!File.Exists(queryFilePath))
            {
                Console.WriteLine("Fails neeksiste, ievadiet pareizu path: ");
                queryFilePath = Directory.GetCurrentDirectory() + @"\" + Console.ReadLine();
            }

            // Aizstajejvaicajumi
            Console.WriteLine("Ievadiet faila nosaukumu(piemeram wildcard.txt), kas satur aizstajejvaicajumus: ");
            queryListFilePath = Directory.GetCurrentDirectory() + @"\" + Console.ReadLine();
            while (!File.Exists(queryListFilePath))
            {
                Console.WriteLine("Fails neeksiste, ievadiet pareizu path: ");
                queryListFilePath = Directory.GetCurrentDirectory() + @"\" + Console.ReadLine();
            }

            // Rezultata fails

            Console.WriteLine("Ievadiet faila nosaukumu(results.txt), kur izvadit rezultatus: ");
            resultFilePath = Directory.GetCurrentDirectory() + @"\" + Console.ReadLine();


        }



        /// <summary>
        /// Failaa izvada GetPostings
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="query"></param>
        /// <param name="postings"></param>
        static void WritePostings(TextWriter tw, List<string> query, List<string> postings)
        {
            var termsAndPostings = query.Zip(postings, (first, second) => first + "\nResults:" + second).ToList();
            tw.WriteLine("\nGetPostings");
            foreach (string termAndPost in termsAndPostings)
                tw.WriteLine(termAndPost);
        }

        /// <summary>
        /// Failaa izvada QueryAnd
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="query"></param>
        /// <param name="postings"></param>
        static void WriteAnd(TextWriter tw, List<string> query, List<string> postings)
        {
            tw.WriteLine("\nQueryAnd"); //method
            foreach (string t in query)
            {
                tw.Write($"{t} "); //given terms printed
            }
            tw.Write("\nResults: ");
            if (postings.Any())
            {
                foreach (string p in postings)
                {

                    tw.Write($"{p} ");
                }
            }
            else
            {
                tw.Write("empty"); //ja nav rezultāti
            }
            tw.WriteLine();
        }

        /// <summary>
        /// Failaa izvada QueryOr
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="query"></param>
        /// <param name="postings"></param>
        static void WriteOr(TextWriter tw, List<string> query, List<string> postings)
        {
            tw.WriteLine("\nQueryOr"); //method printed
            foreach (string t in query)
            {
                tw.Write($"{t} "); //given terms printed
            }
            tw.Write("\nResults: ");
            if (postings.Any())
            {
                foreach (string p in postings)
                {

                    tw.Write($"{p} ");
                }
            }
            else
            {
                tw.Write("empty"); //ja nav rezultāti
            }
            tw.WriteLine();
        }

        static void WriteBigram(TextWriter tw, string query, List<KeyValuePair<string, List<string>>> postings)
        {
            tw.WriteLine("\nWildCard"); //method printed

            tw.Write($"{query}"); //given terms printed


            if (postings.Any())
            {
                foreach (string p in postings.Select(x => x.Key))
                {

                    tw.Write("\nResults: ");

                    tw.Write($"{p} ");
                    tw.Write("\nPostings: ");


                    foreach (List<string> a in postings.Where(x => x.Key == p) //tolist lai varētu izvadīt
                    .Select(x => x.Value))
                    {

                        foreach (string finalPosting in a)
                            tw.Write($"{finalPosting} ");
                    }

                }
                /*
                tw.Write("\nPostings: ");
               foreach (string p in postings.Select(x => x.Value).ToList()) { 
               
                    tw.Write($"{p} ");
                }
                */
            }

            else
            {
                tw.Write("empty"); //ja nav rezultāti
            }
            tw.WriteLine();
        }

        /// <summary>
        /// Failaa izvada rezultatus sakartotus pec TF-IDF score
        /// </summary>
        /// <param name="tw"></param>
        /// <param name="query"></param>
        /// <param name="sortedResults"></param>
        static void WriteTFIDF(TextWriter tw, List<string> query, List<KeyValuePair<string, double>> sortedResults)
        {
            tw.WriteLine("\nTF-IDF");
            foreach (string t in query)
            {
                tw.Write($"{t} "); //given terms posted
            }
            tw.Write("\nResults: ");
            if (sortedResults.Any())
            {
                foreach (KeyValuePair<string, double> k in sortedResults)
                {

                    tw.Write($"{k.Key} ");
                }

                /* Ja nu gribās redzēt TF-IDF vērtības
                foreach (KeyValuePair<string, double> k in sortedResults)
                {

                    tw.Write($"{k.Value} ");
                }

                    */
            }
            else
            {
                tw.Write("empty");
            }
            tw.WriteLine();
        }

    }
}