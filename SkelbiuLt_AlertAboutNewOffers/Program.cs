using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkelbiuLt_AlertAboutNewOffers
{
    class Program
    {
        public static Dictionary<string, string> ProductLinkDictionary = new Dictionary<string, string>()
        {
            {"Logitech29", "https://www.skelbiu.lt/skelbimai/?autocompleted=1&keywords=logitech+g29&submit_bn=&cities=465&distance=0&mainCity=1&search=1&category_id=0&type=0&user_type=0&ad_since_min=0&ad_since_max=0&visited_page=1&orderBy=1&detailsSearch=0" },
            {"Logitech920", "https://www.skelbiu.lt/skelbimai/?autocompleted=1&keywords=logitech+g920&submit_bn=&cities=465&distance=0&mainCity=1&search=1&category_id=0&type=0&user_type=0&ad_since_min=0&ad_since_max=0&visited_page=1&orderBy=1&detailsSearch=0" },
            {"Logitech27", "https://www.skelbiu.lt/skelbimai/?autocompleted=1&keywords=logitech+g27&submit_bn=&cities=465&distance=0&mainCity=1&search=1&category_id=0&type=0&user_type=0&ad_since_min=0&ad_since_max=0&visited_page=1&orderBy=1&detailsSearch=0" },
            {"Thrustmaster", "https://www.skelbiu.lt/skelbimai/?autocompleted=1&keywords=thrustmaster&submit_bn=&cities=465&distance=0&mainCity=1&search=1&category_id=0&type=0&user_type=0&ad_since_min=0&ad_since_max=0&visited_page=1&orderBy=1&detailsSearch=0" }
        };

        public static Dictionary<string, int> ProductCountDictionary = new Dictionary<string, int>();

        public static ConsoleColor OriginalConsoleColor = Console.BackgroundColor;
        public static bool NewOfferFound = false;
        public static string MailContentLinks = string.Empty;

        static async Task Main()
        {
            ProductCountDictionary = ProductLinkDictionary.ToDictionary(entry => entry.Key, entry => 0);

            //infinite loop works until console is closed
            while (true)
            {
                await ExtractDataAsync();
                if (NewOfferFound) { MakeCuesAsync(MailContentLinks).ConfigureAwait(false); }

                Console.WriteLine(ReturnCurrentProductCount());

                ResetReportingValues();
                Thread.Sleep(65000);
            }
        }

        static void ResetReportingValues()
        {
            if (NewOfferFound)
            {
                NewOfferFound = false;
                MailContentLinks = string.Empty;
                Console.BackgroundColor = OriginalConsoleColor;
            }
        }

        static string ReturnCurrentProductCount()
        {
            string consoleOutput = $"Ran on {DateTime.Now} - ";
            foreach (var item in ProductCountDictionary)
            {
                consoleOutput += $"{item.Key}({item.Value}); ";
            }
            return consoleOutput;
        }

        static async Task ExtractDataAsync()
        {
            string regexPattern = @"Skelbim.:\s(\d+)";
            //@"<div id=\\""adsNumberFilterBar\\"">.*?(\d+).*?<\/div>"; // doesn't work for some reason but works when html is read from txt file

            using (var client = new HttpClient())
            {
                foreach (var entry in ProductLinkDictionary)
                {
                    string htmlPageResponse = await client.GetStringAsync(entry.Value);

                    int numberOfProductAdverts = ReturnNumberOfProductAdverts(htmlPageResponse, regexPattern);

                    CheckProductCountChange(entry, numberOfProductAdverts);
                }
            }
        }

        static void CheckProductCountChange(KeyValuePair<string, string> product, int numberOfProductAdverts)
        {
            int previousRecorderNumberOfProductAdverts = ProductCountDictionary[product.Key];

            //if increase is detected then it will inform the user
            if (numberOfProductAdverts > previousRecorderNumberOfProductAdverts && previousRecorderNumberOfProductAdverts != 0)
            {
                NewOfferFound = true;
                MailContentLinks += $"<a href='{product.Value}'>{product.Key}</a><br>";
            }
            //update product count
            ProductCountDictionary[product.Key] = numberOfProductAdverts;
        }

        static async Task MakeCuesAsync(string emailContentLinks) // visual, audio, email
        {
            Console.BackgroundColor = ConsoleColor.Red;
            WindowsFunction.BringConsoleToFront();

            new Thread(() => Console.Beep(470, 1000)).Start();

            await SendMailAsync(emailContentLinks);
        }

        //extracts number of available offers from html page string
        static int ReturnNumberOfProductAdverts(string htmlPage, string pattern)
        {
            var match = Regex.Match(htmlPage, pattern);
            var isNumber = int.TryParse(match.Groups[1].Value, out int result);
            return isNumber ? result : 0;
        }

        static async Task SendMailAsync(string emailContentLinks)
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
            await client.SendMailAsync(mailMessage);
        }
    }
}
