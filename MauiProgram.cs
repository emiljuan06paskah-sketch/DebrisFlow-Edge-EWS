using DebrisFlowDashboard_Alprog;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting; // INI WAJIB DITAMBAHKAN

namespace DebrisFlowDashboard_Alprog    
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp() // INI WAJIB DITAMBAHKAN UNTUK OLAH CITRA
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}