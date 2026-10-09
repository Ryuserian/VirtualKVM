#!/bin/sh
# VirtualKVM USB gadget 활성화 (키보드 /dev/hidg0 + 마우스 /dev/hidg1)
#
# 단일 소스(single source of truth):
#  - Windows 앱: 이 파일을 리소스로 임베드하여 Connect 시 /tmp/virtualkvm_usb_setup.sh 로 업로드·실행
#  - Pi 단독: systemd(virtualkvm-usb-gadget.service) 또는 rc.local 로 부팅 시 실행
# configfs 설정은 재부팅하면 사라지므로 매 부팅 후 1회 실행이 필요하다.
#
# 사전 조건: /boot/firmware/config.txt 에 dtoverlay=dwc2 가 활성화되어
# OTG 포트가 디바이스 모드(UDC)로 동작해야 한다.
#
# 멱등(idempotent): 이미 활성화되어 있으면 그대로 종료한다.
set -e

GADGET=/sys/kernel/config/usb_gadget/g1

# 0) 이미 활성화 확인 — 바인딩되어 있으면 유지
if [ -e "$GADGET/UDC" ] && [ -n "$(cat "$GADGET/UDC" 2>/dev/null)" ]; then
    echo "[usb-gadget] already bound to $(cat "$GADGET/UDC")"
    exit 0
fi

# 1) 모듈 로드
modprobe libcomposite || true
modprobe usb_f_hid || true

cd /sys/kernel/config/usb_gadget/
mkdir -p g1
cd g1

# 2) 디바이스 식별 정보 (미완성 gadget 이 남아있으면 초기화 후 재구성)
echo 0 > UDC || true
rm -f configs/c.1/hid.usb0 configs/c.1/hid.usb1 || true
echo 0x1d6b > idVendor   # Linux Foundation
echo 0x0104 > idProduct  # Multifunction Composite Gadget
echo 0x0100 > bcdDevice
echo 0x0200 > bcdUSB
mkdir -p strings/0x409
echo "fedcba9876543210" > strings/0x409/serialnumber
echo "todostudio"       > strings/0x409/manufacturer
echo "HID KeyMouse"    > strings/0x409/product

# 3) 구성 + 문자열
mkdir -p configs/c.1/strings/0x409
echo "Config 1: Keyboard And Mouse" > configs/c.1/strings/0x409/configuration
echo 10 > configs/c.1/MaxPower

# 4) 키보드 HID function (report_length 8)
mkdir -p functions/hid.usb0
echo 1 > functions/hid.usb0/protocol
echo 1 > functions/hid.usb0/subclass
echo 8 > functions/hid.usb0/report_length
echo -ne \\x05\\x01\\x09\\x06\\xa1\\x01\\x05\\x07\\x19\\xe0\\x29\\xe7\\x15\\x00\\x25\\x01\\x75\\x01\\x95\\x08\\x81\\x02\\x95\\x01\\x75\\x08\\x81\\x03\\x95\\x05\\x75\\x01\\x05\\x08\\x19\\x01\\x29\\x05\\x91\\x02\\x95\\x01\\x75\\x03\\x91\\x03\\x95\\x06\\x75\\x08\\x15\\x00\\x25\\x65\\x05\\x07\\x19\\x00\\x29\\x65\\x81\\x00\\xc0 > functions/hid.usb0/report_desc

# 5) 마우스 HID function (report_length 3)
mkdir -p functions/hid.usb1
echo 2 > functions/hid.usb1/protocol
echo 1 > functions/hid.usb1/subclass
echo 8 > functions/hid.usb1/report_length
echo -ne \\x05\\x01\\x09\\x02\\xa1\\x01\\x09\\x01\\xa1\\x00\\x05\\x09\\x19\\x01\\x29\\x03\\x15\\x00\\x25\\x01\\x95\\x03\\x75\\x01\\x81\\x02\\x95\\x01\\x75\\x05\\x81\\x03\\x05\\x01\\x09\\x30\\x09\\x31\\x15\\x81\\x25\\x7f\\x75\\x08\\x95\\x02\\x81\\x06\\xc0\\xc0 > functions/hid.usb1/report_desc

# 6) function -> config 연결
ln -sf functions/hid.usb0 configs/c.1/
ln -sf functions/hid.usb1 configs/c.1/

# 7) UDC 바인딩 (활성화)
UDC=$(ls /sys/class/udc | head -n1)
if [ -z "$UDC" ]; then
    echo "[usb-gadget] ERROR: /sys/class/udc 비어있음 — dtoverlay=dwc2 활성화 후 재부팅 필요" >&2
    exit 1
fi
echo "$UDC" > UDC

echo "[usb-gadget] bound to $UDC — /dev/hidg0(키보드), /dev/hidg1(마우스) 준비됨"
