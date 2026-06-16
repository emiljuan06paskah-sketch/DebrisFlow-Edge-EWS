using System;
using Microsoft.Maui.Controls;

namespace DebrisFlowDashboard_Alprog
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = EntryUsername.Text?.Trim();
            string pass = EntryPassword.Text;

            if (!Preferences.ContainsKey($"user_pass_{user}"))
            {
                await DisplayAlert("Ditolak", "Username tidak ditemukan!", "OK");
                return;
            }

            string savedPass = Preferences.Get($"user_pass_{user}", "");
            if (pass != savedPass)
            {
                await DisplayAlert("Ditolak", "Password salah!", "OK");
                return;
            }

            string role = Preferences.Get($"user_role_{user}", "Masyarakat");

            // Titipkan info user yang sedang login saat ini ke sistem
            Preferences.Set("session_username", user);
            Preferences.Set("session_role", role);

            Application.Current.MainPage = new FaceVerificationPage(user, role);
        }

        private void OnGoToRegisterClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new RegisterPage();
        }
    }
}