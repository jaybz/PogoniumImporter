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
using Android.Graphics.Drawables;
using Android.Graphics;

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

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            string passcode = Helpers.Settings.PogoniumPasscode;

            if(string.IsNullOrEmpty(passcode))
            {
                ShowAlert(Resources.GetString(Resource.String.importError), Resources.GetString(Resource.String.noPasscodeError), (senderAlert, args) => {
                    StopService(intent);
                });
                return StartCommandResult.NotSticky;
            }

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
                    Format.Transparent
                    );

                LayoutInflater inflater = GetSystemService(LayoutInflaterService).JavaCast<LayoutInflater>();
                this.shareLayout = inflater.Inflate(Resource.Layout.Share, null).JavaCast<LinearLayout>();
                shareLayoutParams.SoftInputMode = SoftInput.AdjustPan;
                shareLayoutParams.Gravity = GravityFlags.Center | GravityFlags.Top;

                this.windowManager.AddView(shareLayout, shareLayoutParams);

                EditText pokeName = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsPokemonName);
                pokeName.Text = importedPokemon.Name;
                pokeName.Click += (object sender, EventArgs ev) =>
                {
                    EditTextHandler(pokeName);
                };

                EditText levelText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsPokemonLevel);
                levelText.Text = string.Format("{0:0.0}", importedPokemon.Level);
                List<IInputFilter> levelFilters = new List<IInputFilter>(levelText.GetFilters());
                levelFilters.Add(new PokemonLevelFilter(1.0f, 39.0f));
                levelText.SetFilters(levelFilters.ToArray<IInputFilter>());
                levelText.TextChanged += (object sender, Android.Text.TextChangedEventArgs ev) =>
                {
                    float i = 0;
                    float.TryParse(levelText.Text, out i);
                    if (i < 1.0f)
                        levelText.Text = "1.0";
                    else if (i > 39.0f)
                        levelText.Text = "39.0";
                    else
                    {
                        string newText = string.Format("{0:0.0}", i);
                        if (levelText.Text != newText)
                            levelText.Text = newText;
                    }
                };
                levelText.Click += (object sender, EventArgs ev) =>
                {
                    EditTextHandler(levelText);
                };

                EditText atkText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsAttack);
                atkText.Text = importedPokemon.Attack.ToString();
                List<IInputFilter> atkFilters = new List<IInputFilter>(atkText.GetFilters());
                atkFilters.Add(new MinMaxFilter(0, 15));
                atkText.SetFilters(atkFilters.ToArray<IInputFilter>());
                atkText.TextChanged += (object sender, Android.Text.TextChangedEventArgs ev) =>
                {
                    int i = 0;
                    int.TryParse(atkText.Text, out i);
                    if (i < 0)
                        atkText.Text = "0";
                    else if (i > 15)
                        atkText.Text = "15";
                    else
                    {
                        string newText = i.ToString();
                        if (atkText.Text != newText)
                            atkText.Text = newText;
                    }
                };
                atkText.Click += (object sender, EventArgs ev) =>
                {
                    EditTextHandler(atkText);
                };

                EditText defText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsDefense);
                defText.Text = importedPokemon.Defense.ToString();
                List<IInputFilter> defFilters = new List<IInputFilter>(defText.GetFilters());
                defFilters.Add(new MinMaxFilter(0, 15));
                defText.SetFilters(defFilters.ToArray<IInputFilter>());
                defText.TextChanged += (object sender, Android.Text.TextChangedEventArgs ev) =>
                {
                    int i = 0;
                    int.TryParse(defText.Text, out i);
                    if (i < 0)
                        defText.Text = "0";
                    else if (i > 15)
                        defText.Text = "15";
                    else
                    {
                        string newText = i.ToString();
                        if (defText.Text != newText)
                            defText.Text = newText;
                    }
                };
                defText.Click += (object sender, EventArgs ev) =>
                {
                    EditTextHandler(defText);
                };

                EditText staText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsStamina);
                staText.Text = importedPokemon.Stamina.ToString();
                List<IInputFilter> staFilters = new List<IInputFilter>(staText.GetFilters());
                staFilters.Add(new MinMaxFilter(0, 15));
                staText.SetFilters(staFilters.ToArray<IInputFilter>());
                staText.TextChanged += (object sender, Android.Text.TextChangedEventArgs ev) =>
                {
                    int i = 0;
                    int.TryParse(staText.Text, out i);
                    if (i < 0)
                        staText.Text = "0";
                    else if (i > 15)
                        staText.Text = "15";
                    else
                    {
                        string newText = i.ToString();
                        if (staText.Text != newText)
                            staText.Text = newText;
                    }
                };
                staText.Click += (object sender, EventArgs ev) =>
                {
                    EditTextHandler(staText);
                };

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
                        ShowAlert(Resources.GetString(Resource.String.importError), e.Message, (senderAlert, args) => { });

                    }
                    processingBar.Visibility = ViewStates.Gone;
                };

                // hacky way to not block UI
                importButton.TextChanged += async (object sender, Android.Text.TextChangedEventArgs ev) =>
                {
                    try
                    {
                        await importedPokemon.RetrieveData(passcode);
                    }
                    catch (System.Exception e)
                    {
                        ShowAlert(Resources.GetString(Resource.String.importError), e.Message, (senderAlert, args) => {
                            StopService(intent);
                        });
                    }
                    processingBar.Visibility = ViewStates.Gone;
                    importButton.Enabled = true;
                    pokeName.Text = importedPokemon.Name;
                };
                importButton.Text = importButton.Text;
            }

            return StartCommandResult.NotSticky;
        }

        private void ShowAlert(string title, string message, EventHandler<DialogClickEventArgs> onClick)
        {
            Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(new ContextThemeWrapper(this, Resource.Style.Theme_AppCompat_Light_Dialog));
            alert.SetTitle(title);
            alert.SetMessage(message);
            alert.SetPositiveButton(Resources.GetString(Resource.String.Dismiss), onClick);
            Dialog alertDialog = alert.Create();
            alertDialog.Window.SetType(WindowManagerTypes.SystemAlert);
            alertDialog.Show();
        }

        private void EditTextHandler(EditText parent)
        {
            Dialog dialog = new Dialog(this);
            dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            dialog.SetContentView(Resource.Layout.EditTextDialog);
            dialog.SetTitle("");
            dialog.Window.ClearFlags(WindowManagerFlags.DimBehind | WindowManagerFlags.AltFocusableIm);
            dialog.Window.Attributes.Gravity = GravityFlags.Bottom | GravityFlags.FillHorizontal;
            dialog.Window.Attributes.Width = WindowManagerLayoutParams.MatchParent;
            dialog.Window.Attributes.Height = WindowManagerLayoutParams.WrapContent;
            dialog.Window.SetSoftInputMode(SoftInput.AdjustPan | SoftInput.StateAlwaysVisible);
            dialog.Window.SetType(WindowManagerTypes.Phone);
            dialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.White));

            Button dialogButton = dialog.FindViewById<Button>(Resource.Id.ok);
            dialogButton.Click += (object sender, EventArgs ev) =>
            {
                dialog.Dismiss();
            };

            int currentY = 0;
            EditText input = dialog.FindViewById<EditText>(Resource.Id.editTextInput);
            input.Text = parent.Text;
            input.InputType = parent.InputType;
            input.SetFilters(parent.GetFilters());
            input.ViewTreeObserver.GlobalLayout += (object sender, EventArgs e) =>
            { // Hack to detect the user closing the soft keyboard
                int[] loc = new int[2];
                input.GetLocationOnScreen(loc);

                if (currentY == 0)
                    currentY = loc[1];
                else if (currentY < loc[1])
                    dialog.Dismiss();
                else
                    currentY = loc[1];
            };
            input.TextChanged += (object sender, TextChangedEventArgs ev) =>
            {
                parent.Text = input.Text;
            };

            dialog.Show();
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

    public class PokemonLevelFilter : Java.Lang.Object, IInputFilter
    {
        private float minLevel;
        private float maxLevel;

        public PokemonLevelFilter(float minLevel, float maxLevel)
        {
            this.minLevel = minLevel;
            this.maxLevel = maxLevel;
        }

        public ICharSequence FilterFormatted(ICharSequence source, int start, int end, ISpanned dest, int dstart, int dend)
        {
            string replaced = dest.ToString().Substring(dstart, dend - dstart);
            string val = dest.ToString().Substring(0, dstart) + source.ToString().Substring(start, end - start) + dest.ToString().Substring(dend);

            if (val.Length == 0)
                return null;

            float input;

            try
            {
                input = float.Parse(val);
            }
            catch (System.Exception e)
            {
                if (e is OverflowException || e is FormatException)
                {
                    return new Java.Lang.String(replaced);
                }

                throw;
            }

            if (input < minLevel || input > maxLevel)
            {
                return new Java.Lang.String(replaced);
            }

            float doubleValue = input * 2.0f;
            if(System.Math.Floor(doubleValue) != doubleValue)
            {
                return new Java.Lang.String(replaced);
            }

            return null;
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
            string replaced = dest.ToString().Substring(dstart, dend - dstart);
            string val = dest.ToString().Substring(0, dstart) + source.ToString().Substring(start, end - start) + dest.ToString().Substring(dend);

            if (val.Length == 0)
                return null;

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
                        return new Java.Lang.String(replaced);
                    }

                    throw;
                }

                if (input < minInt || input > maxInt)
                {
                    return new Java.Lang.String(replaced);
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
                        return new Java.Lang.String(replaced);
                    }

                    throw;
                }

                if (input < minFloat || input > maxFloat)
                {
                    return new Java.Lang.String(replaced);
                }

                return null;
            }

            throw new NotImplementedException();
        }
    }
}