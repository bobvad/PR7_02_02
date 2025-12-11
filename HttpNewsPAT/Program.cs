using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
            SingIn("student","Asdfg123");
            Console.Read();
        }
        public static void SingIn(string Login, string Password)
        {
            string url = "http://news.permaviat.ru/ajax/login.php";
            Debug.WriteLine($"Выполняем запрос: {url}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();

            string postData = $"Login={Login}&password={Password}";
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
    }
}
