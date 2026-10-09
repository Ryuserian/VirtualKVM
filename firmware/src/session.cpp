#include "session.h"

#include <arpa/inet.h>
#include <fcntl.h>
#include <poll.h>
#include <unistd.h>

#include <cerrno>
#include <cstdio>
#include <cstring>

namespace virtualkvm {

Session::Session(int client_fd) : client_fd_(client_fd) {
    int flags = ::fcntl(client_fd_, F_GETFL, 0);
    ::fcntl(client_fd_, F_SETFL, flags | O_NONBLOCK);
}

Session::~Session() {
    Close();
}

void Session::Close() {
    if (hid_fd_ >= 0) {
        ::close(hid_fd_);
        hid_fd_ = -1;
    }
    if (client_fd_ >= 0) {
        ::close(client_fd_);
        client_fd_ = -1;
    }
    alive_ = false;
}

bool Session::Resolve(uint32_t magic) {
    if (magic == kTypeKeyboardMagic) {
        dev_path_ = kKeyboardDevice;
        report_size_ = kKeyboardReportSize;
    } else if (magic == kTypeMouseMagic) {
        dev_path_ = kMouseDevice;
        report_size_ = kMouseReportSize;
    } else {
        std::fprintf(stderr, "[session] unknown magic 0x%08x, drop\n", magic);
        return false;
    }

    hid_fd_ = ::open(dev_path_, O_WRONLY | O_NONBLOCK);
    if (hid_fd_ < 0) {
        std::fprintf(stderr, "[session] open %s failed: %s\n", dev_path_,
                     std::strerror(errno));
        return false;
    }

    std::fprintf(stderr, "[session] attached magic=0x%08x -> %s\n", magic,
                 dev_path_);
    return true;
}

bool Session::WriteReport() {
    if (hid_fd_ < 0)
        return true; // 악수 전 리포트는 무시

    ssize_t n = ::write(hid_fd_, report_buf_, report_size_);
    if (n < 0) {
        // hidg 가 대상 PC 의 NAK 로 일시 블록될 수 있다 -> EAGAIN 은 드롭.
        if (errno == EAGAIN || errno == EWOULDBLOCK)
            return true;
        std::fprintf(stderr, "[session] write %s failed: %s\n", dev_path_,
                     std::strerror(errno));
        return false;
    }
    return true;
}

void Session::Push(const uint8_t *data, size_t len) {
    if (!handshake_done_) {
        while (len > 0 && hs_fill_ < 4) {
            hs_buf_[hs_fill_] = *data;
            hs_fill_++;
            data++;
            len--;
        }
        if (hs_fill_ == 4) {
            uint32_t magic;
            std::memcpy(&magic, hs_buf_, 4);
            magic = ntohl(magic);
            handshake_done_ = true;
            if (!Resolve(magic)) {
                alive_ = false;
                return;
            }
        }
    }

    while (len > 0 && alive_) {
        size_t need = report_size_ - fill_;
        size_t take = (len < need) ? len : need;
        std::memcpy(report_buf_ + fill_, data, take);
        fill_ += take;
        data += take;
        len -= take;

        if (fill_ == report_size_) {
            fill_ = 0;
            if (!WriteReport()) {
                alive_ = false;
                return;
            }
        }
    }
}

bool Session::Pump() {
    uint8_t buf[64];
    while (alive_) {
        ssize_t n = ::read(client_fd_, buf, sizeof(buf));
        if (n == 0) // FIN
            break;
        if (n < 0) {
            if (errno == EAGAIN || errno == EWOULDBLOCK)
                return alive_;
            if (errno == EINTR)
                continue;
            break;
        }
        Push(buf, static_cast<size_t>(n));
    }
    return alive_;
}

} // namespace virtualkvm
