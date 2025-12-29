using HtmlAgilityPack;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpNewsPAT
{
    internal class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _logFilePath = "trace_debug.log";
        private static readonly TraceSource _traceSource = new TraceSource("HttpNewsPAT");

        static async Task Main(string[] args)
        {
            // Настройка трассировки в файл
            SetupTracing();

            _traceSource.TraceEvent(TraceEventType.Start, 0, "Программа запущена");

            try
            {
                string token = await SingInHttpClientAsync("admin", "admin");

                if (string.IsNullOrWhiteSpace(token))
                {
                    Console.WriteLine("Не удалось авторизоваться.");
                    _traceSource.TraceEvent(TraceEventType.Error, 1, "Ошибка авторизации");
                    return;
                }

                _traceSource.TraceEvent(TraceEventType.Information, 2, $"Токен получен: {token.Substring(0, Math.Min(10, token.Length))}...");

                string htmlContents = await GetContentHttpClientAsync(token);
                ParsingHtml(htmlContents);

                Console.WriteLine("\n=== ДОБАВЛЕНИЕ НОВОЙ ЗАПИСИ ===");
                bool added = await AddNewsAsync(
                    token,
                    $"Новость от {DateTime.Now:dd.MM.yyyy HH:mm}",
                    "Эта новость была автоматически добавлена через консольное приложение на C# с использованием HttpClient и HtmlAgilityPack. " +
                    "Цель — демонстрация программного взаимодействия с веб-интерфейсом.",
                    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRnTQ04WdzI8_nx_D7_gGQK5nyjsunQOHNm5g&s"
                );

                _traceSource.TraceEvent(TraceEventType.Information, 3, $"Результат добавления новости: {added}");
                Console.WriteLine(added ? "Новость успешно добавлена" : "Ошибка при добавлении новости");

                Console.WriteLine("\n=== ОБНОВЛЕННЫЙ СПИСОК НОВОСТЕЙ ===");
                htmlContents = await GetContentHttpClientAsync(token);
                ParsingHtml(htmlContents);
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 4, $"Критическая ошибка: {ex.Message}");
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
            }
            finally
            {
                _traceSource.TraceEvent(TraceEventType.Stop, 5, "Программа завершена");
                _traceSource.Flush();
                _traceSource.Close();
            }

            Console.WriteLine($"\nЛог трассировки сохранен в: {Path.GetFullPath(_logFilePath)}");
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static void SetupTracing()
        {
            try
            {
                _traceSource.Listeners.Clear();

                var fileListener = new TextWriterTraceListener(_logFilePath, "FileListener")
                {
                    TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId | TraceOptions.ThreadId | TraceOptions.Timestamp
                };

                var consoleListener = new ConsoleTraceListener()
                {
                    Name = "ConsoleListener",
                    TraceOutputOptions = TraceOptions.DateTime
                };

                _traceSource.Listeners.Add(fileListener);
                _traceSource.Listeners.Add(consoleListener);

                _traceSource.Switch = new SourceSwitch("MainSwitch", "All");
                _traceSource.Switch.Level = SourceLevels.All;

                foreach (TraceListener listener in _traceSource.Listeners)
                {
                    if (listener is TextWriterTraceListener fileListenerCast)
                    {
                        fileListenerCast.WriteLine($"=== НАЧАЛО СЕССИИ ОТЛАДКИ {DateTime.Now:dd.MM.yyyy HH:mm:ss} ===");
                        fileListenerCast.WriteLine($"Процесс: {Process.GetCurrentProcess().ProcessName} (ID: {Process.GetCurrentProcess().Id})");
                        fileListenerCast.WriteLine($"Пользователь: {Environment.UserName}");
                        fileListenerCast.WriteLine($"Машина: {Environment.MachineName}");
                        fileListenerCast.WriteLine($"Платформа: {Environment.OSVersion}");
                        fileListenerCast.WriteLine(new string('=', 60));
                    }
                }

                _traceSource.TraceInformation("Система трассировки инициализирована");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка настройки трассировки: {ex.Message}");
            }
        }

        public static async Task<bool> AddNewsAsync(string token, string title, string content, string imageUrl)
        {
            _traceSource.TraceEvent(TraceEventType.Start, 100, "Метод AddNewsAsync начал выполнение");

            try
            {
                _traceSource.TraceInformation($"Параметры: Title='{title}', Content length={content?.Length}, ImageUrl='{imageUrl}'");

                string cleanImageUrl = imageUrl?.Replace(" ", "").Replace("\r", "").Replace("\n", "").Trim() ?? "";
                _traceSource.TraceInformation($"Очищенный URL изображения: {cleanImageUrl}");

                string body = $"src={Uri.EscapeDataString(cleanImageUrl)}" +
                              $"&name={Uri.EscapeDataString(title ?? "")}" +
                              $"&description={Uri.EscapeDataString(content ?? "")}";

                _traceSource.TraceInformation($"Тело запроса подготовлено, длина: {body.Length} символов");

                var request = new HttpRequestMessage(HttpMethod.Post, "http://news.permaviat.ru/ajax/add.php");
                request.Headers.Add("Cookie", $"token={token}");
                _traceSource.TraceInformation($"Заголовок Cookie установлен: token={token.Substring(0, Math.Min(10, token.Length))}...");

                request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
                request.Content.Headers.ContentType.CharSet = null;
                _traceSource.TraceInformation($"Контент запроса создан, тип: {request.Content.Headers.ContentType}");

                _traceSource.TraceEvent(TraceEventType.Information, 101, "Отправка POST запроса...");
                var stopwatch = Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(request);
                stopwatch.Stop();

                _traceSource.TraceInformation($"Время выполнения запроса: {stopwatch.ElapsedMilliseconds} мс");
                _traceSource.TraceInformation($"Статус ответа: {response.StatusCode}");

                string responseContent = await response.Content.ReadAsStringAsync();
                _traceSource.TraceInformation($"Ответ сервера: {responseContent}");

                if (!string.IsNullOrEmpty(responseContent))
                {
                    _traceSource.TraceData(TraceEventType.Verbose, 102, "Полный ответ сервера:", responseContent);
                }

                Console.WriteLine($"Статус: {response.StatusCode}");
                Console.WriteLine($"Ответ: {responseContent}");

                bool success = response.IsSuccessStatusCode;
                _traceSource.TraceEvent(success ? TraceEventType.Information : TraceEventType.Warning,
                    103, $"Результат операции: {success}");

                return success;
            }
            catch (HttpRequestException httpEx)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 104,
                    $"Ошибка HTTP запроса: {httpEx.Message}");
                Console.WriteLine($"Ошибка HTTP: {httpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 105,
                    $"Общая ошибка в AddNewsAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
            finally
            {
                _traceSource.TraceEvent(TraceEventType.Stop, 106, "Метод AddNewsAsync завершил выполнение");
            }
        }

        public static async Task<string> SingInHttpClientAsync(string login, string password)
        {
            _traceSource.TraceEvent(TraceEventType.Start, 200, "Метод SingInHttpClientAsync начал выполнение");

            try
            {
                _traceSource.TraceInformation($"Попытка авторизации с логином: {login}");

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("login", login),
                    new KeyValuePair<string, string>("password", password)
                });

                _traceSource.TraceInformation("Данные авторизации подготовлены");

                var stopwatch = Stopwatch.StartNew();
                var response = await _httpClient.PostAsync("http://news.permaviat.ru/ajax/login.php", content);
                stopwatch.Stop();

                _traceSource.TraceInformation($"Время авторизации: {stopwatch.ElapsedMilliseconds} мс");
                _traceSource.TraceInformation($"Статус ответа: {response.StatusCode}");

                string responseContent = await response.Content.ReadAsStringAsync();
                _traceSource.TraceInformation($"Содержимое ответа: {responseContent}");

                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var cookieString = cookies.FirstOrDefault();
                    _traceSource.TraceInformation($"Получены cookies: {cookieString}");

                    var token = cookieString
                        ?.Split(';')[0]
                        ?.Replace("token=", "");

                    if (!string.IsNullOrEmpty(token))
                    {
                        _traceSource.TraceEvent(TraceEventType.Information, 201,
                            $"Токен получен успешно. Длина: {token.Length}");
                        return token;
                    }
                }

                _traceSource.TraceEvent(TraceEventType.Warning, 202, "Токен не найден в cookies");
                return null;
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 203,
                    $"Ошибка входа: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Console.WriteLine($"Ошибка входа: {ex.Message}");
                return null;
            }
            finally
            {
                _traceSource.TraceEvent(TraceEventType.Stop, 204, "Метод SingInHttpClientAsync завершил выполнение");
            }
        }

        public static async Task<string> GetContentHttpClientAsync(string token)
        {
            _traceSource.TraceEvent(TraceEventType.Start, 300, "Метод GetContentHttpClientAsync начал выполнение");

            try
            {
                _traceSource.TraceInformation($"Используется токен: {token.Substring(0, Math.Min(10, token.Length))}...");

                var request = new HttpRequestMessage(HttpMethod.Get, "http://news.permaviat.ru/main");
                request.Headers.Add("Cookie", $"token={token}");

                _traceSource.TraceInformation("GET запрос подготовлен к отправке");

                var stopwatch = Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(request);
                stopwatch.Stop();

                _traceSource.TraceInformation($"Время получения контента: {stopwatch.ElapsedMilliseconds} мс");
                _traceSource.TraceInformation($"Статус ответа: {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();
                _traceSource.TraceInformation($"Получено HTML содержимое, длина: {content.Length} символов");

                if (content.Length < 1000)
                {
                    _traceSource.TraceData(TraceEventType.Verbose, 301, "Содержимое ответа:", content);
                }
                else
                {
                    _traceSource.TraceInformation($"Начало контента: {content.Substring(0, 500)}...");
                }

                return content;
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 302,
                    $"Ошибка получения: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Console.WriteLine($"Ошибка получения: {ex.Message}");
                return null;
            }
            finally
            {
                _traceSource.TraceEvent(TraceEventType.Stop, 303, "Метод GetContentHttpClientAsync завершил выполнение");
            }
        }

        public static void ParsingHtml(string htmlCode)
        {
            _traceSource.TraceEvent(TraceEventType.Start, 400, "Метод ParsingHtml начал выполнение");

            try
            {
                if (string.IsNullOrEmpty(htmlCode))
                {
                    _traceSource.TraceEvent(TraceEventType.Warning, 401, "Пустой HTML код для парсинга");
                    Console.WriteLine("Пустой HTML код для парсинга");
                    return;
                }

                _traceSource.TraceInformation($"Длина HTML кода: {htmlCode.Length} символов");

                var html = new HtmlDocument();
                html.LoadHtml(htmlCode);
                var Document = html.DocumentNode;

                var DivsNews = Document.Descendants("div")
                    .Where(n => n.HasClass("news"))
                    .ToList();

                int newsCount = DivsNews.Count;
                _traceSource.TraceInformation($"Найдено элементов с классом 'news': {newsCount}");
                Console.WriteLine($"\nНайдено новостей: {newsCount}");

                for (int i = 0; i < newsCount; i++)
                {
                    var DivNews = DivsNews[i];
                    _traceSource.TraceInformation($"Обработка новости #{i + 1}");

                    var src = DivNews.ChildNodes[1]?.GetAttributeValue("src", "none") ?? "none";
                    var name = DivNews.ChildNodes[3]?.InnerText?.Trim() ?? "без названия";
                    var description = DivNews.ChildNodes[5]?.InnerText?.Trim() ?? "без описания";

                    _traceSource.TraceInformation($"Новость #{i + 1}: Название='{name}', Src='{src}', Описание длина={description.Length}");

                    Console.WriteLine("\n======================");
                    Console.WriteLine($"Название: {name}");
                    Console.WriteLine($"Изображение: {src}");
                    Console.WriteLine($"Описание: {description}");
                }

                if (newsCount == 0)
                {
                    _traceSource.TraceEvent(TraceEventType.Warning, 402,
                        "Новости не найдены. Структура HTML документа:");
                    _traceSource.TraceData(TraceEventType.Verbose, 403, "HTML структура:",
                        string.Join("\n", Document.Descendants().Take(20).Select(n => $"{n.Name}: {n.GetClasses().FirstOrDefault()}")));
                }
            }
            catch (Exception ex)
            {
                _traceSource.TraceEvent(TraceEventType.Error, 404,
                    $"Ошибка парсинга: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Console.WriteLine($"Ошибка парсинга: {ex.Message}");
            }
            finally
            {
                _traceSource.TraceEvent(TraceEventType.Stop, 405, "Метод ParsingHtml завершил выполнение");
            }
        }
    }
}