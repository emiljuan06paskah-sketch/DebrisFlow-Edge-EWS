using SQLite;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DebrisFlowDashboard_Alprog
{
    // ==========================================================
    // 1. TABEL DATA SENSOR
    // ==========================================================
    public class SensorLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime Waktu { get; set; }
        public double KelembapanTanah { get; set; }
        public int CurahHujan { get; set; }
        public string StatusFuzzy { get; set; }
    }

    // ==========================================================
    // 2. TABEL DATA AKUN USER (BARU UNTUK POIN E)
    // ==========================================================
    public class UserAccount
    {
        [PrimaryKey]
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    // ==========================================================
    // MESIN DATABASE UTAMA
    // ==========================================================
    public class LocalDatabase
    {
        private SQLiteAsyncConnection _db;

        public LocalDatabase()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "DebrisFlowEWS.db3");
            _db = new SQLiteAsyncConnection(dbPath);

            // Membuat kedua tabel secara otomatis di file database yang sama
            _db.CreateTableAsync<SensorLog>().Wait();
            _db.CreateTableAsync<UserAccount>().Wait();

            // Jalankan fungsi pengisian data akun awal jika database kosong
            SeedUserAwal();
        }

        // Mengisi akun default secara otomatis saat pertama kali aplikasi dijalankan
        private void SeedUserAwal()
        {
            var count = _db.Table<UserAccount>().CountAsync().Result;
            if (count == 0)
            {
                _db.InsertAsync(new UserAccount { Username = "Juan", Password = "123", Role = "Pihak BMKG (Super Admin)" }).Wait();
                _db.InsertAsync(new UserAccount { Username = "jj", Password = "123", Role = "Operator Bendungan" }).Wait();
            }
        }

        // --- MANAGEMEN DATA SENSOR ---
        public async Task SimpanLogSensorAsync(double kelembapan, int hujan, string status)
        {
            var logBaru = new SensorLog
            {
                Waktu = DateTime.Now,
                KelembapanTanah = kelembapan,
                CurahHujan = hujan,
                StatusFuzzy = status
            };
            await _db.InsertAsync(logBaru);
        }

        public async Task<List<SensorLog>> AmbilSemuaDataAsync()
        {
            return await _db.Table<SensorLog>().ToListAsync();
        }

        // --- MANAGEMEN DATA USER (BARU) ---
        public async Task<List<UserAccount>> AmbilSemuaUserAsync()
        {
            return await _db.Table<UserAccount>().ToListAsync();
        }

        public async Task SimpanUserAsync(string username, string password, string role)
        {
            var userBaru = new UserAccount { Username = username, Password = password, Role = role };
            await _db.InsertAsync(userBaru); // Menyimpan akun baru ke SQLite
        }

        public async Task HapusUserAsync(string username)
        {
            await _db.DeleteAsync<UserAccount>(username); // Menghapus akun dari SQLite berdasarkan username
        }
    }
}