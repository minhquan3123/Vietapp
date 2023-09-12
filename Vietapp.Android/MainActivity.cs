using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Java.Lang;
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

            // Retrieve and display app usage statistics for installed apps (excluding system apps)
            DisplayAppUsageStatisticsForInstalledApps();
        }

        private void CheckAndRequestUsageStatsPermission()
        {
            var appOps = (Android.App.AppOpsManager)GetSystemService(Context.AppOpsService);
            var mode = appOps.UnsafeCheckOpNoThrow(Android.App.AppOpsManager.OpstrGetUsageStats, Android.OS.Process.MyUid(), PackageName);

            if (mode != Android.App.AppOpsManagerMode.Allowed)
            {
                var intent = new Intent(Settings.ActionUsageAccessSettings);
                StartActivity(intent);
            }
        }

        private void DisplayAppUsageStatisticsForInstalledApps()
        {
            var usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
            var endTime = JavaSystem.CurrentTimeMillis();
            var startTime = endTime - 24 * 60 * 60 * 1000; // 24 hours ago

            var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, endTime);

            if (stats != null)
            {
                // Get a list of all installed apps (excluding system apps)
                var installedApps = packageManager.GetInstalledApplications(PackageInfoFlags.MatchUninstalledPackages);

                var appUsageList = new List<string>();

                foreach (var usageStats in stats)
                {
                    string packageName = usageStats.PackageName;

                    // Check if the package name corresponds to an installed app and is not a system app
                    if (IsInstalledApp(installedApps, packageName))
                    {
                        string appName = GetAppName(packageName);
                        long totalTimeInForeground = usageStats.TotalTimeInForeground / (1000 * 60); // Convert to minutes

                        string appUsageInfo = $"{appName}: {totalTimeInForeground} minutes";
                        appUsageList.Add(appUsageInfo);
                    }
                }

                // Display app usage statistics for installed apps (excluding system apps) in the TextView
                appUsageTextView.Text = string.Join("\n", appUsageList);
            }
        }

        private bool IsInstalledApp(IList<ApplicationInfo> installedApps, string packageName)
        {
            // Filter out system apps by checking their flags
            foreach (var appInfo in installedApps)
            {
                if (appInfo.PackageName == packageName && (appInfo.Flags & ApplicationInfoFlags.System) == 0)
                {
                    return true; // It's an installed non-system app
                }
            }
            return false; // It's either a system app or not installed
        }

        private string GetAppName(string packageName)
        {
            try
            {
                var packageInfo = PackageManager.GetPackageInfo(packageName, PackageInfoFlags.Activities);
                return packageInfo.ApplicationInfo.LoadLabel(packageManager).ToString();
            }
            catch (PackageManager.NameNotFoundException)
            {
                // Handle the case where the package name is not found
                return packageName;
            }
        }
    }
}
