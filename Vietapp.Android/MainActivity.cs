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
            var passwordDialogView = LayoutInflater.Inflate(Resource.Layout.password_dialog, null);
            var passwordEditText = passwordDialogView.FindViewById<EditText>(Resource.Id.passwordEditText);
            var passwordDialog = new AlertDialog.Builder(this);
            passwordDialog.SetTitle("Enter Password");
            passwordDialog.SetView(passwordDialogView);
            passwordDialog.SetPositiveButton("OK", (sender, e) =>
            {
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

            var dialog = passwordDialog.Create();
            dialog.Show();
        }

        private void CreateAppButtons()
        {
            var layout = FindViewById<LinearLayout>(Resource.Id.layout);

            if (layout != null)
            {
                var installedApps = packageManager.GetInstalledApplications(PackageInfoFlags.MatchUninstalledPackages);

                foreach (var appInfo in installedApps)
                {
                    if ((appInfo.Flags & ApplicationInfoFlags.System) == 0)
                    {
                        string appName = appInfo.LoadLabel(packageManager).ToString();
                        var button = new Button(this);
                        button.Text = appName;

                        var textView = new TextView(this);
                        textView.Text = "Usage Time: 0 minutes";

                        button.Click += (sender, e) =>
                        {
                            LaunchAppByPackageName(appInfo.PackageName);
                        };

                        layout.AddView(button);
                        layout.AddView(textView);
                    }
                }
            }
            else
            {
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

        private async void StartBackgroundThread()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(UpdateInterval);

                RunOnUiThread(() =>
                {
                    if (isAuthenticated)
                    {
                        UpdateAppUsageStatistics();
                    }
                });
            }
        }

        private void UpdateAppUsageStatistics()
        {
            var endTime = JavaSystem.CurrentTimeMillis();
            var startTime = endTime - 24 * 60 * 60 * 1000; // 24 hours ago

            var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, endTime);

            if (stats != null)
            {
                foreach (var appInfo in stats)
                {
                    string packageName = appInfo.PackageName;
                    long totalTimeInForeground = appInfo.TotalTimeInForeground / (1000 * 60); // Convert to minutes

                    UpdateUsageTextView(packageName, totalTimeInForeground);
                }
            }
        }

        private void UpdateUsageTextView(string packageName, long totalTimeInForeground)
        {
            var layout = FindViewById<LinearLayout>(Resource.Id.layout);

            if (layout != null)
            {
                for (int i = 0; i < layout.ChildCount; i += 2) // Increment by 2 to skip the TextViews
                {
                    var button = layout.GetChildAt(i) as Button;
                    var textView = layout.GetChildAt(i + 1) as TextView;

                    if (button != null && textView != null)
                    {
                        string appName = button.Text;

                        if (packageName.Equals(packageManager.GetLaunchIntentForPackage(packageName)?.Component?.PackageName))
                        {
                            textView.Text = $"Usage Time: {totalTimeInForeground} minutes";
                            break;
                        }
                    }
                }
            }
        }

    }
}
