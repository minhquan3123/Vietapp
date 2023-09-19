using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using static Java.Util.Jar.Attributes;

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

        string CorrectPassword="";
        bool isAuthenticated = false;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);


            appUsageTextView = FindViewById<TextView>(Resource.Id.appUsageTextView);
            packageManager = PackageManager;
            usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
            appUsageData = new Dictionary<string, long>();
            cancellationTokenSource = new CancellationTokenSource();

            SetPassword("");

            CorrectPassword = Xamarin.Essentials.SecureStorage.GetAsync("passtest").ToString();
            Toast.MakeText(this, CorrectPassword, ToastLength.Short).Show();

            SetPassword(CorrectPassword);

            if (CorrectPassword!="")
            {
                ShowPasswordPrompt();
            }
            else
            {
                ShowPasschange();
            }
        }

        public void CreateFiles(string Passtext)
        {
            var name = "pass.txt";
            var destination = System.IO.Path.Combine(GetposPass(), name);

            File.WriteAllText(destination, Passtext);
        }
        public string GetposPass()
        {
            return Android.App.Application.Context.GetExternalFilesDir(null).ToString();
        }

        public string GetPass()
        {
            await Xamarin.Essentials.SecureStorage.SetAsync("passtest", newPassword);
        }

        private async void ShowPasschange()
        {
            // Create a simple password input dialog
            var passwordchangeView = LayoutInflater.Inflate(Resource.Layout.change_password_dialog, null);
            var Newpass = passwordchangeView.FindViewById<EditText>(Resource.Id.newPass);
            var changepass = new AlertDialog.Builder(this);
            var name = "pass.txt";
            var destination = System.IO.Path.Combine(GetposPass(), name);
            changepass.SetTitle("Enter New Password");
            changepass.SetView(passwordchangeView);
            changepass.SetPositiveButton("save", async (sender, e) =>
            {
                pass = Newpass.Text;
                SetPassword(pass.ToString());

                CorrectPassword = await Xamarin.Essentials.SecureStorage.GetAsync("password");

                SetPassword(CorrectPassword);

                if (!string.IsNullOrEmpty(CorrectPassword))
                {
                    ShowPasswordPrompt();
                }
            });
            changepass.SetNegativeButton("Cancel", (sender, e) =>
            {
                Finish();
            });

            // Show the dialog
            var dialog = changepass.Create();
            dialog.Show();
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