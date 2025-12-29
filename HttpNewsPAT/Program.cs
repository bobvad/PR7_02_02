using HtmlAgilityPack;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using HttpNewsPAT.Classes;
using System.Text.Json;

namespace HttpNewsPAT
{
    internal class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _logFilePath = "trace_debug.log";
        private static readonly TraceSource _traceSource = new TraceSource("HttpNewsPAT");

        static async Task Main(string[] args)
        {
            SetupTracing();

            try
            {
                string token = await SingInHttpClientAsync("admin", "admin");
                if (string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine("Не удалось авторизоваться.");
                    return;
                }

                string html = await GetContentHttpClientAsync(token);
                ParsingHtml(html);

                bool added = await AddNewsAsync(
                    token,
                    $"Новость от {DateTime.Now:dd.MM.yyyy HH:mm}",
                    "Добавлено через консольное приложение на C#.",
                    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRnTQ04WdzI8_nx_D7_gGQK5nyjsunQOHNm5g&s"
                );

                Console.WriteLine(added ? "Новость добавлена" : "Ошибка");

                html = await GetContentHttpClientAsync(token);
                ParsingHtml(html);

                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine(" НАЧИНАЕМ ПАРСИНГ  (ШАГ 16)");
                Console.WriteLine(new string('=', 60));
                ParseQuotesAsync();


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine($"\nЛог сохранен в: {_logFilePath}");
            Console.ReadLine();
        }

        public static async Task ParseQuotesAsync()
        {
            var client = new HttpClient();
            string html = await client.GetStringAsync("https://quotes.toscrape.com");
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var quotes = doc.DocumentNode.SelectNodes("//div[@class='quote']");
            Console.WriteLine("\n Цитаты с quotes.toscrape.com:\n");

            foreach (var q in quotes?.Take(3))
            {
                string text = q.SelectSingleNode(".//span[@class='text']")?.InnerText ?? "";
                string author = q.SelectSingleNode(".//small[@class='author']")?.InnerText ?? "";
                Console.WriteLine($"«{text.Replace("“", "").Replace("”", "")}» — {author}\n");
            }
        }
        private static void SetupTracing()
        {
            try
            {
                _traceSource.Listeners.Clear();
                _traceSource.Listeners.Add(new TextWriterTraceListener(_logFilePath)
                {
                    TraceOutputOptions = TraceOptions.DateTime
                });
                _traceSource.Switch = new SourceSwitch("MainSwitch") { Level = SourceLevels.All };
            }
            catch { }
        }

        public static async Task<bool> AddNewsAsync(string token, string title, string content, string imageUrl)
        {
            try
            {
                string body = $"src={Uri.EscapeDataString(imageUrl)}" +
                              $"&name={Uri.EscapeDataString(title)}" +
                              $"&description={Uri.EscapeDataString(content)}";

                var request = new HttpRequestMessage(HttpMethod.Post, "http://news.permaviat.ru/ajax/add.php");
                request.Headers.Add("Cookie", $"token={token}");
                request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await _httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Статус: {response.StatusCode}");
                Console.WriteLine($"Ответ: {responseContent}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }

        public static async Task<string> SingInHttpClientAsync(string login, string password)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("login", login),
                    new System.Collections.Generic.KeyValuePair<string, string>("password", password)
                });

                var response = await _httpClient.PostAsync("http://news.permaviat.ru/ajax/login.php", content);

                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    return cookies.FirstOrDefault(c => c.StartsWith("token="))
                        ?.Split(';')[0]
                        ?.Replace("token=", "");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка входа: {ex.Message}");
                return null;
            }
        }

        public static async Task<string> GetContentHttpClientAsync(string token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://news.permaviat.ru/main");
                request.Headers.Add("Cookie", $"token={token}");
                var response = await _httpClient.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения: {ex.Message}");
                return null;
            }
        }

        public static void ParsingHtml(string htmlCode)
        {
            try
            {
                var html = new HtmlDocument();
                html.LoadHtml(htmlCode);

                var newsDivs = html.DocumentNode.Descendants("div")
                    .Where(n => n.HasClass("news"))
                    .ToList();

                Console.WriteLine($"\nНайдено новостей: {newsDivs.Count}");

                foreach (var news in newsDivs)
                {
                    var src = news.ChildNodes[1]?.GetAttributeValue("src", "none") ?? "none";
                    var name = news.ChildNodes[3]?.InnerText?.Trim() ?? "без названия";
                    var description = news.ChildNodes[5]?.InnerText?.Trim() ?? "без описания";

                    Console.WriteLine("\n======================");
                    Console.WriteLine($"Название: {name}");
                    Console.WriteLine($"Изображение: {src}");
                    Console.WriteLine($"Описание: {description}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
            }
        }
    }
}