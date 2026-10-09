// signalfd 기반 종료 시그널 처리 (signal()/sigaction() 콜백 방식 미사용)
//
// SIGINT/SIGTERM/SIGHUP/SIGPIPE 를 프로세스 delivery 대신 signalfd 로 차단(block)하고,
// poll() 로 시그널을 감지한다. SIGPIPE 는 무시하고(=write 가 EPIPE 반환), SIGINT/SIGTERM/SIGHUP
// 은 서버 종료를 촉발한다.
#ifndef VIRTUALKVM_SIGNAL_HANDLER_H
#define VIRTUALKVM_SIGNAL_HANDLER_H

namespace virtualkvm {

class SignalHandler {
public:
    SignalHandler();
    ~SignalHandler();

    SignalHandler(const SignalHandler &) = delete;
    SignalHandler &operator=(const SignalHandler &) = delete;

    // poll() 에 등록할 fd. -0 이면 사용 불가.
    int Fd() const { return fd_; }

    // signalfd 에서 시그널을 회수하고 종료 시그널이면 true.
    bool WaitTerminal();

    // signalfd 를 timeout 동안 poll 후 종료 시그널이면 true.
    bool Poll(int timeout_ms);

private:
    int fd_ = -1;
};

} // namespace virtualkvm

#endif // VIRTUALKVM_SIGNAL_HANDLER_H
