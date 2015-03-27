using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using EPiServer.Core;
using EPiServer.Validation;

namespace Nergard.EPi.Tools.LoremIpsum
{
    public class LoremGenerator : IValidate<ContentData>
    {
        #region Vars
        enum TypeOfListContent
        {
            Text,
            Links
        }

        string currentPageLanguageCode = "";

        Random random = new Random();

        #endregion

        /// <summary>
        /// Validation method. Parses the currentpage propertylist and inspects strings,longstrings and xhtmlstring for specific command.
        /// If found command is parsed to generate texts, elements and lists with randomized words.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        IEnumerable<ValidationError> IValidate<ContentData>.Validate(ContentData page)
        {
            const string commandPrefix = "#>";
            const string commandSeparator = ">";

            int numberOfWords = 0;
            int numberOfElements = 0;

            string command = "";
            string content = "";
            string[] splittedCommand = null;
            string tag = "";

            StringBuilder editorReturnContent = new StringBuilder();

            currentPageLanguageCode = EPiServer.Globalization.ContentLanguage.PreferredCulture.TwoLetterISOLanguageName; 

            PropertyDataCollection properties = page.Property;

            foreach (PropertyData propp in properties)
            {
                //Mostly built-in properties that we want to skip
                if (propp.OwnerTab == -1) continue;

                //String and longstring properties
                if (propp.PropertyValueType == typeof(String) && ( propp.Type == PropertyDataType.String || propp.Type == PropertyDataType.LongString ) )
                {
                    //If we can't get a value we just ignore it
                    try
                    {
                        command = page.GetValue(propp.Name).ToString().ToLower().Trim();

                        if (command.StartsWith(commandPrefix))
                        {
                            if (Int32.TryParse(command.Replace(commandPrefix,""),out numberOfWords))
                            {
                                page.SetValue(propp.Name,GetWords(numberOfWords));
                            }
                        }
                    }
                    catch { }
                }
                else if (propp.PropertyValueType == typeof(XhtmlString) && propp.Type == PropertyDataType.LongString) ////Xhtmlstring properties
                {

                    var val = page.GetPropertyValue(propp.Name);

                    if (val == null) continue;

                    //Parse the editorcontent and creata a list of the content in all paragraphs
                    var matches = ParseXhtmlString(page.GetValue(propp.Name).ToString());
                    var paragraphList = Paragraphs(matches);

                    foreach (var paragraph in paragraphList)
                    {
                        command = paragraph;
                        content = "";

                        if (command.StartsWith(commandPrefix))
                        {
                            command = command.Replace(commandPrefix, "");
                            splittedCommand = command.Split(commandSeparator.ToCharArray());

                            tag = splittedCommand[0];

                            if (splittedCommand.Count() == 2)
                            {
                                if (!Int32.TryParse(splittedCommand[1].ToString(), out numberOfWords)) continue;
                            }
                            else if (splittedCommand.Count() == 3)
                            {
                                if (!Int32.TryParse(splittedCommand[1].ToString(), out numberOfElements)) continue;
                                if (!Int32.TryParse(splittedCommand[2].ToString(), out numberOfWords)) continue;
                            }
                            else
                                continue;
                                

                            switch (tag)
                            {
                                case "ollinks":
                                    tag = "ol";
                                    content = GetListContent(TypeOfListContent.Links, numberOfElements, numberOfWords);
                                    break;
                                case "ullinks":
                                    tag = "ul";
                                    content = GetListContent(TypeOfListContent.Links, numberOfElements, numberOfWords);
                                    break;
                                case "oltext":
                                    tag = "ol";
                                    content = GetListContent(TypeOfListContent.Text, numberOfElements, numberOfWords);
                                    break;
                                case "ultext":
                                    tag = "ul";
                                    content = GetListContent(TypeOfListContent.Text, numberOfElements, numberOfWords);
                                    break;
                                default:
                                    content = GetWords(numberOfWords);
                                    break;
                            }

                            //Non paragraph tags need to be inside a p tag
                            if (tag == "p")
                                editorReturnContent.AppendFormat("<{0}>{1}</{2}>", tag, content, tag);
                            else
                                editorReturnContent.AppendFormat("<p><{0}>{1}</{2}></p>", tag, content, tag);
                        }
                        else //We keep wysiwyg content
                            editorReturnContent.AppendFormat("<p>{0}</p>", paragraph);
                    }

                    page.SetValue(propp.Name, editorReturnContent.ToString());
                }
            }

            return Enumerable.Empty<ValidationError>();
        }

        #region Methods

