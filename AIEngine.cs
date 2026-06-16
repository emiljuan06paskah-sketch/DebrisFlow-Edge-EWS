using System;

namespace DebrisFlowDashboard_Alprog
{
    public class AIEngine
    {
        // ==========================================================
        // 1. LOGIKA FUZZY (Untuk Evaluasi Status Sensor Real-Time)
        // ==========================================================
        public static string EvaluasiFuzzy(double kelembapan, double hujan)
        {
            // Proses Fuzzifikasi (Mengubah angka sensor jadi persentase bahaya 0.0 - 1.0)
            double derajatBasah = (kelembapan - 70.0) / (95.0 - 70.0);
            if (derajatBasah < 0) derajatBasah = 0; if (derajatBasah > 1) derajatBasah = 1;

            double derajatLebat = (hujan - 50.0) / (150.0 - 50.0);
            if (derajatLebat < 0) derajatLebat = 0; if (derajatLebat > 1) derajatLebat = 1;

            // Mesin Inferensi (Rule Base: Jika Tanah Basah DAN Hujan Lebat)
            // Menggunakan operator AND (Fuzzy MIN)
            double derajatBahaya = Math.Min(derajatBasah, derajatLebat);

            // Defuzzifikasi (Diterjemahkan agar dipahami ORANG AWAM)
            if (derajatBahaya >= 0.7) return "AWAS (SIAGA 1)";
            else if (derajatBahaya >= 0.4) return "WASPADA (SIAGA 2)";
            else return "AMAN (NORMAL)";
        }

        // ==========================================================
        // 2. NEURAL NETWORK (Feed Forward & Backpropagation)
        // Untuk memprediksi probabilitas longsor esok hari
        // ==========================================================
        private double bobot1 = 0.45; // Bobot sensor tanah
        private double bobot2 = 0.78; // Bobot sensor hujan
        private double bias = -0.5;   // Nilai bias neuron
        private double learningRate = 0.1;

        // Fungsi Aktivasi Sigmoid
        private double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

        // A. Proses Feed Forward (Menebak Hasil)
        public double PrediksiMasaDepan(double inputKelembapan, double inputHujan)
        {
            // Normalisasi angka besar ke skala 0-1 untuk masuk ke Neuron
            double x1 = inputKelembapan / 100.0;
            double x2 = inputHujan / 200.0;

            // Rumus Inti Neuron: (Input * Bobot) + Bias
            double sum = (x1 * bobot1) + (x2 * bobot2) + bias;

            return Sigmoid(sum); // Akan menghasilkan angka persentase 0.00 s/d 1.00
        }

        // B. Proses Backpropagation (Koreksi Belajar Mesin)
        // (Dipanggil jika prediksi mesin salah dibanding kenyataan)
        public void LatihBackprop(double inputKelembapan, double inputHujan, double targetKenyataan)
        {
            double x1 = inputKelembapan / 100.0;
            double x2 = inputHujan / 200.0;

            // 1. Lihat hasil tebakan saat ini
            double tebakan = PrediksiMasaDepan(inputKelembapan, inputHujan);

            // 2. Hitung tingkat Error (Selisih target vs tebakan)
            double error = targetKenyataan - tebakan;

            // 3. Cari nilai turunan derivatif fungsi Sigmoid
            double turunan = tebakan * (1.0 - tebakan);

            // 4. Update bobot baru agar besok lebih pintar (Backprop Rule)
            bobot1 += learningRate * error * turunan * x1;
            bobot2 += learningRate * error * turunan * x2;
            bias += learningRate * error * turunan;
        }

        // ==========================================================
        // 3. NLP CHATBOT ASSISTANT (Intent Recognition)
        // Mengubah data sensor & AI menjadi obrolan bahasa manusia
        // ==========================================================
        public string GetAIResponse(string userInput, double hujan, double tanah)
        {
            // Ubah huruf inputan user jadi kecil semua biar sistem gampang ngeceknya
            string input = userInput.ToLower();

            // Panggil EvaluasiFuzzy untuk tahu status bahayanya
            string statusBahaya = EvaluasiFuzzy(tanah, hujan);

            // Skenario 1: User tanya soal Hujan/Cuaca
            if (input.Contains("hujan") || input.Contains("curah") || input.Contains("air") || input.Contains("cuaca"))
            {
                return $"Asisten: Intensitas curah hujan saat ini tercatat sebesar {hujan} mm/jam. " +
                       (hujan > 100 ? "Ini sangat ekstrem, waspada potensi banjir bandang!" : "Masih dalam batas normal, tetap tenang.");
            }

            // Skenario 2: User tanya soal Tanah/Longsor
            else if (input.Contains("tanah") || input.Contains("longsor") || input.Contains("kelembapan"))
            {
                return $"Asisten: Tingkat kejenuhan air pada tanah saat ini adalah {tanah}%. " +
                       (tanah > 80 ? "Tanah sudah sangat jenuh air, sangat rawan terjadi pergeseran atau longsor!" : "Kondisi kelembapan tanah masih stabil.");
            }

            // Skenario 3: User tanya Status/Aman tidaknya
            else if (input.Contains("status") || input.Contains("aman") || input.Contains("bahaya") || input.Contains("kondisi"))
            {
                return $"Asisten: Berdasarkan kalkulasi sistem, status EWS saat ini adalah: {statusBahaya}. Harap selalu pantau arahan dari petugas pos setempat.";
            }

            // Skenario 4: User menyapa
            else if (input.Contains("halo") || input.Contains("pagi") || input.Contains("siang") || input.Contains("malam") || input.Contains("tes"))
            {
                return "Asisten: Halo! Saya adalah AI Siaga Bencana. Silakan ketik pertanyaan seputar status bahaya, kondisi hujan, atau kelembapan tanah saat ini.";
            }

            // Skenario 5: User ngetik ngasal / kata tidak dikenali
            else
            {
                return "Asisten: Maaf, saya tidak mengerti maksud Anda. Coba gunakan kata kunci seperti 'hujan', 'tanah', 'status', atau 'longsor'.";
            }
        }
    }
}