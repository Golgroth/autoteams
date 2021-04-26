using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using autoteams.Models;

namespace autoteams
{
    public class Program
    {
        private static TeamsController teamsController;
        private const string VERSION = "1.0.0";
        public static void Main(string[] args)
        {
            if (args.Contains("-h") || args.Contains("--help"))
            {
                Console.WriteLine($"autoteams v{VERSION}");
                Console.WriteLine("Usage: autoteams [options]\n");
                Console.WriteLine("Options:\n"
                + " -h, --help\t- print this message\n"
                + " -v, --verbose\t- output debug logs\n"
                + " -u, --update\t- discover classes and synchronize the database only\n"
                + " -c, --console\t- use an in-built command line interface\n");
                Console.WriteLine("Internal commands (only with -c):\n"
                + " exit\t\t- terminates the application\n"
                + " db\t\t- reloads data from the database\n"
                + " switch <c>\t- switches to the specified channel\n"
                + " join <c>\t- joins a meeting in specified channel\n");
                Console.WriteLine("Argument types:\n"
                + " <c>\t\t - channel locator (<teamName>:<channelName>)");
                return;
            }

            ConfigurationManager.LoadConfiguration();

            if (args.Contains("-v") || args.Contains("--verbose"))
                ConfigurationManager.CONFIG.OutputDebug = true;

            using var db = new StorageContext();
            bool isNewlyCreated = db.Database.EnsureCreated();

            if (isNewlyCreated)
                Logger.Info("Created new database.");

            teamsController = new TeamsController(ConfigurationManager.CONFIG.AllowMicrophone,
                                                  ConfigurationManager.CONFIG.AllowWebcam,
                                                  ConfigurationManager.CONFIG.HeadlessMode);

            Console.CancelKeyPress += (_, _) => Exit();

            teamsController.Login().Wait();
            teamsController.DiscoverClasses();

            if (args.Contains("-u") || args.Contains("--update"))
                return;


            teamsController.MeetingScheduler.Initialize();

            if (args.Contains("-c") || args.Contains("--console"))
                while (true)
                {
                    try
                    {
                        string command = Console.ReadLine();

                        if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        {
                            Exit();
                            return;
                        }
                        else if (command.Equals("leave", StringComparison.OrdinalIgnoreCase))
                        {
                            if (teamsController.LeaveMeeting())
                                Logger.Debug("Left meeting...");
                            else
                                Logger.Error("Cannot leave meeting (no meeting to leave).");
                        }
                        else if (command.Equals("db", StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.Info("Reloading data from DB.");
                            teamsController.MeetingScheduler.LoadMeetingsFromDb();
                        }
                        else if (command.StartsWith("join", StringComparison.OrdinalIgnoreCase) || command.StartsWith("switch", StringComparison.OrdinalIgnoreCase))
                        {
                            string[] locator = command[5..].Split(':');
                            TeamsChannel channel = teamsController.Classes.Single(c => c.Name == locator[0]).Channels.Single(c => c.Name == locator[1]);

                            if (command.StartsWith("join", StringComparison.OrdinalIgnoreCase))
                                teamsController.JoinMeeting(channel);
                            else
                                teamsController.SwitchChannel(channel);

                        }
                    }
                    catch (Exception ee)
                    {
                        Logger.Error(ee.ToString());
                        continue;
                    }
                }
            else
                Task.Delay(Timeout.Infinite).Wait();
        }

        public static void Exit()
        {
            teamsController.Dispose();
            Logger.Info("Terminating the program...");
            Environment.Exit(0);
        }
    }
}
