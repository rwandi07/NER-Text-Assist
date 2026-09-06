# NER Text Assist

NER Text Assist adalah aplikasi Windows desktop untuk membantu penyelesaian teks otomatis berdasarkan sumber yang diberikan pengguna, seperti file dokumen maupun teks/kata yang dimasukkan langsung.

## Fokus utama

1. Mengimpor sumber teks.
2. Mengekstrak dan mengindeks konten teks.
3. Menawarkan autocomplete berdasarkan sumber tersebut.
4. Mengembangkan text substitution/command sebagai fitur lanjutan, bukan fokus engine awal.

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

Tahap 0 — Fondasi & Branding.

Versi awal: `0.1.0`
