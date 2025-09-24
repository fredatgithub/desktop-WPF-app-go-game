# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Install Wine and Xvfb for running Windows applications in Linux
RUN apt-get update && apt-get install -y \
    wine64 \
    xvfb \
    x11vnc \
    fluxbox \
    wget \
    && rm -rf /var/lib/apt/lists/*

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy source code and build
COPY . ./
RUN dotnet build -c Release

# Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

# Install Wine, X11, and VNC server for GUI applications
RUN apt-get update && apt-get install -y \
    wine64 \
    xvfb \
    x11vnc \
    fluxbox \
    novnc \
    websockify \
    supervisor \
    && rm -rf /var/lib/apt/lists/*

# Copy the built application
COPY --from=build /app/bin/Release/net6.0-windows/ ./

# Create a startup script
RUN echo '#!/bin/bash\n\
export DISPLAY=:1\n\
Xvfb :1 -screen 0 1024x768x16 &\n\
sleep 2\n\
fluxbox &\n\
sleep 2\n\
x11vnc -display :1 -nopw -listen localhost -xkb -ncache 10 -ncache_cr -httpdir /usr/share/novnc/ -httpport 8080 &\n\
sleep 2\n\
cd /usr/share/novnc && ./utils/launch.sh --vnc localhost:5900 --listen 8080 &\n\
sleep 5\n\
# Create a simple web interface\n\
echo "<!DOCTYPE html>\n\
<html>\n\
<head><title>Go Game</title></head>\n\
<body style=\"margin:0; background:#2c3e50;\">\n\
<div style=\"text-align:center; padding:20px; color:white;\">\n\
<h1>C# WPF Go Game</h1>\n\
<p>This is a C# WPF application for playing Go.</p>\n\
<p><strong>Note:</strong> This Windows application requires a desktop environment to run properly.</p>\n\
<p>To run locally: Clone the repository and use Visual Studio or <code>dotnet run</code></p>\n\
<div style=\"background:#34495e; padding:20px; margin:20px; border-radius:10px; text-align:left;\">\n\
<h3>Features:</h3>\n\
<ul>\n\
<li>Full 19x19 Go board implementation</li>\n\
<li>Complete game rules including capture and Ko rule</li>\n\
<li>Stone placement and validation</li>\n\
<li>Territory counting and scoring</li>\n\
<li>Pass functionality and game ending</li>\n\
<li>Visual feedback with stone animations</li>\n\
</ul>\n\
</div>\n\
<div style=\"background:#27ae60; padding:15px; margin:20px; border-radius:10px;\">\n\
<h3>How to Play Locally:</h3>\n\
<ol style=\"text-align:left;\">\n\
<li>Install .NET 6.0 SDK</li>\n\
<li>Clone this repository</li>\n\
<li>Run: <code>dotnet run</code></li>\n\
<li>Enjoy playing Go!</li>\n\
</ol>\n\
</div>\n\
</div>\n\
</body>\n\
</html>" > /app/index.html\n\
# Start a simple HTTP server to serve the info page\n\
cd /app && python3 -m http.server 8080\n\
' > /app/start.sh

RUN chmod +x /app/start.sh

# Install Python for the HTTP server
RUN apt-get update && apt-get install -y python3 && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

CMD ["/app/start.sh"]