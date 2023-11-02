using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vietapp.Droid
{
    [Service]
    public class AppTrackingService : Service
    {
        private CancellationTokenSource cancellationTokenSource;
        private UsageStatsManager usageStatsManager;
        private const int UpdateInterval = 1000; // Update interval in milliseconds

        public override void OnCreate()
        {
            base.OnCreate();
            usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Task.Run(() => TrackRunningApps());
            return StartCommandResult.NotSticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnDestroy()
        {
            cancellationTokenSource.Cancel();
            base.OnDestroy();
        }

        private async Task TrackRunningApps()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                long endTime = JavaSystem.CurrentTimeMillis();
                long startTime = endTime - 10000; // Change the time interval as needed

                var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, endTime);

                if (stats != null)
                {
                    var appUsageData = new Dictionary<string, long>();

                    foreach (var app in stats)
                    {
                        string packageName = app.PackageName;
                        long timeInForeground = app.TotalTimeInForeground;

                        if (timeInForeground > 0)
                        {
                            // Calculate the time in a readable format, e.g., minutes
                            long timeInMinutes = timeInForeground / (1000 * 60);

                            if (!appUsageData.ContainsKey(packageName))
                            {
                                appUsageData.Add(packageName, timeInMinutes);
                            }
                            else
                            {
                                appUsageData[packageName] += timeInMinutes;
                            }
                        }
                    }

                    // Process the app usage data here (e.g., log or send it to a server)
                    ProcessAppUsageData(appUsageData);
                }

                await Task.Delay(UpdateInterval);
            }
        }

        private void ProcessAppUsageData(Dictionary<string, long> appUsageData)
        {
            // Perform operations with the tracked app usage data, e.g., logging or sending to a server
            foreach (var app in appUsageData)
            {
                string appName = GetAppNameFromPackageName(app.Key);
                long usageTime = app.Value;
                Console.WriteLine($"{appName}: {usageTime} minutes");
                // You can log or send this information to a server
            }
        }

        private string GetAppNameFromPackageName(string packageName)
        {
            PackageManager packageManager = PackageManager;
            ApplicationInfo appInfo;
            try
            {
                appInfo = packageManager.GetApplicationInfo(packageName, PackageInfoFlags.MatchUninstalledPackages);
            }
            catch (PackageManager.NameNotFoundException)
            {
                appInfo = null;
            }

            return appInfo != null ? packageManager.GetApplicationLabel(appInfo).ToString() : packageName;
        }
    }
}
