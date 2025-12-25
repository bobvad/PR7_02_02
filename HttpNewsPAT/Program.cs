using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WebRequest Request = WebRequest.Create("http://news.permaviat.ru/main.php");
            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            {
                Console.WriteLine(Response.StatusDescription);
                using (Stream DataStream = Response.GetResponseStream())
                {
                    using (StreamReader Reader = new StreamReader(DataStream))
                    {
                        string ResponseFromServer = Reader.ReadToEnd();
                        Console.WriteLine(ResponseFromServer);
                    }
                }
            }
            SingIn("user", "user");
            Console.Read();
        }
        public static void SingIn(string login, string password)
        {
            string url = "http://news.permaviat.ru/ajax/login.php";
            Debug.WriteLine($"Выполняем запрос: {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();

            string postData = $"login={login}&password={password}";
            byte[] Data = Encoding.ASCII.GetBytes(postData);
            request.ContentLength = Data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(Data, 0, Data.Length);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

            string responseFromServer = new StreamReader(response.GetResponseStream()).ReadToEnd();
            Console.WriteLine(responseFromServer);
        }
        public static void GetContent(Cookie Token)
        {
            string url = "http://news.permaviat.ru/main";
            Debug.WriteLine($"Выполняем запрос: {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Token);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

            string responseFromServer = new StreamReader(response.GetResponseStream()).ReadToEnd();
            Console.WriteLine(responseFromServer);
        }
        public static void ParsingHtml(string htmlCode)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            var Document = html.DocumentNode;

            IEnumerable<HtmlNode> DivsNews = Document.Descendants(0).Where(n => n.HasClass("news"));

            foreach (HtmlNode DivNews in DivsNews)
            {
                var src = DivNews.ChildNodes[1].GetAttributeValue("src", "none");
                var name = DivNews.ChildNodes[3].InnerText;
                var description = DivNews.ChildNodes[5].InnerText;

                Console.WriteLine(name + "\n" + "Изображение: " + src + "\n" + "Описание: " + description + "\n");
            }
        }
        public static void ParseDNS()
        {
            Console.WriteLine("Парсим DNS...\n");

            var doc = new HtmlWeb().Load("https://www.dns-shop.ru/catalog/17a8a01d16404e77/smartfony/");
            var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'catalog-product')]");

            if (items != null)
            {
                foreach (var item in items.Take(3))
                {
                    var img = item.SelectSingleNode(".//img")?.GetAttributeValue("src", "нет");
                    if (!string.IsNullOrEmpty(img) && img.StartsWith("/"))
                        img = "https://dns-shop.ru" + img;

                    var name = item.SelectSingleNode(".//a")?.InnerText?.Trim() ?? "???";

                    var price = item.SelectSingleNode(".//div[contains(@class, 'price')]")?.InnerText?.Trim() ?? "???";

                    Console.WriteLine($"Изображение: {img}");
                    Console.WriteLine($"Наименование: {name}");
                    Console.WriteLine($"Цена: {price}");
                    Console.WriteLine("---");
                }
            }
        }
    }
}
