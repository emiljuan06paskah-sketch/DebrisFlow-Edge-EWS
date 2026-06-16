using Microsoft.Maui.Media;
using SkiaSharp;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DebrisFlowDashboard_Alprog
{
    public partial class MainDashboardPage : ContentPage
    {
        private SKBitmap _originalBitmap;
        private IDispatcherTimer _sensorTimer;
        private LocalDatabase _database = new LocalDatabase();
        private AIEngine _mesinAI = new AIEngine();

        public MainDashboardPage()
        {
            InitializeComponent();
            StartSensorSimulation();
            CekHakAksesSuperAdmin();
        }

        private async void CekHakAksesSuperAdmin()
        {
            string roleAktif = Preferences.Get("session_role", "");

            if (roleAktif == "Pihak BMKG (Super Admin)")
            {
                PanelSuperAdmin.IsVisible = true;

                var daftarUser = await _database.AmbilSemuaUserAsync();

                if (daftarUser == null || daftarUser.Count == 0)
                {
                    LabelDaftarUser.Text = "Belum ada user yang terdaftar di database SQLite.";
                    return;
                }

                string tampilanFinal = "";
                int nomor = 1;

                foreach (var user in daftarUser)
                {
                    tampilanFinal += $"{nomor}. {user.Username} ({user.Role})\n";
                    nomor++;
                }

                LabelDaftarUser.Text = tampilanFinal;
                LabelDaftarUser.FontAttributes = FontAttributes.None;
            }
        }

        private void StartSensorSimulation()
        {
            _sensorTimer = Dispatcher.CreateTimer();
            _sensorTimer.Interval = TimeSpan.FromSeconds(2);

            _sensorTimer.Tick += async (s, e) =>
            {
                Random rnd = new Random();
                double moisture = Math.Round(rnd.NextDouble() * (92.0 - 84.0) + 84.0, 1);
                int rain = rnd.Next(110, 135);

                LabelMoisture.Text = $"{moisture} % (Saturasi Tinggi)";
                LabelRainfall.Text = $"{rain} mm/jam";

                string statusFuzzy = AIEngine.EvaluasiFuzzy(moisture, rain);
                LabelStatus.Text = statusFuzzy;

                await _database.SimpanLogSensorAsync(moisture, rain, statusFuzzy);
            };
            _sensorTimer.Start();
        }

        private async void OnCaptureRiverClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null) return;
                using var stream = await photo.OpenReadAsync();
                using var skStream = new SKManagedStream(stream);
                var rawBitmap = SKBitmap.Decode(skStream);
                int targetWidth = 600;
                int targetHeight = (int)((float)targetWidth / rawBitmap.Width * rawBitmap.Height);
                _originalBitmap = rawBitmap.Resize(new SKImageInfo(targetWidth, targetHeight), SKFilterQuality.Medium);
                ApplyFilter(_originalBitmap);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void OnNormalFilterClicked(object sender, EventArgs e)
        {
            if (_originalBitmap != null) ApplyFilter(_originalBitmap);
        }

        private void OnGrayscaleFilterClicked(object sender, EventArgs e)
        {
            if (_originalBitmap == null) return;
            var filtered = new SKBitmap(_originalBitmap.Width, _originalBitmap.Height);
            for (int y = 0; y < _originalBitmap.Height; y++)
            {
                for (int x = 0; x < _originalBitmap.Width; x++)
                {
                    var color = _originalBitmap.GetPixel(x, y);
                    byte gray = (byte)(0.3 * color.Red + 0.59 * color.Green + 0.11 * color.Blue);
                    filtered.SetPixel(x, y, new SKColor(gray, gray, gray, color.Alpha));
                }
            }
            ApplyFilter(filtered);
        }

        private void OnEdgeFilterClicked(object sender, EventArgs e)
        {
            if (_originalBitmap == null) return;
            var filtered = new SKBitmap(_originalBitmap.Width, _originalBitmap.Height);
            for (int y = 0; y < _originalBitmap.Height - 1; y++)
            {
                for (int x = 0; x < _originalBitmap.Width - 1; x++)
                {
                    var p1 = _originalBitmap.GetPixel(x, y);
                    var p2 = _originalBitmap.GetPixel(x + 1, y + 1);
                    int diff = Math.Abs(p1.Red - p2.Red) + Math.Abs(p1.Green - p2.Green) + Math.Abs(p1.Blue - p2.Blue);
                    byte edgeColor = diff > 80 ? (byte)255 : (byte)0;
                    filtered.SetPixel(x, y, new SKColor(edgeColor, edgeColor, edgeColor));
                }
            }
            ApplyFilter(filtered);
        }

        private void ApplyFilter(SKBitmap bitmapToDisplay)
        {
            using var image = SKImage.FromBitmap(bitmapToDisplay);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var memoryStream = new MemoryStream(data.ToArray());
            CctvPreview.Source = ImageSource.FromStream(() => memoryStream);
        }

        private async void OnHapusUserClicked(object sender, EventArgs e)
        {
            string targetUser = EntryHapusUser.Text?.Trim();
            if (string.IsNullOrEmpty(targetUser))
            {
                await DisplayAlert("Peringatan", "Ketik username yang ingin dihapus terlebih dahulu!", "OK");
                return;
            }

            string currentUser = Preferences.Get("session_username", "");
            if (targetUser == currentUser)
            {
                await DisplayAlert("Akses Ditolak", "Tidak bisa menghapus akun sendiri saat aktif!", "OK");
                return;
            }

            var daftarUser = await _database.AmbilSemuaUserAsync();
            var userDitemukan = daftarUser.FirstOrDefault(u => u.Username.Equals(targetUser, StringComparison.OrdinalIgnoreCase));

            if (userDitemukan == null)
            {
                await DisplayAlert("Tidak Ditemukan", $"User dengan nama '{targetUser}' tidak terdaftar.", "OK");
                return;
            }

            await _database.HapusUserAsync(userDitemukan.Username);

            string imagePath = Path.Combine(FileSystem.AppDataDirectory, $"{targetUser}_face.png");
            if (File.Exists(imagePath)) File.Delete(imagePath);

            EntryHapusUser.Text = "";
            CekHakAksesSuperAdmin();

            await DisplayAlert("Sukses", $"Akun '{targetUser}' sukses dihapus dari database SQLite terpadu!", "OK");
        }

        // ====================================================================
        // --- POIN BARU: FUNGSI CHATBOT AI YANG CERDAS (MEMANGGIL AIEngine)---
        // ====================================================================
        private async void OnSearchButtonPressed(object sender, EventArgs e)
        {
            string userInput = SearchBarNLP.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(userInput)) return;

            string inputLower = userInput.ToLower();

            // Mencegat perintah export (Ini wajib tetap ada)
            if (inputLower == "export" || inputLower == "excel")
            {
                await ExportKeExcel();
                return;
            }

            // Mencegat perintah khusus Prediksi Jaringan Saraf Tiruan
            if (inputLower == "prediksi" || inputLower == "masa depan" || inputLower == "besok")
            {
                SearchBarNLP.Placeholder = "Neural Network memproses prediksi...";
                SearchBarNLP.Text = "";
                await Task.Delay(1000);

                double kelembapanSaatIni = double.Parse(LabelMoisture.Text.Split(' ')[0]);
                double hujanSaatIni = double.Parse(LabelRainfall.Text.Split(' ')[0]);

                double persentaseBahaya = _mesinAI.PrediksiMasaDepan(kelembapanSaatIni, hujanSaatIni);
                int persenTampil = (int)Math.Round(persentaseBahaya * 100);

                string penjelasanAwam = $"[PREDIKSI NEURAL NETWORK]\n\n" +
                                        $"Berdasarkan data saat ini (Tanah: {kelembapanSaatIni}%, Hujan: {hujanSaatIni} mm), " +
                                        $"kecerdasan buatan menghitung probabilitas terjadinya longsoran debris esok hari adalah sebesar: {persenTampil}%.\n\n" +
                                        $"Kesimpulan untuk warga: ";

                if (persenTampil > 70)
                    penjelasanAwam += "SANGAT BERBAHAYA. Warga di bantaran sungai hilir disarankan segera evakuasi malam ini juga karena material tanah sudah jenuh air.";
                else
                    penjelasanAwam += "Relatif Aman. Lereng tebing masih mampu menahan curah hujan saat ini. Warga bisa beraktivitas seperti biasa dengan tetap waspada.";

                _mesinAI.LatihBackprop(kelembapanSaatIni, hujanSaatIni, 0.9);

                await DisplayAlert("Terjemahan AI untuk Warga", penjelasanAwam, "OK");
                SearchBarNLP.Placeholder = "Tanya AI atau ketik 'export'...";
                return;
            }

            // JIKA BUKAN EXPORT DAN BUKAN PREDIKSI: LEMPAR KE CHATBOT DI AIENGINE
            SearchBarNLP.Placeholder = "AI sedang mengetik jawaban...";
            SearchBarNLP.Text = "";

            // Ambil angka mentah dari label layar
            double nilaiKelembapan = double.Parse(LabelMoisture.Text.Split(' ')[0]);
            double nilaiHujan = double.Parse(LabelRainfall.Text.Split(' ')[0]);

            // Panggil fungsi GetAIResponse yang baru kita buat di AIEngine.cs
            string jawabanChatbot = _mesinAI.GetAIResponse(userInput, nilaiHujan, nilaiKelembapan);

            // Tampilkan jawaban ke dalam bentuk Pop-up
            await DisplayAlert("Asisten AI Cerdas", jawabanChatbot, "Tutup");

            SearchBarNLP.Placeholder = "Tanya AI atau ketik 'export'...";
        }

        // ====================================================================
        // --- FUNGSI EXPORT MULTI-TABEL (SENSOR + USER ACCOUNTS) ---
        // ====================================================================
        private async Task ExportKeExcel()
        {
            try
            {
                var semuaData = await _database.AmbilSemuaDataAsync();
                if (semuaData.Count == 0)
                {
                    await DisplayAlert("Data Kosong", "Database SQLite masih kosong, tunggu beberapa detik.", "OK");
                    return;
                }

                var csvSensor = new System.Text.StringBuilder();
                csvSensor.AppendLine("ID_Log,Waktu_Rekam,Kelembapan_Tanah(%),Curah_Hujan(mm/jam),Status_Bahaya");
                foreach (var data in semuaData)
                {
                    csvSensor.AppendLine($"{data.Id},{data.Waktu:yyyy-MM-dd HH:mm:ss},{data.KelembapanTanah},{data.CurahHujan},{data.StatusFuzzy}");
                }

                var semuaUser = await _database.AmbilSemuaUserAsync();
                var csvUser = new System.Text.StringBuilder();
                csvUser.AppendLine("Username,Role_Akses");
                foreach (var user in semuaUser)
                {
                    csvUser.AppendLine($"{user.Username},{user.Role}");
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                string fileSensorPath = Path.Combine(desktopPath, "Laporan_EWS_DebrisFlow.csv");
                File.WriteAllText(fileSensorPath, csvSensor.ToString());

                string fileUserPath = Path.Combine(desktopPath, "Daftar_Pengguna_EWS.csv");
                File.WriteAllText(fileUserPath, csvUser.ToString());

                await DisplayAlert("Export Sukses!",
                    $"Seluruh Database Relasional Berhasil Diekspor!\n\n" +
                    $"2 File Excel (CSV) telah disimpan di Desktop:\n" +
                    $"1. 'Laporan_EWS_DebrisFlow.csv' ({semuaData.Count} Baris Log Sensor)\n" +
                    $"2. 'Daftar_Pengguna_EWS.csv' ({semuaUser.Count} Akun Terdaftar)",
                    "OK");

                SearchBarNLP.Text = "";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error Export", ex.Message, "OK");
            }
        }
    }
}