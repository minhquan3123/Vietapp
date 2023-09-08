using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.App.Admin;
using Android.App.Usage;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Java.Lang;

namespace Vietapp.Droid
{
    [Activity(Label = "VietAppBeta", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        Button showAppsButton;
        Button showAppUsageButton;
        ListView appListView;
        ListView appUsageListView;
        List<PackageInfo> installedApps;
        List<AppUsageInfo> appUsageInfos;

        Button saveTimeButton;
        EditText hoursEditText;
        EditText minutesEditText;
        EditText secondsEditText;
        ListView savedTimesListView;
        List<string> savedTimes;

        Button toggleSavedTimesButton;
        bool savedTimesVisible = false;
        ArrayAdapter<string> savedTimesAdapter;

        ISharedPreferences sharedPreferences;

        UsageStatsManager usageStatsManager;
        PackageManager packageManager;
        bool isTracking = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Layout
            LinearLayout layout = new LinearLayout(this);
            layout.Orientation = Orientation.Vertical;

            // Buttons
            showAppsButton = new Button(this);
            showAppsButton.Text = "Các app";
            showAppsButton.Click += ShowAppsButton_Click;

            showAppUsageButton = new Button(this);
            showAppUsageButton.Text = "Thời gian sử dụng app";
            showAppUsageButton.Click += ShowAppUsageButton_Click;

            appListView = new ListView(this);
            appListView.Visibility = ViewStates.Gone;

            appUsageListView = new ListView(this);
            appUsageListView.Visibility = ViewStates.Gone;

            LinearLayout buttonsLayout = new LinearLayout(this);
            buttonsLayout.Orientation = Orientation.Horizontal;

            saveTimeButton = new Button(this);
            saveTimeButton.Text = "Lưu thời gian";
            saveTimeButton.Click += SaveTimeButton_Click;

            buttonsLayout.AddView(saveTimeButton);

            LinearLayout timeInputLayout = new LinearLayout(this);
            timeInputLayout.Orientation = Orientation.Horizontal;

            hoursEditText = new EditText(this);
            hoursEditText.Hint = "Giờ";
            minutesEditText = new EditText(this);
            minutesEditText.Hint = "Phút";
            secondsEditText = new EditText(this);
            secondsEditText.Hint = "Giây";

            timeInputLayout.AddView(hoursEditText);
            timeInputLayout.AddView(minutesEditText);
            timeInputLayout.AddView(secondsEditText);

            savedTimes = new List<string>();

            sharedPreferences = GetSharedPreferences("SavedTimes", FileCreationMode.Private);

            LoadSavedTimes();

            toggleSavedTimesButton = new Button(this);
            toggleSavedTimesButton.Text = "Ẩn/Hiện thời gian đã lưu";
            toggleSavedTimesButton.Click += ToggleSavedTimesButton_Click;

            savedTimesAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, savedTimes);

            savedTimesListView = new ListView(this);
            savedTimesListView.Adapter = savedTimesAdapter;
            savedTimesListView.Visibility = ViewStates.Gone;

            usageStatsManager = (UsageStatsManager)GetSystemService(Context.UsageStatsService);
            packageManager = PackageManager;

            layout.AddView(showAppsButton);
            layout.AddView(showAppUsageButton);
            layout.AddView(appListView);
            layout.AddView(appUsageListView);
            layout.AddView(buttonsLayout);
            layout.AddView(timeInputLayout);
            layout.AddView(toggleSavedTimesButton);
            layout.AddView(savedTimesListView);

            SetContentView(layout);
        }

