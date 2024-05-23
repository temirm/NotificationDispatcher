using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NotificationDispatcher
{
    internal class Dispatcher
    {
        /// <summary>
        /// Добавляет сообщение в систему
        /// </summary>
        public void PushNotification(Notification notification)
        {
            // TODO: Implement
            Console.WriteLine($"Pushed: {notification.MessengerAccount} - {notification.Message}");
        }

        /// <summary>
        /// Вовзращает порядок отправки сообщений
        /// </summary>
        public ReadOnlyCollection<ScheduledNotification> GetOrderedNotifications()
        {
            // TODO: Implement
            return new List<ScheduledNotification>().AsReadOnly();
        }
    }
}
