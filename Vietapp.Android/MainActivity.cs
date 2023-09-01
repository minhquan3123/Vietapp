using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;

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

            // Nút
            showAppsButton = new Button(this);
            showAppsButton.Text = "Show Apps";
            showAppsButton.Click += ShowAppsButton_Click;

            // Tạo List
            appListView = new ListView(this);

            //Hiện nút với list
            layout.AddView(showAppsButton);
            layout.AddView(appListView);

            // 
            SetContentView(layout);
        }

        private void ShowAppsButton_Click(object sender, System.EventArgs e)
        {
            // Lấy app
            installedApps = GetInstalledApps();

            //Tạo view cho app lít
            List<string> appNames = installedApps.Select(packageInfo => packageInfo.ApplicationInfo.LoadLabel(PackageManager).ToString()).ToList();

            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, appNames);

            //tạo adapt
            appListView.Adapter = adapter;
        }

        private List<PackageInfo> GetInstalledApps()
        {
            PackageManager packageManager = PackageManager;
            List<PackageInfo> packages = packageManager.GetInstalledPackages(PackageInfoFlags.Activities).ToList();

            return packages;
        }
    }
}