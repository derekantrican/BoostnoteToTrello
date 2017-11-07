using Manatee.Trello;
using Manatee.Trello.ManateeJson;
using Manatee.Trello.WebApi;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BoostnoteToTrello
{
    class Program
    {
        static string userToken = "";
        static string noteLocation = "";
        static string targetCardId = "";
        static void Main(string[] args)
        {
            #region Startup
            ParseStartupArguments(args);
            AuthorizeTrello();
            #endregion Startup

            #region Read Boostnote file
            List<string> fileLines = File.ReadAllLines(noteLocation).ToList();

            string noteTitle = "";
            List<string> noteContent = new List<string>();
            bool addingContent = false;
            foreach (string s in fileLines)
            {
                if (s.Contains("title:"))
                    noteTitle = s.Replace("title:", "").Replace("\"", "").Trim();
                else if (s == "content: '''")
                    addingContent = true;
                else if (s == "'''")
                    addingContent = false;
                else if (addingContent)
                    noteContent.Add(s);
            }

            noteContent = noteContent.Select(p => { p = p.TrimStart(); return p; }).ToList();
            #endregion Read Boostnote file

            #region Parse out code blocks



            List<int> codeBlockLines = new List<int>();
            bool addingCodeBlock = false;
            for (int i = 0; i < noteContent.Count; i++)
            {
                if (Regex.Match(noteContent[i], @"^```").Success && !addingCodeBlock)
                {
                    addingCodeBlock = true;
                    codeBlockLines.Add(i);
                }
                else if (Regex.Match(noteContent[i], "^```").Success)
                    addingCodeBlock = false;
            }

            for (int i = 0; i < codeBlockLines.Count; i++)
                noteContent.Insert(codeBlockLines[i] + i, ""); //"+ i" is necessary because we will be constantly adjusting the size of the list
            #endregion Parse out code blocks


            #region Parse out checklists
            List<List<KeyValuePair<string, bool>>> checkLists = new List<List<KeyValuePair<string, bool>>>();
            List<KeyValuePair<string, bool>> checkList = new List<KeyValuePair<string, bool>>();
            bool addingChecklist = false;
            foreach (string s in noteContent)
            {
                if (Regex.Match(s, @"^- \[(X|x|\s)\]").Success)
                {
                    addingChecklist = true;
                    bool itemChecked = Regex.Match(s, @"^- \[(X|x)\]").Success;
                    checkList.Add(new KeyValuePair<string, bool>(s.Substring(Regex.Match(s, @"^- \[(X|x|\s)\]").Length).Trim(), itemChecked));
                }
                else if (addingChecklist)
                {
                    addingChecklist = false;
                    checkLists.Add(checkList);
                    checkList = new List<KeyValuePair<string, bool>>();
                }
            }

            if (addingChecklist) //If a checklist is at the end of the note, we'll have to call this once more after the loop
                checkLists.Add(checkList);

            noteContent.RemoveAll(p => Regex.Match(p, @"^- \[(X|x|\s)\]").Success);
            #endregion Parse out checklists

            #region Unescape escaped "section characters"
            for (int i = 0; i < noteContent.Count; i++)
            {
                if (noteContent[i].Contains("\\'''"))
                    noteContent[i] = noteContent[i].Replace("\\'''", "'''");
            }
            #endregion Unescape escaped "section characters"

            AddContentToCard(GetTargetCard(targetCardId), noteTitle, noteContent, checkLists);

            Console.WriteLine("-------------------------");
            Console.WriteLine("-----------DONE----------");
            Console.WriteLine("-------------------------");
        }

        private static void ParseStartupArguments(string[] args)
        {
            if (args.Length > 0)
            {
                bool help = false;

                var p = new OptionSet() {

                    { "h|help|?", "Show help",
                        v => help = v != null},
                    { "userToken=|token=", "Trello user token to use (!!--IF YOU DON'T HAVE ONE, LEAVE THIS EMPTY AND RUN THE PROGRAM--!!)",
                        (arg) => { userToken = arg; }},
                    { "file=|filePath=|note=|notePath=", "File location of the note (.cson)",
                        (arg) => { noteLocation = arg; }},
                    { "id=|targetId=|cardId=|targetCardId=", "Short or long id of the Trello card to write to",
                        (arg) => { targetCardId = arg; }}
                };

                p.Parse(args);
                if (help)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    Environment.Exit(0);
                }
            }
        }

        private static void AuthorizeTrello()
        {
            if (string.IsNullOrEmpty(userToken))
            {
                Console.WriteLine("A usertoken is required. Directing to Trello...");
                Process.Start("https://trello.com/1/authorize?expiration=never&scope=read,write&response_type=token&name=BoostnoteToTrello&key=a684a1c697befd196252f414201114b9");
                Environment.Exit(-1);
            }
        }

        private static Card GetTargetCard(string id)
        {
            ManateeSerializer serializer = new ManateeSerializer();
            TrelloConfiguration.Serializer = serializer;
            TrelloConfiguration.Deserializer = serializer;
            TrelloConfiguration.JsonFactory = new ManateeFactory();
            TrelloConfiguration.RestClientProvider = new WebApiClientProvider();
            TrelloConfiguration.ThrowOnTrelloError = true;
            TrelloAuthorization.Default.AppKey = "a684a1c697befd196252f414201114b9";
            TrelloAuthorization.Default.UserToken = userToken;

            return new Card(id, TrelloAuthorization.Default);
        }

        private static void AddContentToCard(Card targetCard, string name, List<string> descLines, List<List<KeyValuePair<string, bool>>> checkLists)
        {
            targetCard.Name = name;

            if (descLines.Count > 0)
                targetCard.Description = string.Join("\n", descLines);

            foreach (CheckList checklist in targetCard.CheckLists) //Delete the card's checklists
                checklist.Delete();

            if (checkLists.Count > 0)
            {
                foreach (List<KeyValuePair<string, bool>> checklist in checkLists)
                {
                    CheckList newChecklist = targetCard.CheckLists.Add("Checklist");
                    foreach (KeyValuePair<string, bool> checklistItem in checklist)
                    {
                        CheckItem newCheckItem = newChecklist.CheckItems.Add(checklistItem.Key);
                        newCheckItem.State = checklistItem.Value ? CheckItemState.Complete : CheckItemState.Incomplete;
                    }
                }
            }

            TrelloProcessor.Flush();
        }
    }
}
