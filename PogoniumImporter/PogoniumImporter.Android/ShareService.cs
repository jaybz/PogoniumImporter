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
using Android.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PogoniumImporter.Droid
{
    [Service]
    [Activity(Theme = "@style/Theme.Custom.Dialog")]
    class ShareService : Service
    {
        public const int ServiceNotificationId = 76778;

        private LinearLayout shareLayout;
        private IWindowManager windowManager;

        private ImportedPokemon importedPokemon = null;

        private ProgressBar processingBar;
        private Button importButton;
        private EditText pokeName;

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            string passcode = Helpers.Settings.PogoniumPasscode;
            string json = intent.GetStringExtra("json") ?? string.Empty;

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

                this.pokeName = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsPokemonName);
                pokeName.Text = importedPokemon.Name;

                EditText levelText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsPokemonLevel);
                levelText.Text = string.Format("{0:0.0}", importedPokemon.Level);
                List<IInputFilter> levelFilters = new List<IInputFilter>(levelText.GetFilters());
                levelFilters.Add(new MinMaxFilter(0.0f, 40.0f));
                levelText.SetFilters(levelFilters.ToArray<IInputFilter>());

                EditText atkText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsAttack);
                atkText.Text = importedPokemon.Attack.ToString();
                List<IInputFilter> atkFilters = new List<IInputFilter>(atkText.GetFilters());
                atkFilters.Add(new MinMaxFilter(0, 15));
                atkText.SetFilters(atkFilters.ToArray<IInputFilter>());

                EditText defText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsDefense);
                defText.Text = importedPokemon.Defense.ToString();
                List<IInputFilter> defFilters = new List<IInputFilter>(defText.GetFilters());
                defFilters.Add(new MinMaxFilter(0, 15));
                defText.SetFilters(defFilters.ToArray<IInputFilter>());

                EditText staText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsStamina);
                staText.Text = importedPokemon.Stamina.ToString();
                List<IInputFilter> staFilters = new List<IInputFilter>(staText.GetFilters());
                staFilters.Add(new MinMaxFilter(0, 15));
                staText.SetFilters(staFilters.ToArray<IInputFilter>());

                TextView ivText = this.shareLayout.FindViewById<TextView>(Resource.Id.resultsIv);
                ComputeIVPercent(atkText, defText, staText, ivText);

                EventHandler<TextChangedEventArgs> ivHandler = (sender, e) =>
                {
                    ComputeIVPercent(atkText, defText, staText, ivText);
                };
                atkText.TextChanged += ivHandler;
                defText.TextChanged += ivHandler;
                staText.TextChanged += ivHandler;

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

                this.processingBar = this.shareLayout.FindViewById<ProgressBar>(Resource.Id.processingBar);
                this.importButton = this.shareLayout.FindViewById<Button>(Resource.Id.importButton);

                importButton.Click += async (object sender, EventArgs ev) =>
                {
                    processingBar.Visibility = ViewStates.Visible;
                    try
                    {
                        string selectedQuickMoveString = quickMoves.SelectedItem.ToString();
                        string selectedChargeMoveString = chargeMoves.SelectedItem.ToString();
                        importedPokemon.QuickMove = quickMoveDictionary[selectedQuickMoveString];
                        importedPokemon.ChargeMove = chargeMoveDictionary[selectedChargeMoveString];

                        importedPokemon.Name = pokeName.Text;
                        importedPokemon.Level = float.Parse(levelText.Text);
                        importedPokemon.Attack = int.Parse(atkText.Text);
                        importedPokemon.Defense = int.Parse(defText.Text);
                        importedPokemon.Stamina = int.Parse(staText.Text);

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
                    processingBar.Visibility = ViewStates.Gone;
                };

                // hacky way to not block UI
                importButton.TextChanged += async (object sender, Android.Text.TextChangedEventArgs e) =>
                {
                    await importedPokemon.RetrieveData(passcode);
                    processingBar.Visibility = ViewStates.Gone;
                    importButton.Enabled = true;
                    pokeName.Text = importedPokemon.Name;
                };
                importButton.Text = importButton.Text;
            }

            return StartCommandResult.NotSticky;
        }

        private void ComputeIVPercent(TextView atkText, TextView defText, TextView staText, TextView ivText)
        {
            int atk, def, sta;

            if (
                int.TryParse(atkText.Text, out atk) &&
                int.TryParse(defText.Text, out def) &&
                int.TryParse(staText.Text, out sta))
            {
                ivText.Text = string.Format("{0}%", ImportedPokemon.ComputeIVPercent(atk, def, sta));
            }
            else
            {
                ivText.Text = string.Empty;
            }
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

    public class MinMaxFilter : Java.Lang.Object, IInputFilter
    {
        private Type filterType;
        private int minInt;
        private int maxInt;
        private float minFloat;
        private float maxFloat;

        public MinMaxFilter(int min, int max)
        {
            filterType = typeof(int);
            minInt = min;
            maxInt = max;
        }

        public MinMaxFilter(float min, float max)
        {
            filterType = typeof(float);
            minFloat = min;
            maxFloat = max;
        }

        public ICharSequence FilterFormatted(ICharSequence source, int start, int end, ISpanned dest, int dstart, int dend)
        {
            string val = dest.ToString().Insert(dstart, source.ToString());

            if (filterType == typeof(int))
            {
                int input;

                try
                {
                    input = int.Parse(val);
                }
                catch (System.Exception e)
                {
                    if (e is OverflowException || e is FormatException) {
                        return new Java.Lang.String(string.Empty);
                    }

                    throw;
                }

                if (input < minInt || input > maxInt)
                {
                    return new Java.Lang.String(string.Empty);
                }

                return null;
            }
            else if (filterType == typeof(float))
            {
                float input;

                try
                {
                    input = float.Parse(val);
                }
                catch (System.Exception e)
                {
                    if (e is OverflowException || e is FormatException)
                    {
                        return new Java.Lang.String(string.Empty);
                    }

                    throw;
                }

                if (input < minFloat || input > maxFloat)
                {
                    return new Java.Lang.String(string.Empty);
                }

                return null;
            }

            throw new NotImplementedException();
        }
    }
}