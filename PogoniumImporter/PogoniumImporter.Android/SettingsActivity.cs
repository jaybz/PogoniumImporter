using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Provider;
using System.Threading.Tasks;
using System.Net.Http;
using System.Json;

namespace PogoniumImporter.Droid
{
    [Activity(Label = "@string/settingsTitle", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/Theme.Custom")]
    public class SettingsActivity : AppCompatActivity
    {
        private const string ReleaseUrl = "https://api.github.com/repos/jaybz/PogoniumImporter/releases";
        private const string ReleasePrefix = "PogoniumImporter.Android";
        private const string ReleaseDownloadUrl = "https://github.com/jaybz/PogoniumImporter/releases";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Settings);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                if (!Settings.CanDrawOverlays(this))
                {
                    Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                    alert.SetTitle(Resources.GetString(Resource.String.dialogPermissionsTitle));
                    alert.SetMessage(Resources.GetString(Resource.String.dialogPermissionsMessage));
                    alert.SetPositiveButton(Resource.String.dialogProceed, (sender, args) =>
                    {
                        Intent myIntent = new Intent(Settings.ActionManageOverlayPermission);
                        StartActivity(myIntent);
                    });
                    alert.SetNegativeButton(Resource.String.dialogCancel, (sender, args) =>
                    {
                        Finish();
                    });
                    alert.Show();
                }
            }

            EditText passcode = FindViewById<EditText>(Resource.Id.passcode);
            passcode.Text = Helpers.Settings.PogoniumPasscode;
            passcode.TextChanged += (object sender, Android.Text.TextChangedEventArgs e) =>
            {
                Helpers.Settings.PogoniumPasscode = e.Text.ToString();
            };

#if DEBUG
            Button shareButtonOld = FindViewById<Button>(Resource.Id.testShareOld);
            shareButtonOld.Visibility = ViewStates.Visible;
            shareButtonOld.Click += (object sender, EventArgs e) =>
            {
                Intent shareIntent = new Intent();
                shareIntent.SetAction(Intent.ActionSend);
                shareIntent.PutExtra(Intent.ExtraText, "{\"PokemonId\":130,\"AtkMin\":12,\"AtkMax\":15,\"DefMin\":9,\"DefMax\":14,\"StamMin\":14,\"StamMax\":14,\"OverallPower\":87,\"Hp\":154,\"Cp\":2973,\"estimatedPokemonLevel\":34.5,\"candyName\":\"Magikarp\"}");
                shareIntent.SetType("application/pokemon-stats");
                shareIntent.SetFlags(ActivityFlags.NewTask);
                StartActivity(shareIntent);
            };

            Button shareButtonOldRefined = FindViewById<Button>(Resource.Id.testShareOldRefined);
            shareButtonOldRefined.Visibility = ViewStates.Visible;
            shareButtonOldRefined.Click += (object sender, EventArgs e) =>
            {
                Intent shareIntent = new Intent();
                shareIntent.SetAction(Intent.ActionSend);
                shareIntent.PutExtra(Intent.ExtraText, "{\"PokemonId\":130,\"AtkMin\":12,\"AtkMax\":12,\"DefMin\":14,\"DefMax\":14,\"StamMin\":14,\"StamMax\":14,\"OverallPower\":87,\"Hp\":154,\"Cp\":2973,\"estimatedPokemonLevel\":34.5,\"candyName\":\"Magikarp\"}");
                shareIntent.SetType("application/pokemon-stats");
                shareIntent.SetFlags(ActivityFlags.NewTask);
                StartActivity(shareIntent);
            };

            Button shareButtonNew = FindViewById<Button>(Resource.Id.testShareNew);
            shareButtonNew.Visibility = ViewStates.Visible;
            shareButtonNew.Click += (object sender, EventArgs e) =>
            {
                Intent shareIntent = new Intent();
                shareIntent.SetAction(Intent.ActionSend);
                shareIntent.PutExtra(Intent.ExtraText, "{\"PokemonId\":130,\"AtkMin\":12,\"AtkMax\":15,\"DefMin\":9,\"DefMax\":14,\"StamMin\":14,\"StamMax\":14,\"OverallPower\":87,\"Hp\":154,\"Cp\":2973,\"uniquePokemon\":\"235kg 0 O 6 92m\",\"estimatedPokemonLevel\":34.5,\"candyName\":\"Magikarp\",\"ivCombinations\":[{\"Atk\":15,\"Def\":9,\"Stam\":14,\"Percent\":84},{\"Atk\":12,\"Def\":14,\"Stam\":14,\"Percent\":89}]}");
                shareIntent.SetType("application/pokemon-stats");
                shareIntent.SetFlags(ActivityFlags.NewTask);
                StartActivity(shareIntent);
            };
#endif
            ProgressDialog progress = new ProgressDialog(this);
            progress.Indeterminate = true;
            progress.SetProgressStyle(ProgressDialogStyle.Horizontal);
            progress.Max = 1;
            progress.SetMessage(Resources.GetString(Resource.String.checkingGameMaster));
            progress.SetCancelable(false);
            progress.Show();

            Task.Run(async () =>
            {
                int total = 0;

                await DatabaseHelper.Initialize().ConfigureAwait(false);
                await DatabaseHelper.RefreshData(new Progress<int>((int status) =>
                {
                    RunOnUiThread(() =>
                    {
                        if(total == 0)
                        {
                            total = status;
                            progress.Indeterminate = false;
                            progress.Progress = 0;
                            progress.Max = total;
                            progress.SetMessage(Resources.GetString(Resource.String.parsingGameMaster));
                        }
                        else if((total + 1) == status)
                        {
                            progress.Indeterminate = true;
                            progress.SetMessage(Resources.GetString(Resource.String.savingGameMaster));
                        }
                        else if ((total + 2) == status)
                        {
                            progress.Indeterminate = true;
                            progress.SetMessage(Resources.GetString(Resource.String.done));
                        }
                        else
                        {
                            progress.Progress = status;
                        }
                    });
                })).ConfigureAwait(false);
                RunOnUiThread(() =>
                {
                    progress.Indeterminate = true;
                    progress.SetMessage(Resources.GetString(Resource.String.checkingForUpdates));
                });

                await AppUpdateCheck().ConfigureAwait(false);

                RunOnUiThread(() =>
                {
                    progress.Dismiss();
                });
            });

        }

