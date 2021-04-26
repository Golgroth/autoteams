using System.IO;
using System.Reflection;
using autoteams.Models;

namespace autoteams
{
    public static class ConfigurationManager
    {
        public const string FIELDS_CONF = "fields.json", USER_CREDS = "credentials.json", APP_CONF = "config.json", INFO_FILE = "README.md";
        public static UserCredentials CREDENTIALS { get; private set; }
        public static FieldsConfig FIELDS { get; private set; }
        public static AppConfiguration CONFIG { get; private set; }

        public static void LoadConfiguration()
        {
            //Extract and load the fields.json file
            Logger.Info("Loading definitions...");
            if (!File.Exists(FIELDS_CONF))
                ExtractFile(FIELDS_CONF);
            FIELDS = Utils.DeserializeJsonFile<FieldsConfig>(FIELDS_CONF);

            //Extract and load the credentials.json file
            Logger.Info("Loading credentials...");
            if (!File.Exists(USER_CREDS))
                ExtractFile(USER_CREDS);
            CREDENTIALS = Utils.DeserializeJsonFile<UserCredentials>(USER_CREDS);

            //Extract and load the config.json file
            Logger.Info("Loading application configuration...");
            if (!File.Exists(APP_CONF))
                ExtractFile(APP_CONF);
            CONFIG = Utils.DeserializeJsonFile<AppConfiguration>(APP_CONF);

            //Extract the readme file
            if (!File.Exists(INFO_FILE))
                ExtractFile(INFO_FILE);
        }

        private static void ExtractFile(string name)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"autoteams.{name}");
            if (stream == null)
            {
                throw new FileNotFoundException("Invalid resource name.");
            }

            FileStream fileStream = File.OpenWrite(name);
            stream.CopyTo(fileStream);

            fileStream.Flush();
            fileStream.Close();
            stream.Close();
        }
    }
}