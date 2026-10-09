# VirtualKVM

라즈베리파이 Zero W 를 USB HID(키보드+마우스) 장치로 만들어, 호스트 PC 의 입력을
다른 컴퓨터로 전달하는 소프트웨어 KVM 입니다.

```
[호스트 PC (Windows)] --Wi-Fi/TCP:5000--> [Raspberry Pi Zero W] --USB HID--> [대상 PC]
   Win32 훅으로 입력 가로채기                       poll 이벤트 루프 / signalfd        /dev/hidg0,1
```

## 주요 기능

- **입력 포워딩**: 키보드/마우스(상대 좌표)를 HID 리포트로 전송. `Alt+Ctrl+Shift` 로 제어권 토글
- **GUI 설정**: 트레이 아이콘 우클릭 → **Configuration** 에서 IP / HID 포트 / SSH 포트 / 계정 입력·저장
  (`%APPDATA%\VirtualKVM\config.json`, 비밀번호는 DPAPI 암호화)
- **장시간 연결 안정성**: TCP/SSH keepalive, 5초 워치독, 데몬 자가 복구, Pi 원격 재부팅
- **Windows 앱**: `.NET 10` (WinForms, `net10.0-windows`)
- **펌웨어**: 단일 스레드 `poll()` + `signalfd` C++ 데몬

## 저장소 구조

```
VirtualKVM.sln
├─ VirtualKVM/                 # Windows 트레이 앱 (.NET 10, WinForms)
│  ├─ UI/                     #   WindowMain(폼·트레이·워치독), WindowConfig(설정 입력)
│  ├─ Input/                  #   Win32 low-level 훅 + RawInput, VK 코드 -> HID Usage 번역
│  ├─ Config/                 #   AppConfig(JSON + DPAPI 암호화 저장)
│  ├─ Program.cs
│  └─ Properties/
├─ RaspberryPi-Lib/           # Pi 제어 라이브러리 (.NET 10)
│  ├─ Core/                   #   Raspberry (엔트리 / 팩토리)
│  ├─ Control/              #   Raspberry_RemoteCtrl (SSH: 데몬 기동/복구)
│  ├─ Transport/            #   Raspberry_LocalCtrl (TCP: HID 리포트 전송/재접속)
│  ├─ Protocol/             #   Enum (HID Usage / Control_Key / MouseButton)
│  └─ Resources/            #   배포용 ARM ELF 바이너리
└─ firmware/                  # Pi Zero W HID 데몬 소스 (C++)
   ├─ src/                  #   main.cpp / server.cpp / session.cpp / signal_handler.cpp
   ├─ systemd/             #   systemd 서비스 유닛
   ├─ Makefile
   └─ README.md           #   펌웨어 전용 상세 가이드
```

## 요구 사항

