// poll() 기반 단일 스레드 이벤트 루프 서버.
// 리스너 + 세션 fd + signalfd 를 한 번의 poll() 로 감시한다.
// (accept 루프용 별도 블로킹 / 스레드 없음 -> signalfd 종료와 자연스럽게 통합)
#ifndef VIRTUALKVM_SERVER_H
#define VIRTUALKVM_SERVER_H

#include <memory>
#include <vector>

#include "session.h"
#include "signal_handler.h"
#include "virtualkvm.h"

namespace virtualkvm {

class Server {
public:
    explicit Server(int port = kDefaultPort);
    ~Server();

    Server(const Server &) = delete;
    Server &operator=(const Server &) = delete;

    bool Bind();
    // signalfd 로 종료 시그널을 받으면 반환 (블로킹)
    void Run();
    void Stop() { running_ = false; }

private:
    void AcceptNew();

    int port_;
    int listen_fd_ = -1;
    bool running_ = false;

    SignalHandler signal_;
    std::vector<std::unique_ptr<Session>> sessions_;
};

} // namespace virtualkvm

#endif // VIRTUALKVM_SERVER_H
