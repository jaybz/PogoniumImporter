using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;

namespace PogoniumImporter.Droid
{
    [Activity(Label = "@string/shareTitle", Icon = "@drawable/icon", Theme = "@android:style/Theme.NoDisplay")]
    [IntentFilter(new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "application/pokemon-stats")]
    public class ShareActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Intent.ActionSend.Equals(Intent.Action) && Intent.Type.Equals("application/pokemon-stats")) {
                Intent serviceIntent = new Intent(this, typeof(ShareService));
                serviceIntent.AddFlags(ActivityFlags.ClearTop);
                serviceIntent.PutExtra("json", Intent.GetStringExtra(Intent.ExtraText));
                StartService(serviceIntent);
            }

            Finish();
        }
    }
}