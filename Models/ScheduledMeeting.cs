using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace autoteams.Models
{
    public class ScheduledMeeting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string DayOfWeek { get; set; }
        public short StartHour { get; set; }
        public short StartMinute { get; set; }
        public int DurationMinutes { get; set; }

        public string ChannelId { get; set; }
        public bool Enabled { get; set; }

        public virtual TeamsChannel Channel { get; set; }

        [NotMapped]
        [JsonIgnore]
        public DateTime Time { get; set; }
    }
}