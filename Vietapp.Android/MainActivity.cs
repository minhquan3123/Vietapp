using System;
using Android.App;
using Android.Content;
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

        Button saveTimeButton;
        EditText hoursEditText;
        EditText minutesEditText;
        EditText secondsEditText;
        ListView savedTimesListView;
        List<string> savedTimes;

        Button toggleSavedTimesButton; // Added button for toggling saved times
        bool savedTimesVisible = false; // Added boolean variable to control visibility
        ArrayAdapter<string> savedTimesAdapter; // Adapter for saved times list view

        ISharedPreferences sharedPreferences;

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

            // Initialize appListView but set its visibility to Gone initially
            appListView = new ListView(this);
            appListView.Visibility = ViewStates.Gone;

            // Create a horizontal LinearLayout for Save Time and Show Saved Times buttons
            LinearLayout buttonsLayout = new LinearLayout(this);
            buttonsLayout.Orientation = Orientation.Horizontal;

            // Button for saving time
            saveTimeButton = new Button(this);
            saveTimeButton.Text = "Lưu thời gian";
            saveTimeButton.Click += SaveTimeButton_Click;


            // Add the Save Time and Show Saved Times buttons to the horizontal layout
            buttonsLayout.AddView(saveTimeButton);
            buttonsLayout.AddView(toggleSavedTimesButton);

            // Create a horizontal LinearLayout for time input
            LinearLayout timeInputLayout = new LinearLayout(this);
            timeInputLayout.Orientation = Orientation.Horizontal;

            // EditText fields for hours, minutes, and seconds
            hoursEditText = new EditText(this);
            hoursEditText.Hint = "Giờ";
            minutesEditText = new EditText(this);
            minutesEditText.Hint = "Phút";
            secondsEditText = new EditText(this);
            secondsEditText.Hint = "Giây";

            // Add the EditText fields to the horizontal layout
            timeInputLayout.AddView(hoursEditText);
            timeInputLayout.AddView(minutesEditText);
            timeInputLayout.AddView(secondsEditText);

            // Initialize the list of saved times
            savedTimes = new List<string>();

            // Retrieve shared preferences
            sharedPreferences = GetSharedPreferences("SavedTimes", FileCreationMode.Private);

            // Load saved times from shared preferences
            LoadSavedTimes();

            // Add the button for toggling saved times
            toggleSavedTimesButton = new Button(this);
            toggleSavedTimesButton.Text = "Ẩn/Hiện thời gian đã lưu";
            toggleSavedTimesButton.Click += ToggleSavedTimesButton_Click;

            // Create an adapter for the saved times list view
            savedTimesAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, savedTimes);

            // ListView for displaying saved times
            savedTimesListView = new ListView(this);
            savedTimesListView.Adapter = savedTimesAdapter;
            savedTimesListView.Visibility = ViewStates.Gone;

            // Add the views to the layout
            layout.AddView(showAppsButton);
            layout.AddView(appListView);
            layout.AddView(buttonsLayout); // Add the horizontal layout for buttons
            layout.AddView(timeInputLayout); // Add the horizontal layout for time input
            layout.AddView(toggleSavedTimesButton); // Add the button for toggling saved times
            layout.AddView(savedTimesListView);

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
                ProgressDialog progressDialog = ProgressDialog.Show(this, "Vui lòng chờ", "Đang Tải...");

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

        private void SaveTimeButton_Click(object sender, EventArgs e)
        {
            string hoursText = hoursEditText.Text.Trim();
            string minutesText = minutesEditText.Text.Trim();
            string secondsText = secondsEditText.Text.Trim();

            // Validate and parse the input
            if (int.TryParse(hoursText, out int hours) && int.TryParse(minutesText, out int minutes) && int.TryParse(secondsText, out int seconds))
            {
                TimeSpan timeSpan = new TimeSpan(hours, minutes, seconds);
                string formattedTime = timeSpan.ToString(@"hh\:mm\:ss");

                savedTimes.Add(formattedTime);
                UpdateSavedTimesListView();

                // Save the updated list of times to shared preferences
                SaveSavedTimes();

                // Clear the input fields
                hoursEditText.Text = "";
                minutesEditText.Text = "";
                secondsEditText.Text = "";
            }
            else
            {
                // Display an error message if the input is not valid
                Toast.MakeText(this, "Hãy nhập thời gian hợp lệ", ToastLength.Short).Show();
            }
        }

        private void UpdateSavedTimesListView()
        {
            savedTimesAdapter.NotifyDataSetChanged(); // Update the ListView adapter
        }

        private void SaveSavedTimes()
        {
            // Serialize the list of saved times to a string and save it to shared preferences
            ISharedPreferencesEditor editor = sharedPreferences.Edit();
            string savedTimesString = string.Join(",", savedTimes);
            editor.PutString("SavedTimes", savedTimesString);
            editor.Apply();
        }

        private void LoadSavedTimes()
        {
            // Retrieve the saved times from shared preferences and deserialize them
            string savedTimesString = sharedPreferences.GetString("SavedTimes", null);

            if (!string.IsNullOrEmpty(savedTimesString))
            {
                savedTimes = savedTimesString.Split(',').ToList();
            }
        }

        private void ToggleSavedTimesButton_Click(object sender, EventArgs e)
        {
            // Toggle the visibility of saved times
            savedTimesVisible = !savedTimesVisible;
            savedTimesListView.Visibility = savedTimesVisible ? ViewStates.Visible : ViewStates.Gone;
        }
    }
}
