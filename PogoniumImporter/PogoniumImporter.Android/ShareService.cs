using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Util;
using PogoniumImporter.PokemonData;
using Java.Lang;

namespace PogoniumImporter.Droid
{
    [Service]
    [Activity(Theme = "@style/Theme.Custom.Dialog")]
    class ShareService : Service
    {
        public const int ServiceNotificationId = 76778;

        private LinearLayout shareLayout;
        private IWindowManager windowManager;

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            string passcode = Helpers.Settings.PogoniumPasscode;
            string json = intent.GetStringExtra("json") ?? string.Empty;

            ImportedPokemon importedPokemon = null;

            try
            {
                importedPokemon = ImportedPokemon.Parse(json);
            }
            catch (ArgumentException)
            {
                Toast.MakeText(this, Resource.String.invalidJson, ToastLength.Short).Show();
                StopService(intent);
            }

            if (importedPokemon != null)
            {
                Notification notification = new Notification.Builder(this)
                    .SetContentTitle(Resources.GetString(Resource.String.appName))
                    .SetContentText(Resources.GetString(Resource.String.notificationText))
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetContentIntent(PendingIntent.GetActivity(Application.Context, 0, new Intent(), 0))
                    .SetOngoing(true)
                    .Build();

                StartForeground(ServiceNotificationId, notification);

                this.windowManager = GetSystemService(WindowService).JavaCast<IWindowManager>();

                WindowManagerLayoutParams shareLayoutParams = new WindowManagerLayoutParams(
                    WindowManagerLayoutParams.MatchParent,
                    WindowManagerLayoutParams.WrapContent,
                    WindowManagerTypes.Phone,
                    WindowManagerFlags.NotTouchModal,
                    Android.Graphics.Format.Transparent
                    );

                LayoutInflater inflater = GetSystemService(LayoutInflaterService).JavaCast<LayoutInflater>();
                this.shareLayout = inflater.Inflate(Resource.Layout.Share, null).JavaCast<LinearLayout>();
                shareLayoutParams.SoftInputMode = SoftInput.AdjustPan;
                shareLayoutParams.Gravity = GravityFlags.Center | GravityFlags.Top;

                this.windowManager.AddView(shareLayout, shareLayoutParams);

                TextView pokeName = this.shareLayout.FindViewById<TextView>(Resource.Id.resultsPokemonName);
                pokeName.Text = importedPokemon.Name;

                TextView levelText = this.shareLayout.FindViewById<TextView>(Resource.Id.resultsPokemonLevel);
                levelText.Text = string.Format("{0} {1:0.0}", Resources.GetString(Resource.String.level), importedPokemon.Level);

                TextView atkText = this.shareLayout.FindViewById<TextView>(Resource.Id.resultsAttack);
                atkText.Text = importedPokemon.Attack.ToString();

                TextView defText = this.shareLayout.FindViewById<TextView>(Resource.Id.resultsDefense);
                defText.Text = importedPokemon.Defense.ToString();

                TextView staText = this.shareLayout.FindViewById<TextView>(Resource.Id.resultsStamina);
                staText.Text = importedPokemon.Stamina.ToString();

                TextView ivText = this.shareLayout.FindViewById<TextView>(Resource.Id.resultsIv);
                ivText.Text = importedPokemon.Percent.ToString() + '%';

                Dictionary<string, PokemonMove> quickMoveDictionary = new Dictionary<string, PokemonMove>();
                List<string> quickMovesList = new List<string>();
                foreach (PokemonMove move in Pokemon.GetQuickMoves(importedPokemon.PokemonId.Value))
                {
                    string moveName = Pokemon.GetMoveString(move);
                    quickMoveDictionary.Add(moveName, move);
                    quickMovesList.Add(moveName);
                }                
                Spinner quickMoves = this.shareLayout.FindViewById<Spinner>(Resource.Id.quickMoveSpinner);
                ArrayAdapter<string> quickMoveAdapter = new ArrayAdapter<string>(quickMoves.Context, Resource.Layout.MoveSpinnerItem, quickMovesList);
                quickMoves.Adapter = quickMoveAdapter;
                quickMoves.SetSelection(quickMoveAdapter.GetPosition(quickMovesList[0]));

                Dictionary<string, PokemonMove> chargeMoveDictionary = new Dictionary<string, PokemonMove>();
                List<string> chargeMovesList = new List<string>();
                foreach (PokemonMove move in Pokemon.GetChargeMoves(importedPokemon.PokemonId.Value))
                {
                    string moveName = Pokemon.GetMoveString(move);
                    chargeMoveDictionary.Add(moveName, move);
                    chargeMovesList.Add(moveName);
                }
                Spinner chargeMoves = this.shareLayout.FindViewById<Spinner>(Resource.Id.chargeMoveSpinner);
                ArrayAdapter<string> chargeMoveAdapter = new ArrayAdapter<string>(chargeMoves.Context, Resource.Layout.MoveSpinnerItem, chargeMovesList);
                chargeMoves.Adapter = chargeMoveAdapter;
                chargeMoves.SetSelection(chargeMoveAdapter.GetPosition(chargeMovesList[0]));

                Button cancelButton = this.shareLayout.FindViewById<Button>(Resource.Id.cancelButton);
                cancelButton.Click += (object sender, EventArgs e) =>
                {
                    StopService(intent);
                };

                ProgressBar processingBar = this.shareLayout.FindViewById<ProgressBar>(Resource.Id.processingBar);
                Button importButton = this.shareLayout.FindViewById<Button>(Resource.Id.importButton);
                importButton.Click += async (object sender, EventArgs ev) =>
                {
                    processingBar.Visibility = ViewStates.Visible;
                    try
                    {
                        string selectedQuickMoveString = quickMoves.SelectedItem.ToString();
                        string selectedChargeMoveString = chargeMoves.SelectedItem.ToString();
                        importedPokemon.QuickMove = quickMoveDictionary[selectedQuickMoveString];
                        importedPokemon.ChargeMove = chargeMoveDictionary[selectedChargeMoveString];
                        bool updated = await importedPokemon.Import(Helpers.Settings.PogoniumPasscode);
                        Toast.MakeText(this, Resources.GetString(updated ? Resource.String.updatedPokemon : Resource.String.addedPokemon), ToastLength.Short).Show();
                        StopService(intent);
                    }
                    catch (System.Exception e)
                    {
                        Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                        alert.SetTitle(Resources.GetString(Resource.String.requestError));
                        alert.SetMessage(e.Message);
                        alert.SetPositiveButton(Resources.GetString(Resource.String.Dismiss), (senderAlert, args) => {
                        });
                        Dialog alertDialog = alert.Create();
                        alertDialog.Window.SetType(WindowManagerTypes.SystemAlert);
                        alertDialog.Show();
                    }
                    processingBar.Visibility = ViewStates.Invisible;

                };
            }

            return StartCommandResult.NotSticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override void OnDestroy()
        {
            if (shareLayout != null)
                windowManager.RemoveView(shareLayout);
            base.OnDestroy();
        }
    }
}