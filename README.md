# WebRemoteServer

**WebRemoteServer** is a simple application that allows you to remotely control your system’s **volume** and **brightness** using a WebSocket connection.


## Features

- **Volume Control**: Adjust the system volume remotely.
- **Brightness Control**: Adjust the monitor brightness remotely.
- **Easy to Use**: Connect and send commands via a WebSocket client.


## Installation

1. Download the latest installer from the [Releases](https://github.com/dreamcatcher45/WebRemoteServer/releases) page.
2. Run the installer and follow the on-screen instructions.
3. The application will be installed and ready to use.


## Usage

1. Run the `WebRemoteServer.exe` file.
2. The server will start and display the IP address it is listening on (e.g., `ws://192.168.1.100:8080`).

### Sending Commands
Connect to the server using a WebSocket client and send the following commands:

- **Volume Control**: Send `a_X` where `X` is a number between 1 and 10 (e.g., `a_5` sets the volume to 50%).
- **Brightness Control**: Send `b_X` where `X` is a number between 1 and 10 (e.g., `b_7` sets the brightness to 70%).


## Support

If you encounter any issues or have questions, please [open an issue](https://github.com/dreamcatcher45/WebRemoteServer/issues) on GitHub.

---

Enjoy using **WebRemoteServer**! 🚀

