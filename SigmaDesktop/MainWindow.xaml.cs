using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SigmaDesktop
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private bool isMusicPlaying = true;

        private const string ALLOWED_DOMAIN = "tchk.site";
        private const string START_URL = "https://tchk.site";
        public MainWindow()
        {
            
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            SetWindowIcon();
            SetAppIcon();
        }
        
        private void SetAppIcon()
        {
            try
            {
                // Пытаемся загрузить иконку из папки src
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "src", "icon2.ico");

                if (System.IO.File.Exists(iconPath))
                {
                    // Загружаем ICO файл
                    var icon = new System.Windows.Media.Imaging.BitmapImage();
                    icon.BeginInit();
                    icon.UriSource = new Uri(iconPath, UriKind.Absolute);
                    icon.EndInit();
                    this.Icon = icon;
                }
                else
                {
                    // Если нет ICO, пробуем загрузить JPG/PNG
                    string imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "src", "icon2.jpg");
                    if (System.IO.File.Exists(imagePath))
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                        bitmap.EndInit();
                        this.Icon = bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось установить иконку: {ex.Message}");
            }
        }
        private void SetWindowIcon()
        {
            try
            {
                // Вариант: ищем файл в разных местах
                string[] possiblePaths = {
                    // Путь относительно EXE файла
                    //System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "icon.jpg"),
                    // Путь относительно корня проекта (для отладки)
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "icon2.jpg"),

                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "src", "icon2.jpg"),

                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "src", "icon2.jpg"),

                };

                string iconPath = null;
                foreach (var path in possiblePaths)
                {
                    string fullPath = System.IO.Path.GetFullPath(path);
                    if (System.IO.File.Exists(fullPath))
                    {
                        iconPath = fullPath;
                        break;
                    }
                }

                if (iconPath != null && System.IO.File.Exists(iconPath))
                {
                    using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(iconPath))
                    {
                        IntPtr hIcon = bitmap.GetHicon();
                        this.Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            hIcon,
                            System.Windows.Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        DestroyIcon(hIcon);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Иконка не найдена");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось загрузить иконку: {ex.Message}");
            }
        }

        // Импортируем функцию для освобождения иконки
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Настройка WebView2
                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync();
                await webView.EnsureCoreWebView2Async(env);

                // Настройки безопасности и ограничений
                webView.CoreWebView2.Settings.IsScriptEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false; // Отключаем F12 для безопасности

                // Блокируем навигацию на другие сайты
                webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
                webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
                webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

                // Загружаем ваш сайт
                webView.CoreWebView2.Navigate(START_URL);

                // Обновляем заголовок окна
                webView.CoreWebView2.DocumentTitleChanged += (s, ev) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Title = $"Мессенджер - {webView.CoreWebView2.DocumentTitle}";
                    });
                };

                // Запуск музыки
                PlayBackgroundMusic();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }




        // Запрещаем открытие новых окон
        private void OnNewWindowRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Проверяем, ведет ли ссылка на разрешенный домен
            if (e.Uri.Contains(ALLOWED_DOMAIN))
            {
                // Если да - открываем в текущем окне
                webView.CoreWebView2.Navigate(e.Uri);
            }
            else
            {
                // Если нет - блокируем
                e.Handled = true;
                MessageBox.Show($"Переход на сторонний сайт запрещен: {e.Uri}",
                              "Доступ запрещен",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
        }

        // Проверка навигации
        private void OnNavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            // Проверяем, наш ли это сайт
            if (!e.Uri.Contains(ALLOWED_DOMAIN) && !e.Uri.StartsWith("about:blank"))
            {
                e.Cancel = true;
                MessageBox.Show($"Доступ к сайту {e.Uri} запрещен.\nПриложение работает только с {ALLOWED_DOMAIN}",
                              "Доступ запрещен",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
        }

        private void OnNavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                MessageBox.Show($"Не удалось загрузить страницу. Ошибка: {e.WebErrorStatus}",
                              "Ошибка загрузки",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void PlayBackgroundMusic()
        {
            try
            {
                // Путь к музыке
                string musicPath = null;

                string path1 = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "ost.mp3");

                string path2 = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "src", "ost.mp3");

                // Проверяем первый путь
                if (System.IO.File.Exists(path1))
                {
                    musicPath = path1;
                }
                // Если нет, проверяем второй путь
                else if (System.IO.File.Exists(path2))
                {
                    musicPath = path2;
                }

                if (System.IO.File.Exists(musicPath))
                {
                    mediaPlayer.Open(new Uri(musicPath, UriKind.Absolute));
                    mediaPlayer.MediaEnded += (s, ev) =>
                    {
                        // Зацикливаем музыку
                        mediaPlayer.Position = TimeSpan.Zero;
                        mediaPlayer.Play();
                    };
                    mediaPlayer.Volume = 0.5; // Громкость 50% (от 0 до 1)
                    mediaPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось воспроизвести музыку: {ex.Message}");
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlayBackgroundMusic();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Останавливаем музыку при закрытии
            mediaPlayer?.Stop();
            mediaPlayer?.Close();
        }

        // Очистка ресурсов при закрытии
        protected override void OnClosed(EventArgs e)
        {
            webView?.Dispose();
            base.OnClosed(e);
        }

    }
}
