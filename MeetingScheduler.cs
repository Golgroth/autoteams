using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using autoteams.Models;
using Microsoft.EntityFrameworkCore;

namespace autoteams
{
    public class MeetingScheduler
    {
        public MeetingScheduler(TeamsController controller)
        {
            _controller = controller;
            ScheduledMeetings = new();
            _ticker = new((_) => SchedulerTick(), null, Timeout.Infinite, Timeout.Infinite);
        }

        private readonly Timer _ticker;

        public HashSet<ScheduledMeeting> ScheduledMeetings { get; private set; }

        private bool _init;

        public void Initialize()
        {
            if (_init) return;
            _init = true;

            LoadMeetingsFromDb();
            _ = Task.Run(async () =>
            {
                int scheduledSecond = ConfigurationManager.CONFIG.SchedulerSyncOnSecond;

                //Use second synchronization if valid config
                if (scheduledSecond is >= 0 and < 60)
                {
                    var now = DateTime.Now;

                    //Set the trigger date that is either in the current minute or in the next minute, if the desired seconds has passed
                    var trigger = now.AddSeconds(scheduledSecond - now.Second < 0 ? 60 - now.Second + scheduledSecond : scheduledSecond);
                    await Task.Delay(trigger - now);

                    Logger.Info($"Waiting to synchronize with time (on {trigger}).");
                }

                _ticker.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
                Logger.Info($"Started scheduler timer at {DateTime.Now}.");
            });
        }

        public void LoadMeetingsFromDb()
        {
            ScheduledMeetings.Clear();

            Logger.Info("Loading scheduled meetings...");

            using var db = new StorageContext();

            var now = DateTime.Now;

            foreach (ScheduledMeeting meeting in db.Meetings.AsNoTracking().Include(s => s.Channel).AsEnumerable())
            {
                try
                {
                    meeting.Time = new DateTime(now.Year, now.Month, now.Day, meeting.StartHour, meeting.StartMinute, 0);
                }
                catch (Exception)
                {
                    Logger.Warn($"Corrupted meeting: {meeting.Channel.ClassroomName}, {meeting.Channel.Name}. Discarding...");
                    continue;
                }

                Logger.Debug($"Found meeting in {meeting.Channel.ClassroomName}, {meeting.Channel.Name} on {meeting.DayOfWeek}s, {meeting.StartHour}:{meeting.StartMinute}");
                ScheduledMeetings.Add(meeting);
            }
        }

        private readonly TeamsController _controller;

        private ScheduledMeeting _currentMeeting;
        private DateTime _lastMeetingStart;

        public void SchedulerTick()
        {
            if (_currentMeeting != null)
            {
                var now = DateTime.Now;
                if (now.CompareTo(_currentMeeting.Time.Add(TimeSpan.FromMinutes(_currentMeeting.DurationMinutes))) >= 0)
                {
                    Logger.Info($"Meeting in {_currentMeeting.Channel.Name}, {_currentMeeting.Channel.ClassroomName} ends now ({now}), after {(int)(now - _lastMeetingStart).TotalMinutes} minutes.");
                    _controller.LeaveMeeting();
                    _currentMeeting = null;
                }
            }
            else
            {
                lock (ScheduledMeetings)
                {
                    foreach (ScheduledMeeting meeting in ScheduledMeetings)
                    {
                        DateTime time = DateTime.Now;
                        if (meeting.DayOfWeek.Equals(time.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            DateTime target = new(time.Year, time.Month, time.Day, meeting.StartHour, meeting.StartMinute, 0);

                            if (time.CompareTo(target) > 0)
                            {
                                var difference = time - target;
                                var timeToBreak = (int)meeting.DurationMinutes / 3;

                                if (difference.TotalMinutes <= (timeToBreak < 15 ? meeting.DurationMinutes : timeToBreak / 3))
                                {
                                    Logger.Info($"Meeting in {meeting.Channel.Name}, {meeting.Channel.ClassroomName} starts now ({time}) for {meeting.DurationMinutes} minutes.");

                                    var minuteDifference = (int)difference.TotalMinutes;

                                    if (minuteDifference > 0)
                                    {
                                        Logger.Info($"We are {minuteDifference} minute{(minuteDifference > 1 ? "s" : "")} late.");
                                    }
                                    bool isSuccess = _controller.JoinMeeting(meeting.Channel);
                                    if (isSuccess)
                                    {
                                        _currentMeeting = meeting;
                                        _lastMeetingStart = time;
                                        Logger.Info("Successfully joined the meeting.");
                                    }
                                    else
                                        Logger.Error("Cannot join meeting.");

                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}