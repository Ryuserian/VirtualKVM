// VirtualKVM HID 데몬(Raspberry Pi Zero W) 공통 헤더
//
// 프로토콜 (Windows 앱과 동일 계약):
//   - TCP 5000 포트로 접속한 각 클라이언트 연결은 첫 4바이트로 장치 종류를 신고한다.
//       0x11111111 -> 키보드 (8바이트 HID 리포트를 /dev/hidg0 으로 전달)
//       0x22222222 -> 마우스 (3바이트 HID 리포트를 /dev/hidg1 으로 전달)
//   - 그 뒤로는 리포트 크기 단위로 반복 수신 -> 해당 hidg 디바이스에 write.
//   - Windows 앱이 5초마다 현재 상태를 재전송(하트비트) 하므로 유휴切断 걱정 없음.
#ifndef VIRTUALKVM_H
#define VIRTUALKVM_H

#include <cstdint>

namespace virtualkvm {

constexpr int      kDefaultPort          = 5000;
constexpr uint32_t kTypeKeyboardMagic    = 0x11111111u;
constexpr uint32_t kTypeMouseMagic       = 0x22222222u;
constexpr size_t   kKeyboardReportSize   = 8;
constexpr size_t   kMouseReportSize      = 3;
constexpr const char *kKeyboardDevice    = "/dev/hidg0";
constexpr const char *kMouseDevice       = "/dev/hidg1";

} // namespace virtualkvm

#endif // VIRTUALKVM_H
