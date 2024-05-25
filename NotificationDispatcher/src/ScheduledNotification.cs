using System;

namespace NotificationDispatcher
{
    internal record ScheduledNotification
    {
        public DateTime ScheduledDeliveryTime { get; set; }
        public required Notification Notification { get; set; }

        public override string ToString()
        {
            return $"{Notification.Id} - {ScheduledDeliveryTime} - {Notification.MessengerAccount}";
        }
    }
}
