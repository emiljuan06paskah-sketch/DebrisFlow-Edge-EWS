using Microsoft.Maui.Media;
using SkiaSharp;
using System.IO;

namespace DebrisFlowDashboard_Alprog
{
    public partial class RegisterPage : ContentPage
    {
        private byte[] _faceData;

        public RegisterPage()
        {
            InitializeComponent();
            SetupRolePicker();
        }

        // FUNGSI BARU: Mengatur isi menu dropdown sesuai ketersediaan kursi Super Admin
        private void SetupRolePicker()
        {
            // Sistem mengecek apakah sudah ada yang pernah mendaftar sebagai BMKG
            bool isSuperAdminTaken = Preferences.Get("has_super_admin", false);

            PickerRole.Items.Clear();
            PickerRole.Items.Add("Operator Bendungan"); // Opsi ini selalu ada

            // Jika kursi Super Admin masih kosong, tampilkan opsinya
            if (!isSuperAdminTaken)
            {
                PickerRole.Items.Add("Pihak BMKG (Super Admin)");
            }
        }

        private async void OnCaptureFaceClicked(object sender, EventArgs e)
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _faceData = memoryStream.ToArray();

                CameraPreview.Source = ImageSource.FromStream(() => new MemoryStream(_faceData));
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string user = EntryUsername.Text?.Trim();
            string pass = EntryPassword.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || PickerRole.SelectedIndex == -1 || _faceData == null)
            {
                await DisplayAlert("Gagal", "Semua data (termasuk foto wajah) wajib diisi!", "OK");
                return;
            }

            string selectedRole = PickerRole.SelectedItem.ToString();

            // 1. SIMPAN AKUN KE DATABASE LOKAL
            Preferences.Set($"user_pass_{user}", pass);
            Preferences.Set($"user_role_{user}", selectedRole);

            // 2. BARU: Masukkan nama username ke dalam daftar master database
            string userList = Preferences.Get("daftar_semua_user", "");
            if (string.IsNullOrEmpty(userList))
            {
                userList = user;
            }
            else if (!userList.Contains(user)) // Biar tidak dobel kalau mendaftar ulang
            {
                userList += "," + user;
            }
            Preferences.Set("daftar_semua_user", userList);

            // Jika yang daftar ini milih BMKG, kunci kursinya
            if (selectedRole == "Pihak BMKG (Super Admin)")
            {
                Preferences.Set("has_super_admin", true);
            }

            // Simpan foto wajah
            string imagePath = Path.Combine(FileSystem.AppDataDirectory, $"{user}_face.png");
            File.WriteAllBytes(imagePath, _faceData);

            await DisplayAlert("Sukses", $"User '{user}' berhasil didaftarkan sebagai {selectedRole}!", "OK");
            Application.Current.MainPage = new MainPage();
        }

        private void OnBackToLoginClicked(object sender, EventArgs e) => Application.Current.MainPage = new MainPage();
    }
}