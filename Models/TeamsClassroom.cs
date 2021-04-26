using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using OpenQA.Selenium;

namespace autoteams.Models
{
    public class TeamsClassroom
    {
        [Key]
        public string Name { get; set; }
        public virtual ICollection<TeamsChannel> Channels { get; set; }

        [NotMapped]
        [JsonIgnore]
        public IWebElement Element { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TeamsClassroom room && this.Name.Equals(room.Name, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}