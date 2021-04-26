using OpenQA.Selenium;

namespace autoteams
{
    public static class Selectors
    {
        public static By ByAttribute(string attributeName, string attributeValue) => By.XPath($"//*[@{attributeName}='{attributeValue}']");
        public static By ByTitle(string title) => ByAttribute("title", title);
    }
}