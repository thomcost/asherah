# An example Dockerfile for running the sample. Will capture any interesting findings for containerized flows.

FROM mcr.microsoft.com/dotnet/core/runtime:2.2

# NOTE : dotnet enables debugging and profiling by default causing some system level writes 
# Disabling them ensures that our application can run in a read-only environment.
ENV COMPlus_EnableDiagnostics=0

RUN apt-get update && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app/publish
ADD ReferenceApp/bin/Release/netcoreapp2.0/publish/ .

ENTRYPOINT ["dotnet", "ReferenceApp.dll"]