# VirtualKVM 펌웨어 (Raspberry Pi Zero W HID 데몬)

라즈베리파이 Zero W 를 USB复合 HID(키보드 `/dev/hidg0` + 마우스 `/dev/hidg1`) 장치로
노출하고, TCP:5000 으로 수신한 HID 리포트를 대상 PC 로 전달하는 데몬입니다.
단일 스레드 `poll()` 이벤트 루프 + `signalfd` 기반 종료 처리로 구현되어 있습니다.

## 빌드

### 방법 A — Raspberry Pi Zero W 에서 직접 빌드 (권장)

Zero W 의 resources 가 빈약하지만 데몬이 매우 작기 때문에 직접 빌드가 가장 간단합니다.

```bash
# 1) 빌드 도구 설치
sudo apt update
sudo apt install -y build-essential git

# 2) 코드 가져오기
git clone https://github.com/Ryuserian/VirtualKVM.git
cd VirtualKVM/firmware

# 3) 빌드
make
# -> build/RaspberryPiZeroW-HID 생성
```

### 방법 B — PC 에서 크로스 컴파일 (WSL Ubuntu)

Zero W 보드 없이 PC 에서 빠르게 빌드하고 파일만 복사할 때 사용합니다.

```bash
# WSL Ubuntu 에서
sudo apt update
sudo apt install -y g++-arm-linux-gnueabihf make

cd /mnt/h/VSProject/dc/VirtualKVM/firmware
make CROSS_COMPILE=arm-linux-gnueabihf-
# -> build/RaspberryPiZeroW-HID (ARM 32bit hard-float ELF)

# 파일 복사
scp build/RaspberryPiZeroW-HID pi@192.168.0.101:~/Desktop/
```

> 참고: `firmware/Makefile` 의 `ARCHFLAGS`
> - 네이티브 빌드: Pi gcc 기본값 (Zero W = armv6zk hard-float)
> - 크로스 빌드: `-march=armv6zk -mtune=arm1176jzf-s -mfpu=vfp -mfloat-abi=hard`

### 빌드 옵션 정리

| 명령 | 설명 |
|---|---|
| `make` | 네이티브 빌드 |
| `make CROSS_COMPILE=arm-linux-gnueabihf-` | armv6 hard-float 크로스 빌드 |
| `make clean` | `build/` 삭제 |
| `make install` | `/usr/local/bin` + systemd 서비스 설치 |

## 실행

### 수동 실행

```bash
# USB gadget 구성 (첫 1회 또는 리부팅 후)
# Windows 앱의 Connect 가 자동으로 처리하지만, 수동으로는:
sudo sh setup_usb_gadget.sh   # (TODO: future)

# 데몬 직접 실행
./build/RaspberryPiZeroW-HID 5000
```

### systemd 서비스로 상주 (권장)

```bash
sudo install -m 755 build/RaspberryPiZeroW-HID /usr/local/bin/
sudo cp systemd/virtualkvm.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now virtualkvm.service

# 로그 확인
journalctl -u virtualkvm -f
```

`Restart=always` + Windows 앱의 5초 워치독으로 이중 안전망을 구성합니다.

## USB gadget 구성 (HID 노출)

대상 PC 에 키보드/마우스로 인식되게 하려면 configfs 로 USB gadget 을 만들어야 합니다.
Windows 앱이 SSH 접속 후 아래 내용을 setup 스크립트로 자동 주입하지만,
직접 구성해야 하는 경우:

