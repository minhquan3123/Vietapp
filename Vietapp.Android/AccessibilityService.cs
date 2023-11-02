using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Service.Autofill;
using Android.Views.Accessibility;
using System;
using System.Windows;

namespace Vietapp.Droid
{
    [Service(Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE")]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    public class ForegroundAppTrackerService : AccessibilityService
    {

        public event EventHandler StateChanged;
        public override void OnAccessibilityEvent(AccessibilityEvent e)
        {
            if (e.EventType == EventType.WindowStateChanged)
            {
                if (e.PackageName != null)
                {
                    string foregroundApp = e.PackageName.ToString();
                    ProcessForegroundApp(foregroundApp);
                }
            }
        }

        public override void OnInterrupt()
        {
            // Handle interruptions or errors
        }

        public override void OnServiceConnected()
        {
            base.OnServiceConnected();
            // Additional setup or initialization can be performed here
        }

        private void ProcessForegroundApp(string packageName)
        {
            // Perform actions with the foreground app (packageName)
            // For instance, log or handle the foreground app change
        }
    }

}
