# Features

## Parameters

The application can be run without any parameters (default), although it can accept the following parameters:

- -h, --help - this parameter will cause the program to print help information and terminate afterwards
- -v, --verbose - this option will force the program to output debug-level logs (overrides **outputDebug** in _config.json_)
- -u, --update - this option will cause the program to synchronize the MS Teams data with the database and terminate afterwards
- -c, --console - this parameter will enable an in-built command line interface, usually used for debugging

## The console

The in-built CLI allows the user to input the following commands:

- `exit` - terminates the program
- `db` - reloads data from the database (can be used to update meeting data)
- `switch <channel>` - switches to the specified channel
- `join <channel>` - joins a meeting in the specified channel

The argument `<channel>` has the following format: _`<teamName>:<channelName>`_ and is used to designate channels.

# Configuration

Field structure:
`<fieldName>(<defaultValue>)`

## config.json

This file contains the general configuration for the application.

Fields:

- `headlessMode(false)` - if set to true, the web browser will run in headless mode (will not be visible). May increase performance.
- `allowMicrophone(true)` - if set to true, will automatically allow microphone access for the MS Teams web application
- `allowWebcam(true)` - if set to true, will automatically allow video device access for the MS Teams web application
- `searchWaitTime(5)` - the time (in seconds) that a program will wait to find an element without timing out
- `loginToPasswordWaitTimeMilis(700)` - the time (in milliseconds) that the program will wait for the transition from the email input to the password input in the login prompt of Microsoft Account Services. Increase, if experiencing errors during login phase.
- `maxLoadAttemptsForRefresh(5)` - the maximum number of checks for the web application loaded state after login before refreshing it.
- `pageLoadedCheckIntervalMilis(4000)` - the time (in milliseconds) that the program will wait between each web application loaded state check
- `outputDebug(false)` - if set to true, the program will output information with log level `DEBUG`
- `schedulerSyncOnSecond(10)` - the second when the scheduler will periodically run checks on. The scheduler runs every minute, scheduled on the specified second.

## credentials.json

This file contains the user credentials for the MS Teams web application.

Fields:

- `login` - the email of the MS Teams user
- `password` - the password of the MS Teams user (in Base64, **UTF-8 encoded**)

## fields.json

This file contains field definitions, usually element locators, for interaction with the DOM of MS Teams web application.
If a locator changes, it can be updated in this file.

Fields:

- `loginProceedButtonId` - the id of the `Next` buttons in the login prompt
- `loginEmailFieldName` - the `name` attribute of the email input field in the login prompt
- `loginPasswordFieldName` - the `name` attribute of the password input field in the login prompt
- `msTeamsMainUrl` - the main URL of the web application
- `msTeamsMainSchoolUrl` - the URL of the web application following the initialization phase
- `msTeamsLoginCheckStallUrl` - the URL of the web application containing the `Stay signed in` prompt
- `menuTeamListButtonId` - the id of the team list button
- `menuTeamProfilePictureXPath` - a relative XPath from the `li` team element to the `profile-picture` element of the team
- `menuTeamChannelListXPath` - a relative XPath from the `li` team element to each channel `a` element
- `menuTeamChannelNameXPath` - a relative XPath from the `a` channel element to each channel name `span` element
- `meetingNoMicPromptButtonXPath` - an absolute XPath to the `Continue without audio or video` button element (used when `allowMicrophone` is set to `false`)

# Data storage

All data is stored in an Sqlite database (**storage.db**), managed by Entity Framework Core.
The data is divided into three main tables: **Classrooms**, **Channels** and **Meetings**.
If the database is not present during program initialization, it will be automatically created.

### Classrooms table

This table contains the list of classrooms (teams). It is synchronized with the actual data each time the program initializes.
Columns:

- `Name` - `the name of the classroom (team)

### Channels table

This table contains the list of channels. It is synchronized with the actual data each time the program initializes.
Columns:

- `ChannelId` - the ID of the channel (as visible in the URL)
- `Name` - the name of the channel
- `ClassroomName` - the name of the classroom (team) the channel belongs to

### Meetings table

This table contains the list of classrooms (teams). This table must be manually filled in by the user.
Columns:

- `Id` - the ID of the meeting (automatically assigned)
- `DayOfWeek` - the day of week that the meeting is scheduled on
- `StartHour` - the starting hour (in 24 hour format) that the meeting is scheduled on
- `StartMinute` - the starting minute that the meeting is scheduled on
- `DurationMinutes` - the duration (in minutes) of the meeting
- `ChannelId` - refers to the `ChannelId` of the channel from the `Channels` table
- `Enabled` - set to `1` if enabled or `0` if disabled