| 구분 | 요구 사항 |
|---|---|
| Windows 앱 빌드 | .NET 10 SDK (`winget install Microsoft.DotNet.SDK.10`) · Visual Studio 2022(17.x) 또는 VS Code(C# Dev Kit) |
| Windows 앱 실행 | Windows 10 이상 + .NET 10 Desktop Runtime (`winget install Microsoft.DotNet.DesktopRuntime.10`) |
| 펌웨어 빌드 | Raspberry Pi OS 에서 `build-essential`, 또는 PC(WSL Ubuntu) 에서 `g++-arm-linux-gnueabihf` |
| 하드웨어 | Raspberry Pi Zero W(또는 Zero 2 W), USB 데이터 케이블 |

## Windows 앱 빌드 & 실행

**Visual Studio**: `VirtualKVM.sln` 열기 → 빌드

**dotnet CLI**:

```bat
dotnet build VirtualKVM.sln -c Release
```

산출물: `VirtualKVM\bin\Release\VirtualKVM.exe`

**VS Code**: `F5` → "VirtualKVM (Debug)" (`.vscode/launch.json` 제공)

**단일 파일(exe 하나만)로 배포** 하려면:

```bat
dotnet publish VirtualKVM\VirtualKVM.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## 사용법

1. Pi 전원을 켜고 네트워크에 붙인다.
2. Pi 의 **USB OTG 포트**를 **데이터 케이블**로 대상 PC 에 연결한다.
3. Windows 에서 `VirtualKVM.exe` 실행 → 트레이 아이콘(구름) 우클릭 →
   **Configuration** 에서 Pi 의 IP/포트/계정 입력 후 확인 (첫 실행 시에만 필요).
4. 우클릭 → **Connect**. 입력기/tray 아이콘이 연결 상태(초록 계열)로 바뀐다.
5. 제어권 전환: **`Alt + Ctrl + Shift`** (다시 누르면 로컬로 복귀).

> 설정은 `%APPDATA%\VirtualKVM\config.json` 에 저장되어 재시작해도 유지됩니다.
> 비밀번호는 DPAPI(현재 Windows 사용자) 로 암호화되어 평문으로 저장되지 않습니다.

## 라즈베리파이 Zero W 준비 & 펌웨어 컴파일

전체 상세 문서는 [`firmware/README.md`](firmware/README.md) 참고. 요약:

### 1) OS 설치 및 사전 설정

Raspberry Pi OS Lite 를 SD 카드에 굽고, 부팅 후:

```bash
# SSH 활성화
sudo raspi-config
#   Interface Options > SSH > Yes

# USB OTG(device mode) 활성화 — config.txt 에 dtoverlay 추가
sudo nano /boot/firmware/config.txt      # 구버전: /boot/config.txt
#        파일 끝에 아래 추가:
#        dtoverlay=dwc2
sudo reboot
```

### 2) 펌웨어 빌드

**A. Pi 에서 직접 빌드 (권장·간단)**

```bash
sudo apt update && sudo apt install -y build-essential git
git clone https://github.com/Ryuserian/VirtualKVM.git
cd VirtualKVM/firmware
make
# -> build/RaspberryPiZeroW-HID
```

**B. PC(WSL Ubuntu) 크로스 컴파일**

```bash
sudo apt update && sudo apt install -y g++-arm-linux-gnueabihf make
cd /mnt/h/VSProject/dc/VirtualKVM/firmware
make CROSS_COMPILE=arm-linux-gnueabihf-
# -> build/RaspberryPiZeroW-HID (ARM 32bit hard-float ELF)

scp build/RaspberryPiZeroW-HID pi@<Pi IP>:~/Desktop/
```

### 3) systemd 서비스로 상주 (권장)

```bash
cd VirtualKVM/firmware
sudo make install        # /usr/local/bin + systemd enable --now
# 또는 수동:
sudo install -m 755 build/RaspberryPiZeroW-HID /usr/local/bin/
sudo cp systemd/virtualkvm.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now virtualkvm.service
journalctl -u virtualkvm -f
```

Windows 앱의 **Connect** 는 SSH 로 접속해 HID 데몬을 `setsid nohup` 으로 자동 기동하고
USB gadget 구성도 자동 주입합니다. systemd 를 쓰면 데몬 crash 시 자동 재기동되어
이중 안전망이 됩니다.

## 연결 안정성 (장시간 사용 대응)

| 계층 | 방어책 | 구현 위치 |
|---|---|---|
| 세션 유지 | TCP keepalive(4s/1s), 하트비트(5초마다 HID 상태 재전송) | [`Transport/Raspberry_LocalCtrl.cs`](RaspberryPi-Lib/Transport/Raspberry_LocalCtrl.cs) |
| SSH 유지 | SSH keepalive(10초), 접속 타임아웃 10초 | [`Control/Raspberry_RemoteCtrl.cs`](RaspberryPi-Lib/Control/Raspberry_RemoteCtrl.cs) |
| 프로세스 | HID 데몬 `setsid nohup` + systemd(Restart=always) | [`firmware/systemd/`](firmware/systemd/) |
| 복구 | 5초 워치독: TCP→SSH→데몬→(3연속 실패 시) Pi 재부팅 | [`UI/WindowMain.cs`](VirtualKVM/UI/WindowMain.cs) |
| 입력 무결성 | WM_SYSKEY 포함 처리, 전환 시 modifier flush, 키 고착(stuck key) 방지 | [`Input/Win32.cs`](VirtualKVM/Input/Win32.cs) |
| 무결성(진단) | 트레이 예외 시 `crash.log` 기록 | [`Program.cs`](VirtualKVM/Program.cs) |

## TCP 프로토콜 (port 5000)

- 연결 후 첫 4바이트 = `0x11111111`(키보드) 또는 `0x22222222`(마우스).
- 이후 키보드는 8바이트, 마우스는 3바이트 HID 리포트를 반복 전송.

| HID 리포트 | 크기 | 포맷 |
|---|---|---|
| 키보드 | 8B | `[modifier][0x00][key1..key6]` |
| 마우스 | 3B | `[buttons][dx][dy]` (signed axis) |

## 문제 해결

| 증상 | 확인 |
|---|---|
| Connect 실패(빨간 아이콘) | Configuration 의 IP/포트/계정 확인, Pi 전원/네트워크 확인 |
| 트레이 아이콘 안 보임 | Windows 11 은 신규 아이콘을 `^` 접힘 영역으로 숨김 — `^` 펼치기 또는 **설정 > 개인화 > 작업표시줄 > 다른 시스템 트레이 아이콘** 에서 켜기 |
| 대상 PC 가 HID 미인식 | USB **데이터** 케이블 / `dtoverlay=dwc2` / configfs gadget 활성 확인 |
| input bind 실패 | `sudo ss -ltnp` 로 5000(설정 포트) 점유 확인 |
| 앱이 바로 종료됨 | 실행 폴더의 `crash.log` 확인 |

## 라이선스

- 본 프로젝트: [MIT License](LICENSE) — Copyright (c) Sangkyu (Ryuserian)
- 펌웨어/데몬 및 빌드 산출물도 동일 MIT 라이선스로 제공됩니다.

### 서드파티 고지

| 라이브러리 | 라이선스 | 용도 |
|---|---|---|
| [SSH.NET](https://github.com/sshnet/SSH.NET) | MIT | Windows 앱의 SSH/SFTP 접속 |
| BouncyCastle.Cryptography (SSH.NET 의존) | MIT | SSH.NET 의 암호화 연산 |
| Microsoft.Extensions.* (SSH.NET 의존) | MIT | SSH.NET 의존성 |
| .NET 10 / WinForms | MIT | 런타임 |
| Linux 커널 USB gadget(헤더) | GPL-2.0 / BSD-3-Clause(헤더) | 펌웨어 인터페이스 (컴파일 시 헤더 참조) |

MIT 라이선스 호환으로 재배포에 제약이 없습니다. 각 라이브러리의 원본 LICENSE 는 NuGet 패키지에 포함됩니다.
