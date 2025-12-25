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
using HttpNewsPAT.Classes;

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
        public static void ParseDNSAdvanced()
        {
            Console.WriteLine("Расширенный парсинг DNS...\n");

            var web = new HtmlWeb();
            web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

            var doc = web.Load("https://www.dns-shop.ru/catalog/17a8a01d16404e77/smartfony/");

            var productCards = doc.DocumentNode.SelectNodes("//div[contains(@class, 'catalog-product')]");

            if (productCards == null || !productCards.Any())
            {
                Console.WriteLine("Карточки товаров не найдены. Пробуем найти по-другому...");

                productCards = doc.DocumentNode.SelectNodes("//div[@data-id='product']") ??
                              doc.DocumentNode.SelectNodes("//a[contains(@class, 'catalog-product__name')]/../..");
            }

            if (productCards != null)
            {
                var products = new List<ProductInfo>();

                foreach (var card in productCards.Take(10))
                {
                    var product = new ProductInfo();

                    var nameNode = card.SelectSingleNode(".//a[contains(@class, 'catalog-product__name')]");
                    product.Name = nameNode?.InnerText?.Trim() ?? "Неизвестно";

                    product.ProductUrl = nameNode?.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(product.ProductUrl) && product.ProductUrl.StartsWith("/"))
                    {
                        product.ProductUrl = "https://www.dns-shop.ru" + product.ProductUrl;
                    }

                    var priceNode = card.SelectSingleNode(".//div[contains(@class, 'product-buy__price')]");
                    product.Price = priceNode?.InnerText?.Trim() ?? "Нет цены";

                    var imgNode = card.SelectSingleNode(".//img[contains(@class, 'catalog-product__image')]");
                    product.ImageUrl = imgNode?.GetAttributeValue("src", "") ??
                                      imgNode?.GetAttributeValue("data-src", "");

                    if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl.StartsWith("/"))
                    {
                        product.ImageUrl = "https://www.dns-shop.ru" + product.ImageUrl;
                    }

                    var ratingNode = card.SelectSingleNode(".//div[contains(@class, 'catalog-product__rating')]");
                    product.Rating = ratingNode?.InnerText?.Trim() ?? "Нет рейтинга";

                    products.Add(product);
                }

                foreach (var product in products)
                {
                    Console.WriteLine($"Название: {product.Name}");
                    Console.WriteLine($"Цена: {product.Price}");
                    Console.WriteLine($"Рейтинг: {product.Rating}");
                    Console.WriteLine($"Ссылка: {product.ProductUrl}");
                    Console.WriteLine($"Изображение: {product.ImageUrl}");
                    Console.WriteLine(new string('=', 50));
                }
            }
        }
    }
}
