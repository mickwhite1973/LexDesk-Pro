using CommunityToolkit.WinUI.Notifications;
using System;
using System.Windows;

namespace Web_Studio_Browser
{
    public partial class App : Application
    {
        private readonly object _toastActivationLock = new();

        private string _pendingToastAction = "";
        private string _pendingToastEventId = "";

        public App()
        {
            // Register as early as possible so Windows notification
            // clicks can be received whether LexDesk is already open
            // or Windows has just launched it from the notification.
            ToastNotificationManagerCompat.OnActivated +=
                ToastNotificationManagerCompat_OnActivated;
        }

        private void ToastNotificationManagerCompat_OnActivated(
            ToastNotificationActivatedEventArgsCompat e)
        {
            try
            {
                ToastArguments args =
                    ToastArguments.Parse(
                        e.Argument ?? "");

                string action =
                    args.Contains("action")
                        ? args["action"]
                        : "";

                string eventId =
                    args.Contains("eventId")
                        ? args["eventId"]
                        : "";

                if (string.IsNullOrWhiteSpace(eventId))
                    return;

                lock (_toastActivationLock)
                {
                    _pendingToastAction =
                        action?.Trim() ?? "";

                    _pendingToastEventId =
                        eventId.Trim();
                }

                Dispatcher.BeginInvoke(
                    new Action(
                        TryDeliverPendingCalendarToast));
            }
            catch
            {
                // Notification activation must never stop LexDesk opening.
            }
        }

        private void TryDeliverPendingCalendarToast()
        {
            try
            {
                if (Application.Current.MainWindow
                    is not Web_Studio_Browser.MainWindow mainWindow)
                {
                    // LexDesk may still be starting.
                    // Keep the activation pending until MainWindow loads.
                    return;
                }

                string action;
                string eventId;

                lock (_toastActivationLock)
                {
                    action =
                        _pendingToastAction;

                    eventId =
                        _pendingToastEventId;

                    if (string.IsNullOrWhiteSpace(eventId))
                        return;

                    _pendingToastAction = "";
                    _pendingToastEventId = "";
                }

                mainWindow.HandleLexDeskCalendarToastActivation(
                    action,
                    eventId);
            }
            catch
            {
                // Notification activation must never affect startup.
            }
        }

        internal static void TryDeliverPendingCalendarToast(
            MainWindow mainWindow)
        {
            try
            {
                if (mainWindow == null)
                    return;

                if (Current is not App app)
                    return;

                app.TryDeliverPendingCalendarToast();
            }
            catch
            {
                // Pending notification delivery must never affect startup.
            }
        }
    }
}