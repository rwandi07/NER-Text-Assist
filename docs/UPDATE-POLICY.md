# NER Text Assist — Update Policy

Dokumen ini adalah aturan permanen agar versi baru dapat dipasang di atas versi lama tanpa uninstall manual.

## Identitas yang dikunci

- App name: `NER Text Assist`
- Executable: `NER.TextAssist.exe`
- Installer AppId: `{DCA82FC0-4F1F-4B40-9D19-54FB35F6C7A1}`
- Default install directory: `%LOCALAPPDATA%\Programs\NER Text Assist`

Jangan mengubah nilai di atas setelah rilis pertama kecuali memang ingin membuat aplikasi yang dianggap berbeda oleh Windows.

## Versioning

Gunakan Semantic Versioning:

- `0.1.0` = fondasi awal
- `0.2.0` = fitur baru yang kompatibel
- `0.2.1` = perbaikan bug
- `1.0.0` = rilis stabil pertama

Saat membuat release baru, naikkan versi pada:

1. `Directory.Build.props`
2. `installer/NER-Text-Assist.iss`

## Data pengguna

Data sumber, indeks autocomplete, command, dan pengaturan pengguna tidak boleh diletakkan di folder instalasi.

Gunakan lokasi data aplikasi:

`%LOCALAPPDATA%\NER Text Assist\`

Dengan begitu installer dapat mengganti file program tanpa menghapus data pengguna.

## Aturan installer

- Installer versi baru menggunakan `AppId` yang sama.
- Installer menutup aplikasi yang sedang berjalan sebelum mengganti file program.
- Tidak perlu uninstall versi lama sebelum memasang versi baru.
- File aplikasi lama dapat diganti oleh build baru.
- Data pengguna harus tetap berada di luar `{app}`.

## Branding identity

Logo resmi NER adalah identitas utama aplikasi. Jangan mengganti base icon dengan ikon lain pada release berikutnya; perubahan branding hanya boleh berupa penyesuaian nama/typography `NER Text Assist` di sekitar logo resmi.
