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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vietapp.Droid
{
    [Activity(Label = "VietappBETA", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        PackageManager packageManager;
        UsageStatsManager usageStatsManager;
        Dictionary<string, long> appUsageData;
        CancellationTokenSource cancellationTokenSource;
        const int UpdateInterval = 1000;

        string CorrectPassword = "";
        bool isAuthenticated = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            packageManager = PackageManager;
            usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
            appUsageData = new Dictionary<string, long>();
            cancellationTokenSource = new CancellationTokenSource();

            CorrectPassword = GetPass();
            CheckAndRequestUsageStatsPermission();
            Toast.MakeText(this, CorrectPassword, ToastLength.Short).Show();

            if (CorrectPassword != "")
            {
                ShowPasswordPrompt();
            }
            else
            {
                ShowPasschange();
            }

            // Create app buttons
            CreateAppButtons();
        }

        public void CreateFiles(string Passtext)
        {
            var name = "pass.txt";
            var destination = System.IO.Path.Combine(GetposPass(), name);

            File.WriteAllText(destination, Passtext);
        }

        public string GetposPass()
        {
            return Android.App.Application.Context.GetExternalFilesDir("pass.txt").ToString();
        }

        public string GetPass()
        {
            var name = "pass.txt";
            var destination = System.IO.Path.Combine(GetposPass(), name);
            if (System.IO.File.Exists(destination))
            {
                return File.ReadAllText(destination);
            }
            else
            {
                CreateFiles("pass");
            }
            return ("");
        }

        private void ShowPasschange()
        {
            // Create a simple password input dialog
            var passwordchangeView = LayoutInflater.Inflate(Resource.Layout.change_password_dialog, null);
            var Newpass = passwordchangeView.FindViewById<EditText>(Resource.Id.newPass);
            var changepass = new AlertDialog.Builder(this);
            var name = "pass.txt";
            var destination = System.IO.Path.Combine(GetposPass(), name);
            changepass.SetTitle("Enter New Password");
            changepass.SetView(passwordchangeView);
            changepass.SetPositiveButton("save", (sender, e) =>
            {
                File.WriteAllText(destination, (Newpass.Text).ToString());
                CorrectPassword = GetPass();

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

                    // Start the background thread
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

        private void CreateAppButtons()
        {
            // Find the LinearLayout with the ID "layout"
            var layout = FindViewById<LinearLayout>(Resource.Id.layout);

            if (layout != null)
            {
                // Only proceed if the layout is found
                var installedApps = packageManager.GetInstalledApplications(PackageInfoFlags.MatchUninstalledPackages);

                foreach (var appInfo in installedApps)
                {
                    if ((appInfo.Flags & ApplicationInfoFlags.System) == 0)
                    {
                        string appName = appInfo.LoadLabel(packageManager).ToString();
                        var button = new Button(this);
                        button.Text = appName;

                        button.Click += (sender, e) =>
                        {
                            LaunchAppByPackageName(appInfo.PackageName);
                        };

                        // Add the button to the layout
                        layout.AddView(button);
                    }
                }
            }
            else
            {
                // Handle the case where the layout is not found
                Console.WriteLine("Layout not found with ID 'layout'");
            }
        }


        private void LaunchAppByPackageName(string packageName)
        {
            Intent intent = PackageManager.GetLaunchIntentForPackage(packageName);
            if (intent != null)
            {
                StartActivity(intent);
            }
            else
            {
                Toast.MakeText(this, "App not found.", ToastLength.Short).Show();
            }
        }

        private void StartBackgroundThread()
        {
            Task.Run(() =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // Delay for the update interval
                    Task.Delay(UpdateInterval).Wait();

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

                // Display app usage statistics in the console (customize this part for your UI)
                foreach (var kvp in appUsageData)
                {
                    string packageName = kvp.Key;
                    long totalTimeInForeground = kvp.Value;

                    Console.WriteLine($"App Package Name: {packageName}, Usage Time (minutes): {totalTimeInForeground}");
                }
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Stop the background thread when the activity is destroyed
            cancellationTokenSource.Cancel();
        }
    }
}
