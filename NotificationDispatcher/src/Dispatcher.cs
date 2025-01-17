﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NotificationDispatcher
{
    internal class Dispatcher
    {
        private readonly List<Notification> _notifications = [];
        private bool _hasScheduledNotifications = true;

        private List<ScheduledNotification> _scheduledNotifications = [];

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

            ScheduleNotifications();

            return _scheduledNotifications.AsReadOnly();
        }

        private void ScheduleNotifications()
        {
            _scheduledNotifications.Clear();

            foreach (var notification in _notifications.OrderBy(n => n.Created))
            {
                var scheduledNotification = GetScheduled(notification);
                _scheduledNotifications.Add(scheduledNotification);
            }

            _scheduledNotifications = _scheduledNotifications.OrderBy(n => n.ScheduledDeliveryTime).ToList();
            _hasScheduledNotifications = true;
        }

        private ScheduledNotification GetScheduled(Notification notification)
        {
            DateTime scheduledTime = notification.Created;

            if (notification.Priority == NotificationPriority.Low)
            {
                var lastScheduledLowPriority = _scheduledNotifications
                    .Where(n => n.Notification.Priority == NotificationPriority.Low)
                    .LastOrDefault(n => n.Notification.MessengerAccount == notification.MessengerAccount);

                if (lastScheduledLowPriority is not null && scheduledTime - lastScheduledLowPriority.ScheduledDeliveryTime < _24_HOURS)
                {
                    scheduledTime = lastScheduledLowPriority.ScheduledDeliveryTime + _24_HOURS;
                }
            }

            ScheduledNotification scheduledNotification = new()
            {
                Notification = notification,
                ScheduledDeliveryTime = scheduledTime,
            };

            AdjustUntilCorrectlyScheduled(scheduledNotification);
            return scheduledNotification;
        }

        private void AdjustUntilCorrectlyScheduled(ScheduledNotification scheduledNotification)
        {
            DateTime scheduledTime = scheduledNotification.ScheduledDeliveryTime;

            bool isCorrectlyScheduled = false;
            while (!isCorrectlyScheduled)
            {
                var scheduledAfterWithin1Minute = _scheduledNotifications
                    .Where(n => n.Notification.MessengerAccount == scheduledNotification.Notification.MessengerAccount)
                    .LastOrDefault(n => n.ScheduledDeliveryTime >= scheduledTime && n.ScheduledDeliveryTime < scheduledTime + _1_MINUTE);
                if (scheduledAfterWithin1Minute is not null)
                {
                    scheduledTime = scheduledAfterWithin1Minute.ScheduledDeliveryTime + _1_MINUTE;
                    continue;
                }

                var scheduledBeforeWithin1Minute = _scheduledNotifications
                    .Where(n => n.Notification.MessengerAccount == scheduledNotification.Notification.MessengerAccount)
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
        }
    }
}
