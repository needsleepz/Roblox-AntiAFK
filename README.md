# Roblox-AntiAFK

A modern, lightweight anti-AFK tool for Roblox with a Windows 11 Copilot-style dark UI.

## 🇬🇧 English

### Features

- **Anti-AFK** — Keeps your Roblox character active by simulating key presses
- **Two action modes** — Camera Shift (default) or Jump
- **Auto window management** — Optionally maximize/restore Roblox windows before action
- **Hide window contents** — Make Roblox transparent while performing actions
- **Screensaver mode** — Black screen overlay that closes on mouse movement
- **Show/Hide Roblox** — Quickly toggle Roblox window visibility
- **Exit Roblox** — Kill all Roblox processes from the tray
- **System tray** — Minimizes to tray, double-click to toggle Start/Stop
- **Dynamic tray icons** — Changes icon based on running state
- **Persistent settings** — Saved to `%LocalAppData%\Roblox-AntiAFK\settings.json`

### Screenshots

<p align="center">
  <img src="https://via.placeholder.com/380x480?text=Roblox-AntiAFK" alt="Screenshot" />
</p>

### How It Works

1. **Start** — Detects all open Roblox windows
2. **Every 15 minutes** — For each Roblox window:
   - Optionally maximizes the window for a configured delay
   - Activates the window and sends 3 key presses (Camera Shift: I → O, or Space)
   - Restores the previous foreground window
   - Optionally hides window contents during the process
3. **Auto-stop** — Stops automatically when no Roblox windows are detected
4. **Stop** — Repairs all windows (restores transparency, visibility) and stops

### Usage

1. Run `RBX_AntiAFK.exe`
2. Open Roblox and join a game
3. Click **Start** or double-click the tray icon
4. The status indicator will show "Running" with a green dot
5. Click **Stop** or double-click the tray icon to stop

### Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Action Type | Camera Shift | Key press mode (Camera Shift or Jump) |
| Open Roblox for | ✗ (disabled) | Maximize Roblox window before action |
| Delay (sec) | 3 | Seconds to keep Roblox maximized |
| Hide window contents | ✗ | Make Roblox transparent during action |

### System Requirements

- **OS:** Windows 10/11 (x64)
- **Runtime:** Self-contained — no .NET installation required
- **Approximate size:** ~69 MB

### Build from Source

```bash
git clone https://github.com/needsleepz/Roblox-AntiAFK.git
cd Roblox-AntiAFK
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\RBX_AntiAFK.exe`

### Credits

- **Author:** [NeedSleep.Dev](https://github.com/needsleepz)
- **Original project:** [JunkBeat/AntiAFK-Roblox](https://github.com/JunkBeat/AntiAFK-Roblox)

---

## 🇹🇭 ภาษาไทย

### คุณสมบัติ

- **ป้องกัน AFK** — ทำให้ตัวละคร Roblox ขยับอยู่เสมอ ไม่โดนเตะออก
- **สองโหมดดำเนินการ** — เลื่อนกล้อง (ค่าเริ่มตาย) หรือกระโดด
- **จัดการหน้าต่างอัตโนมัติ** — เปิดหน้าต่าง Roblox ขึ้นมาก่อนทำงาน (เลือกได้)
- **ซ่อนเนื้อหาหน้าต่าง** — ทำให้หน้าต่าง Roblox โปร่งใสขณะทำงาน
- **โหมดสกรีนเซฟเวอร์** — จอดำ ปิดได้ด้วยการขยับเมาส์
- **แสดง/ซ่อน Roblox** — สลับการแสดงหน้าต่าง Roblox ได้เลย
- **ปิด Roblox** — ฆ่าโปรเซส Roblox ทั้งหมดจากถาดระบบ
- **ถาดระบบ** — ย่อไปถาดระบบ ดับเบิลคลิกเพื่อเริ่ม/หยุด
- **ไอคอนเปลี่ยนตามสถานะ** — เปลี่ยนไอคอนตามสถานะการทำงาน
- **ตั้งค่าถาวร** — เก็บไว้ที่ `%LocalAppData%\Roblox-AntiAFK\settings.json`

### วิธีการทำงาน

1. **เริ่ม** — ค้นหาหน้าต่าง Roblox ที่เปิดอยู่ทั้งหมด
2. **ทุก 15 นาที** — สำหรับแต่ละหน้าต่าง Roblox:
   - เปิดหน้าต่างขึ้นมา (ถ้าเปิดใช้งาน) ตามเวลาที่ตั้งไว้
   - เลือกหน้าต่างแล้วกดปุ่ม 3 ครั้ง (เลื่อนกล้อง: I → O, หรือ Space)
   - คืนหน้าต่างที่เคยใช้อยู่
   - ซ่อนเนื้อหาหน้าต่างได้ (ถ้าเปิดใช้งาน)
3. **หยุดอัตโนมัติ** — หยุดเมื่อไม่พบหน้าต่าง Roblox
4. **หยุด** — ซ่อมแซมหน้าต่างทั้งหมด (คืนความโปร่งใส, การมองเห็น) แล้วหยุด

### วิธีใช้

1. รัน `RBX_AntiAFK.exe`
2. เปิด Roblox แล้วเข้าเกม
3. กดปุ่ม **Start** หรือดับเบิลคลิกไอคอนถาดระบบ
4. สถานะจะเปลี่ยนเป็น "Running" พร้อมจุดเขียว
5. กด **Stop** หรือดับเบิลคลิกไอคอนถาดระบบเพื่อหยุด

### การตั้งค่า

| การตั้งค่า | ค่าเริ่มต้น | คำอธิบาย |
|-----------|-------------|----------|
| Action Type | Camera Shift | โหมดกดปุ่ม (เลื่อนกล้อง หรือ กระโดด) |
| Open Roblox for | ✗ (ปิด) | เปิดหน้าต่าง Roblox ขึ้นมาก่อนทำงาน |
| Delay (วินาที) | 3 | ระยะเวลาที่เปิดหน้าต่าง Roblox |
| Hide window contents | ✗ | ทำหน้าต่าง Roblox โปร่งใสขณะทำงาน |

### ความต้องการของระบบ

- **ระบบปฏิบัติการ:** Windows 10/11 (x64)
- **Runtime:** ไม่ต้องติดตั้ง .NET (โปรแกรมรวมไว้แล้ว)
- **ขนาดโปรแกรม:** ~69 MB

### บิลด์จากซอร์สโค้ด

```bash
git clone https://github.com/needsleepz/Roblox-AntiAFK.git
cd Roblox-AntiAFK
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

ไฟล์ผลลัพธ์: `bin\Release\net8.0-windows\win-x64\publish\RBX_AntiAFK.exe`

### เครดิต

- **ผู้สร้าง:** [NeedSleep.Dev](https://github.com/needsleepz)
- **โปรเจกต์ต้นฉบับ:** [JunkBeat/AntiAFK-Roblox](https://github.com/JunkBeat/AntiAFK-Roblox)

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.