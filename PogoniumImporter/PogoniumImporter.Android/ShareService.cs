﻿using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using PogoniumImporter.PokemonData;
using Java.Lang;
using Android.Text;
using System.Threading.Tasks;
using Android.Graphics.Drawables;
using Android.Graphics;
using static Android.Widget.TextView;

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

        private TextView ivText;
        private EditText pokeName, levelText, atkText, defText, staText;
        private Spinner quickMoves, chargeMoves;
        private Button importButton, cancelButton;
        private ProgressBar processingBar;
        private ArrayAdapter<string> quickMoveAdapter, chargeMoveAdapter;
        private Dictionary<string, Move> quickMoveDictionary = new Dictionary<string, Move>();
        private Dictionary<string, Move> chargeMoveDictionary = new Dictionary<string, Move>();

        private Intent intent;

        private Handler mainThread = new Handler();

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            this.intent = intent;

            string passcode = Helpers.Settings.PogoniumPasscode;
            if (string.IsNullOrEmpty(passcode))
            {
                ShowAlert(Resource.String.importError, Resource.String.noPasscodeError, (senderAlert, args) =>
                {
                    StopSelf();
                });
            }
            else
            {
                Task.Run(async () =>
                {
                    await DatabaseHelper.Initialize().ConfigureAwait(false);
                });

                string json = intent.GetStringExtra("json") ?? string.Empty;
                try
                {
                    importedPokemon = ImportedPokemon.Parse(json);
                }
                catch (ArgumentException)
                {
                    Toast.MakeText(this, Resource.String.invalidJson, ToastLength.Short).Show();
                    StopSelf();
                    return StartCommandResult.NotSticky;
                }

                InitializeOverlay();

                Notification notification = new Notification.Builder(this)
                    .SetContentTitle(Resources.GetString(Resource.String.appName))
                    .SetContentText(Resources.GetString(Resource.String.notificationText))
                    .SetSmallIcon(Resource.Drawable.Icon)
                    .SetContentIntent(PendingIntent.GetActivity(Application.Context, 0, new Intent(), 0))
                    .SetOngoing(true)
                    .Build();

                StartForeground(ServiceNotificationId, notification);

                // Set this here to allow exiting the dialog when RetrieveData is stuck
                cancelButton.Click += (object sender, EventArgs e) =>
                {
                    StopSelf();
                };

                Task.Run(async () =>
                {
                    try
                    {
                        await importedPokemon.RetrieveData(passcode).ConfigureAwait(false);
                    }
                    catch (System.Exception e)
                    {
                        mainThread.Post(() =>
                        {
                            HideLayout();
                            ShowAlert(Resource.String.importError, e.Message, (senderAlert, args) =>
                            {
                                StopSelf();
                            });
                        });
                    }

                    mainThread.Post(() =>
                    {
                        FillData();
                        SetInputFilters();
                        SetEventHandlers();
                        importButton.Enabled = true;
                        processingBar.Visibility = ViewStates.Gone;
                    });
                });
            }

            return StartCommandResult.NotSticky;
        }

        private void InitializeOverlay()
        {
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

            pokeName = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsPokemonName);
            levelText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsPokemonLevel);
            atkText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsAttack);
            defText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsDefense);
            staText = this.shareLayout.FindViewById<EditText>(Resource.Id.resultsStamina);
            ivText = this.shareLayout.FindViewById<TextView>(Resource.Id.resultsIv);

            quickMoves = this.shareLayout.FindViewById<Spinner>(Resource.Id.quickMoveSpinner);
            chargeMoves = this.shareLayout.FindViewById<Spinner>(Resource.Id.chargeMoveSpinner);
            quickMoveAdapter = new ArrayAdapter<string>(quickMoves.Context, Resource.Layout.MoveSpinnerItem);
            chargeMoveAdapter = new ArrayAdapter<string>(chargeMoves.Context, Resource.Layout.MoveSpinnerItem);
            quickMoves.Adapter = quickMoveAdapter;
            chargeMoves.Adapter = chargeMoveAdapter;

            cancelButton = this.shareLayout.FindViewById<Button>(Resource.Id.cancelButton);
            processingBar = this.shareLayout.FindViewById<ProgressBar>(Resource.Id.processingBar);
            importButton = this.shareLayout.FindViewById<Button>(Resource.Id.importButton);
        }

        private void FillData()
        {
            pokeName.Text = importedPokemon.Name;
            levelText.Text = string.Format("{0:0.0}", importedPokemon.Level);
            atkText.Text = importedPokemon.Attack.ToString();
            defText.Text = importedPokemon.Defense.ToString();
            staText.Text = importedPokemon.Stamina.ToString();
            ivText.Text = ComputeIVPercent(atkText.Text, defText.Text, staText.Text);

            List<string> quickMovesList = new List<string>();
            quickMoveDictionary.Clear();
            Pokemon pokemon = null;

            Task.Run(async () =>
            {
                pokemon = await GameMaster.GetPokemon(importedPokemon.PokemonId.Value).ConfigureAwait(false);
            }).Wait();

            foreach (Move move in pokemon.QuickMoves)
            {
                string moveName = move.FriendlyName;
                quickMoveDictionary.Add(moveName, move);
                quickMovesList.Add(moveName);
            }
            quickMoveAdapter.Clear();
            quickMoveAdapter.AddAll(quickMovesList);

            List<string> chargeMovesList = new List<string>();
            chargeMoveDictionary.Clear();
            foreach (Move move in pokemon.ChargeMoves)
            {
                string moveName = move.FriendlyName;
                chargeMoveDictionary.Add(moveName, move);
                chargeMovesList.Add(moveName);
            }
            chargeMoveAdapter.Clear();
            chargeMoveAdapter.AddAll(chargeMovesList);

            chargeMoves.SetSelection(chargeMoveAdapter.GetPosition(importedPokemon.ChargeMove.FriendlyName));
            quickMoves.SetSelection(quickMoveAdapter.GetPosition(importedPokemon.QuickMove.FriendlyName));
        }

        private void SetEventHandlers()
        {
            pokeName.Click += EditTextHandler;
            levelText.Click += EditTextHandler;
            atkText.Click += EditTextHandler;
            defText.Click += EditTextHandler;
            staText.Click += EditTextHandler;
            levelText.TextChanged += LevelTextChangedHandler;
            atkText.TextChanged += IvTextChangedHandler;
            defText.TextChanged += IvTextChangedHandler;
            staText.TextChanged += IvTextChangedHandler;

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

                    bool updated = await importedPokemon.Import(Helpers.Settings.PogoniumPasscode).ConfigureAwait(false);
                    mainThread.Post(() =>
                    {
                        Toast.MakeText(this, Resources.GetString(updated ? Resource.String.updatedPokemon : Resource.String.addedPokemon), ToastLength.Short).Show();
                    });
                    StopSelf();
                }
                catch (System.Exception e)
                {
                    mainThread.Post(() =>
                    {
                        ShowAlert(Resource.String.importError, e.Message, (senderAlert, args) => { });
                    });
                }
                mainThread.Post(() =>
                {
                    processingBar.Visibility = ViewStates.Gone;
                });
            };
        }

        private void SetInputFilters()
        {
            AddInputFilter(levelText, new PokemonLevelFilter(1.0f, 39.0f));
            AddInputFilter(atkText, new MinMaxFilter(0, 15));
            AddInputFilter(defText, new MinMaxFilter(0, 15));
            AddInputFilter(staText, new MinMaxFilter(0, 15));
        }

        private void AddInputFilter(EditText editText, IInputFilter filter)
        {
            List<IInputFilter> currentFilters = editText.GetFilters().ToList();
            currentFilters.Add(filter);
            editText.SetFilters(currentFilters.ToArray<IInputFilter>());
        }

        private void ShowAlert(int titleId, int messageId, EventHandler<DialogClickEventArgs> onClick)
        {
            ShowAlert(titleId, Resources.GetString(messageId), onClick);
        }

        private void ShowAlert(int titleId, string message, EventHandler<DialogClickEventArgs> onClick)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(new ContextThemeWrapper(this, Resource.Style.Theme_AppCompat_Light_Dialog));
            alert.SetTitle(titleId);
            alert.SetMessage(message);
            alert.SetPositiveButton(Resources.GetString(Resource.String.Dismiss), onClick);
            Dialog alertDialog = alert.Create();
            alertDialog.Window.SetType(WindowManagerTypes.SystemAlert);
            alertDialog.Show();
        }

        private void LevelTextChangedHandler(object sender, TextChangedEventArgs e)
        {
            if (!(sender is EditText))
                return;

            EditText parent = (EditText)sender;

            float i = 0;
            float.TryParse(parent.Text, out i);
            if (i < 1.0f)
                parent.Text = "1.0";
            else if (i > 39.0f)
                parent.Text = "39.0";
            else
            {
                string newText = string.Format("{0:0.0}", i);
                if (parent.Text != newText)
                    parent.Text = newText;
            }
        }

        private void IvTextChangedHandler(object sender, TextChangedEventArgs e)
        {
            if (!(sender is EditText))
                return;

            EditText parent = (EditText)sender;

            int i = 0;
            int.TryParse(parent.Text, out i);
            if (i < 0)
                parent.Text = "0";
            else if (i > 15)
                parent.Text = "15";
            else
            {
                string newText = i.ToString();
                if (parent.Text != newText)
                    parent.Text = newText;
            }

            ivText.Text = ComputeIVPercent(atkText.Text, defText.Text, staText.Text);
        }

        private void EditTextHandler(object sender, EventArgs eventArgs)
        {
            if (!(sender is EditText))
                return;

            EditText parent = (EditText)sender;

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
            dialogButton.Click += (object s, EventArgs e) =>
            {
                dialog.Dismiss();
            };

            int currentY = 0;
            EditText input = dialog.FindViewById<EditText>(Resource.Id.editTextInput);
            string originalText = parent.Text;
            input.Text = originalText;
            input.InputType = parent.InputType;
            input.SetFilters(parent.GetFilters());
            input.ViewTreeObserver.GlobalLayout += (object s, EventArgs e) =>
            { // Hack to detect the user closing the soft keyboard
                int[] loc = new int[2];
                input.GetLocationOnScreen(loc);

                if (currentY == 0)
                    currentY = loc[1];
                else if (currentY < loc[1])
                {
                    dialog.Dismiss();
                    parent.Text = originalText;
                }
                else
                    currentY = loc[1];
            };
            input.TextChanged += (object s, TextChangedEventArgs e) =>
            {
                parent.Text = input.Text;
            };
            input.EditorAction += (object s, EditorActionEventArgs e) =>
            {
                if(e.ActionId == Android.Views.InputMethods.ImeAction.ImeNull || e.ActionId == Android.Views.InputMethods.ImeAction.Done)
                {
                    e.Handled = true;
                    dialog.Dismiss();
                }
            };

            dialog.Show();
        }

        private string ComputeIVPercent(string atkText, string defText, string staText)
        {
            int atk, def, sta;

            if (
                int.TryParse(atkText, out atk) &&
                int.TryParse(defText, out def) &&
                int.TryParse(staText, out sta))
                return string.Format("{0}%", ImportedPokemon.ComputeIVPercent(atk, def, sta));
            else
                return string.Empty;
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

        private void HideLayout()
        {
            if (shareLayout != null)
                shareLayout.Visibility = ViewStates.Gone;
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