        /// <summary>
        /// Creates a list of li elements with either texts or links. Links are non functional with # as href
        /// </summary>
        /// <param name="type"></param>
        /// <param name="numberOfItems">Number of li elements</param>
        /// <param name="numberOfWords">Number of randomized words in plain text or link text</param>
        /// <returns></returns>
        private string GetListContent(TypeOfListContent type,int numberOfItems, int numberOfWords)
        {
            StringBuilder returnString = new StringBuilder();
            string template = "";

            if (type == TypeOfListContent.Text)
                template = "<li>{0}</li>";
            else
                template = @"<li><a>{0}</a></li>";

            for (int x = 1; x <= numberOfItems; x++)
            {
                returnString.AppendFormat(template,GetWords(numberOfWords));
            }
            
            return returnString.ToString();
        }

        /// <summary>
        /// Get the specified number of words. The words are selected randomly and selected from different sets depending of current page language.
        /// </summary>
        /// <param name="numberOfWordsToGet">How many words to get</param>
        /// <param name="languageCode">TwoletterIsoLanguageName to get localized words</param>
        /// <returns></returns>
        private string GetWords(int numberOfWordsToGet)
        {
            string loremWords = "";
            string languageCode = currentPageLanguageCode;
            StringBuilder returnWords = new StringBuilder();

            switch (languageCode)
	        {
                case "sv":
                    loremWords = "Möjlig oss ta Bli skadliga kunde Dubbdäck behöver partiklar de jag finns Han de vill Men vi tittade början bråttom sig en Regeringen minska plånboken inför utreda, för stockholms Tycker vilka minska på, om ordförande tycker Därefter partiklar gå ned Kan in tror men, vi material vilka Nästa budgetförhandlingar om partikelnivåerna utredning hem annat möjligheten Annat miljöproblem stadsmiljöborgarrådet lätt dubbdäcken har, tittade införa Ifall vi du därefter göteborg September vinter än vi snöröjningen Lika nästa fattas dubbdäck minska september Per bli på själv";
                    break;
                case "en":
                    loremWords = "Far far away, behind the word mountains, far from the countries Vokalia and Consonantia, there live the blind texts. Separated they live in Bookmarksgrove right at the coast of the Semantics, a large language ocean. A small river named Duden flows by their place and supplies it with the necessary regelialia. It is a paradisematic country, in which roasted parts of sentences fly into your mouth. Even the all-powerful Pointing has no control about the blind texts it is an almost unorthographic life One day however a small line of blind text by the name of Lorem Ipsum decided to leave for the far World of Grammar.";
                    break;
		        default:
                    loremWords = "Lorem ipsum dölör sit ämet Cönsectetur ådipisicing elit sed do Eiusmöd tempör incididunt ut låbore Et dölore mägnä äliqua åccumsån posuere etiåm Adipiscing mässa möllis risus äc mauris iåculis Habitåsse curåbitur tempör Nunc vitåe pulvinar feugiat cursus äugue purus Nunc änte habitåsse dönec cursus Nåm pellentesque fringilla nön å viverrä Erat eråt quåm volutpät mi purus Odio imperdiet a egestas ligulå Nisl håbitåsse nisi åt cönsectetur ligulä felis quam Cursus neque tempus rutrum eros Egestås gråvidå interdum sit fusce rhöncus söllicitudin congue Proin nunc sem åccumsån Felis quis integer pellentesque diäm Cönsequåt cömmodö eget hac";
                    break;
	        }    

            string[] wordArray = loremWords.Split(' ');

            for (int x = 1; x<=numberOfWordsToGet;x++)
            {
                returnWords.AppendFormat("{0} ", wordArray[random.Next(wordArray.Length)]);
            }

            return returnWords.ToString().Trim();
        }

        /// <summary>
        /// Parses the content from a XhtmlEditor and splits it on p tags
        /// </summary>
        /// <param name="xhtmlContent"></param>
        private MatchCollection ParseXhtmlString(string xhtmlContent)
        {
            String pattern = @"(?<=<p.*>).*(?=</p>)";
            return Regex.Matches(HttpUtility.HtmlDecode(xhtmlContent), pattern);
        }

        /// <summary>
        /// Returns a list of content from a matchcollection
        /// </summary>
        /// <param name="matches"></param>
        /// <returns></returns>
        private IList<string> Paragraphs(MatchCollection matches)
        {
            List<string> pgraphList = new List<string>();

            foreach (Match match in matches)
            {
                pgraphList.Add(match.Value);
            }

            return pgraphList;
        }

        #endregion
    }
}


