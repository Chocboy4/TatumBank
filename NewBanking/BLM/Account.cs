using System;
using System.Collections.Generic;
using System.Text;

namespace NewBanking.BLM
{
    public class Account
    {
        public string Name { get; set; } = "";
        public string Pin { get; set; } = "";
        public decimal Balance { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsFrozen { get; set; }

        public bool NotificationsEnabled { get; set; } = true;
        public List<Notification> Notifications { get; set; } = new();
        public List<string> Transactions { get; set; } = new();
    }


    public class Notification
    {
        public string Message { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public bool IsRead { get; set; }
    }
}
