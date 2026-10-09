# VirtualKVM

라즈베리파이 Zero W 를 USB HID(키보드+마우스) 장치로 만들어, 호스트 PC 의 입력을
다른 컴퓨터로 전달하는 소프트웨어 KVM 입니다.

```
[호스트 PC (Windows)] --Wi-Fi/TCP:5000--> [Raspberry Pi Zero W] --USB HID--> [대상 PC]
   Win32 훅으로 입력 가로채기                       poll 이벤트 루프 / signalfd        /dev/hidg0,1
```

## 저장소 구조 (기능별)

```
VirtualKVM.sln
├─ VirtualKVM/                 # Windows 트레이 앱 (.NET Framework 4.8)
│  ├─ UI/                     #   WindowMain (폼, 트레이 메뉴, 워치독 타이머)
│  ├─ Input/                  #   Win32 low-level 훅, VK 코드 -> HIDUsage 번역
│  ├─ Program.cs
│  └─ Properties/
├─ RaspberryPi-Lib/           # Pi 제어 라이브러리 (.NET)
│  ├─ Core/                   #   Raspberry (엔트리 / 팩토리)
│  ├─ Control/              #   Raspberry_RemoteCtrl (SSH: 데몬 기동/복구)
│  ├─ Transport/            #   Raspberry_LocalCtrl (TCP: HID 리포트 전송/재접속)
│  ├─ Protocol/             #   Enum (HID Usage / Control_Key / MouseButton)
│  └─ Resources/            #   배포용 ARM ELF 바이너리
└─ firmware/                  # Pi Zero W HID 데몬 소스 (C++ / 폴백 빌드용)
   ├─ src/                  #   main.cpp / server.cpp / session.cpp / signal_handler.cpp
   ├─ systemd/             #   systemd 서비스 유닛
   ├─ Makefile
   └─ README.md           #   펌웨어 전용 빌드 안내
```

## 빌드

### Windows 앱

Visual Studio 2019+ 또는 MSBuild:

```bat
msbuild VirtualKVM.sln /p:Configuration=Release
```

산출물: `VirtualKVM\bin\Release\VirtualKVM.exe`

### 라즈베리파이 Zero W 펌웨어

라즈베리파이 Zero W 에서 직접 빌드하거나, PC(WSL Ubuntu) 에서 크로스 컴파일할 수 있습니다.
자세한 내용은 [`firmware/README.md`](firmware/README.md) 참고.

**Pi 에서 직접 빌드 (권장·간단):**

```bash
sudo apt update && sudo apt install -y build-essential
cd ~/VirtualKVM/firmware
make
```

**PC(WSL Ubuntu) 크로스 컴파일:**

```bash
sudo apt update && sudo apt install -y g++-arm-linux-gnueabihf
cd /mnt/h/VSProject/dc/VirtualKVM/firmware
make CROSS_COMPILE=arm-linux-gnueabihf-
```

산출물: `firmware/build/RaspberryPiZeroW-HID` (ARM ELF)

> 참고: Windows 앱의 `Install()` 은 원래 `RaspberryPi-Lib/Resources/RaspberryPiZeroW-HID`
> 파일을 Pi 의 `Desktop/` 에 올리는 방식으로 데몬을 배포했습니다.
> 커밋된 ELF 바이너리 대신 소스에서 최신 빌드 결과를 넣고 싶으면 빌드한 파일을
> `RaspberryPi-Lib/Resources/RaspberryPiZeroW-HID` 로 복사하세요.

## 사용법

1. Pi 의 전원을 켜고 네트워크에 붙인다(기본 IP `192.168.0.101`, 계정 `pi`/`raspberry`).
2. Pi 의 USB OTG 포트를 **데이터 케이블**로 대상 PC 에 연결한다.
3. Windows 에서 VirtualKVM.exe 실행 → 트레이 아이콘 우클릭 → **Connect**.
4. 제어권 전환: **`Alt + Ctrl + Shift`** (다시 누르면 로컬로 복귀).

## 연결 안정성 (장시간 사용 대응)

Wi-Fi/NAT 환경에서 장시간 사용 중 끊김 현상을 여러 층에서 방어합니다:

| 계층 | 방어책 | 구현 위치 |
|---|---|---|
| 세션 유지 | TCP keepalive(4s/1s), 하트비트(5초마다 HID 상태 재전송) | [`Transport/Raspberry_LocalCtrl.cs`](RaspberryPi-Lib/Transport/Raspberry_LocalCtrl.cs) |
| SSH 유지 | SSH keepalive@openssh.com(10초), 접속 타임아웃 10초 | [`Control/Raspberry_RemoteCtrl.cs`](RaspberryPi-Lib/Control/Raspberry_RemoteCtrl.cs) |
| 프로세스 | HID 데몬을 `setsid nohup`+systemd(Restart=always) 로 이중守护 | [`firmware/systemd/`](firmware/systemd/) |
| 복구 | 5초 워치독: TCP→SSH→데몬→(3연속 실패 시) Pi 재부팅 | [`UI/WindowMain.cs`](VirtualKVM/UI/WindowMain.cs) |
| 입력 무결성 | WM_SYSKEY 포함 처리, 전환 시 modifier flush, 키 고착(stuck key) 방지 | [`Input/Win32.cs`](VirtualKVM/Input/Win32.cs) |

## TCP 프로토콜 (port 5000)

- 연결 후 첫 4바이트 = 네트워크 바이트순 `0x11111111`(키보드) 또는 `0x22222222`(마우스).
- 이후 키보드는 8바이트, 마우스는 3바이트 HID 리포트를 반복 전송.

| HID 리포트 | 크기 | 포맷 |
|---|---|---|
| 키보드 | 8B | `[modifier][0x00][key1..key6]` |
| 마우스 | 3B | `[buttons][dx][dy]` (signed axis) |

## 라이선스

[MIT](LICENSE)
