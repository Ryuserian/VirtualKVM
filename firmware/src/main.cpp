// VirtualKVM HID 데몬 진입점 (Raspberry Pi Zero W)
//
// 단일 poll() 이벤트 루프: 리스너/세션/signalfd 를 동시에 감시한다.
// 종료 시그널(SIGINT/SIGTERM/SIGHUP) 은 signal()/sigaction() 콜백 없이
// signalfd 기반 SignalHandler 로만 처리한다.
#include <cstdio>
#include <cstdlib>
#include <unistd.h>

#include "server.h"

int main(int argc, char *argv[]) {
    int port = virtualkvm::kDefaultPort;
    if (argc > 1)
        port = std::atoi(argv[1]);

    virtualkvm::Server server(port);

    // 부팅 직후 USB gadget( /dev/hidg* ) 준비 지연에 대비해 계속 재시도한다.
    while (!server.Bind()) {
        std::fprintf(stderr, "[main] bind failed, retry in 3s...\n");
        sleep(3);
    }

    server.Run();

    std::fprintf(stderr, "[main] shutdown\n");
    return 0;
}
