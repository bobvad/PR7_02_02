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
                ParseRiaNewsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine($"\nЛог сохранен в: {_logFilePath}");
            Console.ReadLine();
        }
        public static async Task ParseRiaNewsAsync()
        {
            try
            {
                _traceSource.TraceEvent(TraceEventType.Information, 1, "Начинаю парсинг RIA.RU через RSS");
                Console.WriteLine("\n=== Парсинг RIA.RU (через RSS) ===");

                var response = await _httpClient.GetAsync("https://ria.ru/export/rss2/archive/index.xml");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Ошибка загрузки RSS RIA.RU: {response.StatusCode}");
                    return;
                }

                var xml = await response.Content.ReadAsStringAsync();
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(xml);

                var items = xmlDoc.SelectNodes("//item");
                if (items == null || items.Count == 0)
                {
                    Console.WriteLine("RSS: Новости не найдены");
                    return;
                }

                Console.WriteLine($"Найдено новостей в RSS: {items.Count}");

                for (int i = 0; i < Math.Min(5, items.Count); i++)
                {
                    var item = items[i];
                    string title = item["title"]?.InnerText?.Trim() ?? "Без заголовка";
                    string link = item["link"]?.InnerText?.Trim() ?? "Без ссылки";
                    string pubDate = item["pubDate"]?.InnerText?.Trim() ?? "";

                    Console.WriteLine("\n------------------");
                    Console.WriteLine($"Заголовок: {title}");
                    Console.WriteLine($"Ссылка: {link}");
                    if (!string.IsNullOrEmpty(pubDate))
                        Console.WriteLine($"Дата: {pubDate}");
                }

                _traceSource.TraceEvent(TraceEventType.Information, 5, $"Успешно получено {Math.Min(5, items.Count)} новостей из RSS RIA.RU");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге RSS RIA.RU: {ex.Message}");
                _traceSource.TraceEvent(TraceEventType.Error, 6, $"Ошибка RSS RIA.RU: {ex}");
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