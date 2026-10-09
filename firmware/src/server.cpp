#include "server.h"

#include <arpa/inet.h>
#include <fcntl.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <poll.h>
#include <sys/socket.h>
#include <unistd.h>

#include <cerrno>
#include <cstdio>
#include <cstring>

namespace virtualkvm {

Server::Server(int port) : port_(port) {}

Server::~Server() {
    Stop();
    for (auto &s : sessions_)
        s->Close();
    sessions_.clear();
    if (listen_fd_ >= 0) {
        ::close(listen_fd_);
        listen_fd_ = -1;
    }
}

bool Server::Bind() {
    listen_fd_ = ::socket(AF_INET, SOCK_STREAM | SOCK_NONBLOCK, 0);
    if (listen_fd_ < 0) {
        std::fprintf(stderr, "[server] socket: %s\n", std::strerror(errno));
        return false;
    }

    int yes = 1;
    ::setsockopt(listen_fd_, SOL_SOCKET, SO_REUSEADDR, &yes, sizeof(yes));

    sockaddr_in addr;
    std::memset(&addr, 0, sizeof(addr));
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = htonl(INADDR_ANY);
    addr.sin_port = htons(static_cast<uint16_t>(port_));

    if (::bind(listen_fd_, reinterpret_cast<sockaddr *>(&addr), sizeof(addr)) < 0) {
        std::fprintf(stderr, "[server] bind :%d: %s\n", port_, std::strerror(errno));
        ::close(listen_fd_);
        listen_fd_ = -1;
        return false;
    }

    if (::listen(listen_fd_, 8) < 0) {
        std::fprintf(stderr, "[server] listen: %s\n", std::strerror(errno));
        ::close(listen_fd_);
        listen_fd_ = -1;
        return false;
    }

    std::fprintf(stderr, "[server] listening on :%d\n", port_);
    return true;
}

void Server::AcceptNew() {
    while (true) {
        int fd = ::accept4(listen_fd_, nullptr, nullptr, SOCK_NONBLOCK);
        if (fd < 0) {
            if (errno == EAGAIN || errno == EWOULDBLOCK)
                return;
            if (errno == EINTR)
                continue;
            std::fprintf(stderr, "[server] accept: %s\n", std::strerror(errno));
            return;
        }

        int yes = 1;
        ::setsockopt(fd, SOL_SOCKET, SO_KEEPALIVE, &yes, sizeof(yes));
        ::setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, &yes, sizeof(yes));

        sessions_.push_back(std::unique_ptr<Session>(new Session(fd)));
        std::fprintf(stderr, "[server] session accepted (active=%zu)\n",
                     sessions_.size());
    }
}

void Server::Run() {
    running_ = true;

    std::vector<pollfd> fds;
    fds.reserve(sessions_.size() + 2);

    while (running_) {
        fds.clear();

        pollfd lf;
        lf.fd = listen_fd_;
        lf.events = POLLIN;
        lf.revents = 0;
        fds.push_back(lf);

        pollfd sf;
        sf.fd = signal_.Fd();
        sf.events = POLLIN;
        sf.revents = 0;
        fds.push_back(sf);

        const size_t session_base = 2;
        for (auto &s : sessions_) {
            pollfd pf;
            pf.fd = s->Fd();
            pf.events = POLLIN;
            pf.revents = 0;
            fds.push_back(pf);
        }

        int rc = ::poll(fds.data(), fds.size(), 1000); // 1s 타임아웃으로 running_ 점검
        if (rc < 0) {
            if (errno == EINTR)
                continue;
            std::fprintf(stderr, "[server] poll: %s\n", std::strerror(errno));
            break;
        }

        // 종료 시그널 (signalfd)
        if (fds[1].revents & POLLIN) {
            if (signal_.WaitTerminal())
                running_ = false;
        }

        if (fds[0].revents & POLLIN)
            AcceptNew();

        // 세션 소비 + 죽은 세션 제거 (역순 erase)
        for (size_t i = sessions_.size(); i-- > 0;) {
            pollfd &pf = fds[session_base + i];
            if (pf.fd >= 0 && (pf.revents & (POLLIN | POLLHUP | POLLERR))) {
                if (!sessions_[i]->Pump()) {
                    std::fprintf(stderr, "[server] session closed (active=%zu)\n",
                                 sessions_.size() - 1);
                    sessions_.erase(sessions_.begin() + static_cast<long>(i));
                }
            }
        }
    }

    std::fprintf(stderr, "[server] shutdown\n");
}

} // namespace virtualkvm
