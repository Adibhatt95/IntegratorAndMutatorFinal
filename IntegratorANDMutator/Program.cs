using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.PlayerTasks;
using IntegratorANDMutator.Meta;
using IntegratorANDMutator.Nodes;
using IntegratorANDMutator.Score;
using System.Threading;

using System.Xml.Linq;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
//using System.Data.DataSet;

namespace IntegratorANDMutator
{
    internal class Program
    {//this is the heavily modified code by me, Aditya. see comments in the code for clarity. If any more questions, then contact me on the email thread.
        private static readonly Random Rnd = new Random();
        static Dictionary<string, int> cardname = new Dictionary<string, int>();
        static Dictionary<int, string> allCards = new Dictionary<int, string>();
        //private static string locOfMaster = "D:\\GameInnovationLab\\SabberStone-master";//most important line, this indicates location of the project in your machine. edit here only, exactly as shown.
        private static int maxDepth = 13;//maxDepth = 10 and maxWidth = 500 is optimal 
        private static int maxWidth = 100;//keep maxDepth high(around 13) and maxWidth very low (4) for maximum speed

        private static int parallelThreads = 1;// number of parallel running threads//not important
        private static int testsInEachThread = 1;//number of games in each thread//ae ere
                                                 //you are advised not to set more than 3 parallel threads if you are doing this on your laptop, otherwise the laptop will not survive
        private static int parallelThreadsInner = 1;//this his what is important
        private static int testsInEachThreadInner = 1;//linearly

        private static bool parallelOrNot = true;

        private static Stopwatch stopwatch2 = new Stopwatch();
        static Dictionary<int, double> parentEvalFunc = new Dictionary<int, double>();
        static Dictionary<int, double> parentEvalFunc_SIGMOID = new Dictionary<int, double>();
        //you are advised not to set more than 3 parallel threads if you are doing this on your laptop, otherwise the laptop will not survive

