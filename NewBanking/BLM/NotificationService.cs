using NewBanking.Infrastructure;
using NewBanking.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NewBanking.BLM
{
    public static class NotificationService
    {
        private const int MaxStored = 50;

        // Adds a notification to the account. It does NOT save,
        // so call it before your existing BankStorage.SaveAccounts(...)
        public static void Add(Account account, string message)
        {
            if (!account.NotificationsEnabled)
            {
                return;
            }

            account.Notifications.Add(new Notification
            {
                Message = message,
                CreatedAtUtc = DateTime.UtcNow,
                IsRead = false
            });

            // Keep the list from growing forever
            if (account.Notifications.Count > MaxStored)
            {
                account.Notifications.RemoveRange(0, account.Notifications.Count - MaxStored);
            }
        }

        public static void ShowUnreadAlert(Account account)
        {
            int unread = UnreadCount(account);

            if (unread == 0)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ● You have {unread} unread notification{(unread == 1 ? "" : "s")}");
            Console.ResetColor();
            Console.WriteLine();
        }

        public static int UnreadCount(Account account)
        {
            return account.Notifications.Count(n => !n.IsRead);
        }

        // The notifications screen: list, mark as read, clear, turn on/off
        public static void Show(Account current, List<Account> accounts)
        {
            ConsoleUI.Banner("Notifications", $"{UnreadCount(current)} unread");

            if (current.Notifications.Count == 0)
            {
                ConsoleUI.Info("You have no notifications.");
            }
            else
            {
                // Newest first
                for (int i = current.Notifications.Count - 1; i >= 0; i--)
                {
                    Notification n = current.Notifications[i];
                    string time = n.CreatedAtUtc.ToLocalTime().ToString("dd MMM, HH:mm");

                    Console.ForegroundColor = n.IsRead ? ConsoleColor.DarkGray : ConsoleColor.Yellow;
                    Console.Write(n.IsRead ? "   " : " ● ");
                    Console.ForegroundColor = n.IsRead ? ConsoleColor.Gray : ConsoleColor.White;
                    Console.WriteLine(n.Message);

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   {time}\n");
                    Console.ResetColor();

                    n.IsRead = true;
                }
            }

            lock (BankStorage.SyncRoot)
            {
                try
                {
                    BankStorage.SaveAccounts(accounts);
                }
                catch (Exception ex)
                {
                    ConsoleUI.Error($"Could not save: {ex.Message}");
                }
            }

            string state = current.NotificationsEnabled ? "ON" : "OFF";

            Console.WriteLine();
            ConsoleUI.Menu(
                "Clear all notifications",
                $"Turn notifications {(current.NotificationsEnabled ? "off" : "on")} (currently {state})");
            Console.WriteLine("  Press Enter to go back.");
            ConsoleUI.Prompt("Choose");

            string choice = ConsoleHelper.ReadTrimmed();

            if (choice == "1")
            {
                current.Notifications.Clear();
                Save(accounts);
                ConsoleUI.Success("Notifications cleared.");
            }
            else if (choice == "2")
            {
                current.NotificationsEnabled = !current.NotificationsEnabled;
                Save(accounts);
                ConsoleUI.Success($"Notifications are now {(current.NotificationsEnabled ? "ON" : "OFF")}.");
            }
            else
            {
                return;
            }

            ConsoleUI.Pause();
        }

        private static void Save(List<Account> accounts)
        {
            lock (BankStorage.SyncRoot)
            {
                try
                {
                    BankStorage.SaveAccounts(accounts);
                }
                catch (Exception ex)
                {
                    ConsoleUI.Error($"Could not save: {ex.Message}");
                }
            }
        }
    }
}