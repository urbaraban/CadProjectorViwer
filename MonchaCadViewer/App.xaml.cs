using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using AppSt = CadProjectorViewer.Properties.Settings;

namespace CadProjectorViewer
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
		private static List<CultureInfo> m_Languages = new List<CultureInfo>();

		public static List<CultureInfo> Languages
		{
			get
			{
				return m_Languages;
			}
		}

		public App()
		{
			m_Languages.Clear();
			m_Languages.Add(new CultureInfo("en-US")); //Нейтральная культура для этого проекта
			m_Languages.Add(new CultureInfo("ru-RU"));

			this.LoadCompleted += Application_LoadCompleted;
            App.LanguageChanged += App_LanguageChanged;
		}

        private void App_LanguageChanged(object sender, EventArgs e)
        {
			AppSt.Default.DefaultLanguage = Language;
			AppSt.Default.Save();
		}

        //Евент для оповещения всех окон приложения
        public static event EventHandler LanguageChanged;

		private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
			Language = AppSt.Default.DefaultLanguage;
		}

		public static CultureInfo Language
		{
			get
			{
				return System.Threading.Thread.CurrentThread.CurrentUICulture;
			}
			set
			{
				if (value == null) throw new ArgumentNullException("value");
				if (value == System.Threading.Thread.CurrentThread.CurrentUICulture) return;

				//1. Меняем язык приложения:
				System.Threading.Thread.CurrentThread.CurrentUICulture = value;

				//2. Создаём ResourceDictionary для новой культуры
				ResourceDictionary dict = new ResourceDictionary();
				switch (value.Name)
				{
					case "ru-RU":
						dict.Source = new Uri(String.Format("Resources/lang.{0}.xaml", value.Name), UriKind.Relative);
						break;
					default:
						dict.Source = new Uri("Resources/lang.xaml", UriKind.Relative);
						break;
				}

				//3. Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
				ResourceDictionary oldDict = (from d in Application.Current.Resources.MergedDictionaries
											  where d.Source != null && d.Source.OriginalString.StartsWith("Resources/lang.")
											  select d).First();
				if (oldDict != null)
				{
					int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
					Application.Current.Resources.MergedDictionaries.Remove(oldDict);
					Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
				}
				else
				{
					Application.Current.Resources.MergedDictionaries.Add(dict);
				}

				//4. Вызываем евент для оповещения всех окон.
				LanguageChanged(Application.Current, new EventArgs());
			}
		}
	}
}