```bash
sudo su
modprobe usb_f_hid
modprobe libcomposite
cd /sys/kernel/config/usb_gadget/
mkdir -p g1 && cd g1
echo 0x1d6b > idVendor
echo 0x0104 > idProduct
echo 0x0100 > bcdDevice
echo 0x0200 > bcdUSB
mkdir -p strings/0x409
echo "fedcba9876543210" > strings/0x409/serialnumber
echo "todostudio"       > strings/0x409/manufacturer
echo "HID KeyMouse"    > strings/0x409/product
mkdir -p configs/c.1/strings/0x409
echo "Config 1: Keyboard And Mouse" > configs/c.1/strings/0x409/configuration
echo 10 > configs/c.1/MaxPower
mkdir -p functions/hid.usb0    # 키보드
echo 1 > functions/hid.usb0/protocol
echo 1 > functions/hid.usb0/subclass
echo 8 > functions/hid.usb0/report_length
echo -ne \\x05\\x01\\x09\\x06\\xa1\\x01\\x05\\x07\\x19\\xe0\\x29\\xe7\\x15\\x00\\x25\\x01\\x75\\x01\\x95\\x08\\x81\\x02\\x95\\x01\\x75\\x08\\x81\\x03\\x95\\x05\\x75\\x01\\x05\\x08\\x19\\x01\\x29\\x05\\x91\\x02\\x95\\x01\\x75\\x03\\x91\\x03\\x95\\x06\\x75\\x08\\x15\\x00\\x25\\x65\\x05\\x07\\x19\\x00\\x29\\x65\\x81\\x00\\xc0 > functions/hid.usb0/report_desc
mkdir -p functions/hid.usb1    # 마우스
echo 2 > functions/hid.usb1/protocol
echo 1 > functions/hid.usb1/subclass
echo 8 > functions/hid.usb1/report_length
echo -ne \\x05\\x01\\x09\\x02\\xa1\\x01\\x09\\x01\\xa1\\x00\\x05\\x09\\x19\\x01\\x29\\x03\\x15\\x00\\x25\\x01\\x95\\x03\\x75\\x01\\x81\\x02\\x95\\x01\\x75\\x05\\x81\\x03\\x05\\x01\\x09\\x30\\x09\\x31\\x15\\x81\\x25\\x7f\\x75\\x08\\x95\\x02\\x81\\x06\\xc0\\xc0 > functions/hid.usb1/report_desc
ln -sf functions/hid.usb0 configs/c.1/
ln -sf functions/hid.usb1 configs/c.1/
ls /sys/class/udc > UDC
```

> Zero W 2 (RP0) 등 OS/하드웨어에 따라 `/sys/class/udc` 의 노드명과 `otg` 활성화
> 옵션(`dtoverlay=dwc2`) 이 다를 수 있습니다. config.txt 에 `dtoverlay=dwc2` 가
> enabled 되어야 OTG 포트가 디바이스 모드로 동작합니다.

## 동작 / 프로토콜

- TCP `0.0.0.0:5000` 리스너 (단일 poll 루프, accept4 세션 추가).
- 연결 후 첫 4바이트 악수: `0x11111111` → `/dev/hidg0`(키보드), `0x22222222` → `/dev/hidg1`(마우스).
- 매직 판별 후 해당 `hidg` 를 open. 그 뒤로 8바이트/3바이트 HID 리포트를 읽어서 write.
- 종료: signalfd 가 SIGINT/SIGTERM/SIGHUP 을 수신하면 이벤트 루프가 빠져나와 정상 종료.

## 파일 구성

| 파일 | 역할 |
|---|---|
| [`src/main.cpp`](src/main.cpp) | 진입점, 바인딩 재시도, 서버 구동 |
| [`src/server.h/.cpp`](src/server.cpp) | poll 이벤트 루프(accept + 세션 소비 + signalfd) |
| [`src/session.h/.cpp`](src/session.cpp) | 단일 연결: 악수→hidg 연결→리포트 스트림 |
| [`src/signal_handler.h/.cpp`](src/signal_handler.cpp) | signalfd: SIGINT/SIGTERM/SIGHUP(종료), SIGPIPE(무시) |
| [`src/virtualkvm.h`](src/virtualkvm.h) | 프로토콜 상수(포트/매직/디바이스 경로) |
| [`systemd/virtualkvm.service`](systemd/virtualkvm.service) | 상주 실행 + 자동 재기동 |

## 테스트 (빌드 sanity check)

```bash
./build/RaspberryPiZeroW-HID 15000 &   # hidg 없이 데몬만 기동 (테스트용 포트)
# 다른 터미널:
exec 3<>/dev/tcp/127.0.0.1/15000
printf '\x11\x11\x11\x11\x00\x00\x00\x00' >&3
kill -TERM %1   # signalfd 정상 종료 로그 확인
```

## 문제 해결

| 증상 | 확인 |
|---|---|
| bind 실패 루프 | 다른 프로세스가 5000 점유 중? (`sudo ss -ltnp`) |
| `/dev/hidg0` 없음 | USB gadget 미활성 or `dtoverlay=dwc2` 누락 |
| 대상 PC 인식 못함 | USB 케이블이 **데이터** 케이블인지, OTG 포트가 디바이스 모드인지 |
| 입력 지연 | Wi-Fi 지연 (`ping`), poll 1s 타임아웃 내 이벤트 정상 확인 |
