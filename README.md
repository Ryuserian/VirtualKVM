# VirtualKVM

라즈베리파이 Zero W 를 USB HID(키보드+마우스) 장치로 만들어, 호스트 PC 의 입력을
다른 컴퓨터(피에 USB 로 연결된 대상 PC)로 전달하는 KVM 애플리케이션입니다.

## 구성

| 프로젝트 | 설명 |
|---|---|
| `VirtualKVM` | Windows 트레이 상주 앱. low-level 키/마우스 훅으로 입력을 가로채 TCP 로 전달 |
| `RaspberryPi-Lib` | Pi 제어 라이브러리 — SSH(SSH.NET) 로 HID 데몬 구동, TCP(5000) 로 HID 리포트 전송 |
| `RaspberryPi-Lib/Resources/RaspberryPiZeroW-HID` | PiSIDE 실행 파일(ARM ELF). 피에서 USB gadget HID 리포트를 대상 PC 로 전송 |

## 동작 흐름

1. **Connect** 시 앱이 SSH 로 Pi 접속 → USB gadget(HID 키보드/마우스) 설정 확인/구성 →
   HID 데몬 실행.
2. 앱은 Pi:5000 으로 TCP 2개 소켓을 연결하고 악수 매직을 전송한다.
   - 키보드 소켓: `0x11111111`
   - 마우스 소켓: `0x22222222`
3. Win32 low-level 훅이 입력을 가로채 HID 리포트(키보드 8바이트 / 마우스 3바이트)로
   변환해 전송한다.
4. **Alt+Ctrl+Shift 동시 입력**으로 제어권 토글(로컬 ↔ 원격).

## 연결 안정성 (장시간 사용 끊김 대응)

오래 사용 중 연결이 끊기는 주요 원인과 대비책:

| 원인 | 대비 |
|---|---|
| Wi-Fi/NAT 세션 타임아웃(유휴) | TCP keepalive(4s/1s), SSH `KeepAliveInterval` 10s, 워치독 하트비트 |
| HID 데몬이 SSH 세션과 함께 종료 | `setsid nohup` 으로 데몬화하여 구동 |
| Expect 프롬프트 대기 무한정체 | 셸 스트림 방식 제거, `CreateCommand` 실행으로 교체 |
| USB gadget 미활성 | 멱등 setup 스크립트로 재구성 |
| 단선/크래시 | 5초 주기 워치독이 SSH/데몬/TCP 자가 복구, 3회 실패 시 Pi 재부팅 |
| Alt 조합키 놓침(stuck key) | `WM_SYSKEYDOWN/UP` 포함 처리, 제어권 전환 시 modifier flush |

## 빌드

Visual Studio 2019+ (.NET Framework 4.8) 또는:

```
msbuild VirtualKVM.sln /p:Configuration=Release
```

## 사용

1. Pi 에 `pi` 계정으로 접속 가능하게 준비 후 전원을 켠다(기본 IP `192.168.0.101`).
2. 대상 PC 와 피를 USB 케이블(OB)로 연결한다.
3. Windows 에서 VirtualKVM 실행 → 트레이 아이콘 우클릭 → **Connect**.
4. 제어권 전환: `Alt + Ctrl + Shift`.
