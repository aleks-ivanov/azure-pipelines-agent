FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1.17

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        git \
    && rm -rf /var/lib/apt/lists/*
