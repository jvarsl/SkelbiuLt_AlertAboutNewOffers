using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;

namespace SkelbiuLt_AlertAboutNewOffers
{
    class Program
    {
        public static Dictionary<string, string> ProductLinkDictionary = new Dictionary<string, string>()   {
            {"Logitech29","https://www.skelbiu.lt/skelbimai/?autocompleted=1&keywords=logitech+g29&submit_bn=&cities=465&distance=0&mainCity=1&search=1&category_id=0&type=0&user_type=0&ad_since_min=0&ad_since_max=0&visited_page=1&orderBy=1&detailsSearch=0" },
            {"Logitech920","https://www.skelbiu.lt/skelbimai/?autocompleted=1&keywords=logitech+g920&submit_bn=&cities=465&distance=0&mainCity=1&search=1&category_id=0&type=0&user_type=0&ad_since_min=0&ad_since_max=0&visited_page=1&orderBy=1&detailsSearch=0" },
            {"Logitech27","https://www.skelbiu.lt/skelbimai/?autocompleted=1&keywords=logitech+g27&submit_bn=&cities=465&distance=0&mainCity=1&search=1&category_id=0&type=0&user_type=0&ad_since_min=0&ad_since_max=0&visited_page=1&orderBy=1&detailsSearch=0" },
            {"Thrustmaster","https://www.skelbiu.lt/skelbimai/?autocompleted=1&keywords=thrustmaster&submit_bn=&cities=465&distance=0&mainCity=1&search=1&category_id=0&type=0&user_type=0&ad_since_min=0&ad_since_max=0&visited_page=1&orderBy=1&detailsSearch=0" }
        };

        public static Dictionary<string, int> ProductCountDictionary = new Dictionary<string, int>()   {
            {"Logitech29",0 },
            {"Logitech920",0 },
            {"Logitech27",0 },
            {"Thrustmaster",0 }
        };

        public static ConsoleColor OriginalConsoleColor = Console.BackgroundColor;
        static void Main(string[] args)
        {
            string regexPattern = @"Skelbim.:\s(\d+)";

            //infinite loop works until script is killed
            while (true)
            {
                ExtractData(regexPattern);
                Console.Write($"Ran on {DateTime.Now} - ");
                foreach (var item in ProductCountDictionary)
                {
                    Console.Write($"{item.Key}({item.Value}); ");
                }

                if (Console.BackgroundColor == ConsoleColor.Red) { Console.BackgroundColor = OriginalConsoleColor; }
                Console.WriteLine();
                Thread.Sleep(65000);
            }
        }

        static void ExtractData(string regexPattern)
        {
            using (var client = new WebClient())
            {
                bool newOfferFound = false;
                string emailContentLinks = "";
                foreach (var entry in ProductLinkDictionary)
                {
                    string htmlPageResponse = client.DownloadString(entry.Value);
                    int numberOfProductAdverts = ReturnNumberOfProductAdverts(htmlPageResponse, regexPattern);

                    int previousRecorderNumberOfProductAdverts = ProductCountDictionary[entry.Key];

                    //if increase is detected then it will inform the user
                    if (numberOfProductAdverts > previousRecorderNumberOfProductAdverts && previousRecorderNumberOfProductAdverts != 0)
                    {
                        newOfferFound = true;
                        emailContentLinks += $"{entry.Value}\n";
                    }
                    //update offer count
                    ProductCountDictionary[entry.Key] = numberOfProductAdverts;
                }
                //produce sound and send email to get phone notiflication
                if (newOfferFound)
                {
                    MakeCue(emailContentLinks);
                }
            }
        }

        static void MakeCue(string emailContentLinks)
        {
            //visual cue
            Console.BackgroundColor = ConsoleColor.Red;
            //audio cue
            Console.Beep(470, 1000);
            //email cue
            SendEmail(emailContentLinks);
        }

        //extracts number of available offers from html page string
        static int ReturnNumberOfProductAdverts(string htmlPage, string pattern)
        {
            var match = Regex.Match(htmlPage, pattern);
            var isNumber = int.TryParse(match.Groups[1].Value, out int result);
            return isNumber ? result : 0;
        }

        static void SendEmail(string emailContentLinks)
        {

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                UseDefaultCredentials = false,
                EnableSsl = true,
                Credentials = new NetworkCredential(ConfigurationManager.AppSettings.Get("EmailUserName"),
                                    ConfigurationManager.AppSettings.Get("EmailPassword"))
            };
            var mailMessage = new MailMessage()
            {
                From = new MailAddress(ConfigurationManager.AppSettings.Get("EmailUserName"))
            };
            mailMessage.To.Add(ConfigurationManager.AppSettings.Get("ReceiverEmail"));
            mailMessage.Subject = "Naujas skelbimas";
            mailMessage.Body = emailContentLinks;
            mailMessage.IsBodyHtml = true;
            client.Send(mailMessage);
        }
    }
}

