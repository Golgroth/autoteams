using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace autoteams.Models
{
    public class TeamsChannel
    {
        public string ClassroomName { get; set; }
        public string Name { get; set; }
        [Key]
        public string ChannelId { get; set; }

        public virtual TeamsClassroom Classroom { get; set; }
        public virtual ICollection<ScheduledMeeting> Meetings { get; set; }

        public string GetUrl() => $"https://teams.microsoft.com/_#/school/conversations/{Uri.EscapeUriString(Name)}?threadId={ChannelId}&ctx=channel";

        public override bool Equals(object obj)
        {
            return obj is TeamsChannel channel && this.ChannelId.Equals(channel.ChannelId, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ClassroomName, Name, ChannelId);
        }
    }
}