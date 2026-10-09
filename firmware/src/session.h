// 이벤트 기반 세션: poll() 루프가 POLL_IN 을 알려주면 Pump() 로 데이터를 소비한다.
// (블로킹 read / 스레드 없음 -> signalfd 종료와 충돌하지 않음)
#ifndef VIRTUALKVM_SESSION_H
#define VIRTUALKVM_SESSION_H

#include <cstddef>
#include <cstdint>

#include "virtualkvm.h"

namespace virtualkvm {

class Session {
public:
    explicit Session(int client_fd);
    ~Session();

    Session(const Session &) = delete;
    Session &operator=(const Session &) = delete;

    int Fd() const { return client_fd_; }

    // 사용 가능 데이터를 전부 소비. 세션 종료(죽음) 시 false.
    bool Pump();

    bool IsAlive() const { return alive_; }
    void Close();

private:
    void Push(const uint8_t *data, size_t len);
    bool Resolve(uint32_t magic);   // 매직 -> hidg 디바이스 오픈
    bool WriteReport();

    int client_fd_;
    int hid_fd_ = -1;

    bool alive_ = true;
    bool handshake_done_ = false;

    uint8_t hs_buf_[4];
    size_t hs_fill_ = 0;

    const char *dev_path_ = nullptr;
    size_t report_size_ = 0;
    uint8_t report_buf_[kKeyboardReportSize]; // 최대 리포트 크기 버퍼
    size_t fill_ = 0;
};

} // namespace virtualkvm

#endif // VIRTUALKVM_SESSION_H
