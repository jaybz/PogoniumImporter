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

namespace PogoniumImporter.Droid
{
    [Activity(Label = "@string/settingsTitle", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/Theme.Custom")]
    public class SettingsActivity : AppCompatActivity
    {
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
            progress.SetMessage("Checking GameMaster");
            progress.SetCancelable(false);
            progress.Show();

            Task.Run(async () =>
            {
                int total = 0;

                await DatabaseHelper.Initialize();
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
                            progress.SetMessage("Parsing GameMaster");
                        }
                        else if((total + 1) == status)
                        {
                            progress.Indeterminate = true;
                            progress.SetMessage("Saving new GameMaster");
                        }
                        else if ((total + 2) == status)
                        {
                            progress.Indeterminate = true;
                            progress.SetMessage("Done");
                        }
                        else
                        {
                            progress.Progress = status;
                        }
                    });
                }));
                RunOnUiThread(() =>
                {
                    progress.Dismiss();
                });
            });
        }
    }
}
