using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vietapp.Droid
{
    [Activity(Label = "VietappBETA", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        TextView appUsageTextView;
        TextView installedAppsTextView;
        PackageManager packageManager;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            appUsageTextView = FindViewById<TextView>(Resource.Id.appUsageTextView);
            packageManager = PackageManager;

            // Check and request the PACKAGE_USAGE_STATS permission
            CheckAndRequestUsageStatsPermission();

            // Retrieve and display app usage statistics
            DisplayAppUsageStatistics();
        }

        private void CheckAndRequestUsageStatsPermission()
        {
            var appOps = (Android.App.AppOpsManager)GetSystemService(Context.AppOpsService);
            var mode = appOps.CheckOpNoThrow(Android.App.AppOpsManager.OpstrGetUsageStats, Android.OS.Process.MyUid(), PackageName);

            if (mode != Android.App.AppOpsManagerMode.Allowed)
            {
                var intent = new Intent(Settings.ActionUsageAccessSettings);
                StartActivity(intent);
            }
        }

        private void DisplayAppUsageStatistics()
        {
            var usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
            var endTime = JavaSystem.CurrentTimeMillis();
            var startTime = endTime - 24 * 60 * 60 * 1000; // 24 hours ago

            var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, endTime);

            if (stats != null)
            {
                var appUsageList = new List<string>();

                foreach (var usageStats in stats)
                {
                    string packageName = usageStats.PackageName;
                    string appName = GetAppName(packageName);
                    long totalTimeInForeground = usageStats.TotalTimeInForeground / 1000; // Convert to seconds

                    string appUsageInfo = $"{appName}: {totalTimeInForeground} seconds";
                    appUsageList.Add(appUsageInfo);
                }

                // Display app usage statistics in a TextView
                appUsageTextView.Text = string.Join("\n", appUsageList);
            }
        }


        private string GetAppName(string packageName)
        {
            var packageInfo = packageManager.GetPackageInfo(packageName, PackageInfoFlags.Activities);
            return packageInfo.ApplicationInfo.LoadLabel(packageManager).ToString();
        }

        private void GetInstalledApps()
        {
            var apps = packageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);

            if (apps != null)
            {
                var installedAppList = new List<string>();

                foreach (var app in apps)
                {
                    string appName = app.LoadLabel(packageManager).ToString();
                    installedAppList.Add(appName);
                }

                // Display installed apps in the TextView
                installedAppsTextView.Text = string.Join("\n", installedAppList);
            }
        }
    }
}
