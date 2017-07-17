// Helpers/Settings.cs
using Plugin.Settings;
using Plugin.Settings.Abstractions;

namespace PogoniumImporter.Helpers
{
	/// <summary>
	/// This is the Settings static class that can be used in your Core solution or in any
	/// of your client applications. All settings are laid out the same exact way with getters
	/// and setters. 
	/// </summary>
	public static class Settings
	{
		private static ISettings AppSettings
		{
			get
			{
				return CrossSettings.Current;
			}
		}

		public static string PogoniumPasscode
		{
			get
			{
				return AppSettings.GetValueOrDefault(nameof(PogoniumPasscode), string.Empty);
			}
			set
			{
				AppSettings.AddOrUpdateValue(nameof(PogoniumPasscode), value);
			}
		}

	}
}