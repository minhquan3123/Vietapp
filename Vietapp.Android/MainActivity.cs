using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vietapp.Droid
{
    [Activity(Label = "VietAppBeta", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        Button showAppsButton;
        ListView appListView;
        List<PackageInfo> installedApps;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Layout
            LinearLayout layout = new LinearLayout(this);
            layout.Orientation = Orientation.Vertical;

            // Buttons
            showAppsButton = new Button(this);
            showAppsButton.Text = "Apps";
            showAppsButton.Click += ShowAppsButton_Click;

            // Initialize appListView but set its visibility to Gone initially
            appListView = new ListView(this);
            appListView.Visibility = ViewStates.Gone;

            // Add the views to the layout
            layout.AddView(showAppsButton);
            layout.AddView(appListView);

            // Set the layout as the content view
            SetContentView(layout);
        }

        private async void ShowAppsButton_Click(object sender, System.EventArgs e)
        {
            // Toggle the visibility of appListView
            if (appListView.Visibility == ViewStates.Visible)
            {
                appListView.Visibility = ViewStates.Gone; // Hide the ListView
            }
            else
            {
                // Show a loading indicator while retrieving and processing apps
                ProgressDialog progressDialog = ProgressDialog.Show(this, "Please wait", "Loading...");

                await Task.Run(() =>
                {
                    installedApps = GetInstalledUserApps();
                });

                progressDialog.Dismiss();

                List<string> appNames = installedApps.Select(packageInfo => packageInfo.ApplicationInfo.LoadLabel(PackageManager).ToString()).ToList();
                ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, appNames);

                appListView.Adapter = adapter;
                appListView.Visibility = ViewStates.Visible; // Show the ListView
            }
        }

        private List<PackageInfo> GetInstalledUserApps()
        {
            PackageManager packageManager = PackageManager;
            List<PackageInfo> packages = packageManager.GetInstalledPackages(PackageInfoFlags.Activities).ToList();

            // Filter out system apps
            packages = packages.Where(package => (package.ApplicationInfo.Flags & ApplicationInfoFlags.System) == 0).ToList();

            return packages;
        }
    }
}