        private async void ShowAppsButton_Click(object sender, System.EventArgs e)
        {
            if (appListView.Visibility == ViewStates.Visible)
            {
                appListView.Visibility = ViewStates.Gone;
            }
            else
            {
                ProgressDialog progressDialog = ProgressDialog.Show(this, "Vui lòng chờ", "Đang Tải...");

                await Task.Run(() =>
                {
                    installedApps = GetInstalledUserApps();
                });

                progressDialog.Dismiss();

                List<string> appNames = installedApps.Select(packageInfo => packageInfo.ApplicationInfo.LoadLabel(PackageManager).ToString()).ToList();
                ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, appNames);

                appListView.Adapter = adapter;
                appListView.Visibility = ViewStates.Visible;
            }
        }

        private async void ShowAppUsageButton_Click(object sender, System.EventArgs e)
        {
            if (appUsageListView.Visibility == ViewStates.Visible)
            {
                appUsageListView.Visibility = ViewStates.Gone;
            }
            else
            {
                ProgressDialog progressDialog = ProgressDialog.Show(this, "Vui lòng chờ", "Đang Tải...");

                await Task.Run(() =>
                {
                    appUsageInfos = GetAppUsageStats();
                });

                progressDialog.Dismiss();

                List<string> appUsageList = appUsageInfos.Select(info => $"{info.AppName}: {info.UsageTime}").ToList();
                ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, appUsageList);

                appUsageListView.Adapter = adapter;
                appUsageListView.Visibility = ViewStates.Visible;
            }
        }

        private List<PackageInfo> GetInstalledUserApps()
        {
            PackageManager packageManager = PackageManager;
            List<PackageInfo> packages = packageManager.GetInstalledPackages(PackageInfoFlags.Activities).ToList();

            packages = packages.Where(package => (package.ApplicationInfo.Flags & ApplicationInfoFlags.System) == 0).ToList();

            return packages;
        }

        private List<AppUsageInfo> GetAppUsageStats()
        {
            List<AppUsageInfo> appUsageInfoList = new List<AppUsageInfo>();

            // Get app usage stats for the last 24 hours
            long endTime = JavaSystem.CurrentTimeMillis();
            long startTime = endTime - (24 * 60 * 60 * 1000); // 24 hours in milliseconds

            List<UsageStats> usageStatsList = usageStatsManager.QueryUsageStats(UsageStatsInterval.Daily, startTime, endTime).ToList();

            foreach (UsageStats usageStats in usageStatsList)
            {
                string packageName = usageStats.PackageName;
                string appName = getAppNameFromPackage(packageName);
                long usageTime = usageStats.TotalTimeInForeground;

                if (!string.IsNullOrEmpty(appName) && usageTime > 0)
                {
                    AppUsageInfo appUsageInfo = new AppUsageInfo
                    {
                        AppName = appName,
                        UsageTime = TimeSpan.FromMilliseconds(usageTime).ToString(@"hh\:mm\:ss")
                    };

                    appUsageInfoList.Add(appUsageInfo);
                }
            }

            return appUsageInfoList;
        }

        private string getAppNameFromPackage(string packageName)
        {
            try
            {
                ApplicationInfo appInfo = packageManager.GetApplicationInfo(packageName, 0);
                return appInfo.LoadLabel(packageManager).ToString();
            }
            catch (PackageManager.NameNotFoundException)
            {
                return packageName;
            }
        }

        private void SaveTimeButton_Click(object sender, EventArgs e)
        {
            string hoursText = hoursEditText.Text.Trim();
            string minutesText = minutesEditText.Text.Trim();
            string secondsText = secondsEditText.Text.Trim();

            if (int.TryParse(hoursText, out int hours) && int.TryParse(minutesText, out int minutes) && int.TryParse(secondsText, out int seconds))
            {
                TimeSpan timeSpan = new TimeSpan(hours, minutes, seconds);
                string formattedTime = timeSpan.ToString(@"hh\:mm\:ss");

                savedTimes.Add(formattedTime);
                UpdateSavedTimesListView();

                SaveSavedTimes();

                hoursEditText.Text = "";
                minutesEditText.Text = "";
                secondsEditText.Text = "";
            }
            else
            {
                Toast.MakeText(this, "Hãy nhập thời gian hợp lệ", ToastLength.Short).Show();
            }
        }

        private void UpdateSavedTimesListView()
        {
            savedTimesAdapter.NotifyDataSetChanged();
        }

        private void SaveSavedTimes()
        {
            ISharedPreferencesEditor editor = sharedPreferences.Edit();
            string savedTimesString = string.Join(",", savedTimes);
            editor.PutString("SavedTimes", savedTimesString);
            editor.Apply();
        }

        private void LoadSavedTimes()
        {
            string savedTimesString = sharedPreferences.GetString("SavedTimes", null);

            if (!string.IsNullOrEmpty(savedTimesString))
            {
                savedTimes = savedTimesString.Split(',').ToList();
            }
        }

        private void ToggleSavedTimesButton_Click(object sender, EventArgs e)
        {
            savedTimesVisible = !savedTimesVisible;
            savedTimesListView.Visibility = savedTimesVisible ? ViewStates.Visible : ViewStates.Gone;
        }
    }

    public class AppUsageInfo
    {
        public string AppName { get; set; }
        public string UsageTime { get; set; }
    }
}