        private async Task AppUpdateCheck()
        {
            string currentVersion = PackageManager.GetPackageInfo(PackageName, 0).VersionName;

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", Resources.GetString(Resource.String.appName));
            HttpResponseMessage response = await client.GetAsync(ReleaseUrl).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JsonArray releases = (JsonArray)JsonValue.Parse(json);

            foreach (JsonValue release in releases)
            {
                string version = release["tag_name"];
                if (String.Compare(currentVersion, version) < 0)
                {
                    foreach (JsonValue asset in (JsonArray)release["assets"])
                    {
                        string name = asset["name"];
                        if (name.Substring(0, ReleasePrefix.Length).Equals(ReleasePrefix))
                        {
                            RunOnUiThread(() =>
                            {
                                Dialog alertDialog = null;

                                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(new ContextThemeWrapper(this, Resource.Style.Theme_AppCompat_Light_Dialog));
                                alert.SetTitle(Resources.GetString(Resource.String.updateTitle));
                                alert.SetMessage(Resources.GetString(Resource.String.updateMessage));

                                alert.SetPositiveButton(Resources.GetString(Resource.String.updateView), (senderAlert, args) =>
                                {
                                    alertDialog.Dismiss();
                                    StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse(release["html_url"])));
                                });
                                alert.SetNegativeButton(Resources.GetString(Resource.String.updateClose), (senderAlert, args) =>
                                {
                                    alertDialog.Dismiss();
                                });

                                alertDialog = alert.Create();
                                alertDialog.Window.SetType(WindowManagerTypes.SystemAlert);
                                alertDialog.Show();
                            });

                            return;
                        }
                    }
                }
                else
                    return;
            }
        }
    }
}
