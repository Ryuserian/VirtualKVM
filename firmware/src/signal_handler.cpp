#include "signal_handler.h"

#include <poll.h>
#include <signal.h>
#include <sys/signalfd.h>
#include <unistd.h>

#include <cstdio>
#include <cstring>

namespace virtualkvm {

SignalHandler::SignalHandler() {
    sigset_t mask;
    sigemptyset(&mask);
    // signalfd 로 수신할 시그널. 콜백(signal()) 대신 fd 이벤트로 처리한다.
    sigaddset(&mask, SIGINT);
    sigaddset(&mask, SIGTERM);
    sigaddset(&mask, SIGHUP);
    sigaddset(&mask, SIGPIPE);

    // 기본 디스퍼지션(프로세스 종료)을 차단해야 signalfd 가 관찰할 수 있다.
    if (pthread_sigmask(SIG_BLOCK, &mask, nullptr) != 0) {
        std::perror("[signal] pthread_sigmask");
        return;
    }

    fd_ = ::signalfd(-1, &mask, SFD_CLOEXEC);
    if (fd_ < 0)
        std::perror("[signal] signalfd");
}

SignalHandler::~SignalHandler() {
    if (fd_ >= 0)
        ::close(fd_);
}

bool SignalHandler::WaitTerminal() {
    signalfd_siginfo info;
    ssize_t n = ::read(fd_, &info, sizeof(info));
    if (n != static_cast<ssize_t>(sizeof(info)))
        return false;

    std::fprintf(stderr, "[signal] caught %d\n", info.ssi_signo);

    // SIGPIPE 는 무시하고, 종료 시그널만 true 반환
    return info.ssi_signo == SIGINT || info.ssi_signo == SIGTERM ||
           info.ssi_signo == SIGHUP;
}

bool SignalHandler::Poll(int timeout_ms) {
    if (fd_ < 0)
        return false;

    pollfd pfd;
    pfd.fd = fd_;
    pfd.events = POLLIN;
    pfd.revents = 0;

    int rc = ::poll(&pfd, 1, timeout_ms);
    if (rc > 0 && (pfd.revents & POLLIN))
        return WaitTerminal();

    return false;
}

} // namespace virtualkvm
