using Android.App;
using Android.App.Admin;
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
using Xamarin.Essentials;
using Mono;
using Android.Content.Res;
using Java.Lang.Reflect;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

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
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            Window.DecorView.SystemUiVisibility = StatusBarVisibility.Hidden;

            CorrectPassword = GetPass();
            CheckAndRequestUsageStatsPermission();
            StartService(new Intent(this, typeof(AppTrackingService)));

            if (CorrectPassword != "")
            {
                ShowPasswordPrompt();
            }
            else
            {
                ShowPasschange();
            }


            CreateAppButtons();
        }

        protected override void OnDestroy()
        {
            StopService(new Intent(this, typeof(AppTrackingService)));
            base.OnDestroy();
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
        private void LockApp(string name)
        {
            Toast.MakeText(this, name + " has been locked", ToastLength.Short).Show();  
           
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
            var scrollView = new Android.Widget.ScrollView(this);
            var layout = new LinearLayout(this);
            layout.Orientation = Android.Widget.Orientation.Vertical;
            scrollView.AddView(layout);

            var bundle = new Bundle();
            var endTime = JavaSystem.CurrentTimeMillis();
            var startTime = endTime - 24 * 60 * 60 * 1000; // 24 hours ago

            DevicePolicyManager dpm = (DevicePolicyManager)GetSystemService(Context.DevicePolicyService);
            bundle.PutInt("key2", 1);

            if (layout != null)
            {
                var installedApps = packageManager.GetInstalledApplications(PackageInfoFlags.MatchUninstalledPackages);

                foreach (var appInfo in installedApps)
                {
                    if ((appInfo.Flags & ApplicationInfoFlags.System) == 0)
                    {
                        string packageName = appInfo.PackageName;
                        long totalTimeInForeground = GetTotalTimeInForeground(packageName, startTime, endTime);

                        string appName = appInfo.LoadLabel(packageManager).ToString();
                        var button = new Android.Widget.Button(this);
                        button.Text = appName;
                        var textView = new TextView(this);
                        textView.Text = $"Usage Time: {totalTimeInForeground} minutes";

                        button.Click += (sender, e) =>
                        {
                            LockApp(appName);
                        };

                        layout.AddView(button);
                        layout.AddView(textView);

                    }
                }
            }

            SetContentView(scrollView);
        }


        private long GetTotalTimeInForeground(string packageName, long startTime, long endTime)
        {
            var stats = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, endTime);

            if (stats != null)
            {
                foreach (var appInfo in stats)
                {
                    if (appInfo.PackageName.Equals(packageName))
                    {
                        return appInfo.TotalTimeInForeground / (1000 * 60); // Convert to minutes
                    }
                }
            }

            return 0; // App not found in usage stats or no usage time
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
                        
                    }
                });
            }
        }
    }
}

        
