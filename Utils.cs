using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace autoteams
{
    public static class Utils
    {

        public static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            AllowTrailingCommas = true,
            IgnoreReadOnlyProperties = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static T DeserializeJsonFile<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), JSON_OPTIONS);

        public static void SerializeToJsonFile(string path, object o) => File.WriteAllText(path, JsonSerializer.Serialize(o, JSON_OPTIONS));

        public static object ExecuteScript(this IWebDriver driver, string script, params object[] args) => ((IJavaScriptExecutor)driver).ExecuteScript(script, args);

        public static object ExecuteScriptAsync(this IWebDriver driver, string script, params object[] args) => ((IJavaScriptExecutor)driver).ExecuteAsyncScript(script, args);


        public static IWebElement Parent(this IWebElement element) => element.FindElement(By.XPath(".."));
        public static IWebElement Parent(this IWebElement element, int noOfParents) => element.FindElement(By.XPath(string.Join('/', Enumerable.Repeat("..", noOfParents))));


        public static void ScrollIntoView(this IWebDriver driver, IWebElement element) => driver.ExecuteScript("arguments[0].scrollIntoView(true);", element);
        public static void ScrollIntoView(this IWebElement element, IWebDriver driver) => ScrollIntoView(driver, element);

        public static string GetId(this IWebElement element) => element.GetAttribute("id");

        public static bool TryTo(this IWebDriver _, TimeSpan timeout, Func<bool> action, int intervalMilis = 500, int initialDelay = 0)
        {
            if (intervalMilis > timeout.TotalMilliseconds)
            {
                throw new ArgumentException("Interval cannot be larger than timeout.");
            }

            if (intervalMilis < 0)
            {
                throw new ArgumentException("Interval cannot be negative.");
            }
            if (initialDelay < 0)
            {
                throw new ArgumentException("Initial delay cannot be negative.");
            }

            if (initialDelay > 0)
                Thread.Sleep(initialDelay);

            using CancellationTokenSource src = new();

            //cancel the task after the timeout is reached
            src.CancelAfter(timeout);

            return Task.Run(async () =>
            {
                int start = Environment.TickCount;
            Trial:
                try
                {
                    src.Token.ThrowIfCancellationRequested();
                    return action.Invoke();
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (Exception)
                {
                    await Task.Delay(intervalMilis);
                    goto Trial;
                }
            }, src.Token).Result;
        }
    }
}