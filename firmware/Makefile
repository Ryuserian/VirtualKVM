# VirtualKVM HID 데몬 빌드
#
# 1) 라즈베리파이 Zero W 위에서 직접 빌드 (권장, 가장 단순):
#      sudo apt install build-essential
#      make
#
# 2) PC(WSL Ubuntu) 에서 크로스 컴파일:
#      sudo apt install g++-arm-linux-gnueabihf
#      make CROSS_COMPILE=arm-linux-gnueabihf-
#
# 산출물: build/RaspberryPiZeroW-HID

CROSS_COMPILE ?=
CXX      := $(CROSS_COMPILE)g++

# Zero W = ARMv6zk + hard-float.
# Pi 에서 네이티브 빌드하면 Pi gcc 기본 아키텍처를 쓰고,
# PC(크로스) 에서만 armv6 hard-float 플래그를 부여한다.
ARCHFLAGS  :=
ifneq ($(CROSS_COMPILE),)
ARCHFLAGS  := -march=armv6zk -mtune=arm1176jzf-s -mfpu=vfp -mfloat-abi=hard
endif

CXXFLAGS   := -std=c++14 -O2 -Wall -Wextra $(ARCHFLAGS) -pthread
LDFLAGS    := -pthread

SRCS := src/main.cpp src/server.cpp src/session.cpp src/signal_handler.cpp
OBJS := $(SRCS:src/%.cpp=build/%.o)
OUT  := build/RaspberryPiZeroW-HID

.PHONY: all clean install

all: $(OUT)

build:
	mkdir -p build

build/%.o: src/%.cpp | build
	$(CXX) $(CXXFLAGS) -c $< -o $@

$(OUT): $(OBJS)
	$(CXX) $(OBJS) -o $@ $(LDFLAGS)
	@echo "built: $@"

# Windows 앱 의 Install() 이 데몬 파일을 Desktop/ 에 올린 뒤 /usr/local/bin 에 설치하던 흐름을
# systemd 서비스로 승격할 때 사용:
install: $(OUT)
	sudo install -m 755 $(OUT) /usr/local/bin/RaspberryPiZeroW-HID
	sudo cp systemd/virtualkvm.service /etc/systemd/system/
	sudo systemctl daemon-reload
	sudo systemctl enable --now virtualkvm.service

clean:
	rm -rf build