        static string ArgsCardClass = "";
        private static void Main(string[] args)
        {
            ArgsCardClass = args[1].ToString();
            IntegratorANDMutator.CreateAndMutate createMutateObj = new CreateAndMutate();//this is the class I added which contains all functions1
                                                                                         //this above object will help you mutate or create a deck, without worrying about underlying code.
            Dictionary<int, Dictionary<int, List<Card>>> victoryMany = new Dictionary<int, Dictionary<int, List<Card>>>();
            Dictionary<int, float> winRates = new Dictionary<int, float>();
            allCards = getAllCards();
            Console.WriteLine(allCards.Count + " is the count of allcards " + cardname.Count + " is the count of cardname");
            Dictionary<int, string> results = new Dictionary<int, string>();
            Dictionary<int, List<Card>> resultsMutated = new Dictionary<int, List<Card>>();
            bool end = false;
            List<Card> playerDeck2 = Decks.MidrangeJadeShaman;
            stopwatch2.Start();
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = parallelThreads;
            string folderName = args[0].ToString();//"2d7d2018-11-26-56-AM";//"Level-0";//"Level-0";//"Level-3"; //"Level-4";//"Warlock4-0";//

            int level = int.Parse(folderName.Split('-')[1]);
            Dictionary<int, List<Card>> nDecks = new Dictionary<int, List<Card>>();
            bool checkParentValid = false;
            while (true)
            {

                while (Directory.Exists(folderName))
                {
                    Console.WriteLine("for Paladin evoluation code found " + folderName + " checking if number of items =  10,000");
                    Thread.Sleep(1000);
                    string path = folderName + "/Decks.txt";
                    //nDecks = getDecksFromFile(path);
                    int gameCount = Directory.GetFiles(folderName + "/Overall", "*", SearchOption.AllDirectories).Length;//gets number of decks

                    while (gameCount < 10000)
                    {
                        gameCount = Directory.GetFiles(folderName + "/Overall", "*", SearchOption.AllDirectories).Length;
                    }
                    Thread.Sleep(40000);
                    int deckCount = 1;// nDecks.Count;//gameCount / 200;

                    Console.WriteLine("number of items cleared check for " + folderName + ", number of decks =" + deckCount);
                    int currDeckID = 0;
                    Console.WriteLine("folderName is:" + folderName);
                    Dictionary<int, double> deckAndEvalFunction = new Dictionary<int, double>();
                    Dictionary<int, double> deckAndSDFunc = new Dictionary<int, double>();

                    Dictionary<int, double> deckAndEvalFunction_SIGMOID = new Dictionary<int, double>();
                    Dictionary<int, double> deckAndSDFunc_SIGMOID = new Dictionary<int, double>();


                    {

                        float winRateInitial = 0.0f;
                        float winRateLater = 0.0f;

                        int j = 0;
                        while (currDeckID < deckCount)
                        {
                            path = folderName + "/Overall/Deck" + currDeckID;
                            Dictionary<int, string>[] receivingFrom = new Dictionary<int, string>[]
                            {
                                new Dictionary<int, string>(),
                                new Dictionary<int, string>()
                            };
                            receivingFrom = getOverallDeckResultsFromFile(path, currDeckID);

                            Dictionary<int, string> gameResults = receivingFrom[0];
                            Dictionary<int, string> cardStats = receivingFrom[1];
                            Console.WriteLine("Deck=" + currDeckID + " has " + gameResults.Count + " results");
                            Console.WriteLine("Deck=" + currDeckID + " has card stats " + cardStats.Count + " results");
                            string[] receivingFrm = getOverallFINALResultOfLevel(gameResults, cardStats);//replaceWithEVALUE(gameResults)

                            string overallFinalDeckResultInLevel = receivingFrm[0];
                            double averageEvalValue = double.Parse(receivingFrm[1]);
                            //average with parent code....start
                            Console.WriteLine("eval value for deck=" + currDeckID + " in " + folderName + " before shifting average =" + averageEvalValue);
                            if (checkParentValid)
                            {
                                averageEvalValue = (averageEvalValue + parentEvalFunc[currDeckID]) / 2;

                            }
                            Console.WriteLine("eval value for deck=" + currDeckID + " in " + folderName + " after shifting average =" + averageEvalValue);
                            //average with parent code....end
                            string cardStatsForThisDeck = receivingFrm[2];
                            double sdForDeck = double.Parse(receivingFrm[3]);

                            receivingFrm = getOverallFINALResultOfLevel(replaceWithEVALUE(gameResults), cardStats);//replaceWithEVALUE(gameResults)
                            double averageEvalSIGMOIDValue = double.Parse(receivingFrm[1]);
                            double sdSIGMOIDForDeck = double.Parse(receivingFrm[3]);
                            //average with parent code....start
                            Console.WriteLine("sigmoid value for deck=" + currDeckID + " in " + folderName + " before shifting average =" + averageEvalSIGMOIDValue);
                            if (checkParentValid)
                            {
                                averageEvalSIGMOIDValue = (averageEvalSIGMOIDValue + parentEvalFunc_SIGMOID[currDeckID]) / 2;

                            }
                            //average with parent code....end
                            Console.WriteLine("sigmoid value for deck=" + currDeckID + " in " + folderName + " after shifting average =" + averageEvalSIGMOIDValue);
                            //string[] receivingFrom2 = getOverallFINALResultOfLevelOriginalEval(gameResults,cardStats);



                            string stringAllResultsFromResultsDict = getAllStringsFromSpecifiedResults(gameResults, cardStats)[0];
                            if (!deckAndEvalFunction.ContainsKey(currDeckID))
                            {
                                deckAndEvalFunction.Add(currDeckID, averageEvalValue);
                            }
                            if (!deckAndSDFunc.ContainsKey(currDeckID))
                            {
                                deckAndSDFunc.Add(currDeckID, sdForDeck);
                            }

                            if (!deckAndEvalFunction_SIGMOID.ContainsKey(currDeckID))
                            {
                                deckAndEvalFunction_SIGMOID.Add(currDeckID, averageEvalSIGMOIDValue);
                            }
                            if (!deckAndSDFunc_SIGMOID.ContainsKey(currDeckID))
                            {
                                deckAndSDFunc_SIGMOID.Add(currDeckID, sdSIGMOIDForDeck);
                            }

                            string gameStatAddr = folderName + "/OverallStats";
                            Console.WriteLine("currently on deck =" + currDeckID);
                            List<Card> playerDeck = getDeckFromFile(path);
                            if (!Directory.Exists(gameStatAddr))
                            {
                                Directory.CreateDirectory(gameStatAddr);
                            }
                            gameStatAddr = gameStatAddr + "/Deck2-" + currDeckID + ".csv";
                            string gameFinalResultAddr = folderName + "/OverallLevelStats2.csv";
                            string gameFinalCardStatResultAddr = folderName + "/OverallLevelCardStats2.txt";
                            createMutateObj.printToFile(playerDeck, gameStatAddr);//printed once in begining
                            Stopwatch stopwatch = new Stopwatch();

                            stopwatch.Start();
                            try
                            {
                                // foreach (var key in gameResults.Keys)
                                {
                                    using (StreamWriter tw = File.AppendText(gameStatAddr))
                                    {
                                        tw.WriteLine(stringAllResultsFromResultsDict);
                                        tw.Close();
                                    }
                                }
                                if (!File.Exists(gameFinalResultAddr))
                                {
                                    File.Create(gameFinalResultAddr).Dispose();
                                    using (StreamWriter tw = File.AppendText(gameFinalResultAddr))
                                    {
                                        tw.WriteLine("Deck:" + currDeckID + " -- " + overallFinalDeckResultInLevel);
                                        //tw.WriteLine(stringAllResultsFromResultsDict);

                                        //is this is where we put cardstats.
                                        tw.Close();
                                    }
                                }
                                else
                                {
                                    using (StreamWriter tw = File.AppendText(gameFinalResultAddr))
                                    {
                                        tw.WriteLine("Deck:" + currDeckID + " -- " + overallFinalDeckResultInLevel);
                                        //tw.WriteLine(stringAllResultsFromResultsDict);
                                        tw.Close();
                                    }
                                }
                                if (!File.Exists(gameFinalCardStatResultAddr))
                                {
                                    File.Create(gameFinalCardStatResultAddr).Dispose();
                                    using (StreamWriter tw = File.AppendText(gameFinalCardStatResultAddr))
                                    {

                                        tw.WriteLine("Deck:" + currDeckID + " -- " + cardStatsForThisDeck);
                                        //is this is where we put cardstats.
                                        tw.Close();
                                    }
                                }
                                else
                                {
                                    using (StreamWriter tw = File.AppendText(gameFinalCardStatResultAddr))
                                    {

                                        tw.WriteLine("Deck:" + currDeckID + " -- " + cardStatsForThisDeck);
                                        tw.Close();
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            j++;
                            currDeckID++;
                            stopwatch.Stop();
                            long seconds = (stopwatch.ElapsedMilliseconds / 1000);//(stop - start).ToString();//
                            TimeSpan t = TimeSpan.FromSeconds(seconds);
                            stopwatch.Reset();
                            Console.WriteLine("deck " + currDeckID + " completed in " + t.ToString());
                        }
                    }
                    foreach (int key in results.Keys)
                    {
                        Console.WriteLine("Game " + key + " : " + results[key] + "\n");
                        if (resultsMutated.ContainsKey(key))
                        {
                            //createMutateObj.print(resultsMutated[key]);
                        }
                    }
                    // Console.WriteLine("Before Mutation Victory Decks:");
                    stopwatch2.Stop();
                    TimeSpan tempeForOverall = TimeSpan.FromSeconds(stopwatch2.ElapsedMilliseconds / 1000);
                    Console.WriteLine("Overall time taken:" + tempeForOverall.ToString());
                    //Console.ReadLine();
                    level++;
                    string tempFolderName = folderName;
                    folderName = folderName.Split('-')[0] + "-" + level.ToString();
                    Console.WriteLine("DeckAndEvals dictionary count=" + deckAndEvalFunction.Count);
                    Console.WriteLine("DeckAndEvals dictionary count=" + deckAndEvalFunction_SIGMOID.Count);
                    // bool success = mutateAndWrite(folderName, nDecks, deckAndEvalFunction_SIGMOID);
                    //Directory.CreateDirectory(folderName + "/Overall");
                    var top10 = deckAndEvalFunction.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);//not top 10
                    //string top10string = produceDictionaryToString(top10);
                    string top10string = getStringvalueBothDictionaries(deckAndEvalFunction, deckAndSDFunc);//_SIGMOID


                    if (!File.Exists("eachLevelTop100-Normal.csv"))
                    {
                        File.Create("eachLevelTop100-Normal.csv").Dispose();
                        using (StreamWriter tw = File.AppendText("eachLevelTop100-Normal.csv"))
                        {

                            tw.WriteLine(top10string);
                            //is this is where we put cardstats.
                            tw.Close();
                        }
                    }
                    else
                    {
                        using (StreamWriter tw = File.AppendText("eachLevelTop100-Normal.csv"))
                        {

                            tw.WriteLine(top10string);
                            tw.Close();
                        }
                    }
                    top10string = getStringvalueBothDictionaries(deckAndEvalFunction_SIGMOID, deckAndSDFunc_SIGMOID);
                    if (!File.Exists("eachLevelTop100-Sigmoid.csv"))
                    {
                        File.Create("eachLevelTop100-Sigmoid.csv").Dispose();
                        using (StreamWriter tw = File.AppendText("eachLevelTop100-Sigmoid.csv"))
                        {

                            tw.WriteLine(top10string);
                            //is this is where we put cardstats.
                            tw.Close();
                        }
                    }
                    else
                    {
                        using (StreamWriter tw = File.AppendText("eachLevelTop100-Sigmoid.csv"))
                        {

                            tw.WriteLine(top10string);
                            tw.Close();
                        }
                    }

                    // zipAndDelete(tempFolderName);
                    parentEvalFunc = deckAndEvalFunction;
                    parentEvalFunc_SIGMOID = deckAndEvalFunction_SIGMOID;
                    //checkParentValid = true;//comment this to toggle from shifting average to non - shifting average true if want shifting average
                    //end of each level
                }

            }
        }

        public static void zipAndDelete(string foldername)
        {
            string startPath = foldername;
            string zipPath = "ZIP" + foldername + ".zip";
            string extractPath = "Zips";


            ZipFile.CreateFromDirectory(startPath, zipPath);
            //ZipFile.ExtractToDirectory(zipPath, extractPath);
            Directory.Delete(foldername + "/Overall", true);
        }

        public static bool mutateAndWrite(string folderName, Dictionary<int, List<Card>> nDecks, Dictionary<int, double> deckAndEvalValue)
        {
            nDecks = mutate(nDecks, deckAndEvalValue);
            printDecksToNewFolder(folderName, nDecks);
            return true;
        }


        public static void printDecksToNewFolder(string folderName, Dictionary<int, List<Card>> nDecks)
        {
            String build = "";
            string checker = "";
            for (int i = 0; i < 100; i++)
            {

                List<Card> playerDeck = nDecks[i];
                string tempbuild = "";
                for (int j = 0; j < playerDeck.Count; j++)
                {
                    tempbuild += playerDeck[j].Name.ToString() + "*";
                    //Console.WriteLine("Amount is {0} and type is {1}", playerDeck[j].amount, playerDeck[i].type);
                }

                build += i.ToString() + "*" + tempbuild + "\r\n";
                //playerDeck = createMutateObj.createRandomDeck(allCards, cardname);
            }
            Directory.CreateDirectory(folderName);
            folderName += "/Decks.txt";
            using (TextWriter tw = new StreamWriter(folderName))
            {
                tw.WriteLine(build);
                tw.Close();
            }
        }
        public static string[] getOverallFINALResultOfLevel(Dictionary<int, string> gameResults, Dictionary<int, string> cardStats)
        {
            // gameResults = replaceWithEVALUE(gameResults);
            string overallFinalResult = "";
            double[] arrEvalFunc = new double[gameResults.Count];
            double[] turns = new double[gameResults.Count];
            Dictionary<string, int> AllGamesCardStats = new Dictionary<string, int>();
            int wins = 0;
            double seconds = 0;
            string tempgameRes = "";
            string cardStatsTemp = "";
            int tempi = 0;
            Console.WriteLine("Count of gameResults=" + gameResults.Count);
            for (int i = 0; i < gameResults.Count; i++)
            {
                try
                {
                    string[] temp = gameResults[i].Split(new string[] { "healthdiff:" }, StringSplitOptions.None);
                    tempgameRes = gameResults[i];
                    cardStatsTemp = cardStats[i];
                    tempi = i;
                    // Console.WriteLine("at line 262");
                    arrEvalFunc[i] = double.Parse(temp[1].Split('&')[0]);
                    // Console.WriteLine("at line 264");
                    turns[i] = int.Parse(gameResults[i].Split(new string[] { "turns:" }, StringSplitOptions.None)[1].Split(' ')[0]);//Time taken:
                    //Console.WriteLine("at line 266");
                    string[] splitEachGameCardStats = cardStats[i].Split(new string[] { "**" }, StringSplitOptions.None);
                    //Console.WriteLine("at line 268");
                    //Console.WriteLine("sploteach game total array length=" + splitEachGameCardStats.Length + " and at this:");
                    for (int j = 0; j < splitEachGameCardStats.Length - 1; j++)
                    {
                        // Console.WriteLine("sploteach game string length=" + splitEachGameCardStats[j].Length);
                        if (splitEachGameCardStats[j].Length > 1)
                        {
                            if (!AllGamesCardStats.ContainsKey(splitEachGameCardStats[j].Split('*')[0]))
                            {
                                AllGamesCardStats.Add(splitEachGameCardStats[j].Split('*')[0], int.Parse(splitEachGameCardStats[j].Split('*')[1]));
                            }
                            else
                            {
                                AllGamesCardStats[splitEachGameCardStats[j].Split('*')[0]] += int.Parse(splitEachGameCardStats[j].Split('*')[1]);
                            }
                        }
                        else
                        {
                            Console.WriteLine("found with less than length 1 at j=" + j);
                        }
                    }
                    DateTime dateTime = DateTime.ParseExact((gameResults[i].Split(new string[] { "Time taken:" }, StringSplitOptions.None)[1]), "HH:mm:ss", CultureInfo.InvariantCulture);
                    seconds += dateTime.Second;
                    if (gameResults[i].Contains("Player1: WON"))
                    {
                        wins++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error Occured!!!!!!!!!!!!!!!!!!!!!!" + e.Message);
                    Console.WriteLine("game results i was=" + tempgameRes + " card stats i was=" + cardStatsTemp + " i was =" + tempi);
                    string[] temp = new string[4];
                    temp[0] = "Error Occured";
                    temp[1] = "-500";
                    temp[2] = "-500 cardStats Error";
                    temp[3] = "-500 cardStats Error";
                    return temp;
                }
            }
            seconds = seconds / gameResults.Count;
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            float winrateDiv = (float)((float)wins / (gameResults.Count)) * 100;
            overallFinalResult += "winRate=" + winrateDiv + "%, ";
            AllGamesCardStats = AllGamesCardStats.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
            string cardStatsForThisDeck = produceCardStatsString(AllGamesCardStats);
            double average = arrEvalFunc.Average();
            double averageEvalValue = average;
            double sumOfSquaresOfDifferences = arrEvalFunc.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / arrEvalFunc.Length);
            double sdHealth = sd;
            overallFinalResult += "Mean health difference:" + average + ", sd health difference:" + sd + ", variance health difference:" + sumOfSquaresOfDifferences / arrEvalFunc.Length + ", ";
            average = turns.Average();
            sumOfSquaresOfDifferences = turns.Select(val => (val - average) * (val - average)).Sum();
            sd = Math.Sqrt(sumOfSquaresOfDifferences / turns.Length);
            overallFinalResult += "Mean no. turns:" + average + ", sd no. turns:" + sd + ", variance no. turns:" + sumOfSquaresOfDifferences / arrEvalFunc.Length + ", ";
            overallFinalResult += "Average time taken for games:" + t.ToString();
            string[] bothAnswers = new string[4];
            bothAnswers[0] = overallFinalResult;
            bothAnswers[1] = averageEvalValue.ToString();
            bothAnswers[2] = cardStatsForThisDeck;
            bothAnswers[3] = sdHealth.ToString();
            return bothAnswers;
        }


        public static string[] getOverallFINALResultOfLevelOriginalEval(Dictionary<int, string> gameResults, Dictionary<int, string> cardStats)
        {
            //gameResults = replaceWithEVALUE(gameResults);
            string overallFinalResult = "";
            double[] arrEvalFunc = new double[gameResults.Count];
            double[] turns = new double[gameResults.Count];
            Dictionary<string, int> AllGamesCardStats = new Dictionary<string, int>();
            int wins = 0;
            double seconds = 0;
            string tempgameRes = "";
            string cardStatsTemp = "";
            int tempi = 0;
            Console.WriteLine("Count of gameResults=" + gameResults.Count);
            for (int i = 0; i < gameResults.Count; i++)
            {
                try
                {
                    string[] temp = gameResults[i].Split(new string[] { "healthdiff:" }, StringSplitOptions.None);
                    tempgameRes = gameResults[i];
                    cardStatsTemp = cardStats[i];
                    tempi = i;
                    // Console.WriteLine("at line 262");
                    arrEvalFunc[i] = double.Parse(temp[1].Split('&')[0]);
                    // Console.WriteLine("at line 264");
                    turns[i] = int.Parse(gameResults[i].Split(new string[] { "turns:" }, StringSplitOptions.None)[1].Split(' ')[0]);//Time taken:
                    //Console.WriteLine("at line 266");
                    string[] splitEachGameCardStats = cardStats[i].Split(new string[] { "**" }, StringSplitOptions.None);
                    //Console.WriteLine("at line 268");
                    //Console.WriteLine("sploteach game total array length=" + splitEachGameCardStats.Length + " and at this:");
                    for (int j = 0; j < splitEachGameCardStats.Length - 1; j++)
                    {
                        // Console.WriteLine("sploteach game string length=" + splitEachGameCardStats[j].Length);
                        if (splitEachGameCardStats[j].Length > 1)
                        {
                            if (!AllGamesCardStats.ContainsKey(splitEachGameCardStats[j].Split('*')[0]))
                            {
                                AllGamesCardStats.Add(splitEachGameCardStats[j].Split('*')[0], int.Parse(splitEachGameCardStats[j].Split('*')[1]));
                            }
                            else
                            {
                                AllGamesCardStats[splitEachGameCardStats[j].Split('*')[0]] += int.Parse(splitEachGameCardStats[j].Split('*')[1]);
                            }
                        }
                        else
                        {
                            Console.WriteLine("found with less than length 1 at j=" + j);
                        }
                    }
                    DateTime dateTime = DateTime.ParseExact((gameResults[i].Split(new string[] { "Time taken:" }, StringSplitOptions.None)[1]), "HH:mm:ss", CultureInfo.InvariantCulture);
                    seconds += dateTime.Second;
                    if (gameResults[i].Contains("Player1: WON"))
                    {
                        wins++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error Occured!!!!!!!!!!!!!!!!!!!!!!" + e.Message);
                    Console.WriteLine("game results i was=" + tempgameRes + " card stats i was=" + cardStatsTemp + " i was =" + tempi);
                    string[] temp = new string[4];
                    temp[0] = "Error Occured";
                    temp[1] = "-500";
                    temp[2] = "-500 cardStats Error";
                    temp[3] = "-500 cardStats Error";
                    return temp;
                }
            }
            seconds = seconds / gameResults.Count;
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            float winrateDiv = (float)((float)wins / (gameResults.Count)) * 100;
            overallFinalResult += "winRate=" + winrateDiv + "%, ";
            AllGamesCardStats = AllGamesCardStats.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
            string cardStatsForThisDeck = produceCardStatsString(AllGamesCardStats);
            double average = arrEvalFunc.Average();
            double averageEvalValue = average;
            double sumOfSquaresOfDifferences = arrEvalFunc.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / arrEvalFunc.Length);
            double sdHealth = sd;
            overallFinalResult += "Mean health difference:" + average + ", sd health difference:" + sd + ", variance health difference:" + sumOfSquaresOfDifferences / arrEvalFunc.Length + ", ";
            average = turns.Average();
            sumOfSquaresOfDifferences = turns.Select(val => (val - average) * (val - average)).Sum();
            sd = Math.Sqrt(sumOfSquaresOfDifferences / turns.Length);
            overallFinalResult += "Mean no. turns:" + average + ", sd no. turns:" + sd + ", variance no. turns:" + sumOfSquaresOfDifferences / arrEvalFunc.Length + ", ";
            overallFinalResult += "Average time taken for games:" + t.ToString();
            string[] bothAnswers = new string[4];
            bothAnswers[0] = overallFinalResult;
            bothAnswers[1] = averageEvalValue.ToString();
            bothAnswers[2] = cardStatsForThisDeck;
            bothAnswers[3] = sdHealth.ToString();
            return bothAnswers;
        }


        public static Dictionary<int, string> replaceWithEVALUE(Dictionary<int, string> gameResults)// Dictionary<int, double> parentEvalFunc = new Dictionary<int, double>();
        {
            double e = System.Math.E;
            double arrEvalFunc = 0.0;
            for (int i = 0; i < gameResults.Count; i++)
            {
                try
                {
                    string[] temp = gameResults[i].Split(new string[] { "healthdiff:" }, StringSplitOptions.None);
                    arrEvalFunc = double.Parse(temp[1].Split('&')[0]);
                    arrEvalFunc = getEVALUE(arrEvalFunc);
                    gameResults[i] = temp[0] + "healthdiff:" + arrEvalFunc.ToString() + "&" + temp[1].Split('&')[1];
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " error in replacing funtion");
                }
            }
            return gameResults;
        }



        public static double getEVALUE(double arrEvalFunc)
        {
            arrEvalFunc = System.Math.Exp(arrEvalFunc) / (System.Math.Exp(arrEvalFunc) + 1);
            return arrEvalFunc;
        }

        public static string[] getAllStringsFromSpecifiedResults(Dictionary<int, string> gameResults, Dictionary<int, string> cardStats)
        {
            string overallFinalResult = "";

            for (int i = 0; i < gameResults.Count - 1; i++)
            {
                try
                {
                    overallFinalResult += gameResults[i] + "\n";
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error Occured!!!!!!!!!!!!!!!!!!!!!!" + e.Message);

                    string[] temp = new string[3];
                    temp[0] = "Error Occured";
                    temp[1] = "-500";
                    temp[2] = "-500 cardStats Error";
                    return temp;
                }
            }
            overallFinalResult += gameResults[gameResults.Count - 1];
            string[] bothAnswers = new string[3];
            bothAnswers[0] = overallFinalResult;
            bothAnswers[1] = "irrelevant";
            bothAnswers[2] = "irrelevant";
            return bothAnswers;
        }

        public static string produceCardStatsString(Dictionary<string, int> cardStats)
        {
            string build = "";
            var ordered = cardStats.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (string key in cardStats.Keys)
            {
                build += key + "*" + cardStats[key] + "**";
            }

            return build;
        }
        public static string produceDictionaryToString(Dictionary<int, double> deckEvalFunc)
        {
            string build = "";
            foreach (int key in deckEvalFunc.Keys)
            {
                build += "Deck:" + key + "* Evaluation:--" + deckEvalFunc[key] + "**" + "\n";
            }

            return build;
        }

        public static string getStringvalueBothDictionaries(Dictionary<int, double> deckAndEval, Dictionary<int, double> deckAndSigmoid)
        {
            string build = "";
            for (int i = 0; i < deckAndEval.Count; i++)
            {
                build += deckAndEval[i] + ":" + deckAndSigmoid[i] + ",";
            }
            return build;
        }
        public static Dictionary<int, List<Card>> mutate(Dictionary<int, List<Card>> nDecks, Dictionary<int, double> deckAndEvalValue)
        {
            //nDecks = nDecks.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            var top10 = deckAndEvalValue.OrderByDescending(pair => pair.Value).Take(10).ToDictionary(pair => pair.Key, pair => pair.Value);
            int i = 0;

            nDecks = mutateChainedProbabilityMethod(nDecks, top10);
            return nDecks;
        }
        public static Dictionary<int, List<Card>> mutateChainedProbabilityMethod(Dictionary<int, List<Card>> nDecks, Dictionary<int, double> top10Decks)
        {//mutate bottom 90% decks
            Console.WriteLine("decks count = " + nDecks.Count);
            for (int i = 0; i < nDecks.Count; i++)
            {
                if (top10Decks.ContainsKey(i))
                {
                    Console.WriteLine("Deck " + i + " was not mutated.");
                    continue;
                }
                else
                {
                    Console.WriteLine("Mutating Deck=" + i + ":");
                    bool mutateorNot = true;
                    int j = 0;
                    int cardToSwap = cardname[nDecks[i][j].Name];
                    CreateAndMutate createandMutate = new CreateAndMutate();
                    nDecks[i] = createandMutate.mutateSpecificCard(nDecks[i], allCards, cardname, cardToSwap);
                    j++;
                    while (mutateorNot && j < 30)
                    {
                        Random r = new Random();
                        if (r.Next(0, 2) == 0)
                        {
                            cardToSwap = cardname[nDecks[i][j].Name];
                            nDecks[i] = createandMutate.mutateSpecificCard(nDecks[i], allCards, cardname, cardToSwap);
                            j++;
                        }
                        else
                        {
                            mutateorNot = false;
                        }

                    }
                    Console.WriteLine("End of Mutating Deck " + i);
                }
            }
            return nDecks;
        }
        public static List<Card> getDeckFromFile(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
            path += "/" + Files[0].Name;
            List<Card> currDeck = new List<Card>();
            string[] textLines = System.IO.File.ReadAllLines(path);
            Console.WriteLine("lines size=" + textLines.Length + "in path=" + path);
            string[] cards = textLines[0].Split('*');
            for (int i = 0; i < 30; i++)
            {
                currDeck.Add(Cards.FromName(cards[i]));
            }
            return currDeck;
        }
        public static Dictionary<int, List<Card>> getDecksFromFile(string path)
        {
            Dictionary<int, List<Card>> nDecks = new Dictionary<int, List<Card>>();

            string[] textLines = System.IO.File.ReadAllLines(path);
            //int arg = 3;
            int currDeckInd = 0;
            Console.WriteLine("lines size=" + textLines.Length);
            while (currDeckInd <= (textLines.Length))
            {
                if (textLines[currDeckInd].Length < 3)
                {
                    Console.WriteLine("textLines length was less than 3");
                    break;
                }
                if (!nDecks.ContainsKey(currDeckInd))
                {
                    List<Card> currDeck = new List<Card>();
                    string[] cards = textLines[currDeckInd].Split('*');
                    for (int i = 1; i < 31; i++)
                    {
                        currDeck.Add(Cards.FromName(cards[i]));
                    }
                    // Console.WriteLine("currDeck=" + currDeckInd + " size of deck=" + currDeck.Count);
                    nDecks.Add(currDeckInd, currDeck);
                }
                currDeckInd++;
            }


            return nDecks;
        }

        public static Dictionary<int, string>[] getOverallDeckResultsFromFile(string path, int currDeckID)
        {
            Dictionary<int, string> overallResults = new Dictionary<int, string>();
            Dictionary<int, string> overallCardStatsResults = new Dictionary<int, string>();
            DirectoryInfo d = new DirectoryInfo(path);//Assuming Test is your Folder

            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files
            int i = 0;
            string tempPath = path;
            foreach (FileInfo File in Files)
            {
                tempPath += "/" + File.Name;
                string[] textLines = System.IO.File.ReadAllLines(tempPath);

                if (!overallResults.ContainsKey(i))
                {
                    overallResults.Add(i, textLines[1]);
                    overallCardStatsResults.Add(i, textLines[2]);
                }
                i++;
                tempPath = path;
            }
            Console.WriteLine(overallResults.Count + " is the number of results (should be 400)");
            Dictionary<int, string>[] sendingBack = new Dictionary<int, string>[]
                {
                    new Dictionary<int, string>(),
                    new Dictionary<int, string>()
                };
            sendingBack[0] = overallResults;
            sendingBack[1] = overallCardStatsResults;
            return sendingBack;
        }




        public static string getWinRateTimeMean(List<Card> player1Deck, int where, List<Card> player2Deck, string gameLogAddr)
        {

            int[] wins = Enumerable.Repeat(0, 1000).ToArray();
            long sum_Timetaken = 0;
            int winss = 0;
            object[] temp()
            {
                object[] obj = new object[1002];
                for (int i = 0; i < parallelThreadsInner * testsInEachThreadInner; i++)
                {
                    obj[i] = new Stopwatch();
                }
                return obj;
            }
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 8;// parallelThreads;// Environment.ProcessorCount;//parallelThreadsInner+10;
                                                       // Console.WriteLine(Environment.ProcessorCount);
            object[] stopwatches = temp();
            string res = "";
            Parallel.For(0, parallelThreadsInner * testsInEachThreadInner, parallelOptions, j =>
            {
                // int i = 0;
                // long max = 0;
                // while (!end)

                //for (int i = testsInEachThreadInner * j; i < (j + 1) * testsInEachThreadInner; i++)//(int i = 0; i < 10 ; i++) //

                int i = j;
                //Console.WriteLine("Environment:" + Environment.ProcessorCount);
                // Console.WriteLine("Inner i, or here inside getWinRateTimeMean at here= " + i);
                ((Stopwatch)stopwatches[i]).Start();
                // start = DateTime.Now;
                string s = "";
                bool retry = true;
                while (retry)
                {
                    try
                    {
                        s = FullGame(player1Deck, i, player2Deck, gameLogAddr);
                        Console.WriteLine(s);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        s = e.Message.ToString();
                        CreateAndMutate createAndMutate = new CreateAndMutate();
                        //   Console.WriteLine("Player 1 deck that caused issue:");
                        // createAndMutate.print(player1Deck);
                    }
                    //stop = DateTime.Now;
                    if (s.ToLower().Contains("present"))
                    {
                        retry = true;
                    }
                    else
                    {
                        retry = false;
                    }
                }
                ((Stopwatch)stopwatches[i]).Stop();
                long seconds = (((Stopwatch)stopwatches[i]).ElapsedMilliseconds / 1000);//(stop - start).ToString();//
                                                                                        // Console.WriteLine("secondes:" + seconds);
                                                                                        //  Console.Write("Seconds: " + seconds);
                                                                                        // TimeSpan tempe = TimeSpan.FromSeconds(seconds);
                                                                                        // Console.WriteLine("time taken for "+ i +":" + tempe);
                sum_Timetaken = sum_Timetaken + seconds;
                // Console.WriteLine("sum_TimeTaken in loop:" + sum_Timetaken);
                //((Stopwatch)stopwatches[i]).Reset();


                if (s.Contains("Player1: WON"))
                {
                    wins[i]++;
                    // winss++;
                    // Console.WriteLine("Winss:" + winss);
                }

                // Console.WriteLine("Max was:" + max);
                // max = 0;
                res = s;
            });
            // Console.WriteLine("Starting test setup. v6.7: Not running in Parallel at ALL run in parallel " + parallelThreads + "x and in each parallel, no of tasks:" + testsInEachThread + " and inner parallel:" + parallelThreadsInner + " and each within inner parallel, inner tasks:" + testsInEachThreadInner + " times, different decks, get winrates and time avg of each and print max depth =" + maxDepth + " , max width = " + maxWidth + "");

            for (int i = 0; i < (parallelThreadsInner * testsInEachThreadInner); i++)
                // Console.WriteLine("i:" + i + " wins:" + wins[i]);
                sum_Timetaken = 0;
            for (int i = 0; i < (parallelThreadsInner * testsInEachThreadInner); i++)
            {
                //  Console.WriteLine("i:" + i + " Times:" + ((Stopwatch)stopwatches[i]).ElapsedMilliseconds / 1000);
                sum_Timetaken = sum_Timetaken + ((Stopwatch)stopwatches[i]).ElapsedMilliseconds / 1000;
            }
            //Console.WriteLine("New sum_timetaken=" + sum_Timetaken);
            TimeSpan t = TimeSpan.FromSeconds(sum_Timetaken / (parallelThreadsInner * testsInEachThreadInner));
            float winsSum = 0;
            for (int i = 0; i < (parallelThreadsInner * testsInEachThreadInner); i++)
                if (wins[i] > 0)
                    winsSum++;
            //winsSum = winss;
            //Console.WriteLine(winsSum + "is winsSum");
            // Console.WriteLine(winss + "is winss");
            float winrateDiv = (float)((float)winsSum / ((float)parallelThreadsInner * testsInEachThreadInner));
            //return "Win rate =" + winrateDiv * 100 + "% and average time of each round (hh:mm:ss) = " + t.ToString();
            return res + " and average time of game (hh:mm:ss) = " + t.ToString();
        }

        public static string getWinRateTimeMeanLinear(List<Card> player1Deck, int where, List<Card> player2Deck)
        {
            int[] wins = Enumerable.Repeat(0, 1000).ToArray();
            long sum_Timetaken = 0;
            int winss = 0;
            int numOfTests = 70;
            object[] temp()
            {
                object[] obj = new object[1002];
                for (int i = 0; i < numOfTests; i++)
                {
                    obj[i] = new Stopwatch();
                }
                return obj;
            }

            object[] stopwatches = temp();
            //Parallel.For(0, parallelThreadsInner, parallelOptions, j =>
            {
                // int i = 0;
                long max = 0;
                // while (!end)
                for (int i = 0; i < numOfTests; i++) ////
                {
                    Console.WriteLine("Inner i, or here inside getWinRateTimeMean at here= " + i);
                    ((Stopwatch)stopwatches[i]).Start();
                    // start = DateTime.Now;
                    string s = "";
                    try
                    {
                        //s = FullGame(player1Deck, i, player2Deck);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        CreateAndMutate createAndMutate = new CreateAndMutate();
                        Console.WriteLine("Player 1 deck that caused issue:");
                        createAndMutate.print(player1Deck);
                    }
                    //stop = DateTime.Now;
                    ((Stopwatch)stopwatches[i]).Stop();
                    long seconds = (((Stopwatch)stopwatches[i]).ElapsedMilliseconds / 1000);//(stop - start).ToString();//
                    Console.WriteLine("secondes:" + seconds);
                    if (max < seconds)
                    {
                        max = seconds;
                    }
                    TimeSpan tempe = TimeSpan.FromSeconds(seconds);
                    Console.WriteLine("time taken for " + i + ":" + tempe);
                    sum_Timetaken = sum_Timetaken + seconds;
                    Console.WriteLine("sum_TimeTaken in loop:" + sum_Timetaken);
                    //((Stopwatch)stopwatches[i]).Reset();
                    if (s.Contains("Player1: WON"))
                    {
                        wins[i]++;
                        winss++;
                        Console.WriteLine("Winss:" + winss);
                    }
                }
                Console.WriteLine("Max was:" + max);
                max = 0;
            }//);
            Console.WriteLine("Starting test setup. v6.7: all linear, 10 and in each 50 linear all randomly gen decks Not running in Parallel at ALL, this is in Linear Method run in parallel " + parallelThreads + "x and in each parallel, no of tasks:" + testsInEachThread + " and inner parallel:" + parallelThreadsInner + " and each within inner parallel, inner tasks:" + testsInEachThreadInner + " times, different decks, get winrates and time avg of each and print max depth =" + maxDepth + " , max width = " + maxWidth + "");

            for (int i = 0; i < (numOfTests); i++)
                Console.WriteLine("i:" + i + " wins:" + wins[i]);
            sum_Timetaken = 0;
            for (int i = 0; i < (numOfTests); i++)
            {
                Console.WriteLine("i:" + i + " Times:" + ((Stopwatch)stopwatches[i]).ElapsedMilliseconds / 1000);
                sum_Timetaken = sum_Timetaken + ((Stopwatch)stopwatches[i]).ElapsedMilliseconds / 1000;
            }
            Console.WriteLine("New sum_timetaken=" + sum_Timetaken);
            TimeSpan t = TimeSpan.FromSeconds(sum_Timetaken / (numOfTests));
            float winsSum = 0;
            for (int i = 0; i < (numOfTests); i++)
                if (wins[i] > 0)
                    winsSum++;
            //winsSum = winss;
            Console.WriteLine(winsSum + "is winsSum");
            Console.WriteLine(winss + "is winss");
            float winrateDiv = (float)((float)winsSum / ((float)numOfTests));
            return "Win rate =" + winrateDiv * 100 + "% and average time of each round (hh:mm:ss) = " + t.ToString();
        }

        public static Dictionary<int, string> getAllCards()
        {
            Dictionary<int, string> allcards = new Dictionary<int, string>();

            string fileName = "CardDefs.xml";
            if (System.IO.File.Exists(fileName))
            {
                int i = 1;
                XDocument doc = XDocument.Load(fileName);
                var authors = doc.Descendants("Entity").Descendants("Tag").Where(x => x.Attribute("name").Value == "CARDNAME").Elements("enUS");
                // byte[] byteArray = Encoding.UTF8.GetBytes("D:\\GameInnovationLab\\xmldata test.txt");
                //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
                //MemoryStream stream = new MemoryStream(byteArray);
                //System.IO.StreamWriter file = new System.IO.StreamWriter(stream);
                foreach (var author in authors)
                {


                    if (!cardname.ContainsKey(author.Value) && (Cards.FromName(author.Value) != Cards.FromName("Default"))
                        && Cards.FromName(author.Value).Implemented &&
                        (Cards.FromName(author.Value).Set == CardSet.CORE)
                        && Cards.FromName(author.Value).Collectible && (Cards.FromName(author.Value).Type != CardType.HERO) &&
                        (Cards.FromName(author.Value).Type != CardType.ENCHANTMENT) && (Cards.FromName(author.Value).Type != CardType.INVALID)
                        && (Cards.FromName(author.Value).Type != CardType.HERO_POWER) && (Cards.FromName(author.Value).Type != CardType.TOKEN))
                    {
                        if (checkClass(author.Value))
                        {
                            Console.WriteLine(author.Value);
                            Console.WriteLine(Cards.FromName(author.Value).Class);
                            Console.WriteLine(Cards.FromName(author.Value).Type);
                            cardname.Add(author.Value, i);
                            allcards.Add(i, author.Value);
                            i++;

                            // Console.WriteLine(allcards.Count);
                        }
                    }

                }
                /*XmlReader xmlReader = XmlReader.Create(fileName);
			while (xmlReader.Read())
			{
				if ( (xmlReader.Name == "CARDNAME"))
				{
					if (xmlReader.HasAttributes)
					{

						allcards.Add(i, xmlReader.GetAttribute("enUS"));
						i++;
					}
				}
			}*/
                //D:\\GameInnovationLab\\xmldata test.txt


            }
            else
            {
                Console.WriteLine("File not found");
            }
            return allcards;
        }

        public static bool checkClass(string nameOfCard)
        {
            //|| Cards.FromName(author.Value).Class == CardClass.PALADIN
            if (ArgsCardClass.ToLower().Equals("paladin"))
            {
                if (Cards.FromName(nameOfCard).Class == CardClass.PALADIN || (Cards.FromName(nameOfCard).Class == CardClass.NEUTRAL))
                { return true; }
                else { return false; }
            }
            if (ArgsCardClass.ToLower().Equals("hunter"))
            {
                if (Cards.FromName(nameOfCard).Class == CardClass.HUNTER || (Cards.FromName(nameOfCard).Class == CardClass.NEUTRAL))
                { return true; }
                else { return false; }
            }
            if (ArgsCardClass.ToLower().Equals("warlock"))
            {
                if (Cards.FromName(nameOfCard).Class == CardClass.WARLOCK || (Cards.FromName(nameOfCard).Class == CardClass.NEUTRAL))
                { return true; }
                else { return false; }
            }
            return false;

        }
        //the game we need
        public static string FullGame(List<Card> player1Deck, int where, List<Card> player2Deck, string gameLogAddr)
        {
            string logsbuild = "";
            var game = new Game(
                new GameConfig()
                {
                    StartPlayer = 1,
                    Player1Name = "FitzVonGerald",
                    Player1HeroClass = CardClass.WARRIOR,
                    Player1Deck = player1Deck,//Decks.AggroPirateWarrior,
                    Player2Name = "RehHausZuckFuchs",
                    Player2HeroClass = CardClass.SHAMAN,
                    Player2Deck = player2Deck,
                    FillDecks = false,
                    Shuffle = true,
                    SkipMulligan = false
                });
            game.StartGame();

            var aiPlayer1 = new AggroScore();
            var aiPlayer2 = new MidRangeScore();

            List<int> mulligan1 = aiPlayer1.MulliganRule().Invoke(game.Player1.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());
            List<int> mulligan2 = aiPlayer2.MulliganRule().Invoke(game.Player2.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());
            logsbuild += $"Player1: Mulligan {string.Join(",", mulligan1)}";
            logsbuild += "\n";
            logsbuild += $"Player2: Mulligan {string.Join(",", mulligan2)}";
            logsbuild += "\n";
            // Console.WriteLine($"Player1: Mulligan {string.Join(",", mulligan1)}");
            //Console.WriteLine($"Player2: Mulligan {string.Join(",", mulligan2)}");

            game.Process(ChooseTask.Mulligan(game.Player1, mulligan1));
            game.Process(ChooseTask.Mulligan(game.Player2, mulligan2));

            game.MainReady();

            while (game.State != State.COMPLETE)
            {
                //  Console.WriteLine("here:" + where);
                logsbuild += $"Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState} - " +
                    $"ROUND {(game.Turn + 1) / 2} - {game.CurrentPlayer.Name}" + "\n";
                logsbuild += $"Hero[P1]: {game.Player1.Hero.Health} / Hero[P2]: {game.Player2.Hero.Health}" + "\n";
                logsbuild += "\n";
                Console.WriteLine($"Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState} - " +
                   $"ROUND {(game.Turn + 1) / 2} - {game.CurrentPlayer.Name}");//I get round number here, can cut it off right here
                //Console.WriteLine($"Hero[P1]: {game.Player1.Hero.Health} / Hero[P2]: {game.Player2.Hero.Health}");
                //Console.WriteLine("");
                while (game.State == State.RUNNING && game.CurrentPlayer == game.Player1)
                {
                    logsbuild += $"* Calculating solutions *** Player 1 ***" + "\n";
                    //  Console.WriteLine($"* Calculating solutions *** Player 1 ***");//player 1's turn
                    List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player1.Id, aiPlayer1, maxDepth, maxWidth);
                    var solution = new List<PlayerTask>();
                    solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
                    //Console.WriteLine($"- Player 1 - <{game.CurrentPlayer.Name}> ---------------------------");
                    logsbuild += $"- Player 1 - <{game.CurrentPlayer.Name}> ---------------------------" + "\n";
                    foreach (PlayerTask task in solution)
                    {
                        logsbuild += task.FullPrint() + "\n";
                        //  Console.WriteLine(task.FullPrint());//important focus point for you. test this by first uncommenting it
                        game.Process(task);
                        if (game.CurrentPlayer.Choice != null)
                        {
                            logsbuild += $"* Recaclulating due to a final solution ..." + "\n";
                            //    Console.WriteLine($"* Recaclulating due to a final solution ...");
                            break;
                        }
                    }
                }//hello hell0 hello is there anybody in there? Now that you can hear it

                // Random mode for Player 2
                // Console.WriteLine($"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------");//player 2's turn
                logsbuild += $"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------" + "\n";
                while (game.State == State.RUNNING && game.CurrentPlayer == game.Player2)
                {
                    //var options = game.Options(game.CurrentPlayer);
                    //var option = options[Rnd.Next(options.Count)];
                    //Log.Info($"[{option.FullPrint()}]");
                    //game.Process(option);
                    //   Console.WriteLine($"* Calculating solutions *** Player 2 ***");
                    logsbuild += $"* Calculating solutions *** Player 2 ***" + "\n";
                    List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player2.Id, aiPlayer2, maxDepth, maxWidth);
                    var solution = new List<PlayerTask>();
                    solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
                    // Console.WriteLine($"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------");
                    logsbuild += $"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------" + "\n";
                    foreach (PlayerTask task in solution)
                    {
                        //   Console.WriteLine(task.FullPrint());//this is what you neeed to focus on right here
                        logsbuild += task.FullPrint() + "\n";
                        game.Process(task);
                        if (game.CurrentPlayer.Choice != null)
                        {
                            //     Console.WriteLine($"* Recaclulating due to a final solution ...");
                            logsbuild += $"* Recaclulating due to a final solution ..." + "\n";
                            break;
                        }
                    }
                }
            }
            //Console.WriteLine($"Game: {game.State}, Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState}");
            int healthdiff = game.Player1.Hero.Health - game.Player2.Hero.Health;
            logsbuild += "Game: {game.State}, Player1: " + game.Player1.PlayState + " / Player2:" + game.Player2.PlayState + "healthdiff:" + healthdiff + "& turns:" + game.Turn;
            using (StreamWriter tw = File.AppendText(gameLogAddr))
            {
                tw.WriteLine(logsbuild);
                tw.Close();
            }
            return "Game: {game.State}, Player1: " + game.Player1.PlayState + " / Player2:" + game.Player2.PlayState + ", healthdiff:, " + healthdiff + ", & turns:," + game.Turn;
        }
        static Dictionary<int, Dictionary<string, int>> calcuated = new Dictionary<int, Dictionary<string, int>>();//global
        static void calcukateCardFreq(string printed, int idgame, Dictionary<string, int> cardnames)
        {

            string namecard = "";
            //int count = 0;
            if (cardnames.ContainsKey(namecard))
            {
                calcuated[idgame].Add(namecard, 0);
            }
        }
    }
}
