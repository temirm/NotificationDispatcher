using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NotificationDispatcher
{
    internal class Dispatcher
    {
        private readonly List<Notification> _notifications = [];
        private bool _hasScheduledNotifications = true;

        private readonly List<ScheduledNotification> _scheduledNotifications = [];

        private readonly TimeSpan _10_SECONDS = new(hours: 0, minutes: 0, seconds: 10);
        private readonly TimeSpan _1_MINUTE = new(hours: 0, minutes: 1, seconds: 0);
        private readonly TimeSpan _24_HOURS = new(days: 1, hours: 0, minutes: 0, seconds: 0);

        /// <summary>
        /// Добавляет сообщение в систему
        /// </summary>
        public void PushNotification(Notification notification)
        {
            ArgumentNullException.ThrowIfNull(notification, nameof(notification));

            _notifications.Add(notification);
            _hasScheduledNotifications = false;
            _scheduledNotifications.Clear();
        }

        /// <summary>
        /// Вовзращает порядок отправки сообщений
        /// </summary>
        public ReadOnlyCollection<ScheduledNotification> GetOrderedNotifications()
        {
            if (_hasScheduledNotifications)
            {
                return _scheduledNotifications.AsReadOnly();
            }

            Dictionary<string, ScheduledNotification> lastLowPriorityNotifications = [];

            foreach (var notification in _notifications.OrderBy(n => n.Created))
            {
                DateTime scheduledTime = notification.Created;
                ScheduledNotification scheduledNotification = new()
                {
                    Notification = notification,
                    ScheduledDeliveryTime = scheduledTime,
                };

                if (notification.Priority == NotificationPriority.Low)
                {
                    if (lastLowPriorityNotifications.TryGetValue(notification.MessengerAccount, out ScheduledNotification? lastLowPriorityForAccount))
                    {
                        scheduledTime = scheduledTime - lastLowPriorityForAccount.ScheduledDeliveryTime < _24_HOURS
                            ? lastLowPriorityForAccount.ScheduledDeliveryTime + _24_HOURS
                            : scheduledTime;
                    }
                    lastLowPriorityNotifications[notification.MessengerAccount] = scheduledNotification;
                }

                bool isCorrectlyScheduled = false;
                while (!isCorrectlyScheduled)
                {
                    var scheduledAfterWithin1Minute = _scheduledNotifications
                        .Where(n => n.Notification.MessengerAccount == notification.MessengerAccount)
                        .LastOrDefault(n => n.ScheduledDeliveryTime >= scheduledTime && n.ScheduledDeliveryTime < scheduledTime + _1_MINUTE);
                    if (scheduledAfterWithin1Minute is not null)
                    {
                        scheduledTime = scheduledAfterWithin1Minute.ScheduledDeliveryTime + _1_MINUTE;
                        continue;
                    }

                    var scheduledBeforeWithin1Minute = _scheduledNotifications
                        .Where(n => n.Notification.MessengerAccount == notification.MessengerAccount)
                        .LastOrDefault(n => n.ScheduledDeliveryTime <= scheduledTime && n.ScheduledDeliveryTime > scheduledTime - _1_MINUTE);
                    if (scheduledBeforeWithin1Minute is not null)
                    {
                        scheduledTime = scheduledBeforeWithin1Minute.ScheduledDeliveryTime + _1_MINUTE;
                        continue;
                    }

                    var scheduledAfterWithin10Seconds = _scheduledNotifications.LastOrDefault(n => n.ScheduledDeliveryTime >= scheduledTime && n.ScheduledDeliveryTime < scheduledTime + _10_SECONDS);
                    if (scheduledAfterWithin10Seconds is not null)
                    {
                        scheduledTime = scheduledAfterWithin10Seconds.ScheduledDeliveryTime + _10_SECONDS;
                        continue;
                    }

                    var scheduledBeforeWithin10Seconds = _scheduledNotifications.LastOrDefault(n => n.ScheduledDeliveryTime <= scheduledTime && n.ScheduledDeliveryTime > scheduledTime - _10_SECONDS);
                    if (scheduledBeforeWithin10Seconds is not null)
                    {
                        scheduledTime = scheduledBeforeWithin10Seconds.ScheduledDeliveryTime + _10_SECONDS;
                        continue;
                    }

                    isCorrectlyScheduled = true;
                }

                scheduledNotification.ScheduledDeliveryTime = scheduledTime;

                _scheduledNotifications.Add(scheduledNotification);
            }

            return _scheduledNotifications.OrderBy(n => n.ScheduledDeliveryTime).ToList().AsReadOnly();
        }
    }
}
