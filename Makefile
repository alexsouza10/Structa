APP_NAME    := Structa.UI.exe
APP_PROJECT := src/Structa.UI/Structa.UI.csproj
APP_EXE     := src/Structa.UI/bin/Debug/net10.0/$(APP_NAME)

.PHONY: build start stop restart

build:
	dotnet build $(APP_PROJECT)

start: build
	@"$(APP_EXE)" &
	@echo "Structa iniciado."

stop:
	@taskkill //IM $(APP_NAME) //F //T > /dev/null 2>&1 && echo "Structa parado." || echo "Structa nao estava em execucao."

restart: stop start
