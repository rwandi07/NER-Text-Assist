# NER Text Assist

NER Text Assist adalah aplikasi Windows desktop untuk membantu penyelesaian teks otomatis berdasarkan sumber yang diberikan pengguna, seperti file dokumen maupun teks/kata yang dimasukkan langsung.

## Fokus utama

1. Mengimpor sumber teks.
2. Mengekstrak dan mengindeks konten teks.
3. Menawarkan autocomplete berdasarkan sumber tersebut.
4. Mengembangkan text substitution/command sebagai fitur lanjutan, bukan fokus engine awal.

## Tahap 1 — Import Sumber & Ekstraksi Teks

Versi `0.2.2` menyempurnakan fondasi source library lokal:

- import beberapa file sekaligus;
- format awal: TXT, DOCX, CSV, dan XLSX;
- ekstraksi text-only;
- gambar, shape, diagram, text box/visual container, dan konten visual lain diabaikan;
- tab dan line break DOCX dipertahankan sebagai pemisah teks agar kata tidak menempel;
- teks header/footer DOCX ikut diekstrak jika ada;
- tabel DOCX diproses per baris dan batas antarsel dipertahankan sebagai tab;
- XLSX mempertahankan batas kolom sebagai tab, termasuk posisi sel kosong di tengah baris;
- CSV mempertahankan batas field sebagai tab;
- setiap row XLSX/CSV tetap menjadi satu baris teks terpisah;
- deteksi delimiter CSV membaca beberapa baris dan mengabaikan delimiter yang berada di dalam quoted field;
- hasil ekstraksi disimpan lokal agar tidak bergantung pada file asli saat dipakai nanti;
- re-import file yang sama memperbarui source lama;
- preview teks hasil ekstraksi;
- hapus source dari library tanpa menghapus file asli.

OCR, ekstraksi gambar, manual phrase/word source, autocomplete global, dan text substitution belum termasuk Tahap 1.

## Platform

- Windows desktop
- .NET 8
- WPF
- Target awal: Windows 10/11 x64

## Struktur awal

- `src/NER.TextAssist/` — aplikasi desktop
- `installer/` — installer Inno Setup
- `scripts/` — build/release scripts
- `docs/` — aturan arsitektur dan update
- `assets/branding/` — aset logo resmi NER

## Build lokal

```powershell
./scripts/build-release.ps1
```

Script akan menghasilkan publish output self-contained untuk Windows x64. Jika Inno Setup 6 tersedia, installer juga akan dibuat otomatis.

## Update instalasi

Installer menggunakan AppId permanen sehingga release berikutnya dapat dipasang langsung di atas instalasi lama. Jangan mengubah AppId installer setelah rilis pertama.

Lihat `docs/UPDATE-POLICY.md` untuk aturan lengkap.

## Status

Tahap 1 — Import Sumber & Ekstraksi Teks.

Versi: `0.2.2`
