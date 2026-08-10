using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EcoWarga
{
    // ==========================================
    // 5. ENUMS
    // ==========================================
    public enum JenisSampah
    {
        Plastik = 3500,
        Kertas = 2000,
        Logam = 8000,
        Kaca = 1500,
        Organik = 500
    }

    public enum StatusLayanan
    {
        Diajukan,
        Diproses,
        Selesai,
        Dibatalkan
    }

    // ==========================================
    // 6. CUSTOM EXCEPTIONS
    // ==========================================
    public class BeratTidakValidException : Exception
    {
        public BeratTidakValidException(string message) : base(message) { }
    }

    public class MinimumPenjemputanException : Exception
    {
        public MinimumPenjemputanException(string message) : base(message) { }
    }

    // ==========================================
    // 4. MULTIPLE INTERFACES
    // ==========================================
    public interface IValidasiData
    {
        bool Validasi();
    }

    public interface IPersistensiDataLaporan
    {
        void SimpanKeFile(string path);
        void MuatDariFile(string path);
        void TampilkanLaporanRingkas();
    }

    // ==========================================
    // 1. ENCAPSULATION & CONSTRUCTOR
    // ==========================================
    public class Nasabah : IValidasiData
    {
        public string IdNasabah { get; private set; }
        public string Nama { get; private set; }
        public string Alamat { get; private set; }

        public Nasabah(string idNasabah, string nama, string alamat)
        {
            IdNasabah = idNasabah;
            Nama = nama;
            Alamat = alamat;

            if (!Validasi())
            {
                throw new ArgumentException("ID Nasabah, Nama, dan Alamat tidak boleh kosong.");
            }
        }

        public bool Validasi()
        {
            return !string.IsNullOrWhiteSpace(IdNasabah) &&
                   !string.IsNullOrWhiteSpace(Nama) &&
                   !string.IsNullOrWhiteSpace(Alamat);
        }
    }

    // ==========================================
    // 2. INHERITANCE, ABSTRACTION & POLYMORPHISM
    // ==========================================
    public abstract class LayananSampah
    {
        public string IdTransaksi { get; private set; }
        public Nasabah NasabahLayanan { get; private set; }
        public JenisSampah JenisSampahLayanan { get; private set; }
        public double Berat { get; private set; }
        public DateTime Tanggal { get; private set; }
        public StatusLayanan Status { get; set; }

        public LayananSampah(string idTransaksi, Nasabah nasabah, JenisSampah jenis, double berat, DateTime tanggal)
        {
            if (berat <= 0)
                throw new BeratTidakValidException("Berat sampah harus lebih dari 0 kg.");

            IdTransaksi = idTransaksi;
            NasabahLayanan = nasabah;
            JenisSampahLayanan = jenis;
            Berat = berat;
            Tanggal = tanggal;
            Status = StatusLayanan.Diajukan;
        }

        // Abstract method
        public abstract double HitungInsentif();

        public int HitungPoin()
        {
            double insentif = HitungInsentif();
            return (int)Math.Floor(insentif / 1000) * 10;
        }

        public virtual void TampilkanRingkasan()
        {
            Console.WriteLine($"ID Transaksi : {IdTransaksi}");
            Console.WriteLine($"Nasabah      : {NasabahLayanan.Nama} ({NasabahLayanan.IdNasabah})");
            Console.WriteLine($"Jenis Sampah : {JenisSampahLayanan} - {Berat} kg");
            Console.WriteLine($"Status       : {Status}");
            Console.WriteLine($"Insentif     : Rp{HitungInsentif():N0}");
            Console.WriteLine($"Poin         : {HitungPoin()}");
            Console.WriteLine("-------------------------------------------------");
        }
    }

    public class SetoranLangsung : LayananSampah
    {
        public SetoranLangsung(string idTx, Nasabah nasabah, JenisSampah jenis, double berat, DateTime tanggal)
            : base(idTx, nasabah, jenis, berat, tanggal) { }

        public override double HitungInsentif()
        {
            return Berat * (double)JenisSampahLayanan;
        }

        public override void TampilkanRingkasan()
        {
            Console.WriteLine("[ SETORAN LANGSUNG ]");
            base.TampilkanRingkasan();
        }
    }

    public class PenjemputanRumah : LayananSampah
    {
        private const double BIAYA_LAYANAN = 5000;

        public PenjemputanRumah(string idTx, Nasabah nasabah, JenisSampah jenis, double berat, DateTime tanggal)
            : base(idTx, nasabah, jenis, berat, tanggal)
        {
            if (berat < 2.0)
                throw new MinimumPenjemputanException("Berat minimum penjemputan adalah 2 kg.");
        }

        public override double HitungInsentif()
        {
            double hargaDasar = Berat * (double)JenisSampahLayanan;
            double insentifBersih = hargaDasar - BIAYA_LAYANAN;
            return Math.Max(0, insentifBersih); // Tidak boleh di bawah 0
        }

        public override void TampilkanRingkasan()
        {
            Console.WriteLine("[ PENJEMPUTAN RUMAH ]");
            base.TampilkanRingkasan();
        }
    }

    // ==========================================
    // CLASS PENGELOLA UTAMA (Implementing 2 Interfaces)
    // ==========================================
    public class PengelolaEcoWarga : IPersistensiDataLaporan, IValidasiData
    {
        public List<Nasabah> DaftarNasabah { get; private set; } = new List<Nasabah>();
        public List<LayananSampah> DaftarLayanan { get; private set; } = new List<LayananSampah>(); // 3. Polymorphic Collection
        private readonly string logPath = "log_aplikasi.txt";

        public void LogAktivitas(string pesan)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {pesan}{Environment.NewLine}");
            }
            catch { /* Abaikan jika log gagal */ }
        }

        public bool Validasi()
        {
            return DaftarNasabah != null && DaftarLayanan != null;
        }

        // ==========================================
        // 7. FILE I/O & LOGGING
        // ==========================================
        public void SimpanKeFile(string path)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    foreach (var layanan in DaftarLayanan)
                    {
                        string tipe = layanan is SetoranLangsung ? "SL" : "PR";
                        sw.WriteLine($"{tipe}|{layanan.IdTransaksi}|{layanan.NasabahLayanan.IdNasabah}|{layanan.NasabahLayanan.Nama}|{layanan.NasabahLayanan.Alamat}|{layanan.JenisSampahLayanan}|{layanan.Berat}|{layanan.Tanggal:O}|{layanan.Status}");
                    }
                }
                LogAktivitas("Berhasil menyimpan data transaksi ke " + path);
                Console.WriteLine("Data berhasil disimpan.");
            }
            catch (IOException ex)
            {
                LogAktivitas($"ERROR I/O saat simpan: {ex.Message}");
                Console.WriteLine("Terjadi kesalahan saat menyimpan file.");
            }
            finally
            {
                Console.WriteLine("Operasi penyimpanan file selesai.");
            }
        }

        public void MuatDariFile(string path)
        {
            if (!File.Exists(path)) return;

            try
            {
                string[] barisData = File.ReadAllLines(path);
                DaftarLayanan.Clear();
                DaftarNasabah.Clear();

                foreach (var baris in barisData)
                {
                    var data = baris.Split('|');
                    if (data.Length == 9)
                    {
                        string tipe = data[0];
                        string idTx = data[1];
                        string idNasabah = data[2];
                        string nama = data[3];
                        string alamat = data[4];
                        JenisSampah jenis = Enum.Parse<JenisSampah>(data[5]);
                        double berat = double.Parse(data[6]);
                        DateTime tgl = DateTime.Parse(data[7]);
                        StatusLayanan status = Enum.Parse<StatusLayanan>(data[8]);

                        Nasabah n = DaftarNasabah.FirstOrDefault(x => x.IdNasabah == idNasabah);
                        if (n == null)
                        {
                            n = new Nasabah(idNasabah, nama, alamat);
                            DaftarNasabah.Add(n);
                        }

                        LayananSampah layanan;
                        if (tipe == "SL") layanan = new SetoranLangsung(idTx, n, jenis, berat, tgl);
                        else layanan = new PenjemputanRumah(idTx, n, jenis, berat, tgl);

                        layanan.Status = status;
                        DaftarLayanan.Add(layanan);
                    }
                }
                LogAktivitas("Berhasil memuat data dari " + path);
                Console.WriteLine("Data berhasil dimuat dari file.");
            }
            catch (Exception ex)
            {
                LogAktivitas($"ERROR saat memuat: {ex.Message}");
                Console.WriteLine("Gagal memuat data dari file.");
            }
        }

        public void TampilkanLaporanRingkas()
        {
            int jumlahTransaksi = DaftarLayanan.Count;
            double totalBerat = DaftarLayanan.Sum(l => l.Berat);
            double totalInsentif = DaftarLayanan.Sum(l => l.HitungInsentif());
            int totalPoin = DaftarLayanan.Sum(l => l.HitungPoin());

            Console.WriteLine("\n=== LAPORAN RINGKAS ===");
            Console.WriteLine($"Total Transaksi : {jumlahTransaksi}");
            Console.WriteLine($"Total Berat     : {totalBerat} kg");
            Console.WriteLine($"Total Insentif  : Rp{totalInsentif:N0}");
            Console.WriteLine($"Total Poin      : {totalPoin}");
        }
    }

    // ==========================================
    // PROGRAM UTAMA (MENU MINIMAL)
    // ==========================================
    class Program
    {
        static void Main(string[] args)
        {
            PengelolaEcoWarga pengelola = new PengelolaEcoWarga();
            string fileData = "data_transaksi.txt";
            pengelola.MuatDariFile(fileData);

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\n===== MENU ECOWARGA =====");
                Console.WriteLine("1. Tambah Data Nasabah");
                Console.WriteLine("2. Catat Setoran Langsung");
                Console.WriteLine("3. Catat Penjemputan Rumah");
                Console.WriteLine("4. Tampilkan Seluruh Layanan (Polymorphic)");
                Console.WriteLine("5. Cari Transaksi");
                Console.WriteLine("6. Ubah Status Layanan");
                Console.WriteLine("7. Simpan Data ke File");
                Console.WriteLine("8. Tampilkan Laporan Ringkas");
                Console.WriteLine("9. Keluar");
                Console.Write("Pilih menu: ");

                try
                {
                    int pilihan = int.Parse(Console.ReadLine());
                    Console.WriteLine();

                    switch (pilihan)
                    {
                        case 1:
                            Console.Write("ID Nasabah: "); string idN = Console.ReadLine();
                            Console.Write("Nama: "); string nama = Console.ReadLine();
                            Console.Write("Alamat: "); string alamat = Console.ReadLine();
                            Nasabah nBaru = new Nasabah(idN, nama, alamat);
                            pengelola.DaftarNasabah.Add(nBaru);
                            Console.WriteLine("Berhasil tambah nasabah.");
                            pengelola.LogAktivitas($"Tambah nasabah {idN}");
                            break;

                        case 2:
                        case 3:
                            Console.Write("ID Transaksi (Unik): "); string idTx = Console.ReadLine();
                            if (pengelola.DaftarLayanan.Any(l => l.IdTransaksi == idTx))
                            {
                                Console.WriteLine("ID Transaksi sudah ada!");
                                break;
                            }

                            Console.Write("ID Nasabah: "); string idCari = Console.ReadLine();
                            Nasabah ns = pengelola.DaftarNasabah.FirstOrDefault(x => x.IdNasabah == idCari);
                            if (ns == null) { Console.WriteLine("Nasabah tidak ditemukan!"); break; }

                            Console.WriteLine("Pilih Jenis Sampah (0:Plastik, 1:Kertas, 2:Logam, 3:Kaca, 4:Organik): ");
                            JenisSampah jenis = (JenisSampah)Enum.GetValues(typeof(JenisSampah)).GetValue(int.Parse(Console.ReadLine()));

                            Console.Write("Berat (kg): ");
                            double berat = double.Parse(Console.ReadLine());

                            LayananSampah layananBaru;
                            if (pilihan == 2)
                                layananBaru = new SetoranLangsung(idTx, ns, jenis, berat, DateTime.Now);
                            else
                                layananBaru = new PenjemputanRumah(idTx, ns, jenis, berat, DateTime.Now);

                            pengelola.DaftarLayanan.Add(layananBaru);
                            Console.WriteLine("Transaksi berhasil dicatat!");
                            pengelola.LogAktivitas($"Catat transaksi {idTx} - {layananBaru.GetType().Name}");
                            break;

                        case 4:
                            foreach (var lay in pengelola.DaftarLayanan)
                            {
                                lay.TampilkanRingkasan(); // Pemanggilan Polymorphic
                            }
                            break;

                        case 5:
                            Console.Write("Masukkan ID Transaksi atau ID Nasabah: ");
                            string cari = Console.ReadLine();
                            var hasilCari = pengelola.DaftarLayanan.Where(l => l.IdTransaksi == cari || l.NasabahLayanan.IdNasabah == cari).ToList();
                            if (hasilCari.Count > 0)
                            {
                                foreach (var hc in hasilCari) hc.TampilkanRingkasan();
                            }
                            else
                            {
                                Console.WriteLine("Data tidak ditemukan.");
                            }
                            break;

                        case 6:
                            Console.Write("Masukkan ID Transaksi: ");
                            string idUbah = Console.ReadLine();
                            var layUbah = pengelola.DaftarLayanan.FirstOrDefault(l => l.IdTransaksi == idUbah);
                            if (layUbah != null)
                            {
                                Console.WriteLine("Pilih Status Baru (0:Diajukan, 1:Diproses, 2:Selesai, 3:Dibatalkan): ");
                                StatusLayanan statBaru = (StatusLayanan)Enum.GetValues(typeof(StatusLayanan)).GetValue(int.Parse(Console.ReadLine()));
                                layUbah.Status = statBaru;
                                Console.WriteLine("Status berhasil diubah!");
                                pengelola.LogAktivitas($"Ubah status tx {idUbah} ke {statBaru}");
                            }
                            else
                            {
                                Console.WriteLine("Transaksi tidak ditemukan.");
                            }
                            break;

                        case 7:
                            pengelola.SimpanKeFile(fileData);
                            break;

                        case 8:
                            pengelola.TampilkanLaporanRingkas();
                            break;

                        case 9:
                            exit = true;
                            Console.WriteLine("Aplikasi ditutup. Terima kasih!");
                            break;

                        default:
                            Console.WriteLine("Pilihan tidak valid.");
                            break;
                    }
                }
                catch (FormatException ex)
                {
                    Console.WriteLine("Input tidak valid! Pastikan format angka benar.");
                    pengelola.LogAktivitas($"Format Error: {ex.Message}");
                }
                catch (BeratTidakValidException ex)
                {
                    Console.WriteLine($"Error Validasi Berat: {ex.Message}");
                    pengelola.LogAktivitas(ex.Message);
                }
                catch (MinimumPenjemputanException ex)
                {
                    Console.WriteLine($"Error Penjemputan: {ex.Message}");
                    pengelola.LogAktivitas(ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Terjadi kesalahan: {ex.Message}");
                    pengelola.LogAktivitas(ex.Message);
                }
            }
        }
    }
}
