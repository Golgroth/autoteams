using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using autoteams.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using static autoteams.ConfigurationManager;

namespace autoteams
{
    public class TeamsController : IDisposable
    {
        private readonly IWebDriver _driver;
        private int loginTime;
        private readonly bool _microphoneAllowed, _cameraAllowed;


        private readonly MeetingScheduler _scheduler;
        public MeetingScheduler MeetingScheduler => _scheduler;

        private TeamsChannel _currentChannel, _currentMeetingChannel;

        public TeamsChannel CurrentChannel
        {
            get => _currentChannel;
            set
            {
                SwitchChannel(value);
            }
        }

        public TeamsChannel CurrentMeetingChannel
        {
            get => _currentMeetingChannel;
        }

        //For login prompts
        private IWebElement NextButtonElement => _driver.FindElement(By.Id(FIELDS.LoginProceedButtonId));

        public HashSet<TeamsClassroom> Classes { get; private init; }
        public HashSet<TeamsChannel> Channels { get; private init; }

        public TeamsController(bool enableMicrophone = true, bool enableWebcam = true, bool headless = false)
        {
            Logger.Info("Creating browser instance...");

            ChromeOptions options = new();

            options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--start-maximized");
            options.AddArgument("start-maximized");

            if (headless)
                options.AddArgument("headless");

            options.AddUserProfilePreference("profile.default_content_setting_values.geolocation", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.media_stream_mic", enableMicrophone ? 1 : 2);
            options.AddUserProfilePreference("profile.default_content_setting_values.media_stream_camera", enableWebcam ? 1 : 2);

            _microphoneAllowed = enableMicrophone;
            _cameraAllowed = enableWebcam;

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(CONFIG.SearchWaitTime);

            Classes = new();
            Channels = new();
            _scheduler = new(this);
        }

        public async Task Login()
        {
            int startTimestamp = Environment.TickCount;

            Logger.Info("Logging in...");

        Trial:
            try
            {

                _driver.Navigate().GoToUrl(FIELDS.MsTeamsMainUrl);

                _driver.TryTo(TimeSpan.FromSeconds(3), () =>
                {
                    var fmt = _driver.FindElement(By.Name(FIELDS.LoginEmailFieldName));
                    fmt.SendKeys(CREDENTIALS.Login);
                    NextButtonElement.Click();
                    return true;
                }, initialDelay: 200);


                //Wait ~700 ms for the transition between the login and password
                await Task.Delay(CONFIG.LoginToPasswordWaitTimeMilis);

                _driver.TryTo(TimeSpan.FromSeconds(3), () =>
                {
                    var pwd = _driver.FindElement(By.Name(FIELDS.LoginPasswordFieldName));

                    //Send the Base-64 decoded password
                    pwd.SendKeys(Encoding.UTF8.GetString(Convert.FromBase64String(CREDENTIALS.Password)));
                    NextButtonElement.Click();
                    return true;
                });

                //Check if MS login page stalls at 'Stay signed in checkbox' and click the 'Yes' button
                if (_driver.Url.Equals(FIELDS.MsTeamsLoginCheckStallUrl, StringComparison.Ordinal))
                {
                    NextButtonElement.Click();
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Encountered an error during login attempt:");
                Logger.Warn(e.ToString());
                Logger.Info("Attempting to login again.");
                goto Trial;
            }

        Postlogin:

            await Task.Delay(1000);

            ushort cnt = 1;
            Logger.EnterUpdateMode();
            while (!_driver.Url.StartsWith(FIELDS.MsTeamsMainSchoolUrl, StringComparison.InvariantCulture))
            {
                if (cnt > CONFIG.MaxLoadAttemptsForRefresh)
                {
                    Logger.ExitUpdateMode();
                    Logger.Warn("Cannot load site. Reloading...");
                    _driver.Navigate().Refresh();
                    goto Postlogin;
                }

                Logger.Info($"Not loaded yet. Waiting... {cnt}/{CONFIG.MaxLoadAttemptsForRefresh}");

                await Task.Delay(CONFIG.PageLoadedCheckIntervalMilis);
                cnt++;
            }

            Logger.ExitUpdateMode();

            int endTimestamp = Environment.TickCount;
            loginTime = endTimestamp - startTimestamp;

            Logger.Info($"Logged in. Time: {loginTime}ms");
        }

        public void DiscoverClasses()
        {
            int startTimestamp = Environment.TickCount;
            Logger.Info("Discovering classes...");

            //Click the team list button in case the list is not shown
            _driver.FindElement(By.Id(FIELDS.MenuTeamListButtonId)).Click();

            IEnumerable<IWebElement> elements = _driver.FindElements(By.CssSelector("li.team"));

            foreach (IWebElement classElement in elements)
            {
                //Scroll the element into view if not visible
                if (!classElement.Displayed)
                    classElement.ScrollIntoView(_driver);

                //The team name is also in the 'title' attribute of the profile picture
                string name = classElement.FindElement(By.XPath(FIELDS.MenuTeamProfilePictureXPath)).GetAttribute("title");

                Logger.Debug($"Found class: {name}");

                TeamsClassroom room = new()
                {
                    Name = name,
                    Element = classElement.Parent(),
                    Channels = new HashSet<TeamsChannel>(),
                };

                //Expand the class if it is not already expanded in order to show channels
                //The channel list div is removed in the DOM if the class is not expanded
                if (classElement.GetAttribute("aria-expanded").Equals("false", StringComparison.Ordinal))
                    classElement.Click();

                var channelList = classElement.FindElements(By.XPath(FIELDS.MenuTeamChannelListXPath));

                foreach (var channelElement in channelList)
                {
                    string channelName = channelElement.FindElement(By.XPath(FIELDS.MenuTeamChannelNameXPath)).Text;
                    string channelId = channelElement.Parent().GetId()[8..]; //Remove 'channel-' prefix

                    TeamsChannel channel = new()
                    {
                        Name = channelName,
                        Classroom = room,
                        ClassroomName = room.Name,
                        ChannelId = channelId
                    };

                    _ = Channels.Add(channel);

                    room.Channels.Add(channel);

                    Logger.Debug($"  -> channel: {channelName}");
                }

                _ = Classes.Add(room);
            }

            int deltaT = Environment.TickCount - startTimestamp;
            Logger.Info($"Classes indexed. Time: {deltaT}ms.");

            LoadDatabase();

            //Set the current channel which is always the first channel
            if (_currentChannel == null)
            {
                SwitchChannel(Channels.First());
            }
        }

        public void LoadDatabase()
        {
            using var db = new StorageContext();

            IEnumerable<TeamsChannel> dbChannels = db.Channels.AsEnumerable();
            IEnumerable<TeamsClassroom> dbRooms = db.Classrooms.AsEnumerable();

            IEnumerable<TeamsChannel> channelDifference = Channels.Except(dbChannels);
            IEnumerable<TeamsClassroom> roomDifference = Classes.Except(dbRooms);

            if (channelDifference.Any() || roomDifference.Any())
            {
                Logger.Info("Database desynchronized.");
                Logger.Info($"Changing {roomDifference.Count()} classrooms and {channelDifference.Count()} channels.");

                //Create a shallow copies of the instances to prevent System.InvalidOperationException
                db.Classrooms.AddRange(roomDifference.Select(s =>
                    new TeamsClassroom()
                    {
                        Name = s.Name,
                    }
                ));

                db.Channels.AddRange(channelDifference.Select(s =>
                    new TeamsChannel()
                    {
                        Name = s.Name,
                        ClassroomName = s.ClassroomName,
                        ChannelId = s.ChannelId
                    }
                ));

                int total = db.SaveChanges();
                Logger.Info($"Changed {total} records.");
            }
        }

        public void SwitchChannel(TeamsChannel channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            //Removed interaction with the web elements in favour of direct navigation
            Logger.Debug($"Switching to {channel.ClassroomName}:{channel.Name}");
            _driver.Navigate().GoToUrl(channel.GetUrl());
            _currentChannel = channel;
        }

        public bool JoinCurrentChannelMeeting()
        {
            Logger.Debug($"Joining meeting ({_currentChannel.ClassroomName}:{_currentChannel.Name})...");
            _driver.Navigate().Refresh();

            return _driver.TryTo(TimeSpan.FromSeconds(5), () =>
            {
                //Get the join button in the channel
                IWebElement element = _driver.FindElement(By.TagName("calling-join-button")).FindElement(By.XPath("./button"));
                element.ScrollIntoView(_driver);
                element.Click();

                //If no microphone present, click the prompt
                if (!_microphoneAllowed)
                    _driver.TryTo(TimeSpan.FromSeconds(5), () =>
                    {
                        try
                        {
                            IWebElement noMicPromptElement = _driver.FindElement(By.XPath(FIELDS.MeetingNoMicPromptButtonXPath));
                            noMicPromptElement.Click();
                            return true;
                        }
                        catch (NoSuchElementException)
                        {
                            return true;
                        }
                    }, 500, 300);

                //Try to join the meeting using the secondary button
                return _driver.TryTo(TimeSpan.FromSeconds(5), () =>
                 {
                     _driver.FindElement(By.ClassName("join-btn")).Click();
                     _currentMeetingChannel = _currentChannel;
                     return true;
                 }, 500, 300);
            });
        }

        public bool JoinMeeting(TeamsChannel channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            SwitchChannel(channel);
            return JoinCurrentChannelMeeting();
        }

        public bool LeaveMeeting()
        {
            Logger.Debug($"Leaving current meeting ({_currentMeetingChannel.ClassroomName}:{_currentMeetingChannel.Name})...");

            //Go to the main channel view and click the hang-up button
            _driver.Navigate().GoToUrl(_currentChannel.GetUrl());

            //Try to click the button
            return _driver.TryTo(TimeSpan.FromSeconds(5), () =>
            {
                _driver.FindElement(By.Id("hangup-button")).Click();
                _currentMeetingChannel = null;
                return true;
            });
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _driver.Dispose();
        }
    }
}