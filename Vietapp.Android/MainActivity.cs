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
using System.Threading;
using System.Threading.Tasks;

namespace Vietapp.Droid
{
    [Activity(Label = "VietappBETA", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        TextView appUsageTextView;
        PackageManager packageManager;
        UsageStatsManager usageStatsManager;
        Dictionary<string, long> appUsageData;
        CancellationTokenSource cancellationTokenSource;
        const int UpdateInterval = 6000;

        // Define your password here (replace "your_password" with the actual password)
        private string CorrectPassword = "your_password";
        private bool isAuthenticated = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            appUsageTextView = FindViewById<TextView>(Resource.Id.appUsageTextView);
            packageManager = PackageManager;
            usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
            appUsageData = new Dictionary<string, long>();
            cancellationTokenSource = new CancellationTokenSource();

            // Check and request the PACKAGE_USAGE_STATS permission
            CheckAndRequestUsageStatsPermission();

            // Show the password prompt
            ShowPasswordPrompt();
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

        private void ShowPasswordPrompt()
        {
            // Create a simple password input dialog
            var passwordDialogView = LayoutInflater.Inflate(Resource.Layout.password_dialog, null);
            var passwordEditText = passwordDialogView.FindViewById<EditText>(Resource.Id.passwordEditText);
            var passwordDialog = new AlertDialog.Builder(this);
            passwordDialog.SetTitle("Enter Password");
            passwordDialog.SetView(passwordDialogView);
            passwordDialog.SetPositiveButton("OK", (sender, e) =>
            {
                // Check if the entered password is correct
                var enteredPassword = passwordEditText.Text;
                if (enteredPassword == CorrectPassword)
                {
                    isAuthenticated = true;
                    StartBackgroundThread();
                }
                else
                {
                    Toast.MakeText(this, "Incorrect password. Please try again.", ToastLength.Short).Show();
                    Finish();
                }
            });
            passwordDialog.SetNegativeButton("Cancel", (sender, e) =>
            {
                Finish();
            });

            // Show the dialog
            var dialog = passwordDialog.Create();
            dialog.Show();
        }

        private void StartBackgroundThread()
        {
            Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(UpdateInterval);
                    RunOnUiThread(() =>
                    {
                        // Only retrieve and update app usage statistics if authenticated
                        if (isAuthenticated)
                        {
                            UpdateAppUsageStatistics();
                        }
                    });
                }
            });
        }

        private void UpdateAppUsageStatistics()
        {
            var endTime = JavaSystem.CurrentTimeMillis();
            var startTime = endTime - 24 * 60 * 60 * 1000; // 24 hours ago

            var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, endTime);

            if (stats != null)
            {
                // Clear the existing app usage data
                appUsageData.Clear();

                // Get a list of all installed apps (excluding system apps)
                var installedApps = packageManager.GetInstalledApplications(PackageInfoFlags.MatchUninstalledPackages);

                foreach (var usageStats in stats)
                {
                    string packageName = usageStats.PackageName;

                    // Check if the package name corresponds to an installed app and is not a system app
                    if (IsInstalledApp(installedApps, packageName))
                    {
                        long totalTimeInForeground = usageStats.TotalTimeInForeground / (1000 * 60); // Convert to minutes

                        // Update or add the app's usage time in the dictionary
                        appUsageData[packageName] = totalTimeInForeground;
                    }
                }

                // Display app usage statistics for installed apps (excluding system apps) in the TextView
                ShowAppUsageData();
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

        private void ShowAppUsageData()
        {
            var appUsageList = new List<string>();

            foreach (var kvp in appUsageData)
            {
                string packageName = kvp.Key;
                string appName = GetAppName(packageName);
                long totalTimeInForeground = kvp.Value;

                string appUsageInfo = $"{appName}: {totalTimeInForeground} minutes";
                appUsageList.Add(appUsageInfo);
            }

            // Display app usage statistics for installed apps (excluding system apps) in the TextView
            appUsageTextView.Text = string.Join("\n", appUsageList);
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Stop the background thread when the activity is destroyed
            cancellationTokenSource.Cancel();
        }
    }
}